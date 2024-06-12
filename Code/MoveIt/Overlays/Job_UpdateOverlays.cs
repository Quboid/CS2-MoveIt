using Colossal.Mathematics;
using MoveIt.Tool;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    internal partial class MIT_OverlaySystem
    {
#if USE_BURST
        [BurstCompile]
#endif
        protected struct UpdateOverlaysJob : IJobChunk
        {
            [ReadOnly] public ToolFlags m_ToolFlags;
            [ReadOnly] public bool m_IsManipMode;
            [ReadOnly] public float3 m_CameraPosition;
            [ReadOnly] public ComponentTypeHandle<MIO_Type>                         cth_Overlay;

            public ComponentTypeHandle<MIO_Common>                                  cth_CommonData;
            public ComponentTypeHandle<MIO_Circle>                                  cth_Circle;
            public ComponentTypeHandle<MIO_SelectionData>                           cth_SelectionData;
            public ComponentTypeHandle<MIO_Quad>                                    cth_Quad;
            public BufferTypeHandle<MIO_Circles>                                    bth_Circles;
            public BufferTypeHandle<MIO_Lines>                                      bth_Lines;
            public BufferTypeHandle<MIO_DashedLines>                                bth_DashedLines;

            [ReadOnly] public ComponentLookup<Game.Prefabs.BuildingData>            clu_BuildingData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.BuildingExtensionData>   clu_BuildingExtensionData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.ObjectGeometryData>      clu_ObjectGeometryData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.PrefabRef>               clu_PrefabRefs;

            public readonly bool IsManipulating => (m_ToolFlags & ToolFlags.ManipulationMode) > 0;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var data_Overlay        = chunk.GetNativeArray(ref cth_Overlay);
                var data_CommonData     = chunk.GetNativeArray(ref cth_CommonData);
                var data_Circle         = chunk.GetNativeArray(ref cth_Circle);
                var data_SelectionData  = chunk.GetNativeArray(ref cth_SelectionData);
                var data_Quad           = chunk.GetNativeArray(ref cth_Quad);
                var data_Circles        = chunk.GetBufferAccessor(ref bth_Circles);
                var data_Lines          = chunk.GetBufferAccessor(ref bth_Lines);
                var data_DashedLines    = chunk.GetBufferAccessor(ref bth_DashedLines);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var idx))
                {
                    if (data_Overlay[idx].m_Type == OverlayTypes.SelectionCenter)
                    {
                        EarlyUpdateSelectionCenter(data_CommonData, idx, data_Circle, data_SelectionData[idx]);
                    }

                    MIO_Common common = data_CommonData[idx];

                    // Remember to disable Burst!
                    //QLog.Debug($"UPDATE  {data_Overlay[idx].m_Type}  owner:{common.m_Owner.DX()}, {common.m_Flags}/{common.m_Manipulation} Tool:{m_ToolFlags}");

                    if (common.m_Flags == InteractionFlags.None) continue;
                    CalculateLines(ref common, m_CameraPosition);
                    data_CommonData[idx] = common;

                    switch (data_Overlay[idx].m_Type)
                    {
                        case OverlayTypes.Marquee:
                            UpdateMarquee(
                                data_CommonData,
                                idx,
                                data_Quad[idx]);
                            break;

                        case OverlayTypes.MVBuilding:
                            UpdateMVBuilding(
                                common,
                                data_Lines[idx],
                                data_DashedLines[idx]);
                            break;

                        case OverlayTypes.MVControlPoint:
                            UpdateMVControlPoint(
                                data_CommonData,
                                idx);
                            break;

                        case OverlayTypes.MVManipControlPoint:
                            UpdateMVManipControlPoint(
                                data_CommonData,
                                idx);
                            break;

                        case OverlayTypes.MVDecal:
                            UpdateMVDecal(
                                common,
                                data_Lines[idx]);
                            break;

                        case OverlayTypes.SelectionCenter:
                            UpdateSelectionCenter(
                                data_CommonData,
                                idx,
                                data_Circle,
                                data_SelectionData[idx]);
                            break;

                        default:
                            break;
                    }
                }
            }

            private readonly void CalculateLines(ref MIO_Common common, float3 cameraPosition)
            {
                float dist = math.clamp(math.distance(common.m_Transform.m_Position, cameraPosition), 0, Overlay.LINE_MAX_DISTANCE) / Overlay.LINE_MAX_DISTANCE;
                common.m_OutlineWidthFixed = Overlay.LINE_MIN_WIDTH + (Overlay.LINE_MAX_WIDTH - Overlay.LINE_MIN_WIDTH) * dist;

                float3 groundPos = common.m_Transform.m_Position;
                groundPos.y = common.m_TerrainHeight;
                dist = math.clamp(math.distance(groundPos, cameraPosition), 0, Overlay.LINE_MAX_DISTANCE) / Overlay.LINE_MAX_DISTANCE;
                common.m_OutlineWidthGround = Overlay.LINE_MIN_WIDTH + (Overlay.LINE_MAX_WIDTH - Overlay.LINE_MIN_WIDTH) * dist;

                common.m_OutlineColor = Colors.Get(common, m_ToolFlags);
            }


            public readonly void UpdateMVBuilding(MIO_Common common, DynamicBuffer<MIO_Lines> lines, DynamicBuffer<MIO_DashedLines> dashedLines)
            {
                float3 lotHalfSize;
                Game.Prefabs.PrefabRef prefab = clu_PrefabRefs[common.m_Owner];
                if (clu_BuildingExtensionData.HasComponent(prefab))
                {
                    Game.Prefabs.BuildingExtensionData buildingData = clu_BuildingExtensionData[prefab];
                    lotHalfSize = new(buildingData.m_LotSize.x * 4, 0f, buildingData.m_LotSize.y * 4);
                }
                else
                {
                    Game.Prefabs.BuildingData buildingData = clu_BuildingData[prefab];
                    lotHalfSize = new(buildingData.m_LotSize.x * 4, 0f, buildingData.m_LotSize.y * 4);
                }

                DrawTools.CalculateBuildingRectangleLines(common.m_Transform, common.m_OutlineWidthGround, lotHalfSize, ref lines, ref dashedLines);
            }

            public readonly void UpdateMVControlPoint(NativeArray<MIO_Common> data_Common, int idx)
            {
                MIO_Common common = data_Common[idx];
                ColorData.Contexts context = ColorData.Contexts.None;

                if ((common.m_Flags & InteractionFlags.ParentHovering) > 0)             context = ColorData.Contexts.Hovering;
                else if ((common.m_Flags & InteractionFlags.ParentSelected) > 0)        context = ColorData.Contexts.Selected;

                if (context == ColorData.Contexts.None) common.m_Flags = InteractionFlags.None;
                else common.m_OutlineColor = Colors.Get(context);

                data_Common[idx] = common;

                //QLog.Bundle($"[{common.m_Owner.D()}n]", $"{common.m_Flags} Tool:{m_ToolFlags} Context:{context} Manip:{common.m_Manipulation}");
            }

            public readonly void UpdateMVManipControlPoint(NativeArray<MIO_Common> data_Common, int idx)
            {
                MIO_Common common = data_Common[idx];
                ColorData.Contexts context = ColorData.Contexts.None;

                if ((common.m_Flags & InteractionFlags.Hovering) > 0)                   context = ColorData.Contexts.ManipChildHovering;
                else if ((common.m_Flags & InteractionFlags.Selected) > 0)              context = ColorData.Contexts.ManipChildSelected;
                else if ((common.m_Flags & InteractionFlags.ParentHovering) > 0)        context = ColorData.Contexts.ManipParentHovering;
                else if ((common.m_Flags & InteractionFlags.ParentManipulating) > 0)    context = ColorData.Contexts.ManipParentSelected;

                if (context == ColorData.Contexts.None) common.m_Flags = InteractionFlags.None;
                else common.m_OutlineColor = Colors.Get(context);

                data_Common[idx] = common;

                //QLog.Bundle($"[{common.m_Owner.DX()}M]", $"{common.m_Flags} Tool:{m_ToolFlags} Context:{context} Manip:{common.m_Manipulation}");
            }

            public readonly void UpdateMVDecal(MIO_Common common, DynamicBuffer<MIO_Lines> lines)
            {
                Game.Prefabs.PrefabRef prefab = clu_PrefabRefs[common.m_Owner];
                Game.Prefabs.ObjectGeometryData geoData = clu_ObjectGeometryData[prefab];
                float3 lotHalfSize = new(geoData.m_Size.x * 0.5f, 0f, geoData.m_Size.z * 0.5f);

                DrawTools.CalculateRectangleLines(common.m_Transform, common.m_OutlineWidthGround, lotHalfSize, ref lines);
            }

            public readonly void UpdateMarquee(NativeArray<MIO_Common> data_Common, int idx, MIO_Quad quadComp)
            {
                Quad3 quad = quadComp.Quad;

                MIO_Common common = data_Common[idx];
                float3 center = (quad.a + quad.b + quad.c + quad.d) / 4;
                common.m_Transform = new(center, quaternion.identity);
                common.m_TerrainHeight = math.max(math.max(math.max(quad.a.y, quad.b.y), quad.c.y), quad.d.y);
                data_Common[idx] = common;
            }

            public readonly void EarlyUpdateSelectionCenter(NativeArray<MIO_Common> data_Common, int idx, NativeArray<MIO_Circle> data_Circle, MIO_SelectionData selectionData)
            {
                MIO_Common common = data_Common[idx];
                MIO_Circle circle = data_Circle[idx];

                circle.Circle.position = selectionData.m_Position;
                common.m_Transform = new(circle.Circle.position, default);
                common.m_TerrainHeight = selectionData.m_TerrainHeight;
                common.m_Flags = InteractionFlags.Static;

                // Remember to disable burst!
                //QLog.Bundle("EUSelC", $"isManip:{isManip} pos:{circle.Circle.position.DX()} flags:{common.m_Flags} none:{(isManip && selectionData.m_ManipCount == 0) || (!isManip && selectionData.m_NormalCount == 0)}");

                if (selectionData.m_Count == 0)
                {
                    common.m_Flags = InteractionFlags.None;
                }

                data_Common[idx] = common;
                data_Circle[idx] = circle;
            }

            public readonly void UpdateSelectionCenter(NativeArray<MIO_Common> data_Common, int idx, NativeArray<MIO_Circle> data_Circle, MIO_SelectionData selectionData)
            {
                MIO_Common common = data_Common[idx];
                MIO_Circle circle = data_Circle[idx];

                circle.Circle.radius = (common.m_OutlineWidthGround / Overlay.LINE_DEFAULT_WIDTH) * (Overlay.SELECT_SCALE_MULTIPLYER) + Overlay.SELECT_BASE_RADIUS;
                data_Circle[idx] = circle;

                if (m_IsManipMode)
                {
                    // The current action is in Manipulation Mode
                    common.m_OutlineColor = Colors.Get(ColorData.Contexts.ManipChildSelected);
                    common.m_IsManipulatable = true;
                }
                else if (IsManipulating)
                {
                    // We are in Normal Mode, but showing Manipulation Mode (player is holding Alt)
                    common.m_Flags = InteractionFlags.None;
                }
                else
                {
                    // We are in Normal Mode
                    common.m_OutlineColor = Colors.Get(ColorData.Contexts.Selected);
                    common.m_IsManipulatable = false;
                }
                data_Common[idx] = common;
            }
        }
    }
}
