using MoveIt.Moveables;
using QCommonLib;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        private readonly float3 Node_Position
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

        private readonly float Node_Angle => Rotation.Y();

        private readonly quaternion Node_Rotation
        {
            get => m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Rotation;
        }


        private readonly bool Node_SetUpdated()
        {
            TryAddUpdate(m_Entity);

            if (TryGetBuffer<Game.Net.ConnectedEdge>(out var buffer, true))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity seg = buffer[i].m_Edge;
                    Game.Net.Edge edge = m_Manager.GetComponentData<Game.Net.Edge>(seg);
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
            return true;
        }

        private readonly void Node_TransformEnd()
        { }


        private readonly bool Node_MoveBy(State state, float3 newPosition, float3 delta)
        {
            return Node_MoveTo(state, newPosition, delta);
        }

        private readonly bool Node_MoveTo(State state, float3 newPosition, float3 delta)
        {
            m_Lookup.gnNode.GetRefRW(m_Entity).ValueRW.m_Position = newPosition;
            if (m_Lookup.gnNodeGeometry.HasComponent(m_Entity))
            {
                m_Lookup.gnNodeGeometry.GetRefRW(m_Entity).ValueRW.m_Bounds += delta;
                m_Lookup.gnNodeGeometry.GetRefRW(m_Entity).ValueRW.m_Offset += delta.y;
            }
            if (m_Lookup.gnElevation.HasComponent(m_Entity))
            {
                m_Lookup.gnElevation.GetRefRW(m_Entity).ValueRW.m_Elevation += delta.y;
            }
            return Node_SetUpdated();
        }

        private readonly bool Node_RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            float3 oldAngles = Node_Rotation.ToEulerDegrees();
            return Node_RotateTo(state, Quaternion.Euler(oldAngles.x, oldAngles.y + delta, oldAngles.z), ref matrix, origin);
        }

        private readonly bool Node_RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            m_Lookup.gnNode.GetRefRW(m_Entity).ValueRW = new()
            {
                m_Position = matrix.MultiplyPoint(m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position - origin),
                m_Rotation = newRotation,
            };
            return Node_SetUpdated();
        }
    }
}
