using DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace DOTS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GenerateAntsSystem : JobComponentSystem
    {
        private EndInitializationEntityCommandBufferSystem endInitBuffer;
        private Random random;

        protected override void OnCreate()
        {
            this.endInitBuffer = this.World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            this.random = new Random((uint)UnityEngine.Random.Range(0, int.MaxValue));
        }

        private struct GenerateAntsJob : IJobForEachWithEntity<AntSeed, MapSize>
        {
            public EntityCommandBuffer Buffer;
            public Random Random;
            
            public void Execute(Entity entity, int index, [ReadOnly] ref AntSeed seed, [ReadOnly] ref MapSize mapSize)
            {
                for(int i = 0; i < seed.Count; i++)
                {
                    var minPos = new float2(-5f, -5f) + mapSize.Value * 0.5f;
                    var maxPos = new float2(5f, 5f) + mapSize.Value * 0.5f;
                    var holdingResources = new HoldingResources();
                    holdingResources.Value = false;

                    var translation = new Translation2D();
                    translation.Value = this.Random.NextFloat2(minPos, maxPos);
                    
                    var rotation = new Rotation2D();
                    rotation.Value = this.Random.NextFloat() * PI * 2f;
                    
                    var speed = new Velocity();
                    speed.Value = seed.InitialSpeed;
                    
                    var stats = new AntStats();
                    stats.Steering = seed.RandomSteering;
                    stats.Acceleration = seed.Acceleration;
                    stats.WallSteerStrength = seed.WallSteerStrength;
                    stats.GoalSteerStrength = seed.GoalSteerStrength;
                    stats.Brightness = this.Random.NextFloat(.75f, 1.25f);
                    
                    var ant = this.Buffer.Instantiate(seed.AntPrefab);
                    this.Buffer.SetComponent(ant, holdingResources);
                    this.Buffer.SetComponent(ant, translation);
                    this.Buffer.SetComponent(ant, rotation);
                    this.Buffer.SetComponent(ant, speed);
                    this.Buffer.SetComponent(ant, stats);
                }

                this.Buffer.RemoveComponent<AntSeed>(entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var generateAntsJob = new GenerateAntsJob
                                  {
                                      Buffer = this.endInitBuffer.CreateCommandBuffer(),
                                      Random = new Random(this.random.NextUInt())
                                  };

            var jobHandle = generateAntsJob.ScheduleSingle(this, inputDeps);
            this.endInitBuffer.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
}
