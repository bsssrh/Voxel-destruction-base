using System.Collections;
using Unity.Collections;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Runtime;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Runtime
{
    /// <summary>
    /// Attach this to fragmentPrefab.
    /// It forces the fragment to use the runtime palette from the source object that spawned it,
    /// preventing template/default palette from overwriting burn/paint colors.
    /// </summary>
    [DisallowMultipleComponent]
    public class VoxelFragmentInheritRuntimePalette : MonoBehaviour
    {
        [Header("Fix Settings")]
        [Tooltip("How many frames to wait/poll for voxelData assignment and possible template overwrites.")]
        [Range(1, 30)]
        public int maxPollFrames = 10;

        [Tooltip("If true, waits until end of frame (after most Start calls) before applying palette.")]
        public bool waitForEndOfFrame = true;

        [Tooltip("If true, also copy voxelMaterialType from source (if fragment has DynamicVoxelObj).")]
        public bool inheritMaterialType = true;

        private Color[] _sourcePaletteCopy;
        private bool _hasSourcePalette;
        private int _sourceId;
        private bool _hasSourceId;

        private void Awake()
        {
            // Read spawn context as early as possible
            if (VoxelFragmentSpawnContext.TryPeek(out var payload))
            {
                _sourcePaletteCopy = payload.paletteCopy;
                _hasSourcePalette = payload.hasPalette;

                _sourceId = payload.sourceInstanceId;
                _hasSourceId = payload.hasSource;

                if (inheritMaterialType && payload.hasMaterialType)
                {
                    var dyn = GetComponent<DynamicVoxelObj>();
                    if (dyn != null)
                        dyn.voxelMaterialType = payload.materialType;
                }
            }
        }

        private void OnEnable()
        {
            StartCoroutine(ApplyWhenReady());
        }

        private IEnumerator ApplyWhenReady()
        {
            // Let template scripts run (Awake/OnEnable/Start) so we can override last.
            if (waitForEndOfFrame)
                yield return new WaitForEndOfFrame();
            else
                yield return null;

            int frames = 0;

            while (frames < maxPollFrames)
            {
                frames++;

                if (!this || gameObject == null)
                    yield break;

                var voxBase = GetComponent<VoxelObjBase>();
                if (voxBase == null)
                    yield break;

                // We can only apply after voxelData exists.
                if (voxBase.voxelData == null || !voxBase.voxelData.palette.IsCreated)
                {
                    yield return null;
                    continue;
                }

                if (_hasSourcePalette && _sourcePaletteCopy != null && _sourcePaletteCopy.Length > 0)
                {
                    if (!PaletteMatches(voxBase.voxelData.palette, _sourcePaletteCopy))
                    {
                        ForceSetPalette(voxBase.voxelData, _sourcePaletteCopy);

                        // Trigger mesh rebuild using public API if possible
                        TryRequestMeshRegen(voxBase);
                    }
                }

                // One successful pass is enough; if something overwrites later, you can increase maxPollFrames.
                yield break;
            }
        }

        private static bool PaletteMatches(NativeArray<Color> current, Color[] desired)
        {
            if (!current.IsCreated || desired == null) return false;
            if (current.Length != desired.Length) return false;

            // Quick compare (exact). Your palette is stored as floats; if you generate colors deterministically, exact compare works.
            // If you need tolerant compare later, we can add epsilon.
            for (int i = 0; i < desired.Length; i++)
            {
                if (current[i] != desired[i])
                    return false;
            }
            return true;
        }

        private static void ForceSetPalette(VoxelData data, Color[] desiredPalette)
        {
            if (data == null || desiredPalette == null)
                return;

            if (data.palette.IsCreated)
                data.palette.Dispose();

            data.palette = new NativeArray<Color>(desiredPalette, Allocator.Persistent);
        }

        private static void TryRequestMeshRegen(VoxelObjBase voxBase)
        {
            // If you have a public method like RequestMeshRegeneration() use it.
            // Otherwise, we safely poke typical flags via known methods if present.
            var dyn = voxBase as DynamicVoxelObj;
            if (dyn != null)
            {
                dyn.RequestMeshRegeneration();
                return;
            }

            // Fallback: if base class has a public method - call it
            // (If not, we just re-enable component to force rebuild in many VDP setups)
            voxBase.enabled = false;
            voxBase.enabled = true;
        }
    }
}
