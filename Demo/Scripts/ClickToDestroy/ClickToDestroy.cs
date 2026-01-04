using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelModifications;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Demo
{
    public class ClickToDestroy : MonoBehaviour
    {
        public Camera cam;

        public float destructionRadius = 2;

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
    
        private void Update()
        {
            bool oneClick = Input.GetKey(KeyCode.LeftShift);
        
            if ((!oneClick && Input.GetMouseButton(0)) || (oneClick && Input.GetMouseButtonDown(0)))
            {
                Ray r = cam.ScreenPointToRay(Input.mousePosition);
            
                if (!Physics.Raycast(r, out RaycastHit hit, 999))
                    return;
            
                DynamicVoxelObj vo = hit.transform.GetComponentInParent<DynamicVoxelObj>();
            
                if (vo == null)
                    return;
                
                bool destructionStarted = vo.AddDestruction_Sphere(hit.point, destructionRadius);

                if (vo.TryGetComponent(out VoxelColorModifier colorModifier))
                    colorModifier.ApplyImpactColor(hit.collider, hit.point, impactType, paintRadius, paintNoise, paintFalloff, paintIntensity);
            }
        }

        public void SwitchToMovement()
        {
            SceneManager.LoadScene(0);
        }
    }
}
