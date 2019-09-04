using System;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Components
{
    public struct RenderMesh : ISharedComponentData, IEquatable<RenderMesh>
    {
        public Mesh Mesh;
        public Material Material;

        public bool Equals(RenderMesh other)
        {
            return this.Mesh == other.Mesh &&
                   this.Material == other.Material;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            
            if(!ReferenceEquals(this.Mesh, null))
            {
                hash ^= this.Mesh.GetHashCode();
            }
            if(!ReferenceEquals(this.Material, null))
            {
                hash ^= this.Material.GetHashCode();
            }

            return hash;
        }
    }
}
