using System;
using System.Runtime.CompilerServices;
using DOTS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;

namespace DOTS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class ProcessAntsSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem bufferSystem;
        private EntityQuery simulationSettingsQuery;
        private EntityQuery colonyQuery;
        private EntityQuery resourceQuery;
        private NativeMultiHashMap<int2, (float2 position, float radius)> hashedObstacles;
        private Random random;
        
        protected override void OnCreate()
        {
            this.bufferSystem = this.World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>();
            this.simulationSettingsQuery = this.EntityManager.CreateEntityQuery(
                typeof(MapSize),
                typeof(ObstacleBucketResolution),
                typeof(AntColors));
            
            this.colonyQuery = this.EntityManager.CreateEntityQuery(typeof(Colony), typeof(Translation2D));
            
            this.resourceQuery = this.EntityManager.CreateEntityQuery(typeof(Resource), typeof(Translation2D));
            
            this.hashedObstacles = new NativeMultiHashMap<int2, (float2 position, float radius)>(1024, Allocator.Persistent);
            this.random = new Random((uint)UnityEngine.Random.Range(0, int.MaxValue));
        }

        protected override void OnDestroy()
        {
            this.hashedObstacles.Dispose();
        }

        [BurstCompile]
        private struct HashObstaclesJob : IJobForEachWithEntity<Obstacle, Translation2D>
        {
            public MapSize MapSize;
            public ObstacleBucketResolution BucketResolution;
            [WriteOnly]
            public NativeMultiHashMap<int2, (float2 position, float radius)>.ParallelWriter Buckets;
            public EntityCommandBuffer.Concurrent Buffer;

            public void Execute(Entity entity, int index, [ReadOnly] ref Obstacle obstacle, [ReadOnly] ref Translation2D translation)
            {
                var hash = Hash(translation.Value, this.MapSize.Value, this.BucketResolution.Value);

                this.Buckets.Add(hash, (position: translation.Value, obstacle.radius));
                this.Buffer.RemoveComponent<Obstacle>(index, entity);
                this.Buffer.RemoveComponent<Translation2D>(index, entity);
            }
        }
        
        [BurstCompile]
        private struct CalculateWallSteeringJob : IJobForEach<Translation2D, Rotation2D, WallSteering>
        {
            private const float searchDistance = 1.5f;
            
            [ReadOnly]
            public NativeMultiHashMap<int2, (float2 position, float radius)> Buckets;

            public MapSize MapSize;
            public ObstacleBucketResolution BucketResolution;

            public void Execute([ReadOnly]ref Translation2D translation, [ReadOnly] ref Rotation2D rotation, [WriteOnly] ref WallSteering wallSteering)
            {
                int output = 0;

                for(int i = -1; i <= 1; i+=2)
                {
                    float angle = rotation.Value + i * PI * 0.25f;
                    float testX = translation.Value.x + cos(angle) * searchDistance;
                    float testY = translation.Value.y + sin(angle) * searchDistance;

                    var hash = Hash(testX, testY, this.MapSize.Value, this.BucketResolution.Value);
                    
                    if(this.Buckets.ContainsKey(hash))
                    {
                        output -= i;
                    }
                }

                wallSteering.Value = output;
            }
        }
        
        [BurstCompile]
        private struct SteerAntsJob : IJobForEach<AntStats, Rotation2D, WallSteering>
        {
            public Random Random;
            
            public void Execute([ReadOnly] ref AntStats stats, ref Rotation2D rotation, [ReadOnly] ref WallSteering wallSteering)
            {
                rotation.Value += this.Random.NextFloat(-stats.Steering, stats.Steering);
                
                // TODO Implement phero steering

                rotation.Value += wallSteering.Value * stats.WallSteerStrength;
            }
        }

        [BurstCompile]
        private struct AccelerateAntsJob : IJobForEach<AntStats, Velocity, WallSteering>
        {
            public float DeltaTime;
            
            public void Execute([ReadOnly] ref AntStats stats, ref Velocity velocity, [ReadOnly] ref WallSteering wallSteering)
            {
                var targetSpeed = velocity.Value;
                // TODO Implement phero steering
                var pheroSteering = 0f;
                targetSpeed *= 1f - (pheroSteering + abs(wallSteering.Value)) / 3f;

                velocity.Value = velocity.Value + (targetSpeed - velocity.Value) * stats.Acceleration * this.DeltaTime;
            }
        }
        
        [BurstCompile]
        private struct SelectTargetJob : IJobForEach<HoldingResources, AntStats, MeshColor, TargetPosition>
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Translation2D> ColonyPositions;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Translation2D> ResourcePositions;

            public AntColors AntColors;
            public Random Random;
            
            public void Execute(
                [ReadOnly] ref HoldingResources holdingResources,
                [ReadOnly] ref AntStats stats,
                ref MeshColor color,
                ref TargetPosition targetPosition)
            {
                if(holdingResources.Value)
                {
                    targetPosition.Value = this.ColonyPositions[0].Value;
                    
                    color.Value += this.AntColors.Carry * stats.Brightness - color.Value;
                }
                else
                {
                    var index = this.Random.NextInt(this.ResourcePositions.Length);
                    targetPosition.Value = this.ResourcePositions[index].Value;

                    color.Value += this.AntColors.Search * stats.Brightness - color.Value;
                }
            }
        }
        
        [BurstCompile]
        private struct GoalSteeringJob : IJobForEach<AntStats, Translation2D, Rotation2D, TargetPosition, HoldingResources>
        {
            [ReadOnly]
            public NativeMultiHashMap<int2, (float2 position, float radius)> Buckets;

            public MapSize MapSize;
            public ObstacleBucketResolution BucketResolution;
            
            public void Execute(
                [ReadOnly] ref AntStats stats,
                [ReadOnly] ref Translation2D translation,
                ref Rotation2D rotation,
                [ReadOnly] ref TargetPosition targetPosition,
                ref HoldingResources holdingResources)
            {
                if(!LineCast(translation.Value, targetPosition.Value))
                {
                    var distance = targetPosition.Value - translation.Value;
                    float targetAngle = atan2(distance.y, distance.x);

                    var rotationDiff = targetAngle - rotation.Value;
                    if(rotationDiff > PI)
                    {
                        rotation.Value += PI * 2f;
                    }
                    else if(rotationDiff < -PI)
                    {
                        rotation.Value -= PI * 2f;
                    }
                    else if(abs(rotationDiff) < PI * 0.5f)
                    {
                        rotation.Value += rotationDiff * stats.GoalSteerStrength;
                    }
                }

                var diff = translation.Value - targetPosition.Value;
                var sqrmagnitude = dot(diff, diff);
                if(sqrmagnitude < 4f * 4f)
                {
                    holdingResources.Value = !holdingResources.Value;
                    rotation.Value += PI;
                }
            }

            private bool LineCast(float2 from, float2 to)
            {
                var d = to - from;
                float dist = sqrt(dot(d, d));

                int stepCount = (int)ceil(dist);

                for(int i = 0; i < stepCount; i++)
                {
                    float t = (float)i / stepCount;
                    var valToHash = from + (d * t);
                    var hash = Hash(valToHash, this.MapSize.Value, this.BucketResolution.Value);

                    if(this.Buckets.ContainsKey(hash))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [BurstCompile]
        private struct MoveAntsJob : IJobForEach<Translation2D, Rotation2D, Velocity>
        {
            public MapSize MapSize;
            
            public void Execute(ref Translation2D translation, [ReadOnly] ref Rotation2D rotation, [ReadOnly] ref Velocity velocity)
            {
                var vx = cos(rotation.Value) * velocity.Value;
                var vy = sin(rotation.Value) * velocity.Value;

                var newPos = translation.Value + new float2(vx, vy);
                newPos = min(newPos, this.MapSize.Max);
                newPos = max(newPos, this.MapSize.Min);

                translation.Value = newPos;
            }
        }
        
        [BurstCompile]
        private struct CopyTranslationJob : IJobForEach<Translation2D, Translation>
        {
            public MapSize MapSize;


            public void Execute([ReadOnly] ref Translation2D translation2D, ref Translation translation)
            {
                translation.Value = new float3(translation2D.Value / this.MapSize.Value, 0f);
            }
        }
        
        [BurstCompile]
        private struct CopyRotationJob : IJobForEach<Rotation2D, Rotation>
        {
            public void Execute([ReadOnly] ref Rotation2D rotation2D, ref Rotation rotation)
            {
                rotation.Value = quaternion.EulerZXY(0f, 0f, rotation2D.Value);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entity = this.simulationSettingsQuery.GetSingletonEntity();
            var mapSize = this.EntityManager.GetComponentData<MapSize>(entity);
            var bucketResolution = this.EntityManager.GetComponentData<ObstacleBucketResolution>(entity);
            var antColors = this.EntityManager.GetComponentData<AntColors>(entity);

            var colonyPositions = this.colonyQuery.ToComponentDataArray<Translation2D>(Allocator.TempJob, out var colonyGatherHandle);
            var resourcePositions = this.resourceQuery.ToComponentDataArray<Translation2D>(Allocator.TempJob, out var resourceGatherHandle);
            
            var hashObstaclesJob = new HashObstaclesJob();
            hashObstaclesJob.MapSize = mapSize;
            hashObstaclesJob.BucketResolution = bucketResolution;
            hashObstaclesJob.Buckets = this.hashedObstacles.AsParallelWriter();
            hashObstaclesJob.Buffer = this.bufferSystem.CreateCommandBuffer().ToConcurrent();
            inputDeps = hashObstaclesJob.Schedule(this, inputDeps);
            this.bufferSystem.AddJobHandleForProducer(inputDeps);
            
            var calculateWallSteeringJob = new CalculateWallSteeringJob();
            calculateWallSteeringJob.Buckets = this.hashedObstacles;
            calculateWallSteeringJob.MapSize = mapSize;
            calculateWallSteeringJob.BucketResolution = bucketResolution;
            inputDeps = calculateWallSteeringJob.Schedule(this, inputDeps);
            
            var steerAntsJob = new SteerAntsJob();
            steerAntsJob.Random = new Random(this.random.NextUInt());
            inputDeps = steerAntsJob.Schedule(this, inputDeps);
            
            var accelerateAntsJob = new AccelerateAntsJob();
            accelerateAntsJob.DeltaTime = Time.deltaTime;
            inputDeps = accelerateAntsJob.Schedule(this, inputDeps);
            
            var selectTargetJob = new SelectTargetJob();
            selectTargetJob.ColonyPositions = colonyPositions;
            selectTargetJob.ResourcePositions = resourcePositions;
            selectTargetJob.AntColors = antColors;
            selectTargetJob.Random = new Random(this.random.NextUInt());
            inputDeps = JobHandle.CombineDependencies(colonyGatherHandle, resourceGatherHandle, inputDeps);
            inputDeps = selectTargetJob.Schedule(this, inputDeps);
            
            var goalSteeringJob = new GoalSteeringJob();
            goalSteeringJob.Buckets = this.hashedObstacles;
            goalSteeringJob.MapSize = mapSize;
            goalSteeringJob.BucketResolution = bucketResolution;
            inputDeps = goalSteeringJob.Schedule(this, inputDeps);
            
            var moveAntsJob = new MoveAntsJob();
            moveAntsJob.MapSize = mapSize;
            inputDeps = moveAntsJob.Schedule(this, inputDeps);
            
            var copyTranslationJob = new CopyTranslationJob();
            copyTranslationJob.MapSize = mapSize;
            var copyTranslationHandle = copyTranslationJob.Schedule(this, inputDeps);
            
            var copyRotationJob = new CopyRotationJob();
            var copyRotationHandle = copyRotationJob.Schedule(this, inputDeps);

            return JobHandle.CombineDependencies(copyTranslationHandle, copyRotationHandle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int2 Hash(float x, float y, int mapSize, int bucketResolution)
        {
            return Hash(new float2(x, y), mapSize, bucketResolution);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int2 Hash(float2 value, int mapSize, int bucketResolution)
        {
            return (int2)(value / mapSize * bucketResolution);
        }
    }
}
