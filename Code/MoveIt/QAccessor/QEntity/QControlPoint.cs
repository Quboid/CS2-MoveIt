﻿using System;
using MoveIt.Components;
using MoveIt.Moveables;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor.QEntity
{
    internal partial struct QEntity
    {
        private float3 ControlPoint_Position
        {
            get
            {
                if (!_Lookup.MIT_ControlPoint.HasComponent(m_Entity))
                {
                    throw new Exception($"Entity {m_Entity.D()} does not have MIT_ControlPoint component");
                }
                return _Lookup.MIT_ControlPoint.GetRefRO(m_Entity).ValueRO.m_Position;
            }
        }

        private float ControlPoint_Angle => Rotation.Y();

        private readonly quaternion ControlPoint_Rotation => default;

        private readonly bool ControlPoint_TryGetElevation(out float elevation)
        {
            elevation = 0f;
            return false;
        }

        private readonly bool ControlPoint_TrySetElevation(float elevation)
        {
            return false;
        }


        private bool ControlPoint_SetUpdated()
        {
            RefRW<MIT_ControlPoint> cpRef = _Lookup.MIT_ControlPoint.GetRefRW(m_Entity);
            Entity parent = cpRef.ValueRO.m_Parent;
            Game.Net.Edge edge = _Lookup.gnEdge.GetRefRO(parent).ValueRO;
            QEntity node = new(_Manager, ref _Lookup, edge.m_Start, Identity.Node);
            node.Node_SetUpdated();
            node = new(_Manager, ref _Lookup, edge.m_End, Identity.Node);
            node.Node_SetUpdated();

            // Attempting to reset the street name, it does not work
            if (_Manager.HasComponent<Game.Net.Aggregated>(parent))
            {
                Game.Net.Aggregated aggregated = _Manager.GetComponentData<Game.Net.Aggregated>(parent);
                TryAddUpdate(aggregated.m_Aggregate);
            }

            return true;
        }

        private readonly void ControlPoint_TransformEnd()
        { }


        private bool ControlPoint_MoveBy(State state, float3 newPosition, float3 delta)
        {
            return ControlPoint_MoveTo(state, newPosition, delta);
        }

        private bool ControlPoint_MoveTo(State state, float3 newPosition, float3 delta)
        {
            RefRW<MIT_ControlPoint> cpRef = _Lookup.MIT_ControlPoint.GetRefRW(m_Entity);
            Entity parent = cpRef.ValueRO.m_Parent;
            RefRW<Game.Net.Curve> curveRef = _Lookup.gnCurve.GetRefRW(parent);

            cpRef.ValueRW.m_Position = newPosition;
            curveRef.ValueRW.m_Bezier.Set(cpRef.ValueRO.m_ParentKey, newPosition);

            return ControlPoint_SetUpdated();
        }

        private bool ControlPoint_RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            float3 oldAngles = ControlPoint_Rotation.ToEulerDegrees();
            return ControlPoint_RotateTo(state, Quaternion.Euler(oldAngles.x, oldAngles.y + delta, oldAngles.z), ref matrix, origin);
        }

        private bool ControlPoint_RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            RefRW<MIT_ControlPoint> cpRef = _Lookup.MIT_ControlPoint.GetRefRW(m_Entity);
            cpRef.ValueRW.m_Position = (float3)matrix.MultiplyPoint(cpRef.ValueRO.m_Position - origin);
            return ControlPoint_SetUpdated();
        }
    }
}
