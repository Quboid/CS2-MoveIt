using Colossal.Mathematics;
using Game.Rendering;
using QCommonLib;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Overlays
{
    public enum OverlayFlags
    {
        None = 0,
        Hovering = 1,
        Selected = 2,
        Moving = 4,
        Unselect = 8,
        Tool = 16,
        Manipulating = 32,
        Custom = 64,
    }

    public enum ToolFlags
    {
        None = 0,
        Manipulation = 1,
    }

    public static class Utils
    {
        internal static readonly float s_DefaultLineWidth = 0.3f;

        public struct OverlayCommon
        {
            public Game.Objects.Transform Transform { get; set; }
            public DrawTools DrawTool { get; set; }
            public OverlayFlags Flags { get; set; }
            public OverlayRenderSystem.StyleFlags Style { get; set; }
            public float Width { get; set; }
            public QTypes.Manipulate Manipulatable { get; set; }
            public Color CustomColor { get; set; }

            public OverlayCommon()
            {
                Transform = default;
                Flags = default;
                DrawTool = new();
                Style = OverlayRenderSystem.StyleFlags.Projected;
                Width = s_DefaultLineWidth;
                Manipulatable = QTypes.Manipulate.Normal;
                CustomColor = default;
            }

            public readonly OverlayFlags PrimaryFlag
            {
                get
                {
                    if ((Flags & OverlayFlags.Tool) != 0) return OverlayFlags.Tool;
                    if ((Flags & OverlayFlags.Custom) != 0) return OverlayFlags.Custom;
                    if ((Flags & OverlayFlags.Unselect) != 0) return OverlayFlags.Unselect;
                    if ((Flags & OverlayFlags.Hovering) != 0) return OverlayFlags.Hovering;
                    if ((Flags & OverlayFlags.Selected) != 0) return OverlayFlags.Selected;
                    return OverlayFlags.None;
                }
            }
        }

        public interface IOverlay : IDisposable, INativeDisposable
        {
            public OverlayCommon Common { get; set; }

            public void Draw(ToolFlags toolFlags);
        }

        public struct None : IOverlay
        {
            public OverlayCommon Common { get; set; }
            public readonly void Draw(ToolFlags toolFlags) { }
            public readonly void Dispose() { }
            public readonly JobHandle Dispose(JobHandle handle) { return handle; }
        }

        public struct NodeConnection : IOverlay
        {
            public OverlayCommon Common { get; set; }

            public float m_Diameter;
            public float3 m_EndPoint;
            public Line3.Segment m_Line;

            public NodeConnection(Line3.Segment line, float diameter, float3 endPoint, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Flags = flags
                };
                m_Diameter = diameter;
                m_Line = line;
                m_EndPoint = endPoint;
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Color color = Colors.Get(Common, Colors.Styles.Foreground);
                Color semi = color;
                semi.a *= 0.8f;
                Color trans = color;
                trans.a *= 0.25f;

                OverlayCommon common = Common;
                common.Width *= 0.8f;
                Common.DrawTool.LineDashedSimple(common, semi, m_Line);
                common = Common;
                common.Width *= 0.6f;
                Common.DrawTool.CircleFill(common, color, trans, m_Diameter);
            }

            public readonly void Dispose() { }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return handle;
            }
        }

        public struct Circle : IOverlay
        {
            public OverlayCommon Common { get; set; }

            public float m_Diameter;

            public Circle(float3 position, float diameter, OverlayFlags flags)
            {
                Common = new()
                {
                    Transform = new(position, default),
                    Flags = flags,
                };
                m_Diameter = diameter;
            }

            public Circle(float3 position, float diameter, Color c, OverlayRenderSystem.StyleFlags projection)
            {
                Common = new()
                {
                    Transform = new(position, default),
                    Flags = OverlayFlags.Custom,
                    Style = projection,
                    CustomColor = c,
                };
                m_Diameter = diameter;
            }

            public Circle(Game.Objects.Transform transform, float diameter, OverlayFlags flags)
            {
                Common = new()
                {
                    Transform = transform,
                    Flags = flags,
                };
                m_Diameter = diameter;
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Common.DrawTool.CircleSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), m_Diameter);
            }

            public readonly void Dispose() { }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return handle;
            }
        }

        public struct Line : IOverlay
        {
            public OverlayCommon Common { get; set; }

            public Line3.Segment m_Line;
            public float m_Width;

            public Line(Line3.Segment line, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Flags = flags,
                };
                m_Line = line;
                m_Width = s_DefaultLineWidth;
            }

            public Line(Line3.Segment line, Color color, OverlayRenderSystem.StyleFlags style = OverlayRenderSystem.StyleFlags.Projected)
            {
                Common = new()
                {
                    Flags = OverlayFlags.Custom,
                    Style = style,
                    CustomColor = color,
                };
                m_Line = line;
                m_Width = s_DefaultLineWidth;
            }

            public void Draw(ToolFlags toolFlags)
            {
                if (m_Width == default) m_Width = s_DefaultLineWidth;
                Common.DrawTool.LineSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), m_Line);
            }

            public readonly void Dispose() { }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return handle;
            }
        }

        public struct Bounds : IOverlay
        {
            public OverlayCommon Common { get; set; }

            public NativeArray<Line3.Segment> m_Lines = new();

            public Bounds(Bounds3 bounds, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Flags = flags,
                };
                m_Lines = DrawTools.CalculateBoundsLines(bounds);
            }

            public Bounds(Bounds3 bounds, Color c, OverlayRenderSystem.StyleFlags projection)
            {
                Common = new()
                {
                    Flags = OverlayFlags.Custom,
                    Style = projection,
                    CustomColor = c,
                };
                m_Lines = DrawTools.CalculateBoundsLines(bounds);
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Color c = Colors.Get(Common, Colors.Styles.Foreground);
                Common.DrawTool.LineSimple(Common, c, m_Lines[0]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[1]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[2]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[3]);
            }

            public readonly void Dispose() => m_Lines.Dispose();
            public readonly JobHandle Dispose(JobHandle handle) => m_Lines.Dispose(handle);
        }

        public struct Rectangle : IOverlay
        {
            public OverlayCommon Common { get; set; }

            public NativeArray<Line3.Segment> m_Lines = new();

            public Rectangle(Quad3 rect, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Flags = flags,
                };
                m_Lines = DrawTools.CalculateQuadLines(rect);
            }

            public Rectangle(float4 f4, Color c, OverlayRenderSystem.StyleFlags projection)
            {
                Common = new()
                {
                    Flags = OverlayFlags.Custom,
                    Style = projection,
                    CustomColor = c,
                };
                Quad3 rect = new(f4.x, f4.y, f4.z, f4.w);
                m_Lines = DrawTools.CalculateQuadLines(rect);
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Color c = Colors.Get(Common, Colors.Styles.Foreground);
                Common.DrawTool.LineSimple(Common, c, m_Lines[0]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[1]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[2]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[3]);
            }

            public readonly void Dispose()
            {
                m_Lines.Dispose();
            }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return m_Lines.Dispose(handle);
            }
        }

        public struct Diamond : IOverlay
        {
            public OverlayCommon Common { get; set; }

            public float m_Diameter;
            public NativeArray<float3> m_Vertices;

            public Diamond(float3 position, float diameter, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Transform = new(position, default),
                    Flags = flags,
                    Width = 0.2f,
                };
                m_Diameter = diameter;
                m_Vertices = new(8, Allocator.Persistent);
                InitialiseVerticies();
            }

            public Diamond(float3 position, float diameter, Color c, OverlayRenderSystem.StyleFlags projection)
            {
                Common = new()
                {
                    Transform = new(position, default),
                    Flags = OverlayFlags.Custom,
                    Style = projection,
                    CustomColor = c,
                };
                m_Diameter = diameter;
                m_Vertices = new(8, Allocator.Persistent);
                InitialiseVerticies();
            }

            private void InitialiseVerticies()
            {
                float r = m_Diameter / 2;
                float w = Common.Width;
                float h = w / 2;

                m_Vertices[0] = new(-r,     0f, r - h);
                m_Vertices[1] = new(r - w,  0f, r - h);
                m_Vertices[2] = new(r - h,  0f, r);
                m_Vertices[3] = new(r - h,  0f, -r + w);
                m_Vertices[4] = new(r,      0f, -r + h);
                m_Vertices[5] = new(-r + w, 0f, -r + h);
                m_Vertices[6] = new(-r + h, 0f, -r);
                m_Vertices[7] = new(-r + h, 0f, r - w);
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Color c = Colors.Get(Common, Colors.Styles.Foreground);
                quaternion q = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y + 45, 0f);
                float3[] vertices = new float3[8];
                
                for (int i = 0; i < 8; i++)
                {
                    vertices[i] = math.mul(q, m_Vertices[i]) + Common.Transform.m_Position;
                }

                for (int i = 0; i < 8; i += 2)
                {
                    Line3.Segment line = new(vertices[i], vertices[i + 1]);
                    Common.DrawTool.LineSimple(Common, c, line);
                }
            }

            public readonly void Dispose()
            { }

            public readonly JobHandle Dispose(JobHandle handle) => handle;
        }

        public struct Point : IOverlay
        {
            public OverlayCommon Common { get; set; }

            public NativeArray<Line3.Segment> m_Lines = new();

            public Point(float3 point, OverlayFlags flags = OverlayFlags.None)
            {
                Common = new()
                {
                    Transform = new(point, default),
                    Flags = flags,
                    Style = OverlayRenderSystem.StyleFlags.Grid,
                };
                m_Lines = DrawTools.CalculatePointMarkerLines(point);
            }

            public Point(float3 point, Color color)
            {
                Common = new()
                {
                    Transform = new(point, default),
                    Flags = OverlayFlags.Custom,
                    Style = OverlayRenderSystem.StyleFlags.Grid,
                    CustomColor = color,
                };
                m_Lines = DrawTools.CalculatePointMarkerLines(point);
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Color c = Colors.Get(Common, Colors.Styles.Foreground);
                Common.DrawTool.LineSimple(Common, c, m_Lines[0]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[1]);
                Common.DrawTool.LineSimple(Common, c, m_Lines[2]);
            }

            public readonly void Dispose()
            {
                m_Lines.Dispose();
            }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return m_Lines.Dispose(handle);
            }
        }
    }
}
