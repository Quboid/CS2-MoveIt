using Colossal.Entities;
using Colossal.Mathematics;
using MoveIt.Managers;
using MoveIt.Moveables;
using MoveIt.Overlays.DebugOverlays;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Actions.Transform
{
    internal abstract class TransformToolbox : TransformBase
    {
        public Moveable Moveable { get; set; }
    }

    internal abstract class TransformBase : Action
    {
        public override string Name => "TransformBase";
        internal bool m_HasMovedAction = false;

        internal Snapper.Snapper m_Snapper;

        internal TransformState m_Old;
        internal TransformState m_New;
        internal TransformState m_Active;
        internal NativeArray<Entity> m_AllEntities;
        internal NativeList<Neighbour> m_Neighbours;

        //protected TransformState _TransformState;

        /// <summary>
        /// Area for terrain to be updated each frame
        /// </summary>
        internal Bounds3 m_TerrainUpdateBounds;

        public virtual float AngleDelta
        {
            get => m_Active.m_AngleDelta;
            set
            {
                m_HasMovedAction = true;
                m_Active.m_AngleDelta = value;
            }
        }

        public virtual float3 MoveDelta
        {
            get => m_Active.m_MoveDelta;
            set
            {
                m_HasMovedAction = true;
                m_Active.m_MoveDelta = value;
            }
        }

        public float3 m_Center;

        public TransformBase() : base()
        {
            QLookupFactory.Init(_MIT);

            HashSet<MVDefinition> fullDefinitions = _MIT.Selection.GetObjectsToTransformFull();
            m_Old = new TransformStateOld(fullDefinitions.Count);
            m_New = new TransformStateNew(fullDefinitions.Count);

            //HashSet<Moveable> moveables = GetMoveablesToTransformFull();
            //m_Old = new TransformStateOld(moveables.Count);
            //m_New = new TransformStateNew(moveables.Count);

            m_Active = m_New;
            HashSet<Entity> allEntities = new();

            m_InitialBounds = _MIT.Selection.GetTotalBounds(MIT.TERRAIN_UPDATE_MARGIN);
            m_TerrainUpdateBounds = m_InitialBounds;
            m_Center = _MIT.Selection.Center;

            m_CanUseLowSensitivity = true;

            int c = 0;
            foreach (MVDefinition mvd in fullDefinitions)
            {
                Moveable mv = _MIT.Moveables.GetOrCreate<Moveable>(mvd);
                m_Old.m_States[c] = new(_MIT.EntityManager, ref QLookupFactory.Get(), mv, 0f, 0f, m_Center);
                m_New.m_States[c] = new(_MIT.EntityManager, ref QLookupFactory.Get(), mv, 0f, 0f, m_Center);

                NativeArray<Entity> all = m_New.m_States[c].m_Accessor.GetAllEntities();
                foreach (Entity e in all)
                {
                    if (!allEntities.Contains(e))
                    {
                        allEntities.Add(e);
                    }
                }
                c++;
            }

            m_AllEntities = new NativeArray<Entity>(allEntities.ToArray(), Allocator.Persistent);
            m_Neighbours = GetSelectionNeighbours();

            m_Snapper = new Snapper.Snapper(this);

            //DebugDumpStates($"TransformBase.Ctor {_MIT.Selection.Name} ", showOld: true, showNew: false);
        }

        ~TransformBase()
        {
            m_AllEntities.Dispose();
            m_New.Dispose();
            m_Old.Dispose();
            // Do not dispose m_Active, its m_States data is a pointer to m_Old.m_States or m_New.m_States.

            string msg = $"TB.dtor neighbours:{m_Neighbours.Length}";
            for (int i = 0; i < m_Neighbours.Length; i++)
            {
                MVDefinition mvd = new(m_Neighbours[i].m_Identity, m_Neighbours[i].m_Entity, false);
                msg += $"\n    {mvd}";
                if (_MIT.Moveables.Has(mvd))
                {
                    //_MIT.Moveables.RemoveIfUnused(mvd);
                    msg += " Exists";
                    if (_MIT.Moveables.RemoveIfUnused(mvd))
                    {
                        msg += " Removed";
                    }
                }
            }
            QLog.Debug(msg);
            m_Neighbours.Dispose();
        }

        public override ActionState GetActionState() => m_Active;

        internal override bool Uses(MVDefinition mvd)
        {
            for (int i = 0; i < m_Neighbours.Length; i++)
            {
                if (mvd.Equals(m_Neighbours[i].Definition))
                {
                    return true;
                }
            }
            return false;
        }

        public State GetState(MVDefinition mvd)
        {
            foreach (State state in m_Active.m_States)
            {
                if (state.Definition.Equals(mvd))
                {
                    return state;
                }
            }

            throw new Exception($"Failed to find state for {mvd} in TransformAction {ToString()}");
        }

        public override void Do()
        {
            QLookupFactory.Init(_MIT);

            m_TerrainUpdateBounds = _MIT.Selection.GetTotalBounds(MIT.TERRAIN_UPDATE_MARGIN);
            m_Snapper.m_SnapType = Snapper.SnapTypes.None;

            if (!ToolDo())
            {
                return;
            }
            m_Active = m_New;

            Transform();

            ToolDoLast();

            //DebugDumpStates($"Do", showOld:false, showNew:true);
        }

        /// <summary>
        /// Run tool-specific stuff
        /// </summary>
        /// <returns>Should the action continue?</returns>
        protected abstract bool ToolDo();

        /// <summary>
        /// Run tool-specific stuff at the end of the main Do method
        /// </summary>
        protected virtual void ToolDoLast() { }

        /// <summary>
        /// Update selected objects to the new JobStates data
        /// </summary>
        internal void Transform()
        {
            UpdateTerrain();
            for (int i = 0; i < m_Active.m_States.Length; i++)
            {
                Moveable mv = _MIT.Moveables.GetOrCreate<Moveable>(m_Active.m_States[i].Definition);

                if (_MIT.IsManipulating != mv.IsManipulatable) continue;

                mv.MoveIt(this, m_Active.m_States[i], m_UpdateMove, m_UpdateRotate);
                mv.UpdateOverlay();
            }

            if (m_Neighbours.Length > 0)
            {
                TransformNeighbours();
                ControlPointManager.UpdateAllExisting();
            }

            _MIT.m_SelectionDirty = true;
        }

        protected virtual void TransformNeighbours()
        {
            //string msg = $"TRANSNEIGHS {m_Neighbours.Length}";
            //for (int i = 0; i < m_Neighbours.Length; i++)
            //{
            //    msg += $"\n    {m_Neighbours[i].m_Entity.DX()}  {m_Neighbours[i].m_InitialCurve.a.DX()} :: {m_Neighbours[i].m_InitialCurve.b.DX()} :: {m_Neighbours[i].m_InitialCurve.c.DX()} :: {m_Neighbours[i].m_InitialCurve.d.DX()}";
            //}
            //QLog.Debug(msg);

            var lookup = _MIT.GetComponentLookup<Game.Net.Curve>();
            lookup.Update(_MIT);
            NeighboursJob job = new()
            {
                m_Neighbours    = m_Neighbours,
                gnCurve         = lookup,
            };

            JobHandle neighboursHandle = job.Schedule(m_Neighbours.Length, new());
            neighboursHandle.Complete();
        }

        public override void Undo()
        {
            MIT.Log.Debug($"TB.Undo |{Phase};{_MIT.Queue.Index}|");
            m_Active = m_Old;
            UndoRedoProcess();
            //DebugDumpStates($"Undo ");
        }

        public override void Redo()
        {
            MIT.Log.Debug($"TB.Redo |{Phase};{_MIT.Queue.Index}|");
            m_Active = m_New;
            UndoRedoProcess();
            //DebugDumpStates($"Redo ");
        }

        private void UndoRedoProcess()
        {
            MIT.Log.Debug($"TB.UndoRedoProcess |{Phase};{_MIT.Queue.Index}| Updates-[Move:{m_UpdateMove},Rotate:{m_UpdateRotate}]");
            m_UpdateMove = true;
            m_UpdateRotate = true;
            m_HasMovedAction = true;
            UpdateStates();
            Phase = Phases.Finalise;
        }

        public override void Finalise()
        {
            MIT.Log.Debug($"TB.Finalise |{Phase};{_MIT.Queue.Index}| Updates-[Move:{m_UpdateMove},Rotate:{m_UpdateRotate},HasMovedAction:{m_HasMovedAction}]");
            Phase = Phases.Cleanup;
            if (!m_HasMovedAction)
            {
                QLog.Debug($"Call to Finalise() but m_HasMovedAction is false");
                return;
            }

            Transform();

            for (int i = 0; i < m_Active.m_States.Length; i++)
            {
                m_Active.m_States[i].TransformEnd(m_AllEntities);
            }

            _MIT.m_SelectionDirty = true;

            m_FinalBounds = _MIT.Selection.GetTotalBounds(MIT.TERRAIN_UPDATE_MARGIN);

            m_Neighbours = GetSelectionNeighbours();
            for (int i = 0; i < m_Neighbours.Length; i++)
            {
                if (_MIT.Moveables.TryGet(m_Neighbours[i].Definition, out Moveable mv))
                {
                    mv.UpdateOverlay();
                }
            }
        }

        public override void Cleanup()
        {
            UpdateTerrain(m_TerrainUpdateBounds);
            UpdateNearbyBuildingConnections(_MIT.EntityManager, m_InitialBounds);
            UpdateNearbyBuildingConnections(_MIT.EntityManager, m_FinalBounds);
            Phase = Phases.Complete;
        }


        /// <summary>
        /// Update states when the action queue is altered
        /// </summary>
        private void UpdateStates()
        {
            if (m_Active.m_States.Length == 0) { return; }

            List<int> toRemove = new();
            for (int i = 0; i < m_Active.m_States.Length; i++)
            {
                if (m_Active.m_States[i].m_Identity == Identity.ControlPoint)
                {
                    State state = m_Active.m_States[i];
                    MVDefinition mvd = state.Definition;
                    if (!State.IsValid(_MIT.EntityManager, mvd.m_Parent))
                    {
                        toRemove.Add(i);
                        continue;
                    }
                    MVControlPoint cp = _MIT.ControlPointManager.GetOrCreateMoveable(mvd);
                    state.UpdateEntity(_MIT.EntityManager, ref QLookupFactory.Get(), cp.m_Entity);
                    m_Active.m_States[i] = state;
                }
                else if (!m_Active.m_States[i].IsValid(_MIT.EntityManager))
                {
                    toRemove.Add(i);
                    continue;
                }
            }

            if (toRemove.Count == m_Active.m_States.Length)
            {
                m_Active.m_States.Dispose();
                m_Active.m_States = new(0, Allocator.Persistent);
            }
            else if (toRemove.Count > 0)
            {
                int newLength = m_Active.m_States.Length - toRemove.Count;
                List<State> newStates = new(newLength);
                for (int i = 0; i < m_Active.m_States.Length; i++)
                {
                    if (!toRemove.Contains(i))
                    {
                        newStates.Add(m_Active.m_States[i]);
                    }
                }
                m_Active.m_States.Dispose();
                m_Active.m_States = new(newLength, Allocator.Persistent);
                for (int i = 0; i < newLength; i++)
                {
                    m_Active.m_States[i] = newStates[i];
                }
            }
        }

        /// <summary>
        /// Calls _MIT.Selection.GetObjectsToTransformFull() and filters out Invalid results, including building extensions
        /// </summary>
        /// <returns>All selected parents, as Moveables</returns>
        internal HashSet<Moveable> GetMoveablesToTransformFull()
        {
            HashSet<MVDefinition> fullDefinitions = _MIT.Selection.GetObjectsToTransformFull();
            HashSet<Moveable> moveables = new();
            foreach (MVDefinition mvd in fullDefinitions)
            {
                if (_MIT.IsValid(mvd))
                {
                    moveables.Add(_MIT.Moveables.GetOrCreate<Moveable>(mvd));
                }
            }
            return moveables;
        }

        //public override HashSet<Overlays.Overlay> GetOverlays(Overlays.ToolFlags toolFlags)
        //{
        //    return m_Snapper.GetOverlays(toolFlags);
        //}


        //public override void Archive(Phases phase, int idx)
        //{
        //    base.Archive(phase, idx);

        //    //MVDefinition[] neighbours = new MVDefinition[m_Neighbours.Length];
        //    //for (int i = 0; i < m_Neighbours.Length; i++)
        //    //{
        //    //    neighbours[i] = m_Neighbours[i].Definition;
        //    //}
        //    //m_Neighbours.Clear();
        //    //QLog.Debug($"TRANSARCHIVE Neighbours:{neighbours.Length}");
        //    //for (int i = 0; i < neighbours.Length; i++)
        //    //{
        //    //    _MIT.Moveables.RemoveIfUnused(neighbours[i]);
        //    //}
        //}

        //public override void Unarchive(Phases phase, int idx)
        //{
        //    base.Unarchive(phase, idx);

        //    //HashSet<MVDefinition> fullDefinitions = _MIT.Selection.GetObjectsToTransformFull();
        //    //HashSet<Moveable> fullSelection = new();
        //    //fullDefinitions.ForEach(mvd => fullSelection.Add(_MIT.Moveables.GetOrCreate(mvd)));
        //    //DebugDumpNeighbours();
        //    //m_Neighbours = GetSelectionNeighbours();
        //}

        /// <summary>
        /// Process movement from the main AngleDelta and MoveDelta values
        /// </summary>
        protected void DoFromDeltas()
        {
            Matrix4x4 matrix = default;
            matrix.SetTRS(m_Center + MoveDelta, Quaternion.Euler(0f, AngleDelta, 0f), Vector3.one);

            for (int i = 0; i < m_Old.Count; i++)
            {
                State old = m_Old.m_States[i];
                if (!old.m_Entity.Exists(_MIT.EntityManager))
                {
                    MIT.Log.Warning($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name} Invalid state: {old}  {QCommon.GetCallerDebug()}");
                    continue;
                }

                float3 position = (float3)matrix.MultiplyPoint(old.m_Position - m_Center);
                float3 oldAngles = old.m_Rotation.ToEulerDegrees();
                quaternion rotation = Quaternion.Euler(oldAngles.x, oldAngles.y + AngleDelta, oldAngles.z);

                // Hack to work around the lack of unaltered original terrain height for terrain conforming
                //if (position.x.Equals(old.m_InitialTerrainPosition.x) && position.z.Equals(old.m_InitialTerrainPosition.z))
                //{
                //    position.y = old.m_InitialTerrainPosition.y + newYOffset;
                //}
                //else
                //{
                //    position.y = _MIT.GetTerrainHeight(position) + newYOffset;
                //}

                //DebugDumpStates();

                m_New.m_States[i].Dispose();
                State state = m_Old.m_States[i].GetCopy(_MIT.EntityManager, ref QLookupFactory.Get());
                state.m_Position        = position;
                state.m_Rotation        = rotation;
                state.m_MoveDelta       = MoveDelta;
                state.m_AngleDelta      = AngleDelta;
                state.m_InitialCenter   = m_Center;
                m_New.m_States[i] = state;
            }
        }

        protected NativeList<Neighbour> GetSelectionNeighbours()
        {
            HashSet<Entity> allSegments = new(); // Each neighbouring segment, with no duplicates
            HashSet<Entity> overlapSegments = new(); // Any duplicates, meaning both of this segment's nodes are selected

            foreach (Moveable mv in GetMoveablesToTransformFull())
            {
                if (mv is MVNode node)
                {
                    foreach ((Entity e, _) in node.m_Segments)
                    {
                        if (allSegments.Contains(e))
                        {
                            overlapSegments.Add(e);
                        }
                        else
                        {
                            allSegments.Add(e);
                        }
                    }
                }
            }

            NativeList<Neighbour> result = new(Allocator.Persistent);
            foreach (Entity e in allSegments)
            {
                if (!overlapSegments.Contains(e))
                {
                    result.Add(new()
                    {
                        m_Entity        = e,
                        m_Identity      = Identity.Segment,
                        m_InitialCurve  = _MIT.EntityManager.GetComponentData<Game.Net.Curve>(e).m_Bezier,
                    });
                }
            }

            return result;
        }


        /// <summary>
        /// Update buildings and networks in or near the passed location
        /// </summary>
        /// <param name="manager">an EntityManager</param>
        /// <param name="bounds">The outer bounds of the rectangle</param>
        /// <returns>The number of search results found</returns>
        internal int UpdateNearbyBuildingConnections(EntityManager manager, Bounds3 bounds)
        {
            bool isRelevant = false;
            foreach (State state in m_Active.m_States)
            {
                if (
                    state.m_Identity == Identity.Building || state.m_Identity == Identity.ServiceUpgrade || state.m_Identity == Identity.Extension ||
                    state.m_Identity == Identity.Node || state.m_Identity == Identity.Segment
                    )
                {
                    isRelevant = true;
                    break;
                }
            }
            if (!isRelevant) return -1;

            //StringBuilder sb = new("UpdateNearbyBuildingConnections");
            Bounds2 outerBounds = new(bounds.min.XZ(), bounds.max.XZ());
            using Searcher.Searcher searcher = new(Searcher.Utils.FilterAllNetworks | Searcher.Filters.Buildings, false, _MIT.m_PointerPos);
            searcher.SearchBounds(outerBounds);
            //searcher.DebugDumpSearchResults();

            DebugBounds.Factory(outerBounds, Overlays.Overlay.DEBUG_TTL, new UnityEngine.Color(0.9f, 0.2f, 0f, 0.6f));

            //sb.AppendFormat("\nStates: {0}", ta.m_Active.m_States.Length);
            foreach (State state in m_Active.m_States)
            {
                if (
                    state.m_Identity == Identity.Building || state.m_Identity == Identity.ServiceUpgrade || state.m_Identity == Identity.Extension ||
                    state.m_Identity == Identity.Node || state.m_Identity == Identity.Segment
                    )
                {
                    state.m_Accessor.UpdateAll();
                    //sb.AppendFormat("\n    {0} (updated: {1})", state.m_Entity.DX(true), c);
                }
            }

            //sb.AppendFormat("\nResults: {0}", searcher.m_Results.Length);
            foreach (Searcher.Result result in searcher.m_Results)
            {
                Entity e = result.m_Entity;
                QAccessor.QObject accessor = new(manager, ref QAccessor.QLookupFactory.Get(), e);
                accessor.UpdateAll();

                if (!Mod.Settings.ShowDebugLines) continue;

                //sb.AppendFormat("\n    {0} - (updated: {1})", e.DX(true), c);

                // Segment
                if (manager.TryGetComponent(e, out Game.Net.Edge edge))
                {
                    float3 posA = manager.GetComponentData<Game.Net.Node>(edge.m_Start).m_Position;
                    float3 posB = manager.GetComponentData<Game.Net.Node>(edge.m_End).m_Position;
                    DebugLine.Factory(new(posA, posB), Overlays.Overlay.DEBUG_TTL, new(1f, 0.5f, 0f, 0.8f));

                    if (!searcher.Has(edge.m_Start))
                    {
                        DebugCircle.Factory(posA, 8, Overlays.Overlay.DEBUG_TTL, new(1f, 0.4f, 0.3f, 0.4f));
                    }
                    if (!searcher.Has(edge.m_End))
                    {
                        DebugCircle.Factory(posB, 8, Overlays.Overlay.DEBUG_TTL, new(1f, 0.4f, 0.3f, 0.4f));
                    }
                }

                // Node
                else if (manager.TryGetComponent(e, out Game.Net.Node node))
                {
                    DebugCircle.Factory(node.m_Position, 8, Overlays.Overlay.DEBUG_TTL, new(1f, 0.5f, 0.0f, 0.8f));
                }

                // Building
                else if (manager.TryGetComponent(e, out Game.Objects.Transform tform))
                {
                    if (manager.TryGetComponent(e, out Game.Buildings.Building _))
                    {
                        var prefab = manager.GetComponentData<Game.Prefabs.PrefabRef>(e).m_Prefab;
                        Quad2 corners = Searcher.Utils.CalculateBuildingCorners(manager, ref accessor, prefab, -0.5f);
                        DebugQuad.Factory(corners, (int)(Overlays.Overlay.DEBUG_TTL * 1.5), new(0.0f, 0.6f, 0.9f, 0.8f));
                    }
                    else
                    {
                        DebugCircle.Factory(tform.m_Position, 8f, (int)(Overlays.Overlay.DEBUG_TTL * 1.5f), new(0.0f, 0.8f, 0.8f, 0.7f));
                    }
                }
            }

            //QLog.XDebug(sb.ToString());
            return searcher.Count;
        }

        internal void UpdateTerrain(Bounds3 area = default)
        {
            Bounds3 bounds = _MIT.Selection.GetTotalBounds();
            if (area.Equals(default))
            {
                area.min = bounds.min - MIT.TERRAIN_UPDATE_MARGIN;
                area.max = bounds.max + MIT.TERRAIN_UPDATE_MARGIN;
            }
            else
            {
                area.min = math.min(area.min, bounds.min - MIT.TERRAIN_UPDATE_MARGIN);
                area.max = math.max(area.max, bounds.max + MIT.TERRAIN_UPDATE_MARGIN);
            }
            m_TerrainUpdateBounds = area;

            //Overlays.DebugBounds.Factory(area, Overlays.Overlay.DEBUG_TTL, new(0.1f, 0.1f, 0.8f, 0.6f));

            _MIT.SetUpdateAreaField(area);
        }

        #region Debug
        public override string ToString()
        {
            return $"{base.ToString()} o{(m_Active == m_Old ? "*" : "")}:{m_Old.Count},n{(m_Active == m_New ? "*" : "")}:{m_New.Count}";
        }


        public string DebugStates(string prefix = "", bool showOld = true, bool showNew = true)
        {
            StringBuilder sb = new(prefix);

            if (m_Old.Count == m_New.Count)
            {
                sb.AppendFormat($"States:{m_Old.Count}:");
                for (int i = 0; i < m_Old.Count; i++)
                {
                    MVDefinition oldState = new(m_Old.m_States[i].m_Identity, m_Old.m_States[i].m_Entity, m_IsManipulationMode, m_Old.m_States[i].m_IsManaged, m_Old.m_States[i].m_Parent, m_Old.m_States[i].m_ParentId, m_Old.m_States[i].m_ParentKey);
                    MVDefinition newState = new(m_New.m_States[i].m_Identity, m_New.m_States[i].m_Entity, m_IsManipulationMode, m_New.m_States[i].m_IsManaged, m_New.m_States[i].m_Parent, m_New.m_States[i].m_ParentId, m_New.m_States[i].m_ParentKey);
                    string b = _MIT.Selection.Has(oldState) ? "B" : "b";
                    string f = _MIT.Selection.HasFull(oldState) ? "F" : "f";
                    if (showOld) sb.AppendFormat("\n  {0}{1} Old: {2}", b, f, m_Old.m_States[i]);

                    b = _MIT.Selection.Has(newState) ? "B" : "b";
                    f = _MIT.Selection.HasFull(newState) ? "F" : "f";
                    if (showNew) sb.AppendFormat("\n  {0}{1} New: {2}", b, f, m_New.m_States[i]);
                }
            }
            else
            {
                if (showOld)
                {
                    sb.AppendFormat($"\nLENGTH MISMATCH Old:{m_Old.Count}:");
                    foreach (State state in m_Old.m_States)
                    {
                        sb.AppendFormat("\n  {0}", state);
                    }
                }
                if (showNew)
                {
                    sb.AppendFormat($"\nLENGTH MISMATCH New:{m_New.Count}:");
                    foreach (State state in m_New.m_States)
                    {
                        sb.AppendFormat("\n  {0}", state);
                    }
                }
            }
            sb.AppendFormat("\nAll Entities:{0}\n", m_AllEntities.Length);
            for (int i = 0; i < m_AllEntities.Length; i++)
            {
                sb.AppendFormat("{0}, ", m_AllEntities[i].DX());
            }

            return sb.ToString();
        }

        public void DebugDumpStates(string prefix = "", bool showOld = true, bool showNew = true)
        {
            MIT.Log.Debug(DebugStates(prefix, showOld, showNew));
        }

        public string DebugNeighbours(int depth = 0)
        {
            string msg = $"Neighbours: {m_Neighbours.Length} Caller:{QCommon.GetCallingMethodName(depth)}";
            for (int i = 0; i < m_Neighbours.Length; i++)
            {
                Bezier4x3 c = m_Neighbours[i].m_InitialCurve;
                msg += $"\n  {m_Neighbours[i].m_Entity.DX(),14} - {c.a.DX()} :: {c.b.DX()} :: {c.c.DX()} :: {c.d.DX()}";
            }
            return msg;
        }

        public void DebugDumpNeighbours(string prefix = "", int depth = 1)
        {
            QLog.Debug(prefix + DebugNeighbours(depth));
        }
        #endregion
    }
}
