using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Runtime
{
    /// <summary>
    /// Lightweight spawn context to pass runtime palette/material from the source object
    /// to newly spawned fragment prefab. Uses a stack to support nested spawns.
    /// </summary>
    public static class VoxelFragmentSpawnContext
    {
        public struct Payload
        {
            public Color[] paletteCopy;
            public bool hasPalette;

            public VoxelMaterialType materialType;
            public bool hasMaterialType;

            public int sourceInstanceId;
            public bool hasSource;
        }

        private static readonly Stack<Payload> _stack = new Stack<Payload>(32);

        public static void PushFromSource(VoxelDestructionPro.VoxelObjects.DynamicVoxelObj source)
        {
            Payload p = default;

            if (source != null)
            {
                p.sourceInstanceId = source.GetInstanceID();
                p.hasSource = true;

                p.materialType = source.voxelMaterialType;
                p.hasMaterialType = true;

                if (source.voxelData != null && source.voxelData.palette.IsCreated && source.voxelData.palette.Length > 0)
                {
                    // IMPORTANT: copy runtime palette to managed array so it survives even if source changes later
                    p.paletteCopy = source.voxelData.palette.ToArray();
                    p.hasPalette = true;
                }
            }

            _stack.Push(p);
        }

        public static bool TryPeek(out Payload payload)
        {
            if (_stack.Count > 0)
            {
                payload = _stack.Peek();
                return true;
            }

            payload = default;
            return false;
        }

        public static void Pop()
        {
            if (_stack.Count > 0)
                _stack.Pop();
        }

        public static void Clear()
        {
            _stack.Clear();
        }
    }
}
