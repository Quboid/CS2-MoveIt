using Colossal.Mathematics;
using Game.Rendering;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Overlays
{
    public readonly struct DrawTools
    {
        private static OverlayRenderSystem.StyleFlags GetProjection(Projection projection)
        {
            return projection switch {
                Projection.Fixed    => 0,
                Projection.Ground   => OverlayRenderSystem.StyleFlags.Projected,
                _ => OverlayRenderSystem.StyleFlags.Projected,
            };
        }

        #region Drawing Lines
        public static void LineSimple(OverlayRenderSystem.Buffer buffer, MIO_Common common, Line3.Segment line, Projection proj)
        {
            buffer.DrawLine(common.m_OutlineColor, common.m_OutlineColor, common.GetWidth(proj), GetProjection(proj), line, common.GetWidth(proj));
        }

        public static void Curve(OverlayRenderSystem.Buffer buffer, MIO_Common common, Bezier4x3 curve, Projection proj, float width = -1, float widthMultiplier = 1f)
        {
            // Unreliable except if Style is Projected
            if (Mathf.Approximately(width, -1)) width = common.GetWidth(proj) * 5;
            buffer.DrawCurve(common.m_OutlineColor, Color.clear, common.GetWidth(proj) * widthMultiplier, GetProjection(proj), curve, width * widthMultiplier, 1f);
        }

        public static void CurveThin(OverlayRenderSystem.Buffer buffer, MIO_Common common, Bezier4x3 curve, Projection proj)
        {
            buffer.DrawCurve(common.m_OutlineColor, Color.clear, common.GetWidth(proj), GetProjection(proj), curve, common.GetWidth(proj), 1f);
        }

        public static void LineDashedSimple(OverlayRenderSystem.Buffer buffer, MIO_Common common, Line2 line, Projection proj, float dashLength = 0f, float gapLength = 0f)
        {
            Line3.Segment line3 = new(new float3(line.a.x, 0, line.a.y), new float3(line.b.x, 0, line.b.y));
            LineDashedSimple(buffer, common, line3, proj, dashLength, gapLength);
        }

        public static void LineDashedSimple(OverlayRenderSystem.Buffer buffer, MIO_Common common, Line3.Segment line, Projection proj, float dashLength = 0f, float gapLength = 0f)
        {
            if (dashLength == 0) dashLength = Overlay.LINE_DEFAULT_WIDTH;
            if (gapLength == 0) gapLength = Overlay.LINE_DEFAULT_WIDTH * 1.5f;
            buffer.DrawDashedLine(common.m_OutlineColor, common.m_OutlineColor, Overlay.LINE_DEFAULT_WIDTH, GetProjection(proj), line, Overlay.LINE_DEFAULT_WIDTH, dashLength, gapLength);
        }
        #endregion

        #region Drawing Circles

        private const float CIRCLE_BEZIER = 0.55228475f;

        public static void CircleSimple(OverlayRenderSystem.Buffer buffer, MIO_Common common, Circle3 circle, Projection proj)
        {
            //if (common.m_Projection == OverlayRenderSystem.StyleFlags.Grid)
            //{
            //    ManualCircle(buffer, common, circle);
            //    return;
            //}

            buffer.DrawCircle(common.m_OutlineColor, common.m_BackgroundColor, common.GetWidth(proj), GetProjection(proj), math.forward().xz, circle.position, circle.radius * 2);
        }

        public static void CircleDashedSimple(OverlayRenderSystem.Buffer buffer, MIO_Common common, Circle3 circle, Projection proj)
        {
            ManualCircle(buffer, common, circle, proj, true);
        }

        public static void ManualCircle(OverlayRenderSystem.Buffer buffer, MIO_Common common, Circle3 circle, Projection proj, bool isDashed = false)
        {
            if (isDashed && !common.m_BackgroundColor.Equals(default))
            {
                MIO_Common innerCommon = common;
                innerCommon.m_OutlineColor = common.m_BackgroundColor;
                CircleSimple(buffer, innerCommon, circle, proj);
            }
            float w = common.GetWidth(proj);
            float3 p = circle.position;
            OverlayRenderSystem.StyleFlags s = GetProjection(proj);

            float r = circle.radius - (w / 2);
            float3 offX = new(r, 0f, 0f);
            float3 offXb = new(r, 0f, r * CIRCLE_BEZIER);
            float3 offXc = new(r, 0f, -r * CIRCLE_BEZIER);
            float3 offZb = new(r * CIRCLE_BEZIER, 0f, r);
            float3 offZc = new(-r * CIRCLE_BEZIER, 0f, r);
            float3 offZ = new(0f, 0f, r);
            Bezier4x3 curve = new(p + offX, p + offXb, p + offZb, p + offZ);
            float length = MathUtils.Length(curve);
            float interval = (length / math.max(math.floor(length), 1)) / 4;

            if (isDashed) buffer.DrawDashedCurve(common.m_OutlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
            else buffer.DrawCurve(common.m_OutlineColor, curve, w);
            curve = new(p + offX, p + offXc, p - offZc, p - offZ);
            if (isDashed) buffer.DrawDashedCurve(common.m_OutlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
            else buffer.DrawCurve(common.m_OutlineColor, curve, w);
            curve = new(p - offX, p - offXc, p + offZc, p + offZ);
            if (isDashed) buffer.DrawDashedCurve(common.m_OutlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
            else buffer.DrawCurve(common.m_OutlineColor, curve, w);
            curve = new(p - offX, p - offXb, p - offZb, p - offZ);
            if (isDashed) buffer.DrawDashedCurve(common.m_OutlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
            else buffer.DrawCurve(common.m_OutlineColor, curve, w);
        }

        #endregion


        #region Static line calculations
        internal static NativeArray<Line3.Segment> CalculateBoundsLines(Bounds3 bounds)
        {
            (float3 a, float3 b) = (bounds.min, bounds.max);
            return CalculateRectangleLines(new(a.x, a.z, b.x, b.z));
        }

        internal static NativeArray<Line3.Segment> CalculateRectangleLines(float4 area)
        {
            float3 tl = new(area.x, 0f, area.y);
            float3 tr = new(area.z, 0f, area.y);
            float3 bl = new(area.x, 0f, area.w);
            float3 br = new(area.z, 0f, area.w);

            var lines = new NativeArray<Line3.Segment>(new Line3.Segment[] {
                    new(tl, bl),
                    new(tl, tr),
                    new(br, bl),
                    new(br, tr),
                }, Allocator.TempJob);
            return lines;
        }

        internal static void CalculateQuadLines(ref NativeArray<Line3.Segment> lines, Quad3 quad)
        {
            lines[0] = quad.ab;
            lines[1] = quad.cd;
            lines[2] = quad.ad;
            lines[3] = quad.bc;
        }

        internal static NativeArray<Line3.Segment> CalculateSegmentLines(Bezier4x3 bezier, float cutStart = 0f, float cutEnd = 0f)
        {
            var lines = new NativeArray<Line3.Segment>(new Line3.Segment[] {
                    CalculcateSegmentEndLine(bezier, cutStart, cutEnd, true),
                    CalculcateSegmentEndLine(bezier, cutStart, cutEnd, false),
                }, Allocator.TempJob);
            return lines;
        }

        internal static NativeArray<Line3.Segment> CalculatePointMarkerLines(float3 point)
        {
            float3 x = new(2, 0, 0);
            float3 y = new(0, 2, 0);
            float3 z = new(0, 0, 2);

            var lines = new NativeArray<Line3.Segment>(new Line3.Segment[] {
                    new(point - x, point + x),
                    new(point - y, point + y),
                    new(point - z, point + z),
                }, Allocator.TempJob);
            return lines;
        }

        internal static Line3.Segment CalculateProtrudedLine(Circle3 startCircle, Circle3 endCircle)
        {
            float2 mag = endCircle.position.XZ() - startCircle.position.XZ();
            float distance = math.distance(startCircle.position, endCircle.position);

            float2 posA = mag * (startCircle.radius / distance);
            float2 posB = mag * (1 - endCircle.radius / distance);

            return new(startCircle.position + posA.ToFloat3(0), startCircle.position + posB.ToFloat3(endCircle.position.y - startCircle.position.y));
        }

        internal static Line3.Segment CalculateNodeProtrusionCurve(Bezier4x3 curve, Circle3 startCircle, Circle3 endCircle, bool isCurveStart)
        {
            Line3.Segment line = isCurveStart ? new(curve.a, curve.b) : new(curve.d, curve.c);
            float cutStart = QIntersect.IntersectionsBetweenLineAndCircleCut(startCircle.XZ(), line, true);
            float cutEnd = QIntersect.IntersectionsBetweenLineAndCircleCut(endCircle.XZ(), line, false);

            return CalculcateSegmentEndLine(curve, cutStart, cutEnd, isCurveStart);
        }

        internal static Line3.Segment CalculcateSegmentEndLine(Bezier4x3 curve, float cutStart, float cutEnd, bool isCurveStart)
        {
            float3 posA;
            float3 posB;
            if (isCurveStart)
            {
                posA = curve.a.LerpAbs(curve.b, cutEnd);
                posB = curve.b.LerpAbs(curve.a, cutStart);
            }
            else
            {
                posA = curve.d.LerpAbs(curve.c, cutEnd);
                posB = curve.c.LerpAbs(curve.d, cutStart);
            }
            return new(posA, posB);
        }

        internal static void CalculateBuildingRectangleLines(Game.Objects.Transform transform, float width, float3 halfSize, ref DynamicBuffer<MIO_Lines> lines, ref DynamicBuffer<MIO_DashedLines> dashed)
        {
            float3 position = transform.m_Position;
            quaternion q = transform.m_Rotation;

            float3 a;
            float3 b;

            // Sides - bring in sides (X) by half of line width, bring in ends (Z) by front/back width to avoid overlap
            a = new(halfSize.x, halfSize.y, -halfSize.z);
            a.x -= width / 2;
            a.z += width;
            a = RotateAroundPivot(position, q, a);
            b = halfSize;
            b.x -= width / 2;
            b.z -= width;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment left = new(a, b);

            a = -halfSize;
            a.x += width / 2;
            a.z += width;
            a = RotateAroundPivot(position, q, a);
            b = new(-halfSize.x, halfSize.y, halfSize.z);
            b.x += width / 2;
            b.z -= width;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment right = new(a, b);

            // Front/back - bring in sides (Z) by half of line width
            a = -halfSize;
            a.z += width / 2;
            a = RotateAroundPivot(position, q, a);
            b = new(halfSize.x, halfSize.y, -halfSize.z);
            b.z += width / 2;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment back = new(a, b);

            a = new(-halfSize.x, halfSize.y, halfSize.z);
            a.z -= width / 2;
            a = RotateAroundPivot(position, q, a);
            b = halfSize;
            b.z -= width / 2;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment front = new(a, b);

            a = new(-halfSize.x, halfSize.y, halfSize.z);
            a.x += width;
            a.z -= width * 1.5f;
            a = RotateAroundPivot(position, q, a);
            b = halfSize;
            b.x -= width;
            b.z -= width * 1.5f;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment dashes = new(a, b);

            lines.Clear();
            lines.Add(new(left));
            lines.Add(new(right));
            lines.Add(new(back));
            lines.Add(new(front));
            dashed.Clear();
            dashed.Add(new(dashes));
        }

        internal static void CalculateRectangleLines(Game.Objects.Transform transform, float width, float3 halfSize, ref DynamicBuffer<MIO_Lines> lines)
        {
            float3 position = transform.m_Position;
            quaternion q = transform.m_Rotation;

            float3 a;
            float3 b;

            // Sides - bring in sides (X) by half of line width, bring in ends (Z) by front/back width to avoid overlap
            a = new(halfSize.x, halfSize.y, -halfSize.z);
            a.x -= width / 2;
            a.z += width;
            a = RotateAroundPivot(position, q, a);
            b = halfSize;
            b.x -= width / 2;
            b.z -= width;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment left = new(a, b);

            a = -halfSize;
            a.x += width / 2;
            a.z += width;
            a = RotateAroundPivot(position, q, a);
            b = new(-halfSize.x, halfSize.y, halfSize.z);
            b.x += width / 2;
            b.z -= width;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment right = new(a, b);

            // Front/back - bring in sides (Z) by half of line width
            a = -halfSize;
            a.z += width / 2;
            a = RotateAroundPivot(position, q, a);
            b = new(halfSize.x, halfSize.y, -halfSize.z);
            b.z += width / 2;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment back = new(a, b);

            a = new(-halfSize.x, halfSize.y, halfSize.z);
            a.z -= width / 2;
            a = RotateAroundPivot(position, q, a);
            b = halfSize;
            b.z -= width / 2;
            b = RotateAroundPivot(position, q, b);
            Line3.Segment front = new(a, b);

            lines.Clear();
            lines.Add(new(left));
            lines.Add(new(right));
            lines.Add(new(back));
            lines.Add(new(front));
        }

        internal static float3 RotateAroundPivot(float3 position, quaternion q, float3 offset)
        {
            return position + math.mul(q, offset);
        }
        #endregion
    }
}
