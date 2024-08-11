using Game.Input;
using MoveIt.Actions.Select;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Input
{
    internal class ApplyButton : InputButton
    {
        internal ApplyButton(ProxyAction action) : base(action) { }
        internal ApplyButton(string mapName, string actionName) : base(mapName, actionName) { }

        internal override void OnPress()
        {
            m_Status = ButtonStatus.Down;
            if (_MIT.MITState == MITStates.Default || _MIT.MITState == MITStates.ToolActive)
            {
                _MIT.Hovered.OnPress = _MIT.Hovered.Definition;
                _MIT.m_ClickPositionAbs = _MIT.m_PointerPos;

                if (!_MIT.Hovered.IsNull && !_MIT.UseMarquee)
                {
                    ObjectClicked();
                }
            }
        }

        internal override void OnClick()
        {
            if (_MIT.UseMarquee)
            {
                ObjectClicked();
            }
        }

        private void ObjectClicked()
        {
            if (_MIT.MITState == MITStates.ToolActive)
            {
                if (_MIT.Hovered.IsNull) return;
                _MIT.ToolboxManager.ObjectClicked(_MIT.Hovered.MV);
                return;
            }

            if (_MIT.MITState != MITStates.Default) return;
            if (_MIT.Hovered.IsNull) return;

            _MIT.MITState = MITStates.Default;
            _MIT.Hovered.MV?.OnClick();

            if (_MIT.m_IsManipulateMode && !_MIT.Hovered.IsManipulatable)
            {
                MIT_ToolTipSystem.instance.Set($"Right-Click to exit Manipulation Mode", 1.5f);
                return;
            }

            if (_MIT.IsManipulating && _MIT.Hovered.IsManipulatable)
            {
                _MIT.SetManipulationMode(true);
                if (!_MIT.Hovered.IsSelected)
                {
                    _MIT.Queue.Push(new SelectAction(true, QKeyboard.Shift, _MIT.Hovered.MV.IsManipChild));
                    _MIT.Queue.Do();
                }
            }
            else
            {
                if (!_MIT.Hovered.IsSelected || QKeyboard.Shift)
                {
                    _MIT.Queue.Push(new SelectAction(false, QKeyboard.Shift));
                    _MIT.Queue.Do();
                }
            }

            if (_MIT.Queue.Current is SelectAction selectAction)
            {
                selectAction.AddHovered(QKeyboard.Shift);
            }
        }

        internal override void OnDragStart()
        {
            if (_MIT.UseMarquee)
            {
                if (!_MIT.Hovered.IsSelected)
                {
                    _MIT.MITState = MITStates.DrawingSelection;
                    _MIT.m_Marquee = new(_MIT.m_PointerPos);
                }
            }
        }

        internal override void OnDragEnd()
        {
            if (_MIT.MITState == MITStates.DrawingSelection)
            {
                if (_MIT.Queue.Current is SelectMarqueeAction sma)
                {
                    sma.AddMarqueeSelection(_MIT.m_Marquee, false);
                }
                else
                {
                    MIT.Log.Debug($"Update DrawingSelection but current action is {_MIT.Queue.Current.Name}");
                }
            }
        }

        internal override void OnHold()
        {
            if (m_Status == ButtonStatus.Down && _MIT.MITState == MITStates.Default && !_MIT.Hovered.OnPress.IsNull)
            {
                _MIT.MoveStart();
            }
            if (_MIT.MITState == MITStates.ApplyButtonHeld)
            {
                _MIT.UpdateSensitivityMode();
            }
        }

        internal override void OnHoldEnd()
        {
            if (m_Status == ButtonStatus.Down && _MIT.MITState == MITStates.ApplyButtonHeld)
            {
                _MIT.EndMove();
            }
            _MIT.ProcessSensitivityMode(false);
        }

        internal override void OnRelease()
        {
            m_Status = ButtonStatus.None;
            _MIT.Hovered.OnPress = new();
            if (_MIT.MITState == MITStates.DrawingSelection)
            {
                _MIT.MITState = MITStates.Default;
            }
            _MIT.m_Marquee?.Dispose();
            _MIT.m_Marquee = null;
        }
    }
}
