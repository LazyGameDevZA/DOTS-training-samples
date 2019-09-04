using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Components
{
    public struct MapSize : IComponentData
    {
        public int Value;

        public float2 Min => new float2(0f);
        public float2 Max => new float2(this.Value);
    }
}
