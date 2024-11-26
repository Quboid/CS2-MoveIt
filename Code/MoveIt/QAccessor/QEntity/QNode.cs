using System;
using MoveIt.Moveables;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor.QEntity
{
    internal partial struct QEntity
    {
        private float3 Node_Position
        {
            get
            {
                if (!_Lookup.gnNode.HasComponent(m_Entity))
                {
                    throw new Exception($"Entity {m_Entity.D()} does not have Net.Node component");
                }
                //QLog.Bundle("NODE", $"Node {m_Entity.D()} is at {m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position.DX()}");
                return _Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position;
            }
        }

        private float Node_Angle => Rotation.Y();

        private quaternion Node_Rotation => _Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Rotation;

        private bool Node_SetUpdated()
        {
            TryAddUpdate(m_Entity);

            if (!TryGetBuffer<Game.Net.ConnectedEdge>(out var buffer, true)) return true;
            
            for (var i = 0; i < buffer.Length; i++)
            {
                Entity seg = buffer[i].m_Edge;
                var edge = _Manager.GetComponentData<Game.Net.Edge>(seg);
                if (!m_Entity.Equals(edge.m_Start) && !m_Entity.Equals(edge.m_End)) continue;

                TryAddUpdate(seg);
                if (!edge.m_Start.Equals(m_Entity)) TryAddUpdate(edge.m_Start);
                else if (!edge.m_End.Equals(m_Entity)) TryAddUpdate(edge.m_End);

                if (!_Lookup.gnAggregated.HasComponent(seg)) continue;
                    
                Game.Net.Aggregated aggregated = _Lookup.gnAggregated.GetRefRO(seg).ValueRO;
                TryAddUpdate(aggregated.m_Aggregate);
            }
            return true;
        }

        private void Node_TransformEnd()
        { }


        private bool Node_MoveBy(State state, float3 newPosition, float3 delta)
        {
            return Node_MoveTo(state, newPosition, delta);
        }

        private bool Node_MoveTo(State state, float3 newPosition, float3 delta)
        {
            _Lookup.gnNode.GetRefRW(m_Entity).ValueRW.m_Position = newPosition;
            if (_Lookup.gnNodeGeometry.HasComponent(m_Entity))
            {
                _Lookup.gnNodeGeometry.GetRefRW(m_Entity).ValueRW.m_Bounds += delta;
                _Lookup.gnNodeGeometry.GetRefRW(m_Entity).ValueRW.m_Offset += delta.y;
            }
            if (_Lookup.gnElevation.HasComponent(m_Entity))
            {
                _Lookup.gnElevation.GetRefRW(m_Entity).ValueRW.m_Elevation += delta.y;
            }
            return Node_SetUpdated();
        }

        private bool Node_RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            float3 oldAngles = Node_Rotation.ToEulerDegrees();
            return Node_RotateTo(state, Quaternion.Euler(oldAngles.x, oldAngles.y + delta, oldAngles.z), ref matrix, origin);
        }

        private bool Node_RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            _Lookup.gnNode.GetRefRW(m_Entity).ValueRW = new()
            {
                m_Position = matrix.MultiplyPoint(_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position - origin),
                m_Rotation = newRotation,
            };
            return Node_SetUpdated();
        }
    }
}
