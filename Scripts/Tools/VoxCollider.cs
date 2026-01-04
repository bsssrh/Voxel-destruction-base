using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelModifications;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Tools
{
    public class VoxCollider : MonoBehaviour
    {
        [Tooltip("The minimum collision relative velocity, increase this if a stationary object still triggers destruction")]
        public float minCollisionRelative = 1;
        
        [Space]
        
        [Tooltip("The radius of the destruction, for sphere destructiontype this is the radius of the sphere")]
        public float destructionRadius = 10;
        [Tooltip("If enabled the destruction radius will be effected by the relative velocity of the collision")]
        public bool useRelativeVelocity;
        [Tooltip("If enabled the destruction radius will be effect by the targets object voxel size")]
        public bool useObjScale;

        [Space] 
        
        [Tooltip("The destruction type that should be used")]
        public DestructionData.DestructionType destructionType = DestructionData.DestructionType.Sphere;

        [Tooltip("The impact type used for voxel color modification.")]
        public ImpactType impactType = ImpactType.Bullet;

        [Header("Paint Settings")]
        [Min(0f)]
        public float paintRadius;

        [Range(0f, 1f)]
        public float paintNoise;

        [Min(0.01f)]
        public float paintFalloff = 1f;

        [Range(0f, 1f)]
        public float paintIntensity = 1f;
        
        private void OnCollisionEnter(Collision other)
        {
            DynamicVoxelObj vox = other.transform.GetComponentInParent<DynamicVoxelObj>();
            
            if (vox == null)
                return;
            
            float mag = other.relativeVelocity.magnitude;

            if (mag < minCollisionRelative)
                return;
            
            float rad = destructionRadius;

            if (useRelativeVelocity)
                rad *= mag * 0.1f;

            if (useObjScale)
                rad /= vox.GetSingleVoxelSize();

            if (vox.TryGetComponent(out VoxelColorModifier colorModifier))
            {
                ContactPoint contact = other.GetContact(0);
                colorModifier.ApplyImpactColor(other.collider, contact.point, impactType, paintRadius, paintNoise, paintFalloff, paintIntensity);
            }

            vox.AddDestruction(new DestructionData(destructionType, other.contacts[0].point, other.contacts[0].point - other.contacts[0].normal * rad, rad));
        }
    }
}

