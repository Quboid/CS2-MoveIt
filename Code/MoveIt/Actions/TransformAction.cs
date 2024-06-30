using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Actions
{
    internal class TransformState : ActionState
    {
        internal NativeArray<State> m_States;
        internal float3 m_MoveDelta = 0f;
        internal float m_AngleDelta = 0f;
        internal bool m_IsNew = false;

        internal int Count => m_States.Length;

        internal TransformState(int length, bool isNew)
        {
            m_States = new NativeArray<State>(length, Allocator.Persistent);
            m_IsNew = isNew;
        }

        public override void Dispose()
        {
            if (m_States.IsCreated)
            {
                for (int i = 0; i < m_States.Length; i++)
                {
                    m_States[i].Dispose();
                }
            }
            m_States.Dispose();
        }

        public override string ToString()
        {
            return $"[TrState:{(m_IsNew ? "New" : "Old")},#{m_States.Length}]";
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
            QLookupFactory.Init(_Tool);

            HashSet<MVDefinition> fullDefinitions = _Tool.Selection.GetObjectsToTransformFull();
            HashSet<Moveable> fullSelection = new();
            fullDefinitions.ForEach(mvd => fullSelection.Add(_Tool.Moveables.GetOrCreate(mvd)));

            m_Old = new(fullSelection.Count, false);
            m_New = new(fullSelection.Count, true);
            m_Active = m_New;

            m_InitialBounds = _Tool.Selection.GetTotalBounds(MIT.TERRAIN_UPDATE_MARGIN);
            m_TerrainUpdateBounds = m_InitialBounds;
            m_Center = _Tool.Selection.Center;

            int c = 0;
            foreach (Moveable mv in fullSelection)
            {
                mv.UpdateYOffset();

                m_Old.m_States[c] = new(_Tool.EntityManager, ref QLookupFactory.Get(), mv);
                m_New.m_States[c] = new(_Tool.EntityManager, ref QLookupFactory.Get(), mv);
                c++;
            }

            m_Snapper = new(this);

            //m_Snapper.DebugDump();
            DebugDumpStates($"TransformAction.Ctor {_Tool.Selection.Name} ", showOld:true, showNew:false);
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

            QLookupFactory.Init(_Tool);

            m_TerrainUpdateBounds = _Tool.Selection.GetTotalBounds(MIT.TERRAIN_UPDATE_MARGIN);

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
                State old = m_Old.m_States[i];

                float3 position = (float3)matrix.MultiplyPoint(m_Old.m_States[i].m_Position - m_Center);
                float3 oldAngles = m_Old.m_States[i].m_Rotation.ToEulerDegrees();
                quaternion rotation = Quaternion.Euler(oldAngles.x, oldAngles.y + AngleDelta, oldAngles.z);
                //float angle = (m_Old.m_States[i].m_Angle + AngleDelta + 360) % 360;
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
                    m_Entity            = old.m_Entity,
                    m_Accessor          = new(_Tool.EntityManager, ref QLookupFactory.Get(), old.m_Entity, old.m_Identity),
                    m_Parent            = old.m_Parent,
                    m_ParentKey         = old.m_ParentKey,
                    m_Prefab            = old.m_Prefab,
                    m_Position          = position,
                    m_InitialPosition   = old.m_InitialPosition,
                    m_Rotation          = rotation,
                    m_InitialRotation   = old.m_InitialRotation,
                    m_YOffset           = newYOffset,
                    m_InitialYOffset    = old.m_InitialYOffset,
                    m_Identity          = old.m_Identity,
                    m_ObjectType        = old.m_ObjectType,
                    m_IsManipulatable   = old.m_IsManipulatable,
                    m_IsManaged         = old.m_IsManaged,
                    m_InitialCurve      = old.m_InitialCurve,
                };
            }
            m_Active = m_New;

            //DebugDumpStates($"Do", showOld:false, showNew:true);
        }

        public override void Undo()
        {
            //MIT.Log.Debug($"{Time.frameCount} TA.Undo |{_Tool.ToolAction}|");
            m_UpdateMove = true;
            m_UpdateRotate = true;
            m_Active = m_Old;
            UpdateStates();
            _Tool.CreationPhase = CreationPhases.Create;
            _Tool.Queue.CreationAction = this;
            //_Tool.Create(this);
            //_Tool.Moveables.UpdateAllOverlays();
            //DebugDumpStates($"Undo");

            base.Undo();
        }

        public override void Redo()
        {
            //MIT.Log.Debug($"{Time.frameCount} TA.Redo |{_Tool.ToolAction}|");
            m_UpdateMove = true;
            m_UpdateRotate = true;
            m_Active = m_New;
            UpdateStates();
            _Tool.CreationPhase = CreationPhases.Create;
            _Tool.Queue.CreationAction = this;
            //_Tool.Create(this);
            //_Tool.Moveables.UpdateAllOverlays();
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
                    if (!State.IsValid(_Tool.EntityManager, mvd.m_Parent))
                    {
                        toRemove.Add(i);
                        continue;
                    }
                    MVControlPoint cp = _Tool.ControlPointManager.GetOrCreate(mvd);
                    state.UpdateEntity(_Tool.EntityManager, ref QLookupFactory.Get(), cp.m_Entity);
                    m_Active.m_States[i] = state;
                }
                else if (!m_Active.m_States[i].IsValid(_Tool.EntityManager))
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
        /// Update selected objects to the new JobStates data
        /// </summary>
        internal void Transform()
        {
            //string msg = $"{Time.frameCount} TA.Trans {m_Active} {m_Active.Count} |TAct:{_Tool.ToolAction}| Manip:{_Tool.m_IsManipulateMode}";

            for (int i = 0; i < m_Active.m_States.Length; i++)
            {
                Moveable mv = _Tool.Moveables.GetOrCreate(m_Active.m_States[i].Definition);
                //msg += $"\n    {mv.D()} {m_Active.m_States[i].Definition} ";

                if (!_Tool.IsManipulating && mv.IsManipulatable) continue;
                if (_Tool.IsManipulating && !mv.IsManipulatable) continue;
                //msg += $" Now:{mv.Transform.m_Position.DX()} State:{m_Active.m_States[i].m_Position.DX()}";

                mv.MoveIt(this, m_Active.m_States[i], m_UpdateMove, m_UpdateRotate);

                mv.UpdateOverlay();
            }

            //QLog.Debug(msg);
            _Tool.m_SelectionDirty = true;
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
                    MVDefinition oldState = new(m_Old.m_States[i].m_Identity, m_Old.m_States[i].m_Entity, m_IsManipulationMode, m_Old.m_States[i].m_IsManaged, m_Old.m_States[i].m_Parent, m_Old.m_States[i].m_ParentKey);
                    MVDefinition newState = new(m_New.m_States[i].m_Identity, m_New.m_States[i].m_Entity, m_IsManipulationMode, m_New.m_States[i].m_IsManaged, m_New.m_States[i].m_Parent, m_New.m_States[i].m_ParentKey);
                    string b = _Tool.Selection.Has(oldState) ? "B" : "b";
                    string f = _Tool.Selection.HasFull(oldState) ? "F" : "f";
                    if (showOld) sb.AppendFormat("\n  {0}{1} Old: {2}", b, f, m_Old.m_States[i]);

                    b = _Tool.Selection.Has(newState) ? "B" : "b";
                    f = _Tool.Selection.HasFull(newState) ? "F" : "f";
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
