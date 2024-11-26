using Unity.Mathematics;

namespace MoveIt.QAccessor.QEntity
{
    internal partial struct QEntity
    {
        private bool Net_TryGetElevation(out float elevation)
        {
            elevation = 0f;
            if (!_Lookup.gnElevation.HasComponent(m_Entity)) return false;

            float2 ele2 = _Lookup.gnElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
            elevation = (ele2.x + ele2.y) / 2;
            return true;
        }

        private bool Net_TrySetElevation(float newElevation)
        {
            if (!_Lookup.gnElevation.HasComponent(m_Entity)) return false;

            float2 ele2 = _Lookup.gnElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
            float delta = newElevation - ((ele2.x + ele2.y) / 2);
            ele2.x += delta;
            ele2.y += delta;

            _Lookup.gnElevation.GetRefRW(m_Entity).ValueRW.m_Elevation = ele2;
            return true;
        }
    }
}
