using System;
using UnityEngine;
using VoxelDestructionPro.VoxelObjects;
using VoxelDestructionPro.VoxelModifications;

namespace VoxelDestructionPro.VoxelModifications
{
    public class Mod_DestroyParticleEffect : VoxModification
    {
        public GameObject particleEffect;
        public float destroyTime = 2f;

        [Space]
        
        public bool setParticleColor;

        private void Start()
        {
            targetObj.onVoxelDestroy += OnVoxelDestroy;
        }

        private void OnVoxelDestroy()
        {
            GameObject obj = Instantiate(particleEffect, transform.position, transform.rotation);
            Destroy(obj, destroyTime);

            if (setParticleColor && obj.TryGetComponent(out ParticleSystem ps))
            {
                var main = ps.main;
                main.startColor = targetObj.GetColorFromVoxelData();
            }
        }
    }
}