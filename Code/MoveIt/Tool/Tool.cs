using Game.Common;
using Game.Net;
using Game.Tools;
using MoveIt.Actions;
using MoveIt.Overlays;
using QCommonLib;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            m_ToolRaycastSystem.collisionMask   = CollisionMask.OnGround | CollisionMask.Overground;
            m_ToolRaycastSystem.areaTypeMask    = Game.Areas.AreaTypeMask.None;
            m_ToolRaycastSystem.typeMask        = TypeMask.Net;
            m_ToolRaycastSystem.raycastFlags    = 0f;
            m_ToolRaycastSystem.netLayerMask    = Layer.Road | Layer.Fence | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.Pathway | Layer.LaneEditor;
            m_ToolRaycastSystem.iconLayerMask   = Game.Notifications.IconLayerMask.None;
            m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.None;

            m_RaycastTerrain = new RaycastTerrain(World);
            m_RaycastSurface = new RaycastSurface(World);
        }

        /// <summary>
        /// Handle when the pointer leaves the UI; wait a few frames before reactiving overlays and mouse buttons
        /// </summary>
        private void UpdateUIHasFocus()
        {
            bool hit = (m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) != 0;

            if (!hit && _UIHasFocusStep == 0) return;

            if (hit)
            {
                _UIHasFocusStep = 3;
            }
            else
            {
                // Focus left UI recently
                _UIHasFocusStep = (short)math.max(0, _UIHasFocusStep - 1);
            }
        }

        internal float GetTerrainHeight(float3 position)
        {
            m_TerrainSystem.AddCPUHeightReader(m_InputDeps);
            Game.Simulation.TerrainHeightData heightData = m_TerrainSystem.GetHeightData(false);
            return Game.Simulation.TerrainUtils.SampleHeight(ref heightData, position);
        }

        internal void QueueOverlayUpdate(Overlay overlay)
        {
            m_PostToolSystem.QueueOverlayUpdate(overlay);
        }

        internal void QueueOverlayUpdateDeferred(Overlay overlay)
        {
            m_PostToolSystem.QueueOverlayUpdateDeferred(overlay);
        }

        internal void ToggleSelectionMode() => SetSelectionMode(!m_MarqueeSelect);

        internal void SetSelectionMode(bool toMarquee)
        {
            m_MarqueeSelect = toMarquee;

            SetManipulationMode(false);
            SetModesTooltip();
        }

        internal void ToggleManipulationMode() => SetManipulationMode(!m_IsManipulateMode);

        internal void SetManipulationMode(bool toManipulate)
        {
            if (m_IsManipulateMode == toManipulate) return;

            Queue.Push(new ModeSwitchAction());
            Queue.Do();
        }

        internal void SetModesTooltip()
        {
            string mode;

            if (m_IsManipulateMode)
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

            MIT_ToolTipSystem.instance.Set($"Mode: {mode}", 1.25f);
        }

        internal void MoveStart()
        {
            if (ToolState == ToolStates.SecondaryButtonHeld) return;
            TransformAction action;
            if (Selection.Has(Hover.OnPress))
            {
                action = new TransformAction();
                Queue.Push(action);
            }
            else
            {
                // Requires OnHold to have fired, causing a 250ms delay
                Queue.Push(new SelectAction());
                Queue.Current.Do();

                action = new TransformAction();
                Queue.Push(action);
            }
            ToolState = ToolStates.ApplyButtonHeld;
            TransformStart();
        }

        internal void RotationStart()
        {
            if (ToolState == ToolStates.ApplyButtonHeld) return;
            TransformAction action;
            action = new TransformAction();
            Queue.Push(action);
            ToolState = ToolStates.SecondaryButtonHeld;

            TransformStart();
        }

        private void TransformStart()
        {
            m_DragPointerOffsetFromSelection = Selection.Center - m_PointerPos;
            CreationPhase = CreationPhases.Positioning;
        }

        internal void EndMove()
        {
            TransformEnd();
        }

        internal void RotationEnd()
        {
            TransformEnd();
        }

        internal void TransformEnd()
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

        //internal void DejankNodes()
        //{
        //    bool previousFailed = false;
        //    string msg = $"Dejanking selection ({Selection.Count})";
        //    foreach (MVDefinition mvd in Selection.Definitions)
        //    {
        //        if (previousFailed) msg += " No, skipping Moveable.";
        //        previousFailed = true;

        //        msg += $"\n   Check {mvd.m_Entity}, node?";
        //        if (mvd.m_Identity != Identity.Node) continue;
        //        msg += $" yes, 2 segments?";
        //        MVNode node = Moveables.GetOrCreate<MVNode>(mvd);
        //        if (node.m_Segments.Count != 2) continue;
        //        msg += $" yes, same prefab?";
        //        KeyValuePair<Entity, bool> segAPair = node.m_Segments.ElementAt(0);
        //        Entity segAEntity = segAPair.Key;
        //        bool segAIsStart = segAPair.Value;
        //        KeyValuePair<Entity, bool> segBPair = node.m_Segments.ElementAt(1);
        //        Entity segBEntity = segBPair.Key;
        //        bool segBIsStart = segBPair.Value;
        //        Entity segAPrefab = EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(segAEntity).m_Prefab;
        //        Entity segBPrefab = EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(segBEntity).m_Prefab;
        //        if (!segAPrefab.Equals(segBPrefab)) continue;
        //        msg += $" yes, straight?";

        //        Curve segACurve = EntityManager.GetComponentData<Curve>(segAEntity);
        //        Curve segBCurve = EntityManager.GetComponentData<Curve>(segBEntity);
        //        Bezier4x3 segABezier = segACurve.m_Bezier;
        //        Bezier4x3 segBBezier = segBCurve.m_Bezier;
        //        Bezier4x3 smooth3D = new(
        //            segAIsStart ? segABezier.b : segABezier.c, segAIsStart ? segABezier.a : segABezier.d,
        //            segBIsStart ? segBBezier.a : segBBezier.d, segBIsStart ? segBBezier.b : segBBezier.c);
        //        Bezier4x2 smooth = new(
        //            segAIsStart ? segABezier.b.XZ() : segABezier.c.XZ(), segAIsStart ? segABezier.a.XZ() : segABezier.d.XZ(), 
        //            segBIsStart ? segBBezier.a.XZ() : segBBezier.d.XZ(), segBIsStart ? segBBezier.b.XZ() : segBBezier.c.XZ());
        //        MathUtils.Distance(new Line2(smooth.a, smooth.d), smooth.b, out float segAT);
        //        MathUtils.Distance(new Line2(smooth.a, smooth.d), smooth.c, out float segBT);
        //        MathUtils.Distance(smooth, smooth.b, out float segATx);
        //        MathUtils.Distance(smooth, smooth.c, out float segBTx);

        //        QLog.Debug($"Curve {segAT}, {segBT};   {segATx}, {segBTx}\n    {smooth3D.a.D()};  {smooth3D.b.D()};  {smooth3D.c.D()};  {smooth3D.d.D()}");

        //        segATx -= segAT;
        //        segBTx -= segBT;
        //        if ((segATx < -0.05 || segATx > 0.05) || (segBTx < -0.05 || segBTx > 0.05)) continue;
        //        msg += $" yes, attempting dejank;";

        //        float3 mag = smooth3D.d - smooth3D.a;
        //        float3 segAOffset = math.lerp(smooth3D.a, smooth3D.d, segAT);
        //        float3 segBOffset = math.lerp(smooth3D.a, smooth3D.d, segBT);
        //        if (segAIsStart) segABezier.a = segAOffset;
        //        else segABezier.d = segAOffset;
        //        if (segBIsStart) segBBezier.a = segBOffset;
        //        else segBBezier.d = segBOffset;

        //        segACurve.m_Bezier = segABezier;
        //        segBCurve.m_Bezier = segBBezier;
        //        EntityManager.SetComponentData(segAEntity, segACurve);
        //        EntityManager.SetComponentData(segBEntity, segBCurve);

        //        EdgeGeometry edge = EntityManager.GetComponentData<EdgeGeometry>(segAEntity);
        //        if (segAIsStart)
        //        {
        //            StartNodeGeometry nodeGeo = EntityManager.GetComponentData<StartNodeGeometry>(segAEntity);
        //            EdgeNodeGeometry geo = nodeGeo.m_Geometry;

        //            Segment segL = geo.m_Left;
        //            segL.m_Left = new(segL.m_Left.d, segL.m_Left.d, segL.m_Left.d, segL.m_Left.d);
        //            segL.m_Right = new(segL.m_Right.d, segL.m_Right.d, segL.m_Right.d, segL.m_Right.d);
        //            float3 a = segL.m_Left.a;
        //            edge.m_Start.m_Right.a = a; // Start node is Left-Right
        //            segL.m_Length = new(0f, 0f);
        //            geo.m_Left = segL;

        //            Segment segR = geo.m_Right;
        //            segR.m_Left = new(segR.m_Left.d, segR.m_Left.d, segR.m_Left.d, segR.m_Left.d);
        //            segR.m_Right = new(segR.m_Right.d, segR.m_Right.d, segR.m_Right.d, segR.m_Right.d);
        //            float3 b = segR.m_Right.a;
        //            edge.m_Start.m_Left.a = b;
        //            segR.m_Length = new(0f, 0f);
        //            geo.m_Right = segL;

        //            geo.m_Middle = new(geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d);
        //            geo.m_Bounds = new(new float3(math.min(a.x, b.x), math.min(a.y, b.y), math.min(a.z, b.z)), new float3(math.max(a.x, b.x), math.max(a.y, b.y), math.max(a.z, b.z)));
        //            nodeGeo.m_Geometry = geo;
        //            EntityManager.SetComponentData(segAEntity, nodeGeo);
        //        }
        //        else
        //        {
        //            EndNodeGeometry nodeGeo = EntityManager.GetComponentData<EndNodeGeometry>(segAEntity);
        //            EdgeNodeGeometry geo = nodeGeo.m_Geometry;

        //            Segment segL = geo.m_Left;
        //            segL.m_Left = new(segL.m_Left.d, segL.m_Left.d, segL.m_Left.d, segL.m_Left.d);
        //            segL.m_Right = new(segL.m_Right.d, segL.m_Right.d, segL.m_Right.d, segL.m_Right.d);
        //            float3 a = segL.m_Left.a;
        //            edge.m_End.m_Left.d = a; // End node is Left-Left
        //            segL.m_Length = new(0f, 0f);
        //            geo.m_Left = segL;

        //            Segment segR = geo.m_Right;
        //            segR.m_Left = new(segR.m_Left.d, segR.m_Left.d, segR.m_Left.d, segR.m_Left.d);
        //            segR.m_Right = new(segR.m_Right.d, segR.m_Right.d, segR.m_Right.d, segR.m_Right.d);
        //            float3 b = segR.m_Right.a;
        //            edge.m_End.m_Right.d = b;
        //            segR.m_Length = new(0f, 0f);
        //            geo.m_Right = segL;

        //            geo.m_Middle = new(geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d);
        //            geo.m_Bounds = new(new float3(math.min(a.x, b.x), math.min(a.y, b.y), math.min(a.z, b.z)), new float3(math.max(a.x, b.x), math.max(a.y, b.y), math.max(a.z, b.z)));
        //            nodeGeo.m_Geometry = geo;
        //            EntityManager.SetComponentData(segAEntity, nodeGeo);
        //        }
        //        EntityManager.SetComponentData(segAEntity, edge);
        //        EntityManager.AddComponent<BatchesUpdated>(segAEntity);
        //        msg += $" A done,";

        //        edge = EntityManager.GetComponentData<EdgeGeometry>(segBEntity);
        //        if (segBIsStart)
        //        {
        //            StartNodeGeometry nodeGeo = EntityManager.GetComponentData<StartNodeGeometry>(segBEntity);
        //            EdgeNodeGeometry geo = nodeGeo.m_Geometry;

        //            Segment segL = geo.m_Left;
        //            segL.m_Left = new(segL.m_Left.d, segL.m_Left.d, segL.m_Left.d, segL.m_Left.d);
        //            segL.m_Right = new(segL.m_Right.d, segL.m_Right.d, segL.m_Right.d, segL.m_Right.d);
        //            float3 a = segL.m_Left.a;
        //            edge.m_Start.m_Right.a = a; // Start node is Left-Right
        //            segL.m_Length = new(0f, 0f);
        //            geo.m_Left = segL;

        //            Segment segR = geo.m_Right;
        //            segR.m_Left = new(segR.m_Left.d, segR.m_Left.d, segR.m_Left.d, segR.m_Left.d);
        //            segR.m_Right = new(segR.m_Right.d, segR.m_Right.d, segR.m_Right.d, segR.m_Right.d);
        //            float3 b = segR.m_Right.a;
        //            edge.m_Start.m_Left.a = b;
        //            segR.m_Length = new(0f, 0f);
        //            geo.m_Right = segR;

        //            geo.m_Middle = new(geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d);
        //            geo.m_Bounds = new(new float3(math.min(a.x, b.x), math.min(a.y, b.y), math.min(a.z, b.z)), new float3(math.max(a.x, b.x), math.max(a.y, b.y), math.max(a.z, b.z)));
        //            nodeGeo.m_Geometry = geo;
        //            EntityManager.SetComponentData(segBEntity, nodeGeo);
        //        }
        //        else
        //        {
        //            EndNodeGeometry nodeGeo = EntityManager.GetComponentData<EndNodeGeometry>(segBEntity);
        //            EdgeNodeGeometry geo = nodeGeo.m_Geometry;

        //            Segment segL = geo.m_Left;
        //            segL.m_Left = new(segL.m_Left.d, segL.m_Left.d, segL.m_Left.d, segL.m_Left.d);
        //            segL.m_Right = new(segL.m_Right.d, segL.m_Right.d, segL.m_Right.d, segL.m_Right.d);
        //            float3 a = segL.m_Left.a;
        //            edge.m_End.m_Left.d = a; // End node is Left-Left
        //            segL.m_Length = new(0f, 0f);
        //            geo.m_Left = segL;

        //            Segment segR = geo.m_Right;
        //            segR.m_Left = new(segR.m_Left.d, segR.m_Left.d, segR.m_Left.d, segR.m_Left.d);
        //            segR.m_Right = new(segR.m_Right.d, segR.m_Right.d, segR.m_Right.d, segR.m_Right.d);
        //            float3 b = segR.m_Right.a;
        //            edge.m_End.m_Right.d = b;
        //            segR.m_Length = new(0f, 0f);
        //            geo.m_Right = segR;

        //            geo.m_Middle = new(geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d, geo.m_Middle.d);
        //            geo.m_Bounds = new(new float3(math.min(a.x, b.x), math.min(a.y, b.y), math.min(a.z, b.z)), new float3(math.max(a.x, b.x), math.max(a.y, b.y), math.max(a.z, b.z)));
        //            nodeGeo.m_Geometry = geo;
        //            EntityManager.SetComponentData(segBEntity, nodeGeo);
        //        }
        //        EntityManager.SetComponentData(segBEntity, edge);
        //        EntityManager.AddComponent<BatchesUpdated>(segBEntity);
        //        msg += $" B done!";

        //        EntityManager.AddComponent<BatchesUpdated>(node.m_Entity);
        //        previousFailed = false;
        //    }
        //    if (previousFailed) msg += " No, skipping Moveable.";
        //    Log.Debug(msg);
        //}

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

        public override string toolID => "MoveItTool";
        public override Game.Prefabs.PrefabBase GetPrefab() => null;
        public override bool TrySetPrefab(Game.Prefabs.PrefabBase prefab) => false;
    }
}
