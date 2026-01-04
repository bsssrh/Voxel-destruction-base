using System;
using UnityEngine;

namespace VoxelDestructionPro.Settings
{
    [Obsolete("Use TagEntry instead.")]
    [Serializable]
    public class VoxelTagColorSettings : TagEntry
    {
        [Obsolete("Use tag instead.")]
        public string Tag
        {
            get => tag;
            set => tag = value;
        }
    }
}
