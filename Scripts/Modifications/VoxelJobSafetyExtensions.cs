using System.Reflection;
using Unity.Jobs;
using UnityEngine;

namespace VoxelDestructionPro.Utils
{
    public static class VoxelJobSafetyExtensions
    {
        /// <summary>
        /// Completes every JobHandle field found on the object (including base classes).
        /// This prevents palette/voxels NativeArray write while a mesher job still reads it.
        /// </summary>
        public static void CompleteAllJobsSafe(this Object obj)
        {
            if (obj == null) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            System.Type t = obj.GetType();

            while (t != null)
            {
                FieldInfo[] fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    if (f.FieldType == typeof(JobHandle))
                    {
                        JobHandle h = (JobHandle)f.GetValue(obj);
                        if (!h.IsCompleted)
                        {
                            h.Complete();
                            f.SetValue(obj, h);
                        }
                    }
                }

                t = t.BaseType;
            }
        }
    }
}
