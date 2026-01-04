using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Interfaces;
using VoxelDestructionPro.Jobs.ColliderBaker;
using VoxelDestructionPro.Jobs.Mesher;
using VoxelDestructionPro.Settings;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace VoxelDestructionPro.VoxelObjects
{
    /// <summary>
    /// This is the base class for all voxel objects
    ///
    /// It handles most important operation, like mesh generation, pivot setting,
    /// collider baking, ...
    ///
    /// You can override it to create custom voxel objects
    /// </summary>
    public class VoxelObjBase : MonoBehaviour
    {
        #region Enums

        public enum ScaleType
        {
            Voxel, Units
        }

        #endregion
        
        #region Parameter

        [Header("Pivot")] 
        
        [Tooltip("If enabled the mesh filter object will be moved to match the pivotPlacement vector")]
        public bool setPivot = true;
        [Tooltip("Defines where the pivot should be placed, (0,0,0) = bottom left, (0.5,0.5,0.5) = center")]
        public Vector3 pivotPlacement = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] [HideInInspector]
        private bool pivotInit;

        [Header("Mesh")] 
        
        [Tooltip("Defines how the objectScale should be used.\nVoxel: The objectscale describes the size of a single voxel in units" +
                 "\nUnits: The objectscale describes the size of a single voxel relative to the length of the voxeldata. Useful for uniform scaling independent of voxel count")]
        public ScaleType scaleType = ScaleType.Voxel;
        [Tooltip("The size of the voxel object, size behaviour is defined by the scaletype")]
        public float objectScale = 1;
        public MeshFilter targetFilter;
        [Tooltip("Assign either a MeshCollider or a BoxCollider to this")]
        public Collider targetCollider;
        [Tooltip("The mesh setting object holds information on how the mesh should be constructed, collider settings and more")]
        public MeshSettingsObj meshSettings;

        [Header("Other")]
        
        [Tooltip("If set to true you can access the public int fields 'startVoxelCount' and 'currentVoxelCount' " +
                 "to calculate how much of the object got destroyed")]
        public bool calculateVoxelCount;

        [HideInInspector]
        public int startVoxelCount;
        [HideInInspector]
        public int currentVoxelCount;
        
        //Private
        public VoxelData voxelData;
        protected IVoxMesher mesher;
        protected ColliderBaker colliderBaker;
        private Mesh cMesh;
        [SerializeField] [HideInInspector]
        protected bool isCreated;

        private int[] voxelColliderMap;
        private Dictionary<int, ColliderRecord> voxelColliders;
        private List<BoxCollider> voxelColliderPool;
        private int nextVoxelColliderId = 1;
        private bool voxelCollidersNeedFullRebuild;

        private struct ColliderRecord
        {
            public BoxCollider collider;
            public int3 min;
            public int3 max;
        }
        
        //Active
        protected bool meshRegenerationActive;
        protected bool colliderBakeActive;
        //Requested
        protected bool meshRegenerationRequested;
        protected bool colliderBakeRequested;
        public bool objectDestructionRequested;
        
        protected bool isValidObject;
        
        //Events
        public Action<Mesh> onMeshGenerated;
        public Action<VoxelData> onVoxeldataChanged;
        public Action onVoxelDestroy;
        
        //Properties
        /// <summary>
        /// The voxeldatas active voxel count, note this expensive to calculate for larger voxel objects
        /// </summary>
        public int ActiveVoxelCount => voxelData.GetActiveVoxelCount();
        public int3 VoxelDataLength => voxelData.length;
        public int VoxelDataVolume => voxelData.Volume;

        protected bool UseVoxelBoxColliders =>
            meshSettings != null &&
            meshSettings.useVoxelBoxColliders &&
            targetCollider is BoxCollider;
        
        /// <summary>
        /// The current voxeldata, can be null
        /// </summary>
        public VoxelData CurrentVoxelData
        {
            get => voxelData;
        }
        
        #endregion

        #region Creation
        
        protected virtual void Start()
        {
            if (voxelData == null)
                isValidObject = false;

            if (meshSettings.freezeRbWhileBaking && 
                meshSettings.useThreadedCollisionBaking &&
                targetCollider is MeshCollider mc && 
                mc.sharedMesh == null)
            {
                Rigidbody rb = GetComponent<Rigidbody>();

                if (rb != null)
                    rb.isKinematic = true;
            }
        }

        protected virtual void CreateJobs()
        {
            //Create the mesher
            if (meshSettings.mesherType == MeshSettingsObj.MeshCalculationType.Simple)
                mesher ??= new GreedyMesher(GetSingleVoxelSize(), voxelData);
            else
                mesher ??= new TripleGreedyMesher(GetSingleVoxelSize(), voxelData);

            if (meshSettings.useThreadedCollisionBaking && targetCollider is MeshCollider)
                colliderBaker ??= new ColliderBaker();
        }
        
        public virtual void Create(bool editorMode = false)
        {
            if (!isActiveAndEnabled)
                return;
            
            if (!AssertVoxelObject())
            {
                isValidObject = false;
                return;
            }
            
            pivotInit = false;

            if (voxelData == null)
            {
                Debug.Log("Trying to create a voxel object without a voxelData");
                return;
            }
            
            CreateJobs();
           
            if (!meshRegenerationActive)
                StartCoroutine(GenerateMesh(editorMode));

            if (editorMode)
                DisposeAll();
            
            isCreated = true;
        }
        
        /// <summary>
        /// Assigns the voxeldata and creates the object if not created yet
        /// Editormode should only be true if this function is ran outside of
        /// play mode
        /// </summary>
        /// <param name="data"></param>
        /// <param name="editorMode"></param>
        public virtual void AssignVoxelData(VoxelData data, bool editorMode = false)
        {
            if (meshRegenerationActive)
                return; //We cant assign it while it is using it
            
            /*
             * We dispose all jobs and create them again because the old voxeldata
             * needs to get Disposed. If a jobs us currently using it it will throw an error,
             * thus we need to also dispose that on first
            */
            
            DisposeAll();

            if (data == null)
            {
                Debug.LogError("AssignVoxelData without valid voxeldata called!", this);
                isValidObject = false;
                return;
            }
            
            voxelData = data;
            if (calculateVoxelCount)
            {
                startVoxelCount = data.GetActiveVoxelCount();
                currentVoxelCount = startVoxelCount;
            }

            if (UseVoxelBoxColliders)
            {
                ClearVoxelBoxColliders();
                voxelCollidersNeedFullRebuild = true;
            }

            isValidObject = true;
            meshRegenerationRequested = true;
            
            if (!isCreated)
                Create(editorMode);
            else if (!editorMode)
                CreateJobs();
            
            if (onVoxeldataChanged != null) 
                onVoxeldataChanged.Invoke(voxelData);
        }
        
        /// Do not call directly, use meshRegenerationRequested instead!
        /// <summary>
        /// </summary>
        /// <param name="editorMode"></param>
        /// <returns></returns>
        protected virtual IEnumerator GenerateMesh(bool editorMode = false)
        {
            meshRegenerationActive = true;
            if (calculateVoxelCount)
                currentVoxelCount = voxelData.GetActiveVoxelCount();
            
            if (voxelData.IsEmpty())
            {
                SetMesh(null, editorMode);
                yield break;
            }
            
            Profiler.BeginSample("Starting Mesher");
            //Let it schedule the greedy job
            mesher.Prepare(voxelData);
            Profiler.EndSample();

            //Let him cook
            while (true)
            {
                if (mesher == null)
                    yield break;
                
                if (mesher.IsGreedyJobFinished() || editorMode)
                    break;
                
                yield return null;
            }
            
            Mesh m;

            if (meshSettings.meshConstructionJob && !editorMode)
            {
                //For complex meshes every operation will take some time
                //we want to minimize the lag by adding some delay
                mesher.StartBuildMesh();
                
                while (!mesher.IsMeshJobFinished()) //This should only take few frames
                    yield return null;
                
                m = mesher.CompleteMeshBuilding();
                yield return null;
            }
            else
            {
                m = mesher.CreateMeshInstantly();
            }
            
            SetMesh(m, editorMode);
            meshRegenerationActive = false;
        }

        /// <summary>
        /// Sets the MeshFilter and MeshCollider Mesh and calculates
        /// Pivot
        /// </summary>
        /// <param name="m"></param>
        /// <param name="editorMode"></param>
        protected void SetMesh(Mesh m, bool editorMode)
        {
            //Destroy the mesh to remove it from memory, 10 seconds delay because the collision could still be
            //baking in background
            if (cMesh != m && !editorMode)
                Destroy(cMesh, 10f); 
            
            cMesh = m;
            if (onMeshGenerated != null)
                onMeshGenerated.Invoke(m);
            
            if (m == null || m.vertexCount == 0)
            {
                if (!editorMode)
                    objectDestructionRequested = true;

                m = null;
            }
            
            //Assign Mesh
            targetFilter.mesh = m;
            
            //Pivot Stuff: pivotInit marks if the pivot is already set to avoid reseting it
            //every time a destruction occurs
            if (!pivotInit)
            {
                pivotInit = true;
                
                if (setPivot && m != null)
                {
                    Vector3 length = m.bounds.max - m.bounds.min;
                    length.x *= pivotPlacement.x;
                    length.y *= pivotPlacement.y;
                    length.z *= pivotPlacement.z;
                    
                    //Get the target localPosition from Mesh.bounds
                    Vector3 localTargetPoint = length + m.bounds.min;
                    
                    targetFilter.transform.localPosition = -localTargetPoint;
                }
            }
            
            Profiler.BeginSample("Setting Mesh Collider");
            
            if (targetCollider != null && m != null)
            {
                if (targetCollider is MeshCollider meshCollider)
                {
                    //No cookingOptions = Better performance, without a job we use none
                    if (!meshSettings.useThreadedCollisionBaking)
                        meshCollider.cookingOptions = MeshColliderCookingOptions.None;
                    else
                        meshCollider.cookingOptions = meshSettings.cookingOptions;
                    
                    //ThreadedColliderBaking is only used in playmode
                    if (editorMode || !meshSettings.useThreadedCollisionBaking)
                        meshCollider.sharedMesh = m;
                    else
                        colliderBakeRequested = true;
                }
                else if (targetCollider is BoxCollider boxCollider)
                {
                    if (UseVoxelBoxColliders)
                    {
                        EnsureVoxelColliderData();
                        if (meshSettings.colliderRebuildMode == MeshSettingsObj.ColliderRebuildMode.Full)
                            voxelCollidersNeedFullRebuild = true;

                        if (voxelCollidersNeedFullRebuild)
                            RebuildVoxelBoxCollidersFull();
                    }
                    else
                    {
                        boxCollider.center = m.bounds.center;
                        boxCollider.size = m.bounds.size;
                    }
                }
            }
            else if (targetCollider != null)
            {
                //Clear collider if not required
                if (targetCollider is MeshCollider meshCollider)
                    meshCollider.sharedMesh = null; 
                else if (targetCollider is BoxCollider boxCollider)
                {
                    if (UseVoxelBoxColliders)
                        ClearVoxelBoxColliders();
                    else
                        boxCollider.size = Vector3.zero;
                }
            }
            
            Profiler.EndSample();
        }

        /// <summary>
        /// Clears the Mesh, Collider, voxelData and created
        /// </summary>
        public virtual void Clear()
        {
            objectDestructionRequested = false;
            meshRegenerationActive = false;
            meshRegenerationRequested = false;
            colliderBakeActive = false;
            colliderBakeRequested = false;
            isValidObject = false;
            
            if (cMesh != null && Application.isPlaying)
                Destroy(cMesh, 10f);
            
            if (targetFilter != null)
                targetFilter.mesh = null;
            if (targetCollider != null)
            {
                if (targetCollider is MeshCollider meshCollider)
                    meshCollider.sharedMesh = null; 
                else if (targetCollider is BoxCollider boxCollider)
                {
                    if (UseVoxelBoxColliders)
                        ClearVoxelBoxColliders();
                    else
                        boxCollider.size = Vector3.zero;
                }
            }
            
            isCreated = false;
            DisposeAll();
        }
        
        /// <summary>
        /// Gets called from the editor quick setup button,
        /// creates some stuff based on the settings defined
        /// on the VoxelManager
        /// </summary>
        public virtual void QuickSetup(VoxelManager manager)
        {
            if (manager == null)
            {
                Debug.LogWarning("Add a Voxel Manager to the scene, if you want to use quick setup");
                return;
            }
            if (targetFilter != null)
            {
                Debug.Log("Can not create a new target Filter, already exists!");
                return;
            }
            
            GameObject nFilter = new GameObject();
            
            nFilter.transform.parent = transform;
            nFilter.gameObject.name = "Voxel Mesh";
            nFilter.transform.localPosition = Vector3.zero;
            
            targetFilter = nFilter.AddComponent<MeshFilter>();
            
            //Collider setup
            if (manager.standardCollider != VoxelManager.ColliderType.None)
            {
                MeshCollider mc = nFilter.AddComponent<MeshCollider>();
                targetCollider = mc;
                mc.cookingOptions = MeshColliderCookingOptions.None;
                
                if (manager.standardCollider == VoxelManager.ColliderType.Convex)
                    mc.convex = true;
            }
            else
                targetCollider = null;
            
            MeshRenderer mr = nFilter.AddComponent<MeshRenderer>();

            mr.material = manager.standardMaterial;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            meshSettings = manager.standardMeshSettings;
        }

        #endregion

        #region Events
        
        /// <summary>
        /// Destroys the mesh object
        /// </summary>
        protected virtual void DestroyVoxObj()
        {
            if (onVoxelDestroy != null)
                onVoxelDestroy.Invoke();
            
            Clear();
            
            if (meshSettings.emptyAction == MeshSettingsObj.EmptyAction.Destroy)
                Destroy(gameObject);
            else if (meshSettings.emptyAction == MeshSettingsObj.EmptyAction.Deactive)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Allows you to block the destruction of the object,
        /// some jobs need to complete before the voxelobject can be
        /// destroyed
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanDestroyObject()
        {
            return true;
        }
        
        private void OnApplicationQuit()
        {
            DisposeAll();
        }

        private void OnDestroy()
        {
            DisposeAll();
        }

        /// <summary>
        /// You can call this multiple times, but once called
        /// all jobs get disposed and you cant use them anymore
        /// </summary>
        protected virtual void DisposeAll()
        {
            if (mesher != null)
                mesher.Dispose();
            mesher = null;
            
            if (voxelData != null)
                voxelData.Dispose();
            voxelData = null;
        }
        
        #endregion

        #region Update
        
        protected virtual void Update()
        {
            if (objectDestructionRequested && CanDestroyObject())
                DestroyVoxObj();
            
            if (!isValidObject)
                return;
            
            if (meshRegenerationRequested && !meshRegenerationActive)
            {
                meshRegenerationRequested = false;
                if (isActiveAndEnabled)
                    StartCoroutine(GenerateMesh());
            }

            if (colliderBakeRequested && !colliderBakeActive && targetCollider is MeshCollider mc)
            {
                colliderBakeRequested = false;
                colliderBakeActive = true;
                colliderBaker.BakeCollider(mc, cMesh);
            }

            if (colliderBakeActive && colliderBaker.isCompleted())
            {
                colliderBakeActive = false;
                colliderBaker.FinishBaking();
                
                if (meshSettings.freezeRbWhileBaking)
                {
                    Rigidbody rb = GetComponent<Rigidbody>();

                    if (rb != null)
                        rb.isKinematic = false;
                }
            }
        }

        #endregion

        #region Other

        protected void RequestVoxelColliderRebuild(int3 min, int3 max)
        {
            if (!UseVoxelBoxColliders)
                return;

            EnsureVoxelColliderData();

            if (meshSettings.colliderRebuildMode == MeshSettingsObj.ColliderRebuildMode.Full)
            {
                voxelCollidersNeedFullRebuild = true;
                return;
            }

            RebuildVoxelBoxCollidersIncremental(min, max);
        }

        public bool TryGetClosestColliderPoint(Vector3 worldPoint, out Vector3 closestPoint)
        {
            if (UseVoxelBoxColliders && voxelColliders != null && voxelColliders.Count > 0)
            {
                bool found = false;
                float bestDistance = float.MaxValue;
                Vector3 bestPoint = worldPoint;

                foreach (var record in voxelColliders.Values)
                {
                    BoxCollider collider = record.collider;
                    if (collider == null || !collider.enabled)
                        continue;

                    Vector3 point = collider.ClosestPoint(worldPoint);
                    float dist = (point - worldPoint).sqrMagnitude;
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestPoint = point;
                        found = true;
                    }
                }

                closestPoint = bestPoint;
                return found;
            }

            if (targetCollider != null)
            {
                closestPoint = targetCollider.ClosestPoint(worldPoint);
                return true;
            }

            closestPoint = worldPoint;
            return false;
        }

        public bool OwnsCollider(Collider collider)
        {
            if (collider == null)
                return false;

            if (collider == targetCollider)
                return true;

            if (UseVoxelBoxColliders && voxelColliders != null)
            {
                foreach (var record in voxelColliders.Values)
                {
                    if (record.collider == collider)
                        return true;
                }
            }

            return targetCollider != null && collider.transform == targetCollider.transform;
        }

        private void EnsureVoxelColliderData()
        {
            if (!UseVoxelBoxColliders || voxelData == null)
                return;

            int volume = voxelData.Volume;
            if (voxelColliderMap == null || voxelColliderMap.Length != volume)
            {
                voxelColliderMap = new int[volume];
                Array.Fill(voxelColliderMap, -1);
                voxelColliders = new Dictionary<int, ColliderRecord>();
                voxelColliderPool = new List<BoxCollider>();
                nextVoxelColliderId = 1;
                voxelCollidersNeedFullRebuild = true;
            }
            else
            {
                voxelColliders ??= new Dictionary<int, ColliderRecord>();
                voxelColliderPool ??= new List<BoxCollider>();
            }

            if (targetCollider is BoxCollider boxCollider && !IsColliderTracked(boxCollider))
            {
                boxCollider.enabled = false;
                boxCollider.center = Vector3.zero;
                boxCollider.size = Vector3.zero;
                voxelColliderPool.Add(boxCollider);
            }
        }

        private bool IsColliderTracked(BoxCollider collider)
        {
            if (collider == null)
                return true;

            if (voxelColliderPool != null && voxelColliderPool.Contains(collider))
                return true;

            if (voxelColliders != null)
            {
                foreach (var record in voxelColliders.Values)
                {
                    if (record.collider == collider)
                        return true;
                }
            }

            return false;
        }

        private void ClearVoxelBoxColliders()
        {
            if (!UseVoxelBoxColliders)
                return;
            
            voxelColliderPool ??= new List<BoxCollider>();
            voxelColliders ??= new Dictionary<int, ColliderRecord>();

            foreach (var record in voxelColliders.Values)
                ReleaseCollider(record.collider);

            voxelColliders.Clear();

            if (voxelColliderMap != null)
                Array.Fill(voxelColliderMap, -1);

            voxelCollidersNeedFullRebuild = false;
        }

        private void RebuildVoxelBoxCollidersFull()
        {
            if (!UseVoxelBoxColliders)
                return;

            EnsureVoxelColliderData();

            if (voxelData == null || voxelData.IsEmpty())
            {
                ClearVoxelBoxColliders();
                return;
            }

            Profiler.BeginSample("Voxel Box Colliders Full Rebuild");

            ClearVoxelBoxColliders();
            int3 min = int3.zero;
            int3 max = voxelData.length - new int3(1, 1, 1);
            RebuildVoxelBoxCollidersInBounds(min, max);
            voxelCollidersNeedFullRebuild = false;

            Profiler.EndSample();
        }

        private void RebuildVoxelBoxCollidersIncremental(int3 min, int3 max)
        {
            if (!UseVoxelBoxColliders)
                return;

            EnsureVoxelColliderData();

            if (voxelData == null || voxelData.IsEmpty())
            {
                ClearVoxelBoxColliders();
                return;
            }

            if (voxelColliderMap == null || voxelColliderMap.Length != voxelData.Volume)
            {
                RebuildVoxelBoxCollidersFull();
                return;
            }

            int3 dirtyMin = min;
            int3 dirtyMax = max;
            if (!ClampBounds(ref dirtyMin, ref dirtyMax))
                return;

            Profiler.BeginSample("Voxel Box Colliders Incremental Rebuild");

            HashSet<int> collidersToRemove = new HashSet<int>();
            int3 length = voxelData.length;

            for (int z = dirtyMin.z; z <= dirtyMax.z; z++)
                for (int y = dirtyMin.y; y <= dirtyMax.y; y++)
                    for (int x = dirtyMin.x; x <= dirtyMax.x; x++)
                    {
                        int id = voxelColliderMap[To1D(x, y, z, length)];
                        if (id >= 0)
                            collidersToRemove.Add(id);
                    }

            foreach (int id in collidersToRemove)
            {
                if (!voxelColliders.TryGetValue(id, out ColliderRecord record))
                    continue;

                dirtyMin = math.min(dirtyMin, record.min);
                dirtyMax = math.max(dirtyMax, record.max);
            }

            if (!ClampBounds(ref dirtyMin, ref dirtyMax))
                return;

            foreach (int id in collidersToRemove)
            {
                if (!voxelColliders.TryGetValue(id, out ColliderRecord record))
                    continue;

                ClearColliderMap(id, record.min, record.max);
                ReleaseCollider(record.collider);
                voxelColliders.Remove(id);
            }

            RebuildVoxelBoxCollidersInBounds(dirtyMin, dirtyMax);

            Profiler.EndSample();
        }

        private void RebuildVoxelBoxCollidersInBounds(int3 min, int3 max)
        {
            if (voxelData == null)
                return;

            int3 length = voxelData.length;

            for (int z = min.z; z <= max.z; z++)
                for (int y = min.y; y <= max.y; y++)
                    for (int x = min.x; x <= max.x; x++)
                    {
                        int startIndex = To1D(x, y, z, length);
                        if (!IsVoxelAvailable(startIndex))
                            continue;

                        int maxX = x;
                        while (maxX + 1 <= max.x && IsVoxelAvailable(To1D(maxX + 1, y, z, length)))
                            maxX++;

                        int maxY = y;
                        while (maxY + 1 <= max.y && IsRowAvailable(x, maxX, maxY + 1, z, length))
                            maxY++;

                        int maxZ = z;
                        while (maxZ + 1 <= max.z && IsLayerAvailable(x, maxX, y, maxY, maxZ + 1, length))
                            maxZ++;

                        int3 boxMin = new int3(x, y, z);
                        int3 boxMax = new int3(maxX, maxY, maxZ);
                        int colliderId = CreateVoxelBoxCollider(boxMin, boxMax);

                        if (colliderId >= 0)
                        {
                            for (int zz = boxMin.z; zz <= boxMax.z; zz++)
                                for (int yy = boxMin.y; yy <= boxMax.y; yy++)
                                    for (int xx = boxMin.x; xx <= boxMax.x; xx++)
                                        voxelColliderMap[To1D(xx, yy, zz, length)] = colliderId;
                        }
                    }
        }

        private bool IsVoxelAvailable(int index)
        {
            return voxelData.voxels[index].active != 0 && voxelColliderMap[index] == -1;
        }

        private bool IsRowAvailable(int minX, int maxX, int y, int z, int3 length)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int index = To1D(x, y, z, length);
                if (!IsVoxelAvailable(index))
                    return false;
            }

            return true;
        }

        private bool IsLayerAvailable(int minX, int maxX, int minY, int maxY, int z, int3 length)
        {
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    int index = To1D(x, y, z, length);
                    if (!IsVoxelAvailable(index))
                        return false;
                }

            return true;
        }

        private int CreateVoxelBoxCollider(int3 min, int3 max)
        {
            BoxCollider collider = AcquireCollider();
            if (collider == null)
                return -1;

            ConfigureCollider(collider);

            float voxelSize = GetSingleVoxelSize();
            Vector3 voxelCenter = new Vector3(
                (min.x + max.x) * 0.5f,
                (min.y + max.y) * 0.5f,
                (min.z + max.z) * 0.5f);
            Vector3 voxelSizeLocal = new Vector3(
                max.x - min.x + 1,
                max.y - min.y + 1,
                max.z - min.z + 1) * voxelSize;

            Transform meshTransform = targetFilter != null ? targetFilter.transform : transform;
            Transform root = GetColliderRootTransform();

            Vector3 worldCenter = meshTransform.TransformPoint(voxelCenter * voxelSize);
            collider.center = root.InverseTransformPoint(worldCenter);

            Vector3 worldSize = Vector3.Scale(voxelSizeLocal, meshTransform.lossyScale);
            Vector3 rootScale = root.lossyScale;
            collider.size = new Vector3(
                rootScale.x != 0f ? Mathf.Abs(worldSize.x / rootScale.x) : 0f,
                rootScale.y != 0f ? Mathf.Abs(worldSize.y / rootScale.y) : 0f,
                rootScale.z != 0f ? Mathf.Abs(worldSize.z / rootScale.z) : 0f);

            int id = nextVoxelColliderId++;
            voxelColliders[id] = new ColliderRecord
            {
                collider = collider,
                min = min,
                max = max
            };

            return id;
        }

        private void ConfigureCollider(BoxCollider collider)
        {
            if (targetCollider is not BoxCollider template || collider == null)
                return;

            collider.isTrigger = template.isTrigger;
            collider.sharedMaterial = template.sharedMaterial;
            collider.contactOffset = template.contactOffset;
            collider.enabled = true;
        }

        private BoxCollider AcquireCollider()
        {
            if (voxelColliderPool == null)
                voxelColliderPool = new List<BoxCollider>();

            if (targetCollider is BoxCollider targetBox && !IsColliderTracked(targetBox))
            {
                targetBox.enabled = true;
                return targetBox;
            }

            for (int i = voxelColliderPool.Count - 1; i >= 0; i--)
            {
                BoxCollider collider = voxelColliderPool[i];
                voxelColliderPool.RemoveAt(i);

                if (collider == null)
                    continue;

                collider.enabled = true;
                return collider;
            }

            Transform root = GetColliderRootTransform();
            return root != null ? root.gameObject.AddComponent<BoxCollider>() : null;
        }

        private void ReleaseCollider(BoxCollider collider)
        {
            if (collider == null)
                return;

            collider.enabled = false;
            collider.center = Vector3.zero;
            collider.size = Vector3.zero;

            voxelColliderPool ??= new List<BoxCollider>();
            if (!voxelColliderPool.Contains(collider))
                voxelColliderPool.Add(collider);
        }

        private Transform GetColliderRootTransform()
        {
            if (targetCollider != null)
                return targetCollider.transform;
            if (targetFilter != null)
                return targetFilter.transform;
            return transform;
        }

        private void ClearColliderMap(int colliderId, int3 min, int3 max)
        {
            int3 length = voxelData.length;

            for (int z = min.z; z <= max.z; z++)
                for (int y = min.y; y <= max.y; y++)
                    for (int x = min.x; x <= max.x; x++)
                    {
                        int index = To1D(x, y, z, length);
                        if (voxelColliderMap[index] == colliderId)
                            voxelColliderMap[index] = -1;
                    }
        }

        private bool ClampBounds(ref int3 min, ref int3 max)
        {
            int3 maxIndex = voxelData.length - new int3(1, 1, 1);
            min = math.max(min, int3.zero);
            max = math.min(max, maxIndex);

            return min.x <= max.x && min.y <= max.y && min.z <= max.z;
        }
        
        /// <summary>
        /// This method checks if the settings are valid,
        /// will abort the mesh creation in case there are invalid settings
        /// </summary>
        /// <returns></returns>
        protected virtual bool AssertVoxelObject()
        {
            if (GetSingleVoxelSize() == 0)
            {
                Debug.LogError("Object scale can not be zero!");
                return false;
            }
            else if (targetFilter == null)
            {
                Debug.LogError("Target filter is null!");
                return false;
            }
            else if (meshSettings == null)
            {
                Debug.LogError("Mesh settings not assigned!");
                return false;
            }
            else if (setPivot && targetFilter.gameObject == gameObject)
            {
                Debug.LogError("If setPivot is enabled the targetFilter needs to be on a child of the object!");
                return false;
            }
            
            return true;
        }

        //Index Stuff
        protected Vector3 To3D(int index, int xMax, int yMax)
        {
            int z = index / (xMax * yMax);
            int idx = index - (z * xMax * yMax);
            int y = idx / xMax;
            int x = idx % xMax;
            return new Vector3(x, y, z);
        }
        
        protected Vector3 To3D(int index)
        {
            int z = index / (voxelData.length.x * voxelData.length.y);
            int idx = index - (z * voxelData.length.x * voxelData.length.y);
            int y = idx / voxelData.length.x;
            int x = idx % voxelData.length.x;
            return new Vector3(x, y, z);
        }
        
        protected int To1D(Vector3 index, int xMax, int yMax)
        {
            return (int)(index.x + xMax * (index.y + yMax * index.z));
        }

        private static int To1D(int x, int y, int z, int3 length)
        {
            return x + length.x * (y + length.y * z);
        }
        
        protected Vector3 GetMinV3(VoxReader.Voxel[] array)
        {
            Vector3 min = array[0].Position;
            
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i].Position.x < min.x)
                    min = new Vector3(array[i].Position.x, min.y, min.z);
                
                if (array[i].Position.y < min.y)
                    min = new Vector3(min.x, array[i].Position.y, min.z);
                
                if (array[i].Position.z < min.z)
                    min = new Vector3(min.x, min.y, array[i].Position.z);
            }
            
            return min;
        }

        public Color GetColorFromVoxelData()
        {
            return voxelData.palette[Random.Range(0, voxelData.palette.Length)];
        }

        public float GetSingleVoxelSize()
        {
            return scaleType switch
            {
                ScaleType.Voxel => objectScale,
                ScaleType.Units => objectScale / math.length(voxelData.length),
                _ => throw new InvalidOperationException()
            };
        }

        public void RequestMeshRegeneration()
        {
            meshRegenerationRequested = true;
        }

        protected GameObject InstantiateVox(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (VoxelManager.Instance == null)
                return Instantiate(prefab, pos, rot);
            else
                return VoxelManager.Instance.InstantiatePooled(prefab, pos, rot);
        }
        
        protected GameObject InstantiateVox(GameObject prefab)
        {
            if (VoxelManager.Instance == null)
                return Instantiate(prefab);
            else
                return VoxelManager.Instance.InstantiatePooled(prefab);
        }
        
        #endregion

        #region Debug

        #if UNITY_EDITOR
        
        [ContextMenu("Show debug info")]
        public void ShowDebugInfo()
        {
            Debug.Log("-- Voxel object " + name + " --");
            Debug.Log("IsValidObject: " + isValidObject);
            Debug.Log("Start voxel count: " + startVoxelCount);
            Debug.Log("Is Created: " + isCreated);
            Debug.Log("Active voxel count: " + ActiveVoxelCount);
            Debug.Log("Voxel data volume: " + VoxelDataVolume);
        }

        #endif
        
        #endregion
    }      
}
