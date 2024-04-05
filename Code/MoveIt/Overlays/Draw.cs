using Colossal.Mathematics;
using Game.Rendering;
using QCommonLib;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Overlays
{
    public struct DrawTools
    {
        #region Drawing Utils
        public static OverlayRenderSystem.Buffer m_OverlayRenderBuffer;
        public const float CIRCLE_BEZIER = 0.55228475f;

        public readonly void LineSimple(Utils.OverlayCommon common, Color outlineColor, Line3.Segment line)
        {
            m_OverlayRenderBuffer.DrawLine(outlineColor, outlineColor, common.Width, common.Style, line, common.Width);
        }

        public readonly void CurveSimple(Utils.OverlayCommon common, Color outlineColor, Bezier4x3 curve)
        {
            m_OverlayRenderBuffer.DrawCurve(outlineColor, Color.clear, common.Width, common.Style, curve, common.Width * 5, 1f);
        }

        public readonly void LineDashedSimple(Utils.OverlayCommon common, Color outlineColor, Line2 line, float dashLength = 0f, float gapLength = 0f)
        {
            Line3.Segment line3 = new(new float3(line.a.x, 0, line.a.y), new float3(line.b.x, 0, line.b.y));
            LineDashedSimple(common, outlineColor, line3, dashLength, gapLength);
        }

        public readonly void LineDashedSimple(Utils.OverlayCommon common, Color outlineColor, Line3.Segment line, float dashLength = 0f, float gapLength = 0f)
        {
            if (dashLength == 0) dashLength = common.Width;
            if (gapLength == 0) gapLength = common.Width * 1.5f;
            m_OverlayRenderBuffer.DrawDashedLine(outlineColor, outlineColor, common.Width, common.Style, line, common.Width, dashLength, gapLength);
        }

        public readonly void CircleSimple(Utils.OverlayCommon common, Color outlineColor, float diameter)
        {
            CircleFill(common, outlineColor, Color.clear, diameter);
        }

        public readonly void CircleSimple(Utils.OverlayCommon common, Color outlineColor, Circle2 circle)
        {
            common.Transform = new(new float3(circle.position.x, common.Transform.m_Position.y, circle.position.y), default);
            CircleFill(common, outlineColor, Color.clear, circle.radius * 2);
        }

        public readonly void CircleFill(Utils.OverlayCommon common, Color outlineColor, Color fillColor,float diameter)
        {
            m_OverlayRenderBuffer.DrawCircle(outlineColor, fillColor, common.Width, common.Style, math.forward().xz, common.Transform.m_Position, diameter);
        }

        public readonly void CircleDashedSimple(Utils.OverlayCommon common, Color outlineColor, float diameter)
        {
            CircleDashedFill(common, outlineColor, Color.clear, diameter);
        }

        public readonly void CircleDashedFill(Utils.OverlayCommon common, Color outlineColor, Color bgColor, float diameter)
        {
            CircleSimple(common, bgColor, diameter);
            float w = common.Width;
            float3 p = common.Transform.m_Position;
            OverlayRenderSystem.StyleFlags s = common.Style;

            float r = (diameter - w) / 2f;
            float3 offX = new(r, 0f, 0f);
            float3 offXb = new(r, 0f, r * CIRCLE_BEZIER);
            float3 offXc = new(r, 0f, -r * CIRCLE_BEZIER);
            float3 offZb = new(r * CIRCLE_BEZIER, 0f, r);
            float3 offZc = new(-r * CIRCLE_BEZIER, 0f, r);
            float3 offZ = new(0f, 0f, r);
            Bezier4x3 curve = new(p + offX, p + offXb, p + offZb, p + offZ);
            float length = MathUtils.Length(curve);
            float interval = (length / math.max(math.floor(length), 1)) / 4;
            m_OverlayRenderBuffer.DrawDashedCurve(outlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
            curve = new(p + offX, p + offXc, p - offZc, p - offZ);
            m_OverlayRenderBuffer.DrawDashedCurve(outlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
            curve = new(p - offX, p - offXc, p + offZc, p + offZ);
            m_OverlayRenderBuffer.DrawDashedCurve(outlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
            curve = new(p - offX, p - offXb, p - offZb, p - offZ);
            m_OverlayRenderBuffer.DrawDashedCurve(outlineColor, Color.clear, w, s, curve, w, interval * 0.8f, interval * 1.2f);
        }
        #endregion

        #region Static line calculations
        internal static NativeArray<Line3.Segment> CalculateBoundsLines(Bounds3 bounds)
        {
            var (a, b) = (bounds.min, bounds.max);
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

        internal static NativeArray<Line3.Segment> CalculateQuadLines(Quad3 quad)
        {
            return new NativeArray<Line3.Segment>(new Line3.Segment[] { quad.ab, quad.cd, quad.ad, quad.bc }, Allocator.TempJob);
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

        internal static Line2 CalculateProtrudedLine(Circle2 startCircle, Circle2 endCircle)
        {
            float2 mag = endCircle.position - startCircle.position;
            float distance = math.distance(startCircle.position, endCircle.position);

            float2 posA = mag * (startCircle.radius / distance);
            float2 posB = mag * (1 - endCircle.radius / distance);

            return new(startCircle.position + posA, startCircle.position + posB);
        }

        internal static Line3.Segment CalculateNodeProtrusionCurve(Bezier4x3 curve, Circle2 startCircle, Circle2 endCircle, bool isCurveStart)
        {
            Line3.Segment line = isCurveStart ? new(curve.a, curve.b) : new(curve.d, curve.c);
            float cutStart = QIntersect.IntersectionsBetweenLineAndCircleCut(startCircle, line, true);
            float cutEnd = QIntersect.IntersectionsBetweenLineAndCircleCut(endCircle, line, false);

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

        internal static NativeArray<Line3.Segment> CalculateBuildingRectangleLines(Game.Objects.Transform transform, float width, float3 halfSize, Allocator allocator = Allocator.TempJob)
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
            Line3.Segment dashed = new(a, b);

            var lines = new NativeArray<Line3.Segment>(new Line3.Segment[] { left, right, back, front, dashed }, allocator);
            return lines;
        }

        internal static float3 RotateAroundPivot(float3 position, quaternion q, float3 offset)
        {
            return position + math.mul(q, offset);
        }
        #endregion
    }
}
