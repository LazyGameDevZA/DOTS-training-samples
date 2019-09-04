using Unity.Entities;
using UnityEngine;

namespace DOTS.Components
{
    public struct AntColors : IComponentData
    {
        public Color Search;
        public Color Carry;
    }
}
