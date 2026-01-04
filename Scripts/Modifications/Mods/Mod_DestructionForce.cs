using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.VoxelModifications;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Tools
{
    public class Mod_DestructionForce : VoxModification
    {
        public bool inverseDirection;
        public float destructionForce = 10f;

        private List<Tuple<Rigidbody, Vector3>> cachedFragments;
        
        private void Start()
        {
            cachedFragments = new List<Tuple<Rigidbody, Vector3>>();
        
            if (targetObj is not DynamicVoxelObj vox)
                Debug.LogWarning("No dynamicVoxelobject found!");
            else
                vox.onFragmentSpawned += OnFragmentSpawned;
        }

        private void OnFragmentSpawned(GameObject obj)
        {
            if (!obj.TryGetComponent(out Rigidbody rb))
                return;
        
            Vector3 direction = obj.transform.position - dyn_targetObj.lastDestructionPoint;

            if (inverseDirection)
                direction = -direction;

            Vector3 f = direction.normalized * destructionForce;
            cachedFragments.Add(new Tuple<Rigidbody, Vector3>(rb, f));
        }

        private void FixedUpdate()
        {
            for (var i = cachedFragments.Count - 1; i >= 0; i--)
            {
                if (cachedFragments[i].Item1 == null)
                {
                    cachedFragments.RemoveAt(i);
                    continue;
                }
                
                if (cachedFragments[i].Item1.isKinematic)
                    continue;
                
                cachedFragments[i].Item1.AddForce(cachedFragments[i].Item2, ForceMode.VelocityChange);
                cachedFragments.RemoveAt(i);
            }
        }
    }
}