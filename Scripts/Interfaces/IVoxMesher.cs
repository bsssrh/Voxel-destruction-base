using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Interfaces
{
     /// <summary>
     /// Interface for the greedy meshers
     /// </summary>
     public interface IVoxMesher
     {
         /// <summary>
         /// Prepares and starts the greedy meshing job
         /// with the voxeldata
         /// </summary>
         /// <param name="data"></param>
         public void Prepare(VoxelData data);
         /// <summary>
         /// Once the greedy mesher is finished you can call this to
         /// start a second job that will create the mesh. This is usefull
         /// for complex meshes that you don't want to create on the main
         /// thread
         /// </summary>
         public void StartBuildMesh();
         /// <summary>
         /// Once the mesh job is finished you can use this to get
         /// the finished mesh
         /// </summary>
         /// <returns></returns>
         public Mesh CompleteMeshBuilding();
         /// <summary>
         /// If you don't want to wait longer for the mesh job to finish
         /// or your mesh is simple enough to not create a lot of lag
         /// use this to instantly create the mesh, once the greedy mesher
         /// is finished
         /// </summary>
         /// <returns></returns>
         public Mesh CreateMeshInstantly();
         /// <summary>
         /// Returns true once the greedy mesher is finished
         /// </summary>
         /// <returns></returns>
         public bool IsGreedyJobFinished();
         /// <summary>
         /// Returns true once the mesh creation job is finished
         /// </summary>
         /// <returns></returns>
         public bool IsMeshJobFinished();
         /// <summary>
         /// Call this to dispose the Greedy mesher
         /// </summary>
         public void Dispose();
     }
}