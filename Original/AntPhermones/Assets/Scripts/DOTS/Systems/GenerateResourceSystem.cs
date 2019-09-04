using DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace DOTS.Systems 
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GenerateResourceSystem : JobComponentSystem
    {
        private EndInitializationEntityCommandBufferSystem endInitBuffer;
        private Random random;

        protected override void OnCreate()
        {
            this.endInitBuffer = this.World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            this.random = new Random((uint)UnityEngine.Random.Range(0, int.MaxValue));
        }

        private struct GenerateResourceJob : IJobForEachWithEntity<ResourceSeed, MapSize>
        {
            public EntityCommandBuffer Buffer;
            public Random Random;

            public void Execute(Entity entity, int index, [ReadOnly] ref ResourceSeed seed, [ReadOnly] ref MapSize mapSize)
            {
                float resourceAngle = this.Random.NextFloat() * 2f * PI;
                var offsetX = cos(resourceAngle) * mapSize.Value * .475f;
                var offsetY = sin(resourceAngle) * mapSize.Value * .475f;
                var resourcePosition = new float2(1f, 1f) * mapSize.Value * .5f + new float2(offsetX, offsetY);
                var colonyTranslation = new Translation2D();
                colonyTranslation.Value = resourcePosition;
                var colonyScale = new NonUniformScale();
                colonyScale.Value = new float3(4f, 4f, .1f) / mapSize.Value;

                var colony = this.Buffer.Instantiate(seed.ResourcePrefab);
                this.Buffer.SetComponent(colony, colonyTranslation);
                this.Buffer.SetComponent(colony, colonyScale);

                this.Buffer.RemoveComponent<ResourceSeed>(entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var generateColonyJob = new GenerateResourceJob
                                    {
                                        Buffer = this.endInitBuffer.CreateCommandBuffer(),
                                        Random = new Random(this.random.NextUInt())
                                    };

            var jobHandle = generateColonyJob.ScheduleSingle(this, inputDeps);
            this.endInitBuffer.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
}