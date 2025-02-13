using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Burst;

namespace Asteroids.Client
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct AsteroidRenderSystem : ISystem
    {
        private float m_Pulse;
        private float m_PulseDelta;
        private const float m_PulseMax = 1.2f;
        private const float m_PulseMin = 0.8f;

        ComponentLookup<PredictedGhostComponent> m_PredictedGhostComponentFromEntity;
        ComponentLookup<URPMaterialPropertyBaseColor> m_URPMaterialPropertyBaseColorFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_Pulse = 1;
            m_PulseDelta = 1;
            m_PredictedGhostComponentFromEntity = state.GetComponentLookup<PredictedGhostComponent>(true);
            m_URPMaterialPropertyBaseColorFromEntity = state.GetComponentLookup<URPMaterialPropertyBaseColor>();
            state.RequireForUpdate<AsteroidTagComponentData>();
        }
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Should ideally not be a hard-coded value
            float astrScale = 30;

            m_Pulse += m_PulseDelta * SystemAPI.Time.DeltaTime;
            if (m_Pulse > m_PulseMax)
            {
                m_Pulse = m_PulseMax;
                m_PulseDelta = -m_PulseDelta;
            }
            else if (m_Pulse < m_PulseMin)
            {
                m_Pulse = m_PulseMin;
                m_PulseDelta = -m_PulseDelta;
            }
            var pulse = m_Pulse;

            m_PredictedGhostComponentFromEntity.Update(ref state);
            m_URPMaterialPropertyBaseColorFromEntity.Update(ref state);
            var scaleJob = new ScaleAsteroids
            {
                predictedFromEntity = m_PredictedGhostComponentFromEntity,
                colorFromEntity = m_URPMaterialPropertyBaseColorFromEntity,
                astrScale = astrScale,
                pulse = pulse
            };
            state.Dependency = scaleJob.ScheduleParallel(state.Dependency);
        }
        [WithAll(typeof(AsteroidTagComponentData))]
        [BurstCompile]
        partial struct ScaleAsteroids : IJobEntity
        {
            [ReadOnly] public ComponentLookup<PredictedGhostComponent> predictedFromEntity;
            [NativeDisableParallelForRestriction] public ComponentLookup<URPMaterialPropertyBaseColor> colorFromEntity;
            public float astrScale;
            public float pulse;
#if !ENABLE_TRANSFORM_V1
            public void Execute(Entity ent, ref LocalTransform localTransform)
            {
                if (colorFromEntity.HasComponent(ent))
                    colorFromEntity[ent] = new URPMaterialPropertyBaseColor{Value = predictedFromEntity.HasComponent(ent) ? new float4(0,1,0,1) : new float4(1,1,1,1)};
                localTransform.Scale = astrScale * pulse;
            }
#else
            public void Execute(Entity ent, ref NonUniformScale scale)
            {
                if (colorFromEntity.HasComponent(ent))
                    colorFromEntity[ent] = new URPMaterialPropertyBaseColor{Value = predictedFromEntity.HasComponent(ent) ? new float4(0,1,0,1) : new float4(1,1,1,1)};
                scale.Value = new float3(astrScale * pulse);
            }
#endif

        }
    }
}
