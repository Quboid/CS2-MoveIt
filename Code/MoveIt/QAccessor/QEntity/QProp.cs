namespace MoveIt.QAccessor.QEntity
{
    internal partial struct QEntity
    {
        private bool PropPlant_TryGetElevation(out float elevation)
        {
            elevation = 0f;
            // Return true if not found as Props will get Elevation component if needed
            if (!_Lookup.goElevation.HasComponent(m_Entity)) return true;

            elevation = _Lookup.goElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
            return true;
        }

        private bool PropPlant_TrySetElevation(float elevation)
        {
            return ManageStaticElevation(elevation);
        }
    }
}
