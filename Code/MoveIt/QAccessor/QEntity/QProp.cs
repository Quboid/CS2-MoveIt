using Unity.Mathematics;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        private readonly bool PropPlant_TryGetElevation(out float elevation)
        {
            elevation = 0f;
            // Return true if not found as Props will get Elevation component if needed
            if (!m_Lookup.goElevation.HasComponent(m_Entity)) return true;

            elevation = m_Lookup.goElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
            return true;
        }

        private readonly bool PropPlant_TrySetElevation(float elevation)
        {
            return ManageStaticElevation(elevation);
        }
    }
}
