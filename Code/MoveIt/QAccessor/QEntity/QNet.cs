using Unity.Mathematics;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        private readonly bool Net_TryGetElevation(out float elevation)
        {
            elevation = 0f;
            if (!m_Lookup.gnElevation.HasComponent(m_Entity)) return false;

            float2 ele2 = m_Lookup.gnElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
            elevation = (ele2.x + ele2.y) / 2;
            return true;
        }

        private readonly bool Net_TrySetElevation(float newElevation)
        {
            if (!m_Lookup.gnElevation.HasComponent(m_Entity)) return false;

            float2 ele2 = m_Lookup.gnElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
            float delta = newElevation - ((ele2.x + ele2.y) / 2);
            ele2.x += delta;
            ele2.y += delta;

            m_Lookup.gnElevation.GetRefRW(m_Entity).ValueRW.m_Elevation = ele2;
            return true;
        }
    }
}
