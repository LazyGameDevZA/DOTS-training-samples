using DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace DOTS.Systems
{
    public struct InstancesPerBatch : IComponentData
    {
        public int Value;
    }
    
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GenerateObstaclesSystem : JobComponentSystem
    {
        private EndInitializationEntityCommandBufferSystem endInitBuffer;
        private Random random;
        
        protected override void OnCreate()
        {
            this.endInitBuffer = this.World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            this.random = new Random((uint)UnityEngine.Random.Range(0, int.MaxValue));
        }

        private struct GenerateObstaclesJob : IJobForEachWithEntity<ObstacleSeed, MapSize>
        {
            public EntityCommandBuffer Buffer;
            public Random Random;
            
            public void Execute(Entity entity, int index, [ReadOnly] ref ObstacleSeed obstacleSeed, [ReadOnly] ref MapSize mapSize)
            {
                for(int i = 1; i <= obstacleSeed.RingCount; i++)
                {
                    float ringRadius = (i / (obstacleSeed.RingCount + 1f)) * (mapSize.Value * 0.5f);
                    float circumference = ringRadius * 2f * PI;
                    int maxCount = (int)ceil(circumference / (2f * obstacleSeed.Radius) * 2f);
                    int offset = this.Random.NextInt(0, maxCount);
                    int holeCount = this.Random.NextInt(1, 3);

                    for(int j = 0; j < maxCount; j++)
                    {
                        float t = (float)j / maxCount;
                        if((t * holeCount) % 1f < obstacleSeed.ObstaclesPerRing)
                        {
                            float angle = (j + offset) / (float)maxCount * (2f * PI);
                            
                            var x = mapSize.Value * .5f + cos(angle) * ringRadius;
                            var y = mapSize.Value * .5f + sin(angle) * ringRadius;
                            
                            Obstacle obstacle = new Obstacle();
                            obstacle.radius = obstacleSeed.Radius;
                            var translation = new Translation2D();
                            translation.Value = new float2(x, y);
                            var nonUniformScale = new NonUniformScale();
                            nonUniformScale.Value = new float3(obstacleSeed.Radius * 2f, obstacleSeed.Radius * 2f, 1f) / mapSize.Value;

                            var obstacleEntity = this.Buffer.Instantiate(obstacleSeed.ObstaclePrefab);
                            this.Buffer.SetComponent(obstacleEntity, obstacle);
                            this.Buffer.SetComponent(obstacleEntity, translation);
                            this.Buffer.SetComponent(obstacleEntity, nonUniformScale);
                        }
                    }
                }
                
                this.Buffer.RemoveComponent<ObstacleSeed>(entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var generateObstaclesJob = new GenerateObstaclesJob
                                       {
                                           Buffer = this.endInitBuffer.CreateCommandBuffer(),
                                           Random = new Random(this.random.NextUInt())
                                       };

            var jobHandle = generateObstaclesJob.ScheduleSingle(this, inputDeps);

            this.endInitBuffer.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
}
