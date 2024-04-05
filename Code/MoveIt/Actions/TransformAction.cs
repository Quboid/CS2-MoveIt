using Colossal.Mathematics;
using MoveIt.Managers;
using MoveIt.Moveables;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Actions
{
    internal class TransformState : ActionState
    {
        internal QNativeArray<State> m_States;
        internal float3 m_MoveDelta = 0f;
        internal float m_AngleDelta = 0f;
        internal bool m_IsNew = false;

        internal int Count => m_States.Length;

        internal TransformState(int length, bool isNew = false)
        {
            m_States = new QNativeArray<State>(length, Allocator.Persistent);
            m_IsNew = isNew;
        }

        public override void Dispose() => m_States.Dispose();

        public override string ToString()
        {
            return $"[TrState:{(m_IsNew ? "New" : "Old")}]";
        }
    }

    internal class TransformKeyAction : TransformAction
    {
        private readonly float3 _FACTOR = new(0.25f, 0.015625f, 0.25f); // y = 1/64
        public override string Name => "TransformKeyAction";

        internal void Process(float3 direction)
        {
            if (!direction.Equals(float3.zero))
            {
                direction.x *= _FACTOR.x;
                direction.y *= _FACTOR.y;
                direction.z *= _FACTOR.z;

                Matrix4x4 matrix = default;
                matrix.SetTRS(Vector3.zero, Quaternion.AngleAxis(Camera.main.transform.localEulerAngles.y, Vector3.up), Vector3.one);
                direction = matrix.MultiplyVector(direction);
            }

            m_HotkeyPressed = true;
            MoveDelta += direction;
            _Tool.ToolAction = ToolActions.Do;
            _Tool.CreationPhase = CreationPhases.Create;
        }
    }

    internal class TransformAction : Action
    {
        public override string Name => "Transform";
        internal bool m_HasMovedAction = false;
        internal bool m_HotkeyPressed = false;

        internal Snapper.Snapper m_Snapper;

        internal TransformState m_Old;
        internal TransformState m_New;
        internal TransformState m_Active;

        private readonly Dictionary<Moveable, int> m_StateMap;

        public float AngleDelta
        {
            get => m_Active.m_AngleDelta;
            set
            {
                m_HasMovedAction = true;
                m_Active.m_AngleDelta = value;
            }
        }

        public float3 MoveDelta
        {
            get => m_Active.m_MoveDelta;
            set
            {
                m_HasMovedAction = true;
                m_Active.m_MoveDelta = value;
            }
        }

        public float3 m_Center;

        public TransformAction() : base()
        {
            QAccessor.QLookup.Reset();

            m_StateMap = new();

            List<Moveable> fullSelection = _Tool.ActiveSelection.GetObjectsToTransformFull();
            m_Old = new(fullSelection.Count);
            m_New = new(fullSelection.Count, true);
            m_Active = m_New;

            Bounds3 bounds = _Tool.ActiveSelection.GetTotalBounds();
            m_UpdateArea = new(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
            m_Center = _Tool.ActiveSelection.Center;

            int c = 0;
            foreach (Moveable mv in fullSelection)
            {
                mv.UpdateYOffset();

                m_Old.m_States[c] = new(mv, _Tool);
                m_New.m_States[c] = new(mv, _Tool);
                m_StateMap.Add(mv, c);
                c++;
            }

            m_Snapper = new(this);

            //m_Snapper.DebugDump();
            //DebugDumpStates($"Ctor", showOld:true, showNew:false);
        }

        ~TransformAction()
        {
            m_New.Dispose();
            m_Old.Dispose();
            // Do not dispose m_Active, its m_States data is a pointer to m_Old.m_States or m_New.m_States
        }

        public override ActionState GetActionState() => m_Active;

        public override void Do()
        {
            float3 newMoveDelta = MoveDelta;
            float newAngleDelta = AngleDelta;

            Bounds3 bounds = _Tool.ActiveSelection.GetTotalBounds();
            m_UpdateArea.xy = math.min(m_UpdateArea.xy, bounds.xz.min);
            m_UpdateArea.zw = math.max(m_UpdateArea.zw, bounds.xz.max);

            m_Snapper.m_SnapType = Snapper.SnapTypes.None;

            //MIT.Log.Debug($"Trans.Do TS:{_Tool.ToolState}, {_Tool.m_ApplyAction.m_PressedTime}/{_Tool.m_SecondaryAction.m_PressedTime}");

            if (_Tool.ToolState == ToolStates.ApplyButtonHeld)
            {
                float y = MoveDelta.y;
                newMoveDelta = _Tool.m_PointerPos - _Tool.m_ClickPositionAbs;// - m_dragStartRelative;
                newMoveDelta.y = y;

                // Snapping
                if (QKeyboard.Alt)
                {
                    if (m_Snapper.Update(out var snapResult))
                    {
                        newMoveDelta = snapResult.m_Delta;
                    }
                }
            }
            else if (_Tool.ToolState == ToolStates.SecondaryButtonHeld)
            {
                // Rotation value, 1 = full 360 (uses screen height, not width, to adapt to ultrawide)
                float angle = (float)(UnityEngine.InputSystem.Mouse.current.position.x.ReadValue() - _Tool.m_MouseStartX) / (float)(Screen.height * 1.5f) * _Tool.RotationDirection;

                // Snapping
                if (QKeyboard.Alt)
                {
                    // Snap to 45 degrees
                    angle = Mathf.Round(angle * 8f) / 8;
                }

                newAngleDelta = angle * 360;
            }

            // If nothing has changed this frame, end now
            m_UpdateMove = false;
            m_UpdateRotate = false;
            if (!MoveDelta.Equals(newMoveDelta) || m_HotkeyPressed)
            {
                m_UpdateMove = true;
            }

            if (!AngleDelta.Equals(newAngleDelta))
            {
                m_UpdateRotate = true;
            }

            if (!m_UpdateMove && !m_UpdateRotate)
            {
                return;
            }

            MoveDelta = newMoveDelta;
            AngleDelta = newAngleDelta;
            m_HotkeyPressed = false;

            Matrix4x4 matrix = default;
            matrix.SetTRS(m_Center + MoveDelta, Quaternion.Euler(0f, AngleDelta, 0f), Vector3.one);

            for (int i = 0; i < m_Old.Count; i++)
            {
                //if (!CanActOn(m_Old.m_States[i])) continue;
                //if (_IsSegmentMove && m_Old.m_States[i].m_Identity != QTypes.Identity.Segment) continue;

                State old = m_Old.m_States[i];

                float3 position = (float3)matrix.MultiplyPoint(m_Old.m_States[i].m_Position - m_Center);
                float angle = (m_Old.m_States[i].m_Angle + AngleDelta + 360) % 360;
                float newYOffset = m_Old.m_States[i].m_InitialYOffset + MoveDelta.y;

                // Hack to work around the lack of unaltered original terrain height for terrain conforming
                //if (position.x.Equals(old.m_InitialTerrainPosition.x) && position.z.Equals(old.m_InitialTerrainPosition.z))
                //{
                //    position.y = old.m_InitialTerrainPosition.y + newYOffset;
                //}
                //else
                //{
                //    position.y = _Tool.GetTerrainHeight(position) + newYOffset;
                //}

                //QLog.Debug($"TA.Do {old.m_Entity.DX()} old:{old.m_Position.D()}, center:{m_Center.D()}, new:{position.D()}");

                //DebugDumpStates();

                m_New.m_States[i].Dispose();
                m_New.m_States[i] = new()
                {
                    m_Entity = old.m_Entity,
                    m_Accessor = new(old.m_Entity, _Tool, old.m_Identity),
                    m_Prefab = old.m_Prefab,
                    m_Position = position,
                    m_InitialPosition = old.m_InitialPosition,
                    m_Angle = angle,
                    m_InitialAngle = old.m_InitialAngle,
                    m_YOffset = newYOffset,
                    m_InitialYOffset = old.m_InitialYOffset,
                    m_Identity = old.m_Identity,
                    m_ObjectType = old.m_ObjectType,
                    m_Data = old.m_Data,
                };
            }
            m_Active = m_New;

            //DumpStates($"Do", showOld:false, showNew:true);

            m_IsManipulate.to = _Tool.Manipulating;
        }

        internal State GetState(CPDefinition cpd)
        {
            return m_Active.m_States[GetStateIndex(cpd)];
        }

        internal void SetState(CPDefinition cpd, State state)
        {
            int idx = GetStateIndex(cpd);
            m_Active.m_States[idx] = state;
        }

        private int GetStateIndex(CPDefinition cpd)
        {
            for (int i = 0; i < m_Active.Count; i++)
            {
                if (m_Active.m_States[i].m_Identity == QTypes.Identity.ControlPoint)
                {
                    StateControlPoint scp = (StateControlPoint)m_Active.m_States[i].m_Data.Get();
                    if (scp.m_Segment == cpd.m_Segment && scp.m_Curvekey == cpd.m_CurveKey)
                    {
                        return i;
                    }
                }
            }
            throw new Exception($"No State found for {cpd}");
        }

        public override void Undo()
        {
            //MIT.Log.Debug($"{Time.frameCount} TA.Undo |{_Tool.ToolAction}|");
            m_UpdateMove = true;
            m_UpdateRotate = true;
            _Tool.CreationPhase = CreationPhases.Create;
            m_Active = m_Old;
            UpdateStates();
            //DebugDumpStates($"Undo");

            base.Undo();
        }

        public override void Redo()
        {
            //MIT.Log.Debug($"{Time.frameCount} TA.Redo |{_Tool.ToolAction}|");
            m_UpdateMove = true;
            m_UpdateRotate = true;
            _Tool.CreationPhase = CreationPhases.Create;
            m_Active = m_New;
            UpdateStates();
            //DebugDumpStates($"Redo");

            base.Redo();
        }

        /// <summary>
        /// Update states when the action queue is altered
        /// Currently only used to renew controlpoints
        /// </summary>
        private void UpdateStates()
        {
            //string msg = $"UpdateStates:{m_Active.Count}";
            for (int i = 0; i < m_Active.m_States.Length; i++)
            {
                if (m_Active.m_States[i].m_Identity == QTypes.Identity.ControlPoint)
                {
                    State state = m_Active.m_States[i];
                    StateControlPoint scp = (StateControlPoint)state.m_Data.Get();
                    CPDefinition cpd = new(scp);
                    ControlPoint cp = _Tool.ControlPointManager.GetOrCreate(cpd);
                    state.UpdateEntity(cp.m_Entity, _Tool);
                    m_Active.m_States[i] = state;
                    //msg += $"\n    {cp.m_Entity.DX()} {cpd} (was:{scp.m_EntityIndex}.{scp.m_EntityVersion})";
                }
            }
            //QLog.Debug(msg);
        }

        /// <summary>
        /// Update selected objects to the new JobStates data
        /// </summary>
        /// <returns>Did the JobStates data include any buildings?</returns>
        internal bool Transform()
        {
            //string msg = $"{Time.frameCount} TA.Trans {m_Active} {m_Active.Count} |TAct:{_Tool.ToolAction}| Manip:{_Tool.Manipulating}";

            bool includesBuilding = false;
            foreach ((Moveable mv, int i) in m_StateMap)
            {
                if (!_Tool.Manipulating && (mv.m_Manipulatable & (QTypes.Manipulate.Normal | QTypes.Manipulate.Child)) == 0) continue;
                //msg += $"\n    {mv.D()} Now:{mv.Transform.m_Position.DX()} State:{m_Active.m_States[i].m_Position.DX()}";
                mv.MoveIt(this, m_Active.m_States[i], m_UpdateMove, m_UpdateRotate);

                if (!includesBuilding && m_Active.m_States[i].m_Identity == QTypes.Identity.Building)
                {
                    includesBuilding = true;
                }
            }
            //QLog.Debug(msg);

            return includesBuilding;
        }

        public override HashSet<Utils.IOverlay> GetOverlays()
        {
            return m_Snapper.GetOverlays();
        }

        public override void UpdateEntityReferences(Dictionary<Entity, Entity> toUpdate)
        {
            MIT.Log.Debug($"TransformAction.UpdateEntityReferences");
        }

        internal override void OnHoldEnd()
        {
            if (!m_HasMovedAction) return;

            _Tool.CreationPhase = CreationPhases.Create;
            _Tool.ToolAction = ToolActions.Do;
        }

        public void DebugDumpStates(string prefix = "", bool showOld = true, bool showNew = true)
        {
            StringBuilder sb = new(prefix);

            if (m_Old.Count == m_New.Count)
            {
                sb.AppendFormat($"States:{m_Old.Count}:");
                for (int i = 0; i < m_Old.Count; i++)
                {
                    string b = _Tool.ActiveSelection.Has(m_Old.m_States[i].m_Entity) ? "B" : "b";
                    string f = _Tool.ActiveSelection.HasFull(m_Old.m_States[i].m_Entity) ? "F" : "f";
                    if (showOld) sb.AppendFormat("\n  {0}{1} Old: {2}", b, f, m_Old.m_States[i]);

                    b = _Tool.ActiveSelection.Has(m_New.m_States[i].m_Entity) ? "B" : "b";
                    f = _Tool.ActiveSelection.HasFull(m_New.m_States[i].m_Entity) ? "F" : "f";
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
           

            QLog.Debug(sb.ToString());
        }
    }
}
