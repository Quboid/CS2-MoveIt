using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        private readonly float3 Lane_Position => BezierPosition(Curve);

        private readonly float Lane_Angle
        {
            get
            {
                float3 mag = Curve.d - Curve.a;
                return math.atan2(mag.z, mag.x) * Mathf.Rad2Deg;
            }
        }

        private readonly quaternion Lane_Rotation => quaternion.EulerXYZ(0f, Angle, 0f);


        private bool Lane_SetUpdated()
        {
            TryAddUpdate(m_Entity);

            Game.Net.Edge edge = m_Lookup.gnEdge.GetRefRO(m_Entity).ValueRO;
            QEntity node = new(ref m_Lookup, edge.m_Start, Identity.Node);
            node.Node_SetUpdated();
            node = new(ref m_Lookup, edge.m_End, Identity.Node);
            node.Node_SetUpdated();

            if (TryGetComponent<Game.Net.Aggregated>(out var component))
            {
                Entity aggregate = component.m_Aggregate;
                TryAddUpdate(aggregate);
            }

            return true;
        }


        private bool Lane_MoveBy(State state, float3 newPosition, float3 delta)
        {
            Bezier4x3 curve = Curve;
            curve.a += delta;
            curve.b += delta;
            curve.c += delta;
            curve.d += delta;
            m_Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = curve;
            return Lane_SetUpdated();
        }

        private bool Lane_MoveTo(State state, float3 newPosition, float3 delta)
        {
            return Lane_MoveBy(state, newPosition, delta);
        }

        private bool Lane_RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return Lane_RotateTo(state, quaternion.identity, ref matrix, origin);
        }

        private bool Lane_RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            Bezier4x3 curve = Curve;

            curve.a = (float3)matrix.MultiplyPoint(curve.a - origin);
            curve.b = (float3)matrix.MultiplyPoint(curve.b - origin);
            curve.c = (float3)matrix.MultiplyPoint(curve.c - origin);
            curve.d = (float3)matrix.MultiplyPoint(curve.d - origin);

            m_Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = curve;
            return Lane_SetUpdated();
        }
    }
}
