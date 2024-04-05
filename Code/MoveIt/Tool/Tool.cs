using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Rendering;
using Game.Tools;
using MoveIt.Actions;
using MoveIt.Overlays;
using QCommonLib;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            m_ToolRaycastSystem.collisionMask = (CollisionMask.OnGround | CollisionMask.Overground | CollisionMask.ExclusiveGround);
            m_ToolRaycastSystem.typeMask = (TypeMask.StaticObjects | TypeMask.Net | TypeMask.Areas | TypeMask.Terrain);// | TypeMask.MovingObjects);
            m_ToolRaycastSystem.raycastFlags = (RaycastFlags)0U;// (RaycastFlags.SubElements | RaycastFlags.Decals | RaycastFlags.Placeholders | RaycastFlags.UpgradeIsMain | RaycastFlags.Outside | RaycastFlags.Cargo | RaycastFlags.Passenger);
            m_ToolRaycastSystem.netLayerMask = (Layer.Road | Layer.Fence | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.Pathway);
            m_ToolRaycastSystem.iconLayerMask = Game.Notifications.IconLayerMask.None;

            m_RaycastTerrain = new RaycastTerrain(World);
        }

        internal float GetTerrainHeight(float3 position)
        {
            m_TerrainSystem.AddCPUHeightReader(m_InputDeps);
            Game.Simulation.TerrainHeightData heightData = m_TerrainSystem.GetHeightData(false);
            return Game.Simulation.TerrainUtils.SampleHeight(ref heightData, position);
        }

        internal void SetSelectionMode(bool? toMarquee = null)
        {
            if (toMarquee is null)
            {
                m_MarqueeSelect = !m_MarqueeSelect;
            }
            else
            {
                m_MarqueeSelect = (bool)toMarquee;
            }

            SetManipulationMode(false);
            
            SetModesTooltip();
        }

        internal void SetManipulationMode(bool? toManipulate = null)
        {
            if (_IsManipulateMode == toManipulate) return;

            if (toManipulate is null)
            {
                _IsManipulateMode = !_IsManipulateMode;
            }
            else
            {
                _IsManipulateMode = (bool)toManipulate;
            }

            SetModesTooltip();
        }

        internal void SetModesTooltip()
        {
            string mode;

            if (_IsManipulateMode)
            {
                mode = "Manipulation";
            }
            else
            {
                if (m_MarqueeSelect)
                {
                    mode = "Marquee";
                }
                else
                {
                    mode = "Single";
                }
            }

            MIT_ToolTipSystem.instance.Set($"Mode: {mode}", 1.5f);
        }

        internal void StartMove()
        {
            if (ToolState == ToolStates.SecondaryButtonHeld) return;
            TransformAction action;
            if (ActiveSelection.Has(Hover.OnPress))
            {
                action = new TransformAction();// Queue.Current as TransformAction;
                Queue.Push(action);
            }
            else
            {
                // Requires OnHold to have fired, causing a 250ms delay
                Queue.Push(new SelectAction());
                ((SelectAction)Queue.Current).Do();

                action = new TransformAction();
                Queue.Push(action);
            }
            ToolState = ToolStates.ApplyButtonHeld;
            StartTransform();
        }

        internal void StartRotation()
        {
            if (ToolState == ToolStates.ApplyButtonHeld) return;
            TransformAction action;
            action = new TransformAction();
            Queue.Push(action);
            ToolState = ToolStates.SecondaryButtonHeld;

            StartTransform();
        }

        private void StartTransform()
        {
            m_DragPointerOffsetFromSelection = ActiveSelection.Center - m_PointerPos;
            CreationPhase = CreationPhases.Positioning;
        }

        internal void EndMove()
        {
            EndTransform();
        }

        internal void EndRotation()
        {
            EndTransform();
        }

        internal void EndTransform()
        {
            if (Queue.Current is TransformAction action)
            {
                action.OnHoldEnd();
            }
            else
            {
                throw new System.Exception($"In EndTransform, action is {Queue.Current.Name} not TransformAction");
            }
            ToolState = ToolStates.Default;
        }

        internal static float GetDistanceBetween2D(Moveables.Moveable a, Moveables.Moveable b)
        {
            float3 posA = a.Transform.m_Position;
            float3 posB = b.Transform.m_Position;
            return posA.DistanceXZ(posB);
        }

        //internal static HashSet<Bounds2> MergeBounds(HashSet<Bounds2> outerList)
        //{
        //    HashSet<Bounds2> innerList = new();
        //    HashSet<Bounds2> newList = new();
        //    int c = 0;
        //    int originalCount = outerList.Count;

        //    do
        //    {
        //        foreach (Bounds2 outer in outerList)
        //        {
        //            m_Instance.AddDebugBounds(outer);

        //            bool merged = false;

        //            float2 outerSize = outer.Size();
        //            float outerArea = outerSize.x * outerSize.y;
        //            foreach (Bounds2 inner in innerList)
        //            {
        //                float2 innerSize = inner.Size();
        //                float separateArea = (innerSize.x * innerSize.y) + outerArea;

        //                Bounds2 encapsulated = inner;
        //                encapsulated = encapsulated.Encapsulate(outer);
        //                float2 encapSize = encapsulated.Size();
        //                float encapsulateArea = encapSize.x * encapSize.y;

        //                if (!merged && encapsulateArea < separateArea)
        //                {
        //                    newList.Add(encapsulated);
        //                    merged = true;
        //                }
        //                else
        //                {
        //                    newList.Add(inner);
        //                }
        //            }
        //            if (!merged)
        //            {
        //                newList.Add(outer);
        //            }

        //            innerList = new HashSet<Bounds2>(newList);
        //            newList.Clear();
        //        }

        //        if (outerList.Count <= innerList.Count)
        //        {
        //            break;
        //        }
        //        outerList = new HashSet<Bounds2>(innerList);
        //        innerList.Clear();

        //        if (c > 1000)
        //        {
        //            Log.Error($"Looped bounds-merge a thousand times", "[M04]");
        //            break;
        //        }

        //        c++;
        //    }
        //    while (true);

        //    foreach (Bounds2 b in innerList)
        //    {
        //        m_Instance.AddDebugBounds(b, new Color(255, 0, 0, 200));
        //    }
        //    QLog.Debug($"\nStart:{originalCount}\nInner:{innerList.Count}");
        //    return innerList;
        //}

        #region Debug lines
        internal void AddDebugLine(Line3.Segment line, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            AddDebugLine(line, GetRandomColor(), projection);
        }

        internal void AddDebugLine(Line3.Segment line, Color color, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            Utils.Line overlay = new(line, color, projection);
            m_OverlaySystem.DebugAdd(overlay);
        }

        internal void AddDebugBounds2(Bounds2 b, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            Bounds3 bounds = new(new(b.min.x, float.MinValue, b.min.y), new(b.max.x, float.MaxValue, b.max.y));
            AddDebugBounds(bounds, GetRandomColor(), projection);
        }

        internal void AddDebugBounds2(Bounds2 b, Color color, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            Bounds3 bounds = new(new(b.min.x, float.MinValue, b.min.y), new(b.max.x, float.MaxValue, b.max.y));
            AddDebugBounds(bounds, color, projection);
        }

        internal void AddDebugBounds(Bounds3 bounds, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            AddDebugBounds(bounds, GetRandomColor(), projection);
        }

        internal void AddDebugBounds(Bounds3 bounds, Color color, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            Utils.Bounds overlay = new(bounds, color, projection);
            m_OverlaySystem.DebugAdd(overlay);
        }

        internal void AddDebugRectangle(float4 area, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            AddDebugRectangle(area, GetRandomColor(), projection);
        }

        internal void AddDebugRectangle(float4 area, Color color, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            Utils.Rectangle overlay = new(area, color, projection);
            m_OverlaySystem.DebugAdd(overlay);
        }

        internal void AddDebugCircle(float3 position, float diameter, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            AddDebugCircle(position, diameter, GetRandomColor(), projection);
        }

        internal void AddDebugCircle(float3 position, float diameter, Color color, OverlayRenderSystem.StyleFlags projection = OverlayRenderSystem.StyleFlags.Projected)
        {
            Utils.Circle overlay = new(position, diameter, color, projection);
            m_OverlaySystem.DebugAdd(overlay);
        }

        internal void ClearDebugOverlays()
        {
            m_OverlaySystem.DebugClear();
        }

        private Color GetRandomColor()
        {
            return new(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0.6f, 1f));
        }
        #endregion

        public override string toolID => "MoveItTool";
        public override Game.Prefabs.PrefabBase GetPrefab() => null;
        public override bool TrySetPrefab(Game.Prefabs.PrefabBase prefab) => false;
    }
}
