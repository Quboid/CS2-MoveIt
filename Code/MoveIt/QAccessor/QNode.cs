using MoveIt.Moveables;
using QCommonLib;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    /// <summary>
    /// Actual accessor for entities, specifically nodes
    /// </summary>
    public struct QNode : IQEntity, IDisposable
    {
        internal static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Entity m_Entity { get; }
        public bool m_IsTopLevel { get; }
        internal QLookup m_Lookup;
        internal QTypes.Identity m_Identity;

        internal QNode(Entity e, SystemBase system, QTypes.Identity identity, bool isTopLevel = true)
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

        internal readonly void SetUpdateTags()
        {
            TryAddUpdate(m_Entity);

            if (TryGetBuffer<Game.Net.ConnectedEdge>(out var buffer, true))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity seg = buffer[i].m_Edge;
                    Game.Net.Edge edge = EntityManager.GetComponentData<Game.Net.Edge>(seg);
                    if (!m_Entity.Equals(edge.m_Start) && !m_Entity.Equals(edge.m_End)) continue;

                    TryAddUpdate(seg);
                    if (!edge.m_Start.Equals(m_Entity)) TryAddUpdate(edge.m_Start);
                    else if (!edge.m_End.Equals(m_Entity)) TryAddUpdate(edge.m_End);

                    if (m_Lookup.gnAggregated.HasComponent(seg))
                    {
                        Game.Net.Aggregated aggregated = m_Lookup.gnAggregated.GetRefRO(seg).ValueRO;
                        TryAddUpdate(aggregated.m_Aggregate);
                    }
                }
            }
        }

        internal static void TryAddUpdate(Entity e)
        {
            if (!EntityManager.HasComponent<Game.Common.Updated>(e))
            {
                EntityManager.AddComponent<Game.Common.Updated>(e);
            }
            if (!EntityManager.HasComponent<Game.Common.BatchesUpdated>(e))
            {
                EntityManager.AddComponent<Game.Common.BatchesUpdated>(e);
            }
        }


        public readonly float3 Position
        {
            get
            {
                if (!m_Lookup.gnNode.HasComponent(m_Entity))
                {
                    throw new Exception($"Entity {m_Entity.D()} does not have Net.Node component");
                }
                //QLog.Bundle("NODE", $"Node {m_Entity.D()} is at {m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position.DX()}");
                return m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position;
            }
        }

        public readonly float Angle
        {
            get => Rotation.Y();
        }

        public readonly quaternion Rotation
        {
            get => m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Rotation;
        }


        public readonly bool MoveBy(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            return Move(data, newPosition, delta);
        }

        public readonly bool Move(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            if (!EntityManager.Exists(m_Entity)) return false;

            m_Lookup.gnNode.GetRefRW(m_Entity).ValueRW.m_Position = newPosition;
            SetUpdateTags();

            return true;
        }

        public readonly bool RotateBy(StateDataWrapper data, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return RotateTo(data, ((Quaternion)Rotation).eulerAngles.y + delta, ref matrix, origin);
        }

        public readonly bool RotateTo(StateDataWrapper data, float angle, ref Matrix4x4 matrix, float3 origin)
        {
            if (!EntityManager.Exists(m_Entity)) return false;

            quaternion newRotation = Quaternion.Euler(0f, angle, 0f);
            m_Lookup.gnNode.GetRefRW(m_Entity).ValueRW = new()
            {
                m_Position = matrix.MultiplyPoint(m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position - origin),
                m_Rotation = newRotation,
            };
            EntityManager.AddComponent<Game.Common.Updated>(m_Entity);

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
