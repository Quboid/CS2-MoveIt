using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        internal enum ID
        {
            Generic,
            Prop,
            Seg,
            Lane,
            CP,
            Node,
            Surface,
        }


        private readonly Bezier4x3 Curve => m_Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;

        private readonly void TryAddUpdate(Entity e)
        {
            if (!m_Manager.HasComponent<Game.Common.Updated>(e))
            {
                m_Manager.AddComponent<Game.Common.Updated>(e);
            }
            if (!m_Manager.HasComponent<Game.Common.BatchesUpdated>(e))
            {
                m_Manager.AddComponent<Game.Common.BatchesUpdated>(e);
            }
        }


        private static Bounds3 MoveBounds3(Bounds3 input, float3 delta)
        {
            input.min += delta;
            input.max += delta;
            return input;
        }

        private static float3 BezierPosition(Bezier4x3 bezier)
        {
            float3 total = bezier.b + bezier.c;
            return total / 2;
        }


        #region Simple entity access

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            if (!m_Manager.HasComponent<T>(m_Entity))
            {
                component = default;
                return false;
            }

            component = m_Manager.GetComponentData<T>(m_Entity);
            return true;
        }

        public readonly bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            if (!m_Manager.HasBuffer<T>(m_Entity))
            {
                buffer = default;
                return false;
            }

            buffer = m_Manager.GetBuffer<T>(m_Entity, isReadOnly);
            return true;
        }

        #endregion
    }
}
