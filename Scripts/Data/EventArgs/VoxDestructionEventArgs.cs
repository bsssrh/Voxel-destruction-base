using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Data.Args
{
    public class VoxDestructionEventArgs : EventArgs
    {
        public bool BlockDestruction { get; set; } = false;
        public DestructionData DestructionDate { get; set; }
    }
}