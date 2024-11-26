using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.QAccessor.QEntity
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


        private Bezier4x3 Curve => _Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;

        private void TryAddUpdate(Entity e)
        {
            if (!_Manager.HasComponent<Game.Common.Updated>(e))
            {
                _Manager.AddComponent<Game.Common.Updated>(e);
            }
            if (!_Manager.HasComponent<Game.Common.BatchesUpdated>(e))
            {
                _Manager.AddComponent<Game.Common.BatchesUpdated>(e);
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

        public bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            if (!_Manager.HasComponent<T>(m_Entity))
            {
                component = default;
                return false;
            }

            component = _Manager.GetComponentData<T>(m_Entity);
            return true;
        }

        public bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            if (!_Manager.HasBuffer<T>(m_Entity))
            {
                buffer = default;
                return false;
            }

            buffer = _Manager.GetBuffer<T>(m_Entity, isReadOnly);
            return true;
        }

        #endregion
    }
}
