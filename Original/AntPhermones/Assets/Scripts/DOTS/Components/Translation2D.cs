using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Components
{
    public struct Translation2D : IComponentData
    {
        public float2 Value;
    }
}
