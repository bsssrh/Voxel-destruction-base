using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Settings;

namespace VoxelDestructionPro.Jobs.Isolator
{
    /// <summary>
    /// Connected component labeling job, simple one thread implementation
    /// Does not use union search or lists
    ///
    /// Labels:
    /// 0 - Inactive voxels, no label
    /// 1 - Base, is linked to the origin
    /// >1 - Unique part label
    ///
    /// This is used to find isolated parts inside voxel objects
    /// </summary>
    [BurstCompile]
    public struct CCL_Job : IJob
    {
        public int3 length;
        [ReadOnly]
        public NativeArray<Voxel> inputVoxels;
        
        public NativeArray<ushort> labeling;
        
        public IsoSettings.IsolationOrigin originType;
        
        public void Execute()
        {
            //Start labeling at 2, 1 is reserved for base voxels
            ushort currentIndex = (ushort)(originType == IsoSettings.IsolationOrigin.None ? 1 : 2);
            
            //Labeling
            for (int i = 0; i < inputVoxels.Length; i++)
            {
                if (inputVoxels[i].active == 0)
                {
                    labeling[i] = 0;
                    continue;
                }

                ushort upper = GetNeighborYMinus1(i);
                ushort left = GetNeighborXMinus1(i);
                ushort forward = GetNeighborZMinus1(i);
                
                //New label for this
                if (upper == 0 && left == 0 && forward == 0)
                {
                    labeling[i] = currentIndex;
                    currentIndex++;
                    continue;
                }
                
                ushort minLabel = ushort.MaxValue;

                if (upper != 0) minLabel = upper;
                if (left != 0) minLabel = ushort_min(minLabel, left);
                if (forward != 0) minLabel = ushort_min(minLabel, forward);

                labeling[i] = minLabel;

                if (upper != 0 && upper != minLabel) 
                    OverrideLabel(upper, minLabel, i);
                if (left != 0 && left != minLabel) 
                    OverrideLabel(left, minLabel, i);
                if (forward != 0 && forward != minLabel) 
                    OverrideLabel(forward, minLabel, i);
            }
            
            if (originType == IsoSettings.IsolationOrigin.None)
                return;
            
            //Search for base voxels and assign 1 to them
            byte primary = 0;
            byte secondary = 1; 
            int3 axis = new int3();
            
            if (originType == IsoSettings.IsolationOrigin.XNeg || originType == IsoSettings.IsolationOrigin.XPos)
            {
                primary = 1;
                secondary = 2;

                if (originType == IsoSettings.IsolationOrigin.XPos)
                    axis[0] = length.x - 1;
            }
            else if (originType == IsoSettings.IsolationOrigin.YNeg || originType == IsoSettings.IsolationOrigin.YPos)
            {
                primary = 0;
                secondary = 2;
                
                if (originType == IsoSettings.IsolationOrigin.YPos)
                    axis[1] = length.y - 1;
            }
            else if (originType == IsoSettings.IsolationOrigin.ZNeg || originType == IsoSettings.IsolationOrigin.ZPos)
            {
                primary = 0;
                secondary = 1;
                
                if (originType == IsoSettings.IsolationOrigin.ZPos)
                    axis[2] = length.z - 1;
            }

            for (axis[primary] = 0; axis[primary] < length[primary]; axis[primary]++)
                for (axis[secondary] = 0; axis[secondary] < length[secondary]; axis[secondary]++)
                    AssureOrigin(axis);
        }

        private void AssureOrigin(int3 axis)
        {
            int index = To1D(axis);

            if (inputVoxels[index].active == 0 || labeling[index] == 1)
                return;

            //1 describes the base label that will not get seperated
            OverrideLabel(labeling[index], 1, labeling.Length);
        }
        
        private int To1D(int3 index)
        {
            return index.x + length.x * (index.y + length.y * index.z);
        }
        
        /* 
        These functions here allow me to get x-1, y-1 and z-1 indexes
        without having to convert to 3D index and back to 1D index.
        They also check if the 3D index would be out of bounds
         */
        private ushort GetNeighborXMinus1(int index)
        {
            int x = index % length.x;
            if (x <= 0)
                return 0;
            
            return labeling[index - 1];
        }

        private ushort GetNeighborYMinus1(int index)
        {
            int y = (index / length.x) % length.y;
            if (y <= 0)
                return 0;
            
            return labeling[index - length.x];
        }

        private ushort GetNeighborZMinus1(int index)
        {
            int z = index / (length.x * length.y);
            if (z <= 0)
                return 0;
            
            return labeling[index - (length.x * length.y)];
        }
        
        private void OverrideLabel(ushort oldLabel, ushort newLabel, int maxIndex)
        {
            for (int i = 0; i < maxIndex; i++)
                if (labeling[i] == oldLabel)
                    labeling[i] = newLabel;
        }

        private ushort ushort_min(ushort a, ushort b)
        {
            return (a < b) ? a : b;
        }
    }
}
