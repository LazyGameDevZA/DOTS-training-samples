using System;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Components
{
    public struct MatProps : ISharedComponentData, IEquatable<MatProps>
    {
        public MaterialPropertyBlock Value;
        
        public bool Equals(MatProps other)
        {
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            var hash = 0;

            if(!ReferenceEquals(this.Value, null))
            {
                hash ^= this.Value.GetHashCode();
            }

            return hash;
        }
    }
}
