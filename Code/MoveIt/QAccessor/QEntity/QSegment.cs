using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor.QEntity
{
    internal partial struct QEntity
    {
        private float3 Segment_Position => BezierPosition(Curve);

        private float Segment_Angle
        {
            get
            {
                float3 mag = Curve.d - Curve.a;
                return math.atan2(mag.z, mag.x) * Mathf.Rad2Deg;
            }
        }

        private quaternion Segment_Rotation => quaternion.EulerXYZ(0f, Angle, 0f);


        private bool Segment_SetUpdated()
        {
            TryAddUpdate(m_Entity);

            Game.Net.Edge edge = _Lookup.gnEdge.GetRefRO(m_Entity).ValueRO;
            QEntity node = new(_Manager, ref _Lookup, edge.m_Start, Identity.Node);
            node.SetUpdated();
            node = new(_Manager, ref _Lookup, edge.m_End, Identity.Node);
            node.SetUpdated();

            if (TryGetComponent<Game.Net.Aggregated>(out var aggregateComp))
            {
                Entity aggregate = aggregateComp.m_Aggregate;
                TryAddUpdate(aggregate);
            }

            return true;
        }

        private readonly void Segment_TransformEnd()
        { }


        private bool Segment_MoveBy(State state, float3 newPosition, float3 delta)
        {
            Segment_SetUpdated();
            return true;
        }

        private bool Segment_MoveTo(State state, float3 newPosition, float3 delta)
        {
            MIT.Log.Info($"QSeg.Move called - new:{newPosition.DX()}, delta:{delta.DX()}, start:{Position.DX()}");
            return false;
        }

        private readonly bool Segment_RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return false;
        }

        private readonly bool Segment_RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            return false;
        }

        private bool Segment_UpdateCurve(State state)
        {
            _Lookup.gnCurve.GetRefRW(m_Entity).ValueRW.m_Bezier = state.m_Curve;
            return Segment_SetUpdated();
        }


        //private static float2 GetLinesIntersection(Line2 line1, Line2 line2)
        //{
        //    float2 point = default;
        //    float a1 = line1.b.y - line1.a.y;
        //    float b1 = line1.a.x - line1.b.x;
        //    float c1 = a1 * line1.a.x + b1 * line1.a.y;

        //    float a2 = line2.b.y - line2.a.y;
        //    float b2 = line2.a.x - line2.b.x;
        //    float c2 = a2 * line2.a.x + b2 * line2.a.y;

        //    float delta = a1 * b2 - a2 * b1;
        //    if (delta > -10 && delta < 10)
        //    {
        //        point = new((line2.b - line1.b) / 2 + line1.b);
        //        return point;
        //    }
        //    point.x = (b2 * c1 - b1 * c2) / delta;
        //    point.y = (a1 * c2 - a2 * c1) / delta;
        //    return point;
        //}
    }
}
