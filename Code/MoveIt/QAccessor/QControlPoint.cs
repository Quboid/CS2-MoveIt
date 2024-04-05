using MoveIt.Components;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    public struct QControlPoint : IQEntity, IDisposable
    {
        internal static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Entity m_Entity { get; }
        public bool m_IsTopLevel { get; }
        internal QLookup m_Lookup;
        internal QTypes.Identity m_Identity;

        internal QControlPoint(Entity e, SystemBase system, QTypes.Identity identity, bool isTopLevel = true)
        {
            m_Lookup = QLookup.Get(system);

            m_Entity = e;
            m_IsTopLevel = isTopLevel;
            m_Identity = identity;

            if (!TryGetComponent<MIT_ControlPoint>(out _))
            {
                return;
            }
        }


        public readonly float3 Position
        {
            get
            {
                if (!m_Lookup.MIT_ControlPoint.HasComponent(m_Entity))
                {
                    throw new Exception($"Entity {m_Entity.D()} does not have MIT_ControlPoint component");
                }
                return m_Lookup.MIT_ControlPoint.GetRefRO(m_Entity).ValueRO.m_Position;
            }
        }

        public readonly float Angle
        {
            get => Rotation.Y();
        }

        public readonly quaternion Rotation
        {
            get => default;
        }


        public readonly bool MoveBy(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            return Move(data, newPosition, delta);
        }

        public readonly bool Move(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            if (!EntityManager.Exists(m_Entity)) return false;

            StringBuilder sb = new();
            sb.AppendFormat("CP.Move {0} ({1}, delta:{2}, old:{3}): ", m_Entity.D(), newPosition.DX(), delta.DX(), Position.DX());
            RefRW<MIT_ControlPoint> cpRef = m_Lookup.MIT_ControlPoint.GetRefRW(m_Entity);
            Entity owner = cpRef.ValueRO.m_Segment;
            RefRW<Game.Net.Curve> curveRef = m_Lookup.gnCurve.GetRefRW(owner);
            Game.Net.Edge edge = m_Lookup.gnEdge.GetRefRO(owner).ValueRO;

            cpRef.ValueRW.m_Position = newPosition;
            curveRef.ValueRW.m_Bezier.Set(cpRef.ValueRO.m_CurveKey, newPosition);

            QNode node = new(edge.m_Start, MIT.m_Instance, QTypes.Identity.Node);
            node.SetUpdateTags();
            node = new(edge.m_End, MIT.m_Instance, QTypes.Identity.Node);
            node.SetUpdateTags();

            //QLog.Debug(sb.ToString());

            return true;
        }


        public readonly bool RotateBy(StateDataWrapper data, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            if (!EntityManager.Exists(m_Entity)) return false;

            return RotateTo(data, ((Quaternion)Rotation).eulerAngles.y + delta, ref matrix, origin);
        }

        public readonly bool RotateTo(StateDataWrapper data, float angle, ref Matrix4x4 matrix, float3 origin)
        {
            if (!EntityManager.Exists(m_Entity)) return false;

            //StringBuilder sb = new();
            //sb.AppendFormat("CP.Rot {0} origin:{1}, angle:{2}, newPos:{3}: ", m_Entity.D(), origin, angle, Position.DX());
            RefRW<MIT_ControlPoint> cpRef = m_Lookup.MIT_ControlPoint.GetRefRW(m_Entity);
            cpRef.ValueRW.m_Position = (float3)matrix.MultiplyPoint(cpRef.ValueRO.m_Position - origin);
            EntityManager.AddComponent<Game.Common.Updated>(m_Entity);
            //QLog.Debug(sb.ToString());

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
