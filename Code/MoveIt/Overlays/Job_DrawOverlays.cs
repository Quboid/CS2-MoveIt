using Colossal.Mathematics;
using Game.Rendering;
using MoveIt.Tool;
using QCommonLib;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Overlays
{
    internal partial class MIT_OverlaySystem
    {
#if USE_BURST
        [BurstCompile]
#endif
        protected struct DrawOverlaysJob : IJobChunk
        {
            [ReadOnly] public OverlayRenderSystem.Buffer m_OverlayRenderBuffer;
            [ReadOnly] public ToolFlags m_ToolFlags;
            [ReadOnly] public bool m_IsManipMode;
            [ReadOnly] public float3 m_CameraPosition;

            [ReadOnly] public ComponentTypeHandle<MIO_Type> cth_Overlay;
            [ReadOnly] public ComponentTypeHandle<MIO_Common> cth_CommonData;
            [ReadOnly] public ComponentTypeHandle<MIO_Bezier> cth_Bezier;
            [ReadOnly] public ComponentTypeHandle<MIO_Bounds> cth_Bounds;
            [ReadOnly] public ComponentTypeHandle<MIO_Circle> cth_Circle;
            [ReadOnly] public ComponentTypeHandle<MIO_Line> cth_Line;
            [ReadOnly] public ComponentTypeHandle<MIO_Quad> cth_Quad;
            [ReadOnly] public BufferTypeHandle<MIO_Lines> bth_Lines;
            [ReadOnly] public BufferTypeHandle<MIO_DashedLines> bth_DashedLines;
            [ReadOnly] public BufferTypeHandle<MIO_Circles> bth_Circles;

            public readonly bool IsManipulating => (m_ToolFlags & ToolFlags.ManipulationMode) > 0;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var data_Overlay = chunk.GetNativeArray(ref cth_Overlay);
                var data_CommonData = chunk.GetNativeArray(ref cth_CommonData);
                var data_Bezier = chunk.GetNativeArray(ref cth_Bezier);
                var data_Bounds = chunk.GetNativeArray(ref cth_Bounds);
                var data_Circle = chunk.GetNativeArray(ref cth_Circle);
                var data_Line = chunk.GetNativeArray(ref cth_Line);
                var data_Quad = chunk.GetNativeArray(ref cth_Quad);
                var data_Lines = chunk.GetBufferAccessor(ref bth_Lines);
                var data_DashedLines = chunk.GetBufferAccessor(ref bth_DashedLines);
                var data_Circles = chunk.GetBufferAccessor(ref bth_Circles);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var idx))
                {
                    MIO_Common common = data_CommonData[idx];

                    // Remember to disable Burst!
                    //QLog.Debug($"  DRAW  {data_Overlay[idx].m_Type}  owner:{common.m_Owner.DX()}, {common.m_Flags}/{common.m_Manipulation} Tool:{m_ToolFlags}");

                    if (common.m_Flags == InteractionFlags.None) continue;

                    switch (data_Overlay[idx].m_Type)
                    {
                        case OverlayTypes.Bounds:
                            DrawBounds(common, data_Bounds[idx].Bounds, Projection.Ground);
                            break;

                        case OverlayTypes.Circle:
                        case OverlayTypes.SelectionCenter:
                            DrawCircle(common, data_Circle[idx].Circle, Projection.Ground);
                            break;

                        case OverlayTypes.Line:
                            DrawLine(common, data_Line[idx].Line, Projection.Ground);
                            break;

                        case OverlayTypes.Marquee:
                            DrawQuad(common, data_Quad[idx].Quad, Projection.Ground);
                            break;


                        case OverlayTypes.MVBuilding:
                            RenderMVBuilding(
                                common,
                                data_Lines[idx],
                                data_DashedLines[idx]);
                            break;

                        case OverlayTypes.MVCircle:
                            RenderMVCircle(
                                common,
                                data_Circle[idx].Circle);
                            break;

                        case OverlayTypes.MVDecal:
                            RenderMVDecal(
                                common,
                                data_Lines[idx]);
                            break;

                        case OverlayTypes.MVNode:
                            RenderMVNode(
                                common,
                                data_Circle[idx].Circle,
                                data_Lines[idx],
                                data_Circles[idx]);
                            break;

                        case OverlayTypes.MVSegment:
                            RenderMVSegment(
                                common,
                                data_Bezier[idx],
                                data_Lines[idx],
                                data_Circles[idx]);
                            break;

                        case OverlayTypes.MVControlPoint:
                            RenderMVControlPoint(
                                common,
                                data_Circle[idx].Circle);
                            break;

                        case OverlayTypes.MVManipControlPoint:
                            RenderMVManipControlPoint(
                                common,
                                data_Circle[idx].Circle);
                            break;

                        case OverlayTypes.MVManipSegment:
                            RenderMVManipSegment(
                                common,
                                data_Lines[idx]);
                            break;

                        default:
                            break;
                    }
                }
            }

            // Render Moveables

            public readonly void RenderMVBuilding(MIO_Common common, DynamicBuffer<MIO_Lines> lines, DynamicBuffer<MIO_DashedLines> dashedLines)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    DrawLine(common, lines[i].Line, Projection.Ground, default, IsManipulating ? 0.25f : 1f);
                }
                if (IsManipulating) return;
                for (int i = 0; i < dashedLines.Length; i++)
                {
                    DrawTools.LineDashedSimple(m_OverlayRenderBuffer, common, dashedLines[i].Line, Projection.Ground);
                }
            }

            public readonly void RenderMVCircle(MIO_Common common, Circle3 circle)
            {
                if (IsManipulating)
                {
                    DrawCircle(common, circle, Projection.Fixed, default, 0.25f);
                    return;
                }

                if (common.ShowShadow) DrawCircle(common, circle, Projection.Ground, Colors.Get(ColorData.Contexts.Shadow), common.m_ShadowOpacity);
                DrawCircle(common, circle, Projection.Fixed);
            }

            public readonly void RenderMVDecal(MIO_Common common, DynamicBuffer<MIO_Lines> lines)
            {
                if (IsManipulating) return;
                for (int i = 0; i < lines.Length; i++)
                {
                    DrawLine(common, lines[i].Line, Projection.Fixed, default, IsManipulating ? 0.25f : 1f);
                    if (common.ShowShadow) DrawLine(common, lines[i].Line, Projection.Ground, Colors.Get(ColorData.Contexts.Shadow), common.m_ShadowOpacity * (IsManipulating ? 0.25f : 1f));
                }
            }

            public readonly void RenderMVNode(MIO_Common common, Circle3 circle, DynamicBuffer<MIO_Lines> lines, DynamicBuffer<MIO_Circles> cpPositions)
            {
                if (IsManipulating) return;
                //{
                //    DrawCircle(common, circle, Projection.Fixed, default, 0.25f);
                //    return;
                //}

                MIO_Common faded = common;

                if (common.ShowShadow) DrawCircle(common, circle, Projection.Ground, Colors.Get(ColorData.Contexts.Shadow), common.m_ShadowOpacity);

                faded.m_OutlineColor = Colors.Get(ColorData.Contexts.Background);
                faded.m_Flags = InteractionFlags.Static;
                DrawCircle(common, circle, Projection.Fixed);
                DrawLinesDashed(faded, lines, Projection.Fixed);
                DrawCircles(faded, cpPositions, Projection.Fixed);
            }

            public readonly void RenderMVSegment(MIO_Common common, MIO_Bezier bezier, DynamicBuffer<MIO_Lines> lines, DynamicBuffer<MIO_Circles> cpPositions)
            {
                if (IsManipulating) return;

                if (common.ShowShadow) DrawCurve(common, bezier.Curve, Projection.Ground, Colors.Get(ColorData.Contexts.Shadow), bezier.Width, common.m_ShadowOpacity);

                for (int i = 0; i < lines.Length; i++)
                {
                    DrawLineDashed(common, lines[i].Line, Projection.Fixed, Colors.Get(common, m_ToolFlags));
                }

                //common.m_OutlineColor = new(1f, 0f, 0f, 0.6f);
                //DrawTools.CircleSimple(m_OverlayRenderBuffer, common, cpPositions[0].Circle);
                //common.m_OutlineColor = new(0.75f, 0.75f, 0f, 0.6f);
                //DrawTools.CircleSimple(m_OverlayRenderBuffer, common, cpPositions[1].Circle);
                //common.m_OutlineColor = new(0f, 1f, 0f, 0.6f);
                //DrawTools.CircleSimple(m_OverlayRenderBuffer, common, cpPositions[2].Circle);
                //common.m_OutlineColor = new(0f, 0.75f, 0.75f, 0.6f);
                //DrawTools.CircleSimple(m_OverlayRenderBuffer, common, cpPositions[3].Circle);
            }

            public readonly void RenderMVControlPoint(MIO_Common common, Circle3 circle)
            {
                DrawCircle(common, circle, Projection.Fixed);
                if (common.ShowShadow) DrawDashedCircle(common, circle, Projection.Ground, Colors.Get(ColorData.Contexts.Shadow), default, common.m_ShadowOpacity);
            }


            public readonly void RenderMVManipControlPoint(MIO_Common common, Circle3 circle)
            {
                DrawCircle(common, circle, Projection.Fixed);
                if (common.ShowShadow) DrawDashedCircle(common, circle, Projection.Ground, Colors.Get(ColorData.Contexts.Shadow), default, common.m_ShadowOpacity);
            }

            public readonly void RenderMVManipSegment(MIO_Common common, DynamicBuffer<MIO_Lines> lines)
            {
                if (!IsManipulating) return;

                Color c = Colors.Get(common, m_ToolFlags);
                for (int i = 0; i < lines.Length; i++)
                {
                    DrawLineDashed(common, lines[i].Line, Projection.Fixed, c);
                }
            }


            // Simple draw calls

            public readonly void DrawCircle(MIO_Common common, Circle3 circle, Projection proj, Color color = default, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;
                DrawTools.CircleSimple(m_OverlayRenderBuffer, common, circle, proj);
            }

            public readonly void DrawDashedCircle(MIO_Common common, Circle3 circle, Projection proj, Color fgColor = default, Color bgColor = default, float opacity = 1f)
            {
                common.m_OutlineColor = fgColor.Equals(default) ? Colors.Get(common, m_ToolFlags) : fgColor;
                common.m_BackgroundColor = bgColor;
                common.m_OutlineColor.a *= opacity;
                DrawTools.CircleDashedSimple(m_OverlayRenderBuffer, common, circle, proj);
            }

            public readonly void DrawLine(MIO_Common common, Line3.Segment line, Projection proj, Color color = default, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;
                DrawTools.LineSimple(m_OverlayRenderBuffer, common, line, proj);
            }

            public readonly void DrawCurve(MIO_Common common, Bezier4x3 curve, Projection proj, Color color = default, float width = -1, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;
                DrawTools.CurveSimple(m_OverlayRenderBuffer, common, curve, proj, width);
            }

            public readonly void DrawLineDashed(MIO_Common common, Line3.Segment line, Projection proj, Color color = default, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;
                DrawTools.LineDashedSimple(m_OverlayRenderBuffer, common, line, proj);
            }

            public readonly void DrawLines(MIO_Common common, DynamicBuffer<MIO_Lines> lines, Projection proj, Color color = default, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;

                for (int i = 0; i < lines.Length; i++)
                {
                    DrawTools.LineSimple(m_OverlayRenderBuffer, common, lines[i].Line, proj);
                }
            }

            public readonly void DrawBounds(MIO_Common common, Bounds3 bounds, Projection proj, Color color = default, float opacity = 1f)
            {
                DrawQuad(common, bounds.ToQuad3(), proj, color, opacity);
            }

            public readonly void DrawQuad(MIO_Common common, Quad3 quad, Projection proj, Color color = default, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;

                DrawTools.LineSimple(m_OverlayRenderBuffer, common, quad.ab, proj); // Left
                DrawTools.LineSimple(m_OverlayRenderBuffer, common, quad.cd, proj); // Right
                DrawTools.LineSimple(m_OverlayRenderBuffer, common, quad.ad, proj); // Top
                DrawTools.LineSimple(m_OverlayRenderBuffer, common, quad.bc, proj); // Bottom
            }

            public readonly void DrawLinesDashed(MIO_Common common, DynamicBuffer<MIO_Lines> lines, Projection proj, Color color = default, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;

                for (int i = 0; i < lines.Length; i++)
                {
                    DrawTools.LineDashedSimple(m_OverlayRenderBuffer, common, lines[i].Line, proj);
                }
            }

            public readonly void DrawCircles(MIO_Common common, DynamicBuffer<MIO_Circles> circles, Projection proj, Color color = default, float opacity = 1f)
            {
                common.m_OutlineColor = color.Equals(default) ? Colors.Get(common, m_ToolFlags) : color;
                common.m_OutlineColor.a *= opacity;

                for (int i = 0; i < circles.Length; i++)
                {
                    DrawTools.CircleSimple(m_OverlayRenderBuffer, common, circles[i].Circle, proj);
                }
            }
        }
    }
}
