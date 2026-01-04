using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Settings;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.VoxelModifications
{
    public class VoxelColorModifier : VoxModification
    {
        public VoxelColorProfile colorProfile;
        private VoxelData lastVoxelData;
        private bool voxelDataCloned;
        private const int MaxImpactWaitFrames = 10;

        private bool impactPending;
        private int impactFrame;
        private ImpactType pendingImpactType;
        private Vector3 pendingPoint;
        private Collider pendingCollider;
        private PendingImpactKind pendingKind;
        private float pendingRadius;
        private float pendingNoise;
        private float pendingFalloff;
        private float pendingIntensity;
        private int lastRemovedFrame = -1;
        private int lastRemovedCount;

        private enum PendingImpactKind
        {
            Point,
            ColliderPoint
        }
        public void ApplyImpactColorUnclamped(
         Vector3 impactPoint,
         ImpactType impactType,
         float paintRadius,
         float paintNoise,
         float paintFalloff,
         float paintIntensity)
        {
            if (paintRadius <= 0f)
                return;

            DynamicVoxelObj voxelObj = dyn_targetObj;
            if (voxelObj == null || colorProfile == null || voxelObj.voxelData == null)
                return;

            string meshTag = GetColliderMeshTag(voxelObj);
            if (!colorProfile.TryGetTagEntry(impactType, meshTag, out TagEntry tagEntry))
                return;

            ApplyColorInternal(voxelObj, impactPoint, tagEntry, Time.frameCount, paintRadius, paintNoise, paintFalloff, paintIntensity);
        }
        private void OnEnable()
        {
            if (dyn_targetObj != null)
                dyn_targetObj.onBeforeVoxelsRemoved += HandleBeforeVoxelsRemoved;
            if (dyn_targetObj != null)
                dyn_targetObj.onVoxelsRemoved += HandleVoxelsRemoved;
        }

        private void OnDisable()
        {
            if (dyn_targetObj != null)
                dyn_targetObj.onBeforeVoxelsRemoved -= HandleBeforeVoxelsRemoved;
            if (dyn_targetObj != null)
                dyn_targetObj.onVoxelsRemoved -= HandleVoxelsRemoved;
        }

        private void HandleBeforeVoxelsRemoved(NativeList<int> removedVoxels)
        {
            lastRemovedCount = removedVoxels.Length;
            lastRemovedFrame = Time.frameCount;

            if (!impactPending || lastRemovedCount <= 0)
                return;

            ApplyPendingImpact();
        }

        private void HandleVoxelsRemoved(NativeList<int> removedVoxels)
        {
            lastRemovedCount = removedVoxels.Length;
            lastRemovedFrame = Time.frameCount;

            if (!impactPending || lastRemovedCount <= 0)
                return;

            if (Time.frameCount - impactFrame > MaxImpactWaitFrames)
            {
                impactPending = false;
                return;
            }

            ApplyPendingImpact();
        }

        public void ApplyImpactColor(Vector3 impactPoint, ImpactType impactType, float paintRadius, float paintNoise, float paintFalloff, float paintIntensity)
        {
            QueueImpact(PendingImpactKind.Point, null, impactPoint, impactType, paintRadius, paintNoise, paintFalloff, paintIntensity);
        }

        private void ApplyImpactColorImmediate(Vector3 impactPoint, ImpactType impactType, int noiseSeed, float paintRadius, float paintNoise, float paintFalloff, float paintIntensity)
        {
            if (paintRadius <= 0f)
                return;

            DynamicVoxelObj voxelObj = dyn_targetObj;
            if (voxelObj == null || colorProfile == null || voxelObj.voxelData == null)
                return;

            if (!voxelObj.TryGetClosestColliderPoint(impactPoint, out Vector3 closestPoint))
                return;
            float maxImpactDistance = Mathf.Max(0.0001f, voxelObj.GetSingleVoxelSize() * 0.5f);
            if ((closestPoint - impactPoint).sqrMagnitude > maxImpactDistance * maxImpactDistance)
                return;

            string meshTag = GetColliderMeshTag(voxelObj);
            if (!colorProfile.TryGetTagEntry(impactType, meshTag, out TagEntry tagEntry))
                return;

            ApplyColorInternal(voxelObj, closestPoint, tagEntry, noiseSeed, paintRadius, paintNoise, paintFalloff, paintIntensity);
        }

        public void ApplyImpactColor(RaycastHit hit, ImpactType impactType, float paintRadius, float paintNoise, float paintFalloff, float paintIntensity)
        {
            ApplyImpactColor(hit.collider, hit.point, impactType, paintRadius, paintNoise, paintFalloff, paintIntensity);
        }

        public void ApplyImpactColor(Collider hitCollider, Vector3 hitPoint, ImpactType impactType, float paintRadius, float paintNoise, float paintFalloff, float paintIntensity)
        {
            QueueImpact(PendingImpactKind.ColliderPoint, hitCollider, hitPoint, impactType, paintRadius, paintNoise, paintFalloff, paintIntensity);
        }

        private void ApplyImpactColorImmediate(Collider hitCollider, Vector3 hitPoint, ImpactType impactType, int noiseSeed, float paintRadius, float paintNoise, float paintFalloff, float paintIntensity)
        {
            if (paintRadius <= 0f)
                return;

            DynamicVoxelObj voxelObj = dyn_targetObj;
            if (voxelObj == null || colorProfile == null || voxelObj.voxelData == null)
                return;

            if (!voxelObj.OwnsCollider(hitCollider))
                return;

            string meshTag = GetColliderMeshTag(voxelObj);
            if (!colorProfile.TryGetTagEntry(impactType, meshTag, out TagEntry tagEntry))
                return;

            ApplyColorInternal(voxelObj, hitPoint, tagEntry, noiseSeed, paintRadius, paintNoise, paintFalloff, paintIntensity);
        }

        private void QueueImpact(PendingImpactKind kind, Collider hitCollider, Vector3 hitPoint, ImpactType impactType, float paintRadius, float paintNoise, float paintFalloff, float paintIntensity)
        {
            impactPending = true;
            impactFrame = Time.frameCount;
            pendingKind = kind;
            pendingCollider = hitCollider;
            pendingPoint = hitPoint;
            pendingImpactType = impactType;
            pendingRadius = paintRadius;
            pendingNoise = paintNoise;
            pendingFalloff = paintFalloff;
            pendingIntensity = paintIntensity;

            if (lastRemovedCount > 0 && lastRemovedFrame == Time.frameCount)
                ApplyPendingImpact();
        }

        private void ApplyPendingImpact()
        {
            if (!impactPending)
                return;

            impactPending = false;

            if (pendingKind == PendingImpactKind.Point)
                ApplyImpactColorImmediate(pendingPoint, pendingImpactType, impactFrame, pendingRadius, pendingNoise, pendingFalloff, pendingIntensity);
            else
                ApplyImpactColorImmediate(pendingCollider, pendingPoint, pendingImpactType, impactFrame, pendingRadius, pendingNoise, pendingFalloff, pendingIntensity);
        }

        private string GetColliderMeshTag(DynamicVoxelObj voxelObj)
        {
            if (voxelObj.targetCollider != null)
                return voxelObj.targetCollider.gameObject.tag;

            if (voxelObj.targetFilter != null)
                return voxelObj.targetFilter.gameObject.tag;

            return "Untagged";
        }

        private void ApplyColorInternal(
            DynamicVoxelObj voxelObj,
            Vector3 impactPoint,
            TagEntry tagEntry,
            int noiseSeed,
            float paintRadius,
            float paintNoise,
            float paintFalloff,
            float paintIntensity)
        {
            EnsureUniqueVoxelData(voxelObj);
            VoxelData voxelData = voxelObj.voxelData;

            // Важно: точку удара конвертим в локальные координаты меша (если есть filter), иначе в локаль объекта.
            Transform meshTransform = voxelObj.targetFilter != null ? voxelObj.targetFilter.transform : voxelObj.transform;

            // Переходим в "воксельные координаты" (в единицах размера одного вокселя).
            Vector3 localPoint = meshTransform.InverseTransformPoint(impactPoint) / voxelObj.GetSingleVoxelSize();

            int3 length = voxelData.length;

            int centerX = Mathf.Clamp(Mathf.RoundToInt(localPoint.x), 0, length.x - 1);
            int centerY = Mathf.Clamp(Mathf.RoundToInt(localPoint.y), 0, length.y - 1);
            int centerZ = Mathf.Clamp(Mathf.RoundToInt(localPoint.z), 0, length.z - 1);

            float radius = Mathf.Max(0f, paintRadius);
            if (radius <= 0f)
                return;

            int radiusCeil = Mathf.CeilToInt(radius);
            int minX = Mathf.Max(0, centerX - radiusCeil);
            int minY = Mathf.Max(0, centerY - radiusCeil);
            int minZ = Mathf.Max(0, centerZ - radiusCeil);
            int maxX = Mathf.Min(length.x - 1, centerX + radiusCeil);
            int maxY = Mathf.Min(length.y - 1, centerY + radiusCeil);
            int maxZ = Mathf.Min(length.z - 1, centerZ + radiusCeil);

            System.Collections.Generic.Dictionary<Color32, byte> paletteLookup = BuildPaletteLookup(voxelData);
            System.Collections.Generic.List<Color> paletteColors = new System.Collections.Generic.List<Color>(voxelData.palette.ToArray());

            bool paletteChanged = false;
            float radiusSquared = radius * radius;
            float edgeNoise = Mathf.Clamp01(paintNoise);
            float intensity = Mathf.Clamp01(paintIntensity);
            float falloff = Mathf.Max(0.01f, paintFalloff);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        int deltaX = x - centerX;
                        int deltaY = y - centerY;
                        int deltaZ = z - centerZ;
                        float distanceSquared = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
                        if (distanceSquared > radiusSquared)
                            continue;

                        int voxelIndex = To1D(x, y, z, length);
                        Voxel voxel = voxelData.voxels[voxelIndex];

                        if (voxel.active == 0)
                            continue;

                        if (edgeNoise > 0f)
                        {
                            float distance = Mathf.Sqrt(distanceSquared);
                            float t = Mathf.Clamp01(distance / radius);
                            float skipChance = edgeNoise * t;
                            if (Hash01(voxelIndex, noiseSeed) < skipChance)
                                continue;
                        }

                        Color originalColor = voxelData.palette[voxel.color];
                        Color targetColor = tagEntry.targetColor;
                        if (tagEntry.blendMode == VoxelColorBlendMode.BlendToOriginal && radius > 0)
                        {
                            float distance = Mathf.Sqrt(distanceSquared);
                            float t = Mathf.Clamp01(distance / radius);
                            t = Mathf.Pow(t, falloff);
                            targetColor = Color.Lerp(tagEntry.targetColor, originalColor, t);
                        }

                        if (intensity < 1f)
                            targetColor = Color.Lerp(originalColor, targetColor, intensity);

                        byte colorIndex = GetOrAddPaletteIndex(paletteLookup, paletteColors, targetColor, ref paletteChanged);
                        voxel.color = colorIndex;
                        voxelData.voxels[voxelIndex] = voxel;
                    }
                }
            }

            if (paletteChanged)
            {
                voxelData.palette.Dispose();
                voxelData.palette = new Unity.Collections.NativeArray<Color>(
                    paletteColors.ToArray(),
                    Unity.Collections.Allocator.Persistent
                );
            }

            voxelObj.RequestMeshRegeneration();
        }

        private static float Hash01(int value, int seed)
        {
            unchecked
            {
                uint hash = (uint)value;
                hash ^= (uint)seed + 0x9e3779b9u + (hash << 6) + (hash >> 2);
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                hash *= 0x846ca68bu;
                hash ^= hash >> 16;
                return (hash & 0x00ffffffu) / 16777215f;
            }
        }

        private static System.Collections.Generic.Dictionary<Color32, byte> BuildPaletteLookup(VoxelData voxelData)
        {
            var lookup = new System.Collections.Generic.Dictionary<Color32, byte>();
            for (byte i = 0; i < voxelData.palette.Length; i++)
            {
                Color32 color32 = voxelData.palette[i];
                if (!lookup.ContainsKey(color32))
                    lookup.Add(color32, i);
            }

            return lookup;
        }

        private static int To1D(int x, int y, int z, int3 length)
        {
            return x + length.x * (y + length.y * z);
        }

        private void EnsureUniqueVoxelData(DynamicVoxelObj voxelObj)
        {
            if (voxelObj == null || voxelObj.voxelData == null)
                return;

            if (voxelObj.voxelData == lastVoxelData && voxelDataCloned)
                return;

            lastVoxelData = voxelObj.voxelData;
            voxelDataCloned = false;

            VoxelData voxelData = voxelObj.voxelData;
            VoxelObjBase[] voxelObjects = Object.FindObjectsOfType<VoxelObjBase>();
            for (int i = 0; i < voxelObjects.Length; i++)
            {
                VoxelObjBase otherObj = voxelObjects[i];
                if (otherObj == null || otherObj == voxelObj)
                    continue;

                if (otherObj.voxelData == voxelData)
                {
                    CachedVoxelData cachedCopy = voxelData.ToCachedVoxelData().GetCopy();
                    voxelObj.voxelData = new VoxelData(cachedCopy);
                    lastVoxelData = voxelObj.voxelData;
                    voxelDataCloned = true;
                    return;
                }
            }
        }

        private static byte GetOrAddPaletteIndex(
            System.Collections.Generic.Dictionary<Color32, byte> paletteLookup,
            System.Collections.Generic.List<Color> paletteColors,
            Color targetColor,
            ref bool paletteChanged)
        {
            Color32 color32 = targetColor;
            if (paletteLookup.TryGetValue(color32, out byte index))
                return index;

            // byte.MaxValue == 255, но индексы 0..254 (255 значений) — безопаснее не упираться в 255.
            // Оставим как у тебя: если переполнено — ищем ближайший.
            if (paletteColors.Count >= byte.MaxValue)
            {
                return FindClosestPaletteIndex(paletteColors, targetColor);
            }

            byte newIndex = (byte)paletteColors.Count;
            paletteColors.Add(targetColor);
            paletteLookup[color32] = newIndex;
            paletteChanged = true;
            return newIndex;
        }

        private static byte FindClosestPaletteIndex(System.Collections.Generic.List<Color> paletteColors, Color targetColor)
        {
            byte closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (byte i = 0; i < paletteColors.Count; i++)
            {
                Color color = paletteColors[i];
                float distance = (color.r - targetColor.r) * (color.r - targetColor.r)
                                 + (color.g - targetColor.g) * (color.g - targetColor.g)
                                 + (color.b - targetColor.b) * (color.b - targetColor.b);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

    }
}
