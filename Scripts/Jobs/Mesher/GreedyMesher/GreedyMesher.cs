using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// This class handles the Greedy meshing and mesh creation
    /// An alternative is TripleGreedyMesher.cs, which splits the load to 3 different jobs
    /// </summary>
    public class GreedyMesher : MesherBase, IVoxMesher
    {
        private JobHandle greedyJobHandle;
        private GreedyMesherJob greedyJob;

        private JobHandle meshJobHandle;
        private MeshJob meshJob;

        /// <summary>
        /// Instead of creating these every time the greedy mesher gets used, we
        /// create them on start and reuse them until disposed
        /// </summary>
        private NativeList<float3> vertices;
        private NativeList<int> triangles;
        private NativeList<Color> colors;
        private NativeList<float3> normals;

        private NativeArray<Voxel> mask;
        private NativeArray<Voxel> inputVoxels;
        private NativeArray<Color> inputPalette;

        private NativeArray<Vector3> boundValues;
        
        private readonly MeshUpdateFlags noUpdateFlags = 
            MeshUpdateFlags.DontRecalculateBounds | 
            MeshUpdateFlags.DontValidateIndices | 
            MeshUpdateFlags.DontNotifyMeshUsers | 
            MeshUpdateFlags.DontResetBoneBounds;
        
        public GreedyMesher(float objScale, VoxelData data)
        {
            vertices = new NativeList<float3>(Allocator.Persistent);
            triangles = new NativeList<int>(Allocator.Persistent);
            colors = new NativeList<Color>(Allocator.Persistent);
            normals = new NativeList<float3>(Allocator.Persistent);
            mask = new NativeArray<Voxel>(GetMaskLength(data.length), Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            inputVoxels = new NativeArray<Voxel>(data.voxels.Length, Allocator.Persistent, 
                NativeArrayOptions.UninitializedMemory);
            boundValues = new NativeArray<Vector3>(2, Allocator.Persistent, 
                NativeArrayOptions.UninitializedMemory);
            
            greedyJob = new GreedyMesherJob()
            {
                vertices = vertices,
                triangles = triangles,
                colors = colors,
                normals = normals,
                mask = mask,
                inputTargetSize = objScale,
                startD = 0,
                endD = 3
            };
            
            meshJob = new MeshJob()
            {
                vert = vertices,
                tris = triangles,
                colors = colors,
                normals = normals,
                boundsValues = boundValues,
            };
        }
        
        protected override void PrepareMesher(VoxelData data)
        {
            if (dataDisposed)
                return;

            greedyJob.inputSize = data.length;
            data.voxels.CopyTo(inputVoxels);

            greedyJob.inputVoxels = inputVoxels;
            EnsurePaletteCopy(data.palette);
            greedyJob.inputPalette = inputPalette;

            greedyJobHandle = greedyJob.Schedule();
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
            if (dataDisposed)
                return;
            
            greedyJobHandle.Complete();

            if (vertices.Length == 0)
                return;
            
            //Constructing a complex Mesh takes a lot of performance, we can use a job instead
            meshJob.dataArray = Mesh.AllocateWritableMeshData(1);
            meshJobHandle = meshJob.Schedule();
        }
        
        protected override Mesh GetMeshFromJob()
        {
            Profiler.BeginSample("Creating mesh");
            
            if (vertices.Length == 0)
                return new Mesh();
            
            meshJobHandle.Complete();

            Mesh mesh = new Mesh();        
            Mesh.ApplyAndDisposeWritableMeshData(meshJob.dataArray, mesh, noUpdateFlags);
            mesh.bounds = new Bounds(meshJob.boundsValues[0], meshJob.boundsValues[1]);

            Profiler.EndSample();
            
            return mesh;
        }

        public Mesh CreateMeshInstantly()
        {
            greedyJobHandle.Complete();
            
            Profiler.BeginSample("Creating mesh");
            
            if (vertices.Length == 0)
                return new Mesh();
            
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
            return greedyJobHandle.IsCompleted;
        }

        public bool IsMeshJobFinished()
        {
            return meshJobHandle.IsCompleted;
        }
        
        protected override void DisposeAll()
        {
            greedyJobHandle.Complete();
            meshJobHandle.Complete();

            if (inputPalette.IsCreated)
                inputPalette.Dispose();

            inputVoxels.Dispose();
            mask.Dispose();
            
            vertices.Dispose();
            normals.Dispose();
            triangles.Dispose();
            colors.Dispose();

            boundValues.Dispose();
        }
    }
}
