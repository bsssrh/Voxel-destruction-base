using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Jobs
{
    /// <summary>
    /// Base class for jobs, all work in the same pattern
    /// nativearrays and jobs get created in constructor
    ///
    /// Data gets disposed when no longer needed
    /// </summary>
    public class VoxelJob : IDisposable
    {
        protected bool dataDisposed;
    
        public void Dispose()
        {
            if (dataDisposed)
                return;

            dataDisposed = true;
        
            DisposeAll();
        }

        protected virtual void DisposeAll()
        {
        
        }
        
        ~VoxelJob()
        {
            Dispose();
        }
    }
}
