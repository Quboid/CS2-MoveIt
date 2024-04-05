using Colossal.Mathematics;
using MoveIt.Moveables;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    public struct QSegmentChild : IQEntity, IDisposable
    {
        internal static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Entity m_Entity { get; }
        public bool m_IsTopLevel { get; }

        internal QLookup m_Lookup;
        internal QTypes.Identity m_Identity;

        private readonly Bezier4x3 Curve => m_Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;

        internal QSegmentChild(Entity e, SystemBase system, QTypes.Identity identity)
        {
            m_Lookup = QLookup.Get(system);

            m_Entity = e;
            m_IsTopLevel = false;
            m_Identity = identity;
        }

        public readonly float3 Position
        {
            get
            {
                Bezier4x3 curve = Curve;
                return (curve.b + curve.c) / 2;
            }
        }

        public readonly float Angle
        {
            get
            {
                Bezier4x3 bezier = m_Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;
                float3 mag = bezier.d - bezier.a;

                return math.atan2(mag.z, mag.x) * Mathf.Rad2Deg;
            }
        }

        public readonly quaternion Rotation => Quaternion.Euler(0f, Angle, 0f);


        public readonly bool MoveBy(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            //m_Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = Geometry.Full.MoveBezier4x3(m_Lookup.gnCurve.GetRefRW(m_Entity).ValueRO.m_Bezier, delta);

            Bezier4x3 curve = m_Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;
            curve.a += delta;
            curve.b += delta;
            curve.c += delta;
            curve.d += delta;
            m_Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = curve;

            EntityManager.AddComponent<Game.Common.Updated>(m_Entity);

            return true;
        }

        public readonly bool Move(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            return MoveBy(data, newPosition, delta);
        }

        public readonly bool RotateBy(StateDataWrapper data, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return RotateTo(data, 0f, ref matrix, origin);
        }

        public readonly bool RotateTo(StateDataWrapper data, float angle, ref Matrix4x4 matrix, float3 origin)
        {
            Bezier4x3 curve = m_Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;
            //curve = Geometry.Full.RotateBezier4x3(bezier, ref matrix, origin);

            curve.a = (float3)matrix.MultiplyPoint(curve.a - origin);
            curve.b = (float3)matrix.MultiplyPoint(curve.b - origin);
            curve.c = (float3)matrix.MultiplyPoint(curve.c - origin);
            curve.d = (float3)matrix.MultiplyPoint(curve.d - origin);

            m_Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = curve;
            return true;
        }



        public readonly T GetComponent<T>() where T : unmanaged, IComponentData
        {
            return EntityManager.GetComponentData<T>(m_Entity);
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            if (!EntityManager.HasComponent<T>(m_Entity))
            {
                component = default;
                return false;
            }

            component = EntityManager.GetComponentData<T>(m_Entity);
            return true;
        }

        public readonly DynamicBuffer<T> GetBuffer<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return EntityManager.GetBuffer<T>(m_Entity, isReadOnly);
        }

        public readonly bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            if (!EntityManager.HasBuffer<T>(m_Entity))
            {
                buffer = default;
                return false;
            }

            buffer = EntityManager.GetBuffer<T>(m_Entity, isReadOnly);
            return true;
        }

        public readonly void Dispose()
        { }
    }
}
