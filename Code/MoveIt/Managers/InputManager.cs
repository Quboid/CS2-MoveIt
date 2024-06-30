using MoveIt.Actions;
using MoveIt.Input;
using MoveIt.Tool;
using QCommonLib;
using System.Diagnostics;
using Unity.Mathematics;

namespace MoveIt.Managers
{
    public class InputManager : MIT_Manager
    {
        private readonly QKey_Binding _Key_MoveDown;
        private readonly QKey_Binding _Key_MoveUp;
        private readonly InputButton _ApplyAction;
        private readonly InputButton _SecondaryAction;

        private long _KeyTime;

        public InputManager()
        {
            _Key_MoveDown = _Tool.m_HotkeySystem.GetBinding(Systems.MIT_HotkeySystem.KEY_MOVEDOWN);
            _Key_MoveUp = _Tool.m_HotkeySystem.GetBinding(Systems.MIT_HotkeySystem.KEY_MOVEUP);

            _ApplyAction = new ApplyButton("Tool", "Apply");
            _SecondaryAction = new SecondaryButton("Tool", "Secondary Apply");
        }

        internal void OnToolEnable()
        {
            _ApplyAction.Enabled = true;
            _SecondaryAction.Enabled = true;
        }

        internal void OnToolDisable()
        {
            _ApplyAction.Enabled = false;
            _SecondaryAction.Enabled = false;
        }

        internal bool Process()
        {
            //QLog.Bundle("Keys", _Tool.m_HotkeySystem.DebugAllBindings() +
            //    $"\n        ApplyAction: {_ApplyAction.Enabled}" +
            //    $"\n    SecondaryAction: {_SecondaryAction.Enabled}");

            _ApplyAction.Update();
            _SecondaryAction.Update();

            if (_Tool.ToolState == ToolStates.Default && _Tool.Selection.Any)
            {
                if (ProcessKeyMovement(out float3 direction, out float _))
                {
                    if (_Tool.Queue.Current is not TransformKeyAction)
                    {
                        TransformKeyAction ta = new();
                        _Tool.Queue.Push(ta);
                    }

                    TransformKeyAction tka = _Tool.Queue.Current as TransformKeyAction;
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

            if (_Key_MoveUp.IsPressed)
            {
                direction.y += magnitude;
            }
            else if (_Key_MoveDown.IsPressed)
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
    }
}
