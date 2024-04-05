using MoveIt.Actions;
using MoveIt.Tool;
using QCommonLib;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Managers
{
    public class HotkeyManager : MIT_Manager
    {
        private readonly QKeyListener _KeyListener;
        private readonly QKeyEventReaction _Key_PageUp;
        private readonly QKeyEventReaction _Key_PageDown;

        private long _KeyTime;

        public HotkeyManager()
        {
            _KeyListener = new GameObject("MIT_KeyListener").AddComponent<QKeyListener>();
            _KeyListener.m_tool = _Tool;

            _Key_PageUp = new QKeyEventReaction(KeyCode.PageUp, EventModifiers.None, QKeyListenerContexts.InTool, _Tool);
            _Key_PageDown = new QKeyEventReaction(KeyCode.PageDown, EventModifiers.None, QKeyListenerContexts.InTool, _Tool);

            _KeyListener.RegisterKeyAction(KeyCode.M, EventModifiers.None, QKeyListenerContexts.Default, KeyHandler_Activate);
            _KeyListener.RegisterKeyAction(KeyCode.M, EventModifiers.None, QKeyListenerContexts.InTool, KeyHandler_Deactivate);
            _KeyListener.RegisterKeyAction(KeyCode.M, EventModifiers.Control, QKeyListenerContexts.InTool, KeyHandler_ToggleMarquee);
            _KeyListener.RegisterKeyAction(KeyCode.M, EventModifiers.Alt, QKeyListenerContexts.InTool, KeyHandler_ToggleManipulate);
            _KeyListener.RegisterKeyAction(KeyCode.Z, EventModifiers.Control, QKeyListenerContexts.InTool, KeyHandler_Undo);
            _KeyListener.RegisterKeyAction(KeyCode.Z, EventModifiers.Control | EventModifiers.Shift, QKeyListenerContexts.InTool, KeyHandler_Redo);
            _KeyListener.RegisterKeyAction(KeyCode.D, EventModifiers.Control, QKeyListenerContexts.InTool, KeyHandler_DebugFreeze);
            _KeyListener.RegisterKeyAction(KeyCode.D, EventModifiers.Control | EventModifiers.Shift, QKeyListenerContexts.InTool, KeyHandler_DebugClear);
            _KeyListener.RegisterKeyAction(KeyCode.D, EventModifiers.Alt, QKeyListenerContexts.InTool, KeyHandler_ClearSelection);
        }

        ~HotkeyManager()
        {
            Object.Destroy(_KeyListener);
        }

        internal bool ProcessHotkeys()
        {
            if (_Tool.ToolState == ToolStates.Default && _Tool.ActiveSelection.Exists)
            {
                if (ProcessKeyMovement(out float3 direction, out float _))
                {
                    if (Queue.Current is not TransformKeyAction)
                    {
                        TransformKeyAction ta = new();
                        Queue.Push(ta);
                    }

                    TransformKeyAction tka = Queue.Current as TransformKeyAction;
                    tka.Process(direction);

                    return true;
                }
            }
            return false;
        }

        private bool ProcessKeyMovement(out float3 direction, out float angle)
        {
            direction = float3.zero;
            angle = 0;

            float magnitude = 8f;
            if (QKeyboard.Shift) magnitude *= QKeyboard.Alt ? 64f : 8f;
            if (QKeyboard.Control) magnitude /= QKeyboard.Alt ? 64f : 8f;

            if (_Key_PageUp.IsPressed(false))
            {
                direction.y += magnitude;
            }
            if (_Key_PageDown.IsPressed(false))
            {
                direction.y -= magnitude;
            }

            if (!direction.Equals(float3.zero) || angle != 0)
            {
                if (_KeyTime == 0)
                {
                    _KeyTime = Stopwatch.GetTimestamp();
                    return true;
                }
                else if (QCommon.ElapsedMilliseconds(_KeyTime) >= 333)
                {
                    return true;
                }
            }
            else
            {
                _KeyTime = 0;
            }

            return false;
        }


        public void KeyHandler_Activate()
        {
            _Tool.RequestEnable();
        }

        public void KeyHandler_Deactivate()
        {
            _Tool.RequestDisable();
        }

        public void KeyHandler_ToggleMarquee()
        {
            _Tool.SetSelectionMode();
        }

        public void KeyHandler_ToggleManipulate()
        {
            _Tool.SetManipulationMode();
        }

        public void KeyHandler_DebugFreeze()
        {
            _Tool.m_OverlaySystem.m_DebugFreeze = !_Tool.m_OverlaySystem.m_DebugFreeze;
        }

        public void KeyHandler_DebugClear()
        {
            _Tool.m_OverlaySystem.DebugClear();
            _Tool.m_RenderSystem.Clear();
        }
        
        public void KeyHandler_ClearSelection()
        {
            _Tool.Selection.Clear();
            _Tool.ControlPointManager.Clear();
            Queue.Invalidate();
        }

        public void KeyHandler_Undo()
        {
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.ToolAction = ToolActions.Undo;
            }
        }

        public void KeyHandler_Redo()
        {
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.ToolAction = ToolActions.Redo;
            }
        }
    }
}
