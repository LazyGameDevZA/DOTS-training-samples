using DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GenerateColonySystem : JobComponentSystem
    {
        private EndInitializationEntityCommandBufferSystem endInitBuffer;

        protected override void OnCreate()
        {
            this.endInitBuffer = this.World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
        }

        private struct GenerateColonyJob : IJobForEachWithEntity<ColonySeed, MapSize>
        {
            public EntityCommandBuffer Buffer;

            public void Execute(Entity entity, int index, [ReadOnly] ref ColonySeed colonySeed, [ReadOnly] ref MapSize mapSize)
            {
                var colonyPosition = new float2(1f, 1f) * mapSize.Value * .5f;
                var colonyTranslation = new Translation2D();
                colonyTranslation.Value = colonyPosition;
                var colonyScale = new NonUniformScale();
                colonyScale.Value = new float3(4f, 4f, .1f) / mapSize.Value;

                var colony = this.Buffer.Instantiate(colonySeed.ColonyPrefab);
                this.Buffer.SetComponent(colony, colonyTranslation);
                this.Buffer.SetComponent(colony, colonyScale);

                this.Buffer.RemoveComponent<ColonySeed>(entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var generateColonyJob = new GenerateColonyJob
                                    {
                                        Buffer = this.endInitBuffer.CreateCommandBuffer()
                                    };

            var jobHandle = generateColonyJob.ScheduleSingle(this, inputDeps);
            this.endInitBuffer.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
}
