using MoveIt.Actions.Transform;
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
        private readonly QKey_Binding _Key_MoveDown2;
        private readonly QKey_Binding _Key_MoveUp2;
        private readonly InputButton _ApplyAction;
        private readonly InputButton _SecondaryAction;

        private long _KeyTime;

        public InputManager()
        {
            _Key_MoveDown = _MIT.m_InputSystem.GetBinding(Inputs.KEY_MOVEDOWN);
            _Key_MoveUp = _MIT.m_InputSystem.GetBinding(Inputs.KEY_MOVEUP);
            _Key_MoveDown2 = _MIT.m_InputSystem.GetBinding(Inputs.KEY_MOVEDOWN2);
            _Key_MoveUp2 = _MIT.m_InputSystem.GetBinding(Inputs.KEY_MOVEUP2);

            _ApplyAction = new ApplyButton(_MIT.m_InputSystem.MouseApply);
            _SecondaryAction = new SecondaryButton(_MIT.m_InputSystem.MouseCancel);
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
            //QLog.Bundle("Keys", _MIT.m_HotkeySystem.DebugAllBindings() +
            //    $"\n        ApplyAction: {_ApplyAction.Enabled}" +
            //    $"\n    SecondaryAction: {_SecondaryAction.Enabled}");

            _ApplyAction.Update();
            _SecondaryAction.Update();

            if (_MIT.MITState == MITStates.Default && _MIT.Selection.Any)
            {
                if (ProcessKeyMovement(out float3 direction, out float _))
                {
                    if (_MIT.Queue.Current is not TransformKeyAction)
                    {
                        TransformKeyAction ta = new();
                        _MIT.Queue.Push(ta);
                    }

                    TransformKeyAction tka = _MIT.Queue.Current as TransformKeyAction;
                    tka?.Process(direction);

                    return true;
                }
            }
            return false;
        }

        private bool ProcessKeyMovement(out float3 direction, out float angle)
        {
            direction = float3.zero;
            angle = 0;

            var magnitude = 8f;
            if (QKeyboard.Shift) magnitude *= QKeyboard.Alt ? 64f : 8f;
            if (QKeyboard.Control) magnitude /= QKeyboard.Alt ? 64f : 8f;

            if (_Key_MoveUp.IsPressed || _Key_MoveUp2.IsPressed)
            {
                direction.y += magnitude;
            }
            else if (_Key_MoveDown.IsPressed || _Key_MoveDown2.IsPressed)
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
