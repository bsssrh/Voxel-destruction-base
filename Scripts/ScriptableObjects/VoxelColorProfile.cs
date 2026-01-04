using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Settings
{
    [CreateAssetMenu(fileName = "Voxel Color Profile", menuName = "VoxelDestruction/VoxelColorProfile")]
    public class VoxelColorProfile : ScriptableObject
    {
        public const string DefaultTag = "Default";

        public List<ImpactEntry> impactEntries = new List<ImpactEntry>();

        public bool TryGetTagEntry(ImpactType impactType, string meshTag, out TagEntry tagEntry)
        {
            tagEntry = null;

            ImpactEntry impactEntry = impactEntries.Find(entry => entry.impactType == impactType);
            if (impactEntry == null)
                return false;

            tagEntry = impactEntry.GetTagEntry(meshTag);
            return tagEntry != null;
        }
    }

    [Serializable]
    public class ImpactEntry
    {
        public ImpactType impactType;
        public List<TagEntry> tagEntries = new List<TagEntry>();

        public TagEntry GetTagEntry(string meshTag)
        {
            if (string.IsNullOrWhiteSpace(meshTag))
                meshTag = "Untagged";

            TagEntry entry = tagEntries.Find(tagEntry => string.Equals(tagEntry.tag, meshTag, StringComparison.Ordinal));
            if (entry != null)
                return entry;

            return tagEntries.Find(tagEntry => string.Equals(tagEntry.tag, VoxelColorProfile.DefaultTag, StringComparison.Ordinal));
        }
    }

    [Serializable]
    public class TagEntry
    {
        public string tag = VoxelColorProfile.DefaultTag;

        [Tooltip("How the impact color should be applied.")]
        public VoxelColorBlendMode blendMode = VoxelColorBlendMode.Override;

        [Tooltip("Color applied to neighboring voxels around the impact point.")]
        public Color targetColor = Color.white;
    }

    public enum VoxelColorBlendMode
    {
        Override,
        BlendToOriginal
    }
}
