using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Jobs.Isolator;
using VoxelDestructionPro.Jobs.Simple;
using VoxelDestructionPro.Settings;
using VoxelDestructionPro.VoxDataProviders;

namespace VoxelDestructionPro.VoxelObjects
{
     /// <summary>
     /// Finds isolated pieces in the voxelobject and removes them
     /// </summary>
    public class IsolatedVoxelObj : VoxelObjBase
    {
        [Header("Isolation")] 
        
        public IsoSettings isoSettings;
        [Tooltip("The isolation origin, defines the axis that is connected to a solid object. " +
                 "If you object has physics you probably want to set this to none so that a new fragment is created whenever split.")]
        public IsoSettings.IsolationOrigin isolationOrigin = IsoSettings.IsolationOrigin.YNeg;
        [Tooltip("You can assign a parent for all the fragments that will get created, " +
                 "this helps keeping the scene organized and keeping track of the fragments. " +
                 "Make sure that this object has a scale of (1, 1, 1)")]
        public Transform fragmentParent;
        
        //Active states
        protected bool isolatorActive;
        protected bool isolationProcessorActive;

        //Requires
        protected bool isolatorRequested;
        
        //Locks
        /// <summary>
        /// Blocks the Isolator from running
        /// </summary>
        protected bool lockIsolatorRun;
        /// <summary>
        /// Blocks the Isolator from starting a mesh reload once it is completed
        /// </summary>
        protected bool lockIsolatorRebuild;
        
        private CCL_Isolator isolator;
        private IsolationProcessor isolationProcessor;

        //Events
        public Action<GameObject> onIsolationFragmentCreated;
        public Action<NativeArray<ushort>> onIsolationDataReturned;
        
        protected override void Start()
        {
            base.Start();

            if (isValidObject && isoSettings.runIsolationOnStart && isoSettings.isolationMode != IsoSettings.IsolationMode.None)
                isolatorRequested = true;
        }

        protected override bool AssertVoxelObject()
        {
            if (isoSettings == null)
            {
                Debug.LogError("No isolation settings assigned!");
                return false;
            }
            else
                return base.AssertVoxelObject();
        }

        protected override void CreateJobs()
        {
            base.CreateJobs();

            if (isoSettings.isolationMode != IsoSettings.IsolationMode.None)
            {
                isolator ??= new CCL_Isolator(isolationOrigin, voxelData);
                if (isoSettings.isolationMode == IsoSettings.IsolationMode.Fragment)
                    isolationProcessor ??= new IsolationProcessor(voxelData);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!isValidObject)
                return;
            
            if (isolatorRequested && !isolatorActive && !lockIsolatorRun && !isolationProcessorActive)
            {
                if (isoSettings.isolationMode == IsoSettings.IsolationMode.None)
                    return;
                
                isolatorRequested = false;
                isolatorActive = true;
                
                isolator.Begin(voxelData);
            }

            if (isolatorActive && isolator.IsFinished())
                FinishIsolation();
            
            if (isolationProcessorActive && isolationProcessor.ProcessorCompleted())
                FinishFragmentProcessing();
        }

        
        /// <summary>
        /// Finishes the Isolation Job
        /// </summary>
        private void FinishIsolation()
        {
            NativeArray<ushort> data = isolator.GetResults();
            Profiler.BeginSample("Setting Isolation data");
            
            if (isoSettings.isolationMode == IsoSettings.IsolationMode.Fragment)
            {
                isolationProcessor.ProcessIsolationData(data, voxelData);
                isolationProcessorActive = true;
            }

            VoxelOverride overrideJob = new VoxelOverride()
            {
                voxels = voxelData.voxels,
                data = data
            };
            
            overrideJob.Run();

            if (onIsolationDataReturned != null) 
                onIsolationDataReturned.Invoke(data);
            
            if (!lockIsolatorRebuild)
                meshRegenerationRequested = true;
            
            Profiler.EndSample();
            
            isolatorActive = false;
        }

        private void FinishFragmentProcessing()
        {
            isolationProcessorActive = false;
            VoxelData[] fragments = isolationProcessor.CreateFragments(voxelData, out Vector3[] positions);

            if (fragments == null)
                return;

            if (targetFilter == null)
            {
                for (int i = 0; i < fragments.Length; i++)
                    fragments[i]?.Dispose();
                return;
            }
            
            for (int i = 0; i < fragments.Length; i++)
            {
                // Temporarily disable the min voxel filter to validate fragment creation.
                int minVoxelCount = 0;
                if (minVoxelCount > 0)
                    if (!fragments[i].ActiveCountLarger(minVoxelCount))
                    {
                        fragments[i].Dispose();
                        continue; 
                    }
                
                GameObject nObj = InstantiateVox(isoSettings.isolationFragmentPrefab, targetFilter.transform.TransformPoint(positions[i] * GetSingleVoxelSize()), transform.rotation);
                DisableDataProviders(nObj);
                nObj.transform.parent = fragmentParent;
                
                VoxelObjBase vox = nObj.GetComponent<VoxelObjBase>();
                vox.setPivot = false;

                if (vox != null)
                {
                    vox.scaleType = ScaleType.Voxel;
                    vox.objectScale = GetSingleVoxelSize();
                    if (vox is IsolatedVoxelObj iso)
                        iso.fragmentParent = fragmentParent;
                    
                    vox.AssignVoxelData(fragments[i]);   
                }
                else
                    fragments[i].Dispose();
                
                if (onIsolationFragmentCreated != null)
                    onIsolationFragmentCreated.Invoke(nObj);
            }
        }

        private static void DisableDataProviders(GameObject obj)
        {
            if (obj == null)
                return;

            VoxDataProvider[] providers = obj.GetComponents<VoxDataProvider>();
            for (int i = 0; i < providers.Length; i++)
                providers[i].enabled = false;
        }

        public override void QuickSetup(VoxelManager manager)
        {
            base.QuickSetup(manager);

            isoSettings = manager.standardIsolationSettings;
            fragmentParent = manager.fragmentParent;
        }

        protected override bool CanDestroyObject()
        {
            if (isolatorActive || isolationProcessorActive)
                return false;
            
            return base.CanDestroyObject();
        }

        protected override void DisposeAll()
        {
            if (isolationProcessor != null)
                isolationProcessor.Dispose();
            isolationProcessor = null;
            if (isolator != null)
                isolator.Dispose();
            isolator = null;
            
            base.DisposeAll();
        }

        protected override void DestroyVoxObj()
        {
            isolatorActive = false;
            isolationProcessorActive = false;
            isolatorRequested = false;
            base.DestroyVoxObj();
        }
    }   
}
