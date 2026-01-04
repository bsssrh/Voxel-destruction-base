using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Interfaces;

namespace VoxelDestructionPro.Jobs.Mesher
{
    /// <summary>
    /// Basically 3 times the Greedy Mesher, needs more memory, runs 3 axis in parallel
    /// </summary>
    public class TripleGreedyMesher : MesherBase, IVoxMesher
    {
        private JobHandle handleD0;
        private JobHandle handleD1;
        private JobHandle handleD2;
        private GreedyMesherJob greedyJobD0;
        private GreedyMesherJob greedyJobD1;
        private GreedyMesherJob greedyJobD2;
        
        private JobHandle meshJobHandle;
        private TripleMeshJob meshJob;
        
        private NativeList<float3> vertices1;
        private NativeList<int> triangles1;
        private NativeList<Color> colors1;
        private NativeList<float3> normals1;
        private NativeList<float3> vertices2;
        private NativeList<int> triangles2;
        private NativeList<Color> colors2;
        private NativeList<float3> normals2;
        private NativeList<float3> vertices3;
        private NativeList<int> triangles3;
        private NativeList<Color> colors3;
        private NativeList<float3> normals3;
        
        private NativeArray<Voxel> mask1;
        private NativeArray<Voxel> mask2;
        private NativeArray<Voxel> mask3;

        private NativeArray<Voxel> voxels;
        private NativeArray<Color> inputPalette;
        
        private NativeArray<Vector3> boundValues;
        
        private readonly MeshUpdateFlags noUpdateFlags = 
            MeshUpdateFlags.DontRecalculateBounds | 
            MeshUpdateFlags.DontValidateIndices |
            MeshUpdateFlags.DontNotifyMeshUsers | 
            MeshUpdateFlags.DontResetBoneBounds;
        
        public TripleGreedyMesher(float objScale, VoxelData data)
        {
            dataDisposed = false;

            vertices1 = new NativeList<float3>(Allocator.Persistent);
            triangles1 = new NativeList<int>(Allocator.Persistent);
            colors1 = new NativeList<Color>(Allocator.Persistent);
            normals1 = new NativeList<float3>(Allocator.Persistent);
            vertices2 = new NativeList<float3>(Allocator.Persistent);
            triangles2 = new NativeList<int>(Allocator.Persistent);
            colors2 = new NativeList<Color>(Allocator.Persistent);
            normals2 = new NativeList<float3>(Allocator.Persistent);
            vertices3 = new NativeList<float3>(Allocator.Persistent);
            triangles3 = new NativeList<int>(Allocator.Persistent);
            colors3 = new NativeList<Color>(Allocator.Persistent);
            normals3 = new NativeList<float3>(Allocator.Persistent);
            
            int maskLength = GetMaskLength(data.length);
            mask1 = new NativeArray<Voxel>(maskLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            mask2 = new NativeArray<Voxel>(maskLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            mask3 = new NativeArray<Voxel>(maskLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            voxels = new NativeArray<Voxel>(data.voxels.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            
            boundValues = new NativeArray<Vector3>(2, Allocator.Persistent, 
                NativeArrayOptions.UninitializedMemory);
            
            //You can't use Nativelist with a parallel job, but you can create 3 different jobs
            greedyJobD0 = new GreedyMesherJob()
            {
                vertices = vertices1,
                triangles = triangles1,
                colors = colors1,
                normals = normals1,
                inputVoxels = voxels,
                mask = mask1,
                inputTargetSize = objScale,
                inputSize = data.length,
                startD = 0,
                endD = 1
            };
            greedyJobD1 = new GreedyMesherJob()
            {
                vertices = vertices2,
                triangles = triangles2,
                colors = colors2,
                normals = normals2,
                inputVoxels = voxels,
                mask = mask2,
                inputTargetSize = objScale,
                inputSize = data.length,
                startD = 1,
                endD = 2
            };
            greedyJobD2 = new GreedyMesherJob()
            {
                vertices = vertices3,
                triangles = triangles3,
                colors = colors3,
                normals = normals3,
                inputVoxels = voxels,
                mask = mask3,
                inputTargetSize = objScale,
                inputSize = data.length,
                startD = 2,
                endD = 3
            };
            meshJob = new TripleMeshJob()
            {
                vert1 = vertices1,
                tris1 = triangles1,
                colors1 = colors1,
                normals1 = normals1,
                vert2 = vertices2,
                tris2 = triangles2,
                colors2 = colors2,
                normals2 = normals2,
                vert3 = vertices3,
                tris3 = triangles3,
                colors3 = colors3,
                normals3 = normals3,
                boundsValues = boundValues
            };
        }

        protected override void PrepareMesher(VoxelData data)
        {
            data.voxels.CopyTo(voxels);

            EnsurePaletteCopy(data.palette);
            greedyJobD0.inputPalette = inputPalette;
            greedyJobD1.inputPalette = inputPalette;
            greedyJobD2.inputPalette = inputPalette;

            handleD0 = greedyJobD0.Schedule();
            handleD1 = greedyJobD1.Schedule();
            handleD2 = greedyJobD2.Schedule();
        }

        private void EnsurePaletteCopy(NativeArray<Color> palette)
        {
            if (inputPalette.IsCreated && inputPalette.Length != palette.Length)
                inputPalette.Dispose();

            if (!inputPalette.IsCreated)
                inputPalette = new NativeArray<Color>(palette.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            if (palette.Length > 0)
                palette.CopyTo(inputPalette);
        }

        public void StartBuildMesh()
        {
            handleD0.Complete();
            handleD1.Complete();
            handleD2.Complete();
            
            meshJob.dataArray = Mesh.AllocateWritableMeshData(1);
            
            meshJobHandle = meshJob.Schedule();
        }
        
        protected override Mesh GetMeshFromJob()
        {
            Profiler.BeginSample("Creating mesh");
            
            meshJobHandle.Complete();
            
            Mesh mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshJob.dataArray, mesh, noUpdateFlags);
            mesh.bounds = new Bounds(meshJob.boundsValues[0], meshJob.boundsValues[1]);
            
            Profiler.EndSample();
            
            return mesh;
        }

        /// <summary>
        /// I would not recommend to use this,
        /// If you want good performance with large meshes
        /// use job based mesh creation
        /// </summary>
        /// <returns></returns>
        public Mesh CreateMeshInstantly()
        {
            handleD0.Complete();
            handleD1.Complete();
            handleD2.Complete();
            
            Profiler.BeginSample("Creating mesh");
            
            handleD0.Complete();
            handleD1.Complete();
            handleD2.Complete();
            
            meshJob.dataArray = Mesh.AllocateWritableMeshData(1);
            
            meshJob.Run();
            
            Mesh mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshJob.dataArray, mesh, noUpdateFlags);
            mesh.bounds = new Bounds(meshJob.boundsValues[0], meshJob.boundsValues[1]);
            
            Profiler.EndSample();
            
            return mesh;
        }

        public bool IsGreedyJobFinished()
        {
            return handleD0.IsCompleted && handleD1.IsCompleted && handleD2.IsCompleted;
        }

        public bool IsMeshJobFinished()
        {
            return meshJobHandle.IsCompleted;
        }

        protected override void DisposeAll()
        {
            handleD0.Complete();
            handleD1.Complete();
            handleD2.Complete();
            meshJobHandle.Complete();

            if (inputPalette.IsCreated)
                inputPalette.Dispose();

            voxels.Dispose();

            mask1.Dispose();
            mask2.Dispose();
            mask3.Dispose();
            
            vertices1.Dispose();
            vertices2.Dispose();
            vertices3.Dispose();
            triangles1.Dispose();
            triangles2.Dispose();
            triangles3.Dispose();
            normals1.Dispose();
            normals2.Dispose();
            normals3.Dispose();
            colors1.Dispose();
            colors2.Dispose();
            colors3.Dispose();

            boundValues.Dispose();
        }
    }
}
