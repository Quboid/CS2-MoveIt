using Colossal.Mathematics;
using MoveIt.Moveables;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor.QEntity
{
    internal partial struct QEntity
    {
        private float3 Lane_Position => BezierPosition(Curve);

        private float Lane_Angle
        {
            get
            {
                float3 mag = Curve.d - Curve.a;
                return math.atan2(mag.z, mag.x) * Mathf.Rad2Deg;
            }
        }

        private quaternion Lane_Rotation => quaternion.EulerXYZ(0f, Angle, 0f);

        //private readonly bool Lane_TryGetElevation(out float elevation)
        //{
        //    elevation = 0f;
        //    // Return true if not found as Props will get Elevation component if needed
        //    if (!m_Lookup.goElevation.HasComponent(m_Entity)) return true;

        //    elevation = m_Lookup.goElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
        //    return true;
        //}

        //private readonly bool Lane_TrySetElevation(float elevation)
        //{
        //    return ManageNetworkElevation(elevation);
        //}

        private bool Lane_SetUpdated()
        {
            TryAddUpdate(m_Entity);

            Game.Net.Edge edge = _Lookup.gnEdge.GetRefRO(m_Entity).ValueRO;
            //QLog.Debug($"QEnt.Lane_SetUpd {m_Entity.DX(true)} edge:{edge.m_Start.DX()}/{edge.m_End.DX()}");
            QEntity node = new(_Manager, ref _Lookup, edge.m_Start, Identity.Node);
            node.Node_SetUpdated();
            node = new(_Manager, ref _Lookup, edge.m_End, Identity.Node);
            node.Node_SetUpdated();

            if (!TryGetComponent<Game.Net.Aggregated>(out var component)) return true;
            
            Entity aggregate = component.m_Aggregate;
            TryAddUpdate(aggregate);

            return true;
        }

        private readonly void Lane_TransformEnd()
        { }


        private bool Lane_MoveBy(State state, float3 newPosition, float3 delta)
        {
            Bezier4x3 curve = _Lookup.gnCurve.GetRefRO(m_Entity).ValueRO.m_Bezier;
            curve.a += delta;
            curve.b += delta;
            curve.c += delta;
            curve.d += delta;
            _Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = curve;
            //SetElevation(state, delta);
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

            _Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = curve;
            return Lane_SetUpdated();
        }
    }
}
