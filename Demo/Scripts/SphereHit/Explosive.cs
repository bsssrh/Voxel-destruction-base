using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelModifications;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Demo
{
    public class Explosive : MonoBehaviour
    {
        public float explosionDelay;
        private float explosionTime;

        [Space]
        public float explosionRadius = 10f;

        // В VDP это по факту "range" разрушения (радиус воксельного воздействия).
        public float explosionForce = 20f;

        public DestructionData.DestructionType destructionType = DestructionData.DestructionType.Sphere;

        [Header("Material Filter")]
        [Tooltip("Control which voxel materials can be destroyed by this explosive.")]
        public VoxelMaterialFilter materialFilter = new()
        {
            affectAllMaterials = true,
            materialTypes = new List<VoxelMaterialType>()
        };

        [Header("Impact Type")]
        [Tooltip("Impact type used for voxel color modification.")]
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

        private void Start()
        {
            explosionTime = Time.time + explosionDelay;
        }

        private void Update()
        {
            if (Time.time <= explosionTime)
                return;

            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                explosionRadius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            IEnumerable<VoxelMaterialType> materials = materialFilter.GetFilter();

            for (int i = 0; i < colliders.Length; i++)
            {
                DynamicVoxelObj vox = colliders[i].GetComponentInParent<DynamicVoxelObj>();
                if (vox == null)
                    continue;

                bool destructionStarted = destructionType == DestructionData.DestructionType.Sphere
                    ? vox.AddDestruction_Sphere(transform.position, explosionForce, materials)
                    : vox.AddDestruction_Cube(transform.position, explosionForce, materials);

                if (destructionStarted && vox.TryGetComponent(out VoxelColorModifier colorModifier))
                {
                    Collider hitCollider = colliders[i];
                    Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
                    colorModifier.ApplyImpactColor(hitCollider, hitPoint, impactType, paintRadius, paintNoise, paintFalloff, paintIntensity);
                }
            }

            Debug.Log("[Explosive] triggered at " + transform.position);
            Destroy(gameObject);
        }
    }
}
