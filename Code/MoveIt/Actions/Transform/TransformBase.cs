using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
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
            HashSet<Moveable> fullSelection = new();
            fullDefinitions.ForEach(mvd => fullSelection.Add(_MIT.Moveables.GetOrCreate(mvd)));

            m_Old = new TransformStateOld(fullSelection.Count);
            m_New = new TransformStateNew(fullSelection.Count);
            m_Active = m_New;
            HashSet<Entity> allEntities = new();

            m_InitialBounds = _MIT.Selection.GetTotalBounds(MIT.TERRAIN_UPDATE_MARGIN);
            m_TerrainUpdateBounds = m_InitialBounds;
            m_Center = _MIT.Selection.Center;

            m_CanUseLowSensitivity = true;

            int c = 0;
            foreach (Moveable mv in fullSelection)
            {
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

            m_Snapper = new Snapper.Snapper(this);

            DebugDumpStates($"TransformBase.Ctor {_MIT.Selection.Name} ", showOld: true, showNew: false);
        }

        ~TransformBase()
        {
            m_AllEntities.Dispose();
            m_New.Dispose();
            m_Old.Dispose();
            // Do not dispose m_Active, its m_States data is a pointer to m_Old.m_States or m_New.m_States
        }

        public override ActionState GetActionState() => m_Active;

        public State GetState(MVDefinition mvd)
        {
            foreach (State state in m_Active.m_States)
            {
                if (state.Definition.Equals(mvd))
                {
                    return state;
                }
            }

            throw new System.Exception($"Failed to find state for {mvd} in TransformAction {ToString()}");
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

            //DebugDumpStates($"Do", showOld:false, showNew:true);
        }

        /// <summary>
        /// Run tool-specific stuff
        /// </summary>
        /// <returns>Should the action continue?</returns>
        protected abstract bool ToolDo();

        /// <summary>
        /// Update selected objects to the new JobStates data
        /// </summary>
        internal void Transform()
        {
            for (int i = 0; i < m_Active.m_States.Length; i++)
            {
                Moveable mv = _MIT.Moveables.GetOrCreate(m_Active.m_States[i].Definition);

                if (_MIT.IsManipulating != mv.IsManipulatable) continue;

                mv.MoveIt(this, m_Active.m_States[i], m_UpdateMove, m_UpdateRotate);
                mv.UpdateOverlay();
            }

            _MIT.m_SelectionDirty = true;
        }

        public override void Undo()
        {
            //MIT.Log.Debug($"{Time.frameCount} TA.Undo |{_MIT.MITAction}|");
            m_UpdateMove = true;
            m_UpdateRotate = true;
            m_Active = m_Old;
            UpdateStates();
            _MIT.CreationPhase = CreationPhases.Create;
            _MIT.Queue.CreationAction = this;
            //DebugDumpStates($"Undo");

            base.Undo();
        }

        public override void Redo()
        {
            //MIT.Log.Debug($"{Time.frameCount} TA.Redo |{_MIT.MITAction}|");
            m_UpdateMove = true;
            m_UpdateRotate = true;
            m_Active = m_New;
            UpdateStates();
            _MIT.CreationPhase = CreationPhases.Create;
            _MIT.Queue.CreationAction = this;
            //DebugDumpStates($"Redo");

            base.Redo();
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
                    MVControlPoint cp = _MIT.ControlPointManager.GetOrCreate(mvd);
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

        //public override HashSet<Overlays.Overlay> GetOverlays(Overlays.ToolFlags toolFlags)
        //{
        //    return m_Snapper.GetOverlays(toolFlags);
        //}

        public override string ToString()
        {
            return $"{base.ToString()} o{(m_Active == m_Old ? "*" : "")}:{m_Old.Count},n{(m_Active == m_New ? "*" : "")}:{m_New.Count}";
        }

        internal override void OnHoldEnd()
        {
            if (!m_HasMovedAction) return;

            for (int i = 0; i < m_Active.m_States.Length; i++)
            {
                m_Active.m_States[i].TransformEnd(m_AllEntities);
            }

            _MIT.m_SelectionDirty = true;

            _MIT.CreationPhase = CreationPhases.Create;
            _MIT.MITAction = MITActions.Do;
        }

        /// <summary>
        /// Process movement from the main AngleDelta and MoveDelta values
        /// </summary>
        protected void DoFromAngleAndMoveDeltas()
        {
            Matrix4x4 matrix = default;
            matrix.SetTRS(m_Center + MoveDelta, Quaternion.Euler(0f, AngleDelta, 0f), Vector3.one);

            for (int i = 0; i < m_Old.Count; i++)
            {
                State old = m_Old.m_States[i];

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


        public string DebugStates(string prefix = "", bool showOld = true, bool showNew = true)
        {
            StringBuilder sb = new(prefix);

            if (m_Old.Count == m_New.Count)
            {
                sb.AppendFormat($"States:{m_Old.Count}:");
                for (int i = 0; i < m_Old.Count; i++)
                {
                    MVDefinition oldState = new(m_Old.m_States[i].m_Identity, m_Old.m_States[i].m_Entity, m_IsManipulationMode, m_Old.m_States[i].m_IsManaged, m_Old.m_States[i].m_Parent, m_Old.m_States[i].m_ParentKey);
                    MVDefinition newState = new(m_New.m_States[i].m_Identity, m_New.m_States[i].m_Entity, m_IsManipulationMode, m_New.m_States[i].m_IsManaged, m_New.m_States[i].m_Parent, m_New.m_States[i].m_ParentKey);
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
    }
}
