using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{

    public struct QSegment : IQEntity, IDisposable
    {
        internal static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Entity m_Entity { get; }
        public bool m_IsTopLevel { get; }

        internal QLookup m_Lookup;
        internal QTypes.Identity m_Identity;

        internal QSegment(Entity e, SystemBase system, QTypes.Identity identity, bool isTopLevel = true)
        {
            m_Lookup = QLookup.Get(system);

            m_Entity = e;
            m_IsTopLevel = isTopLevel;
            m_Identity = identity;

            if (!TryGetComponent<Game.Net.Node>(out _))
            {
                return;
            }
        }

        public readonly float3 Position
        {
            get
            {
                Bezier4x3 bezier = m_Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;
                return bezier.Position();
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
            EntityManager.AddComponent<Game.Common.Updated>(m_Entity);
            EntityManager.AddComponent<Game.Common.BatchesUpdated>(m_Entity);

            Game.Net.Edge edge = m_Lookup.gnEdge.GetRefRO(m_Entity).ValueRO;
            QNode node = new(edge.m_Start, MIT.m_Instance, QTypes.Identity.Node);
            node.SetUpdateTags();
            node = new(edge.m_End, MIT.m_Instance, QTypes.Identity.Node);
            node.SetUpdateTags();

            return true;
        }

        public readonly bool Move(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            MIT.Log.Info($"QSeg.Move called - new:{newPosition.DX()}, delta:{delta.DX()}, start:{Position.DX()}");
            return false;
        }

        public readonly bool RotateBy(StateDataWrapper data, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return false;
        }

        public readonly bool RotateTo(StateDataWrapper data, float angle, ref Matrix4x4 matrix, float3 origin)
        {
            return false;
        }


        public static float2 GetLinesIntersection(Line2 line1, Line2 line2)
        {
            float2 point = default;
            float a1 = line1.b.y - line1.a.y;
            float b1 = line1.a.x - line1.b.x;
            float c1 = a1 * line1.a.x + b1 * line1.a.y;

            float a2 = line2.b.y - line2.a.y;
            float b2 = line2.a.x - line2.b.x;
            float c2 = a2 * line2.a.x + b2 * line2.a.y;

            float delta = a1 * b2 - a2 * b1;
            if (delta > -10 && delta < 10)
            {
                point = new((line2.b - line1.b) / 2 + line1.b);
                return point;
            }
            point.x = (b2 * c1 - b1 * c2) / delta;
            point.y = (a1 * c2 - a2 * c1) / delta;
            return point;
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
