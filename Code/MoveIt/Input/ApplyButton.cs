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

        protected override void OnPress()
        {
            m_Status = ButtonStatus.Down;
            if (_MIT.MITState == MITStates.Default || _MIT.MITState == MITStates.ToolActive)
            {
                _MIT.Hover.Normal.OnPress = _MIT.Hover.Normal.Definition;
                _MIT.Hover.Child.OnPress = _MIT.Hover.Child.Definition;
                //QLog.Debug($"ONPRESS {_MIT.Hovered.OnPress} :: {_MIT.Hover.Normal.OnPress}/{_MIT.Hover.Child.OnPress} click:{(!_MIT.Hovered.IsNull && !_MIT.UseMarquee ? "true" : "false")}");
                _MIT.m_ClickPositionAbs = _MIT.m_PointerPos;

                if (!_MIT.Hovered.IsNull && !_MIT.UseMarquee)
                {
                    ObjectClicked();
                }
            }
        }

        protected override void OnClick()
        {
            if (_MIT.UseMarquee)
            {
                ObjectClicked();
            }
        }

        private void ObjectClicked()
        {
            QLog.Debug($"OBJCLICKED Hov:{_MIT.Hovered.Definition.E()} HovManip:{_MIT.Hovered.IsManipulatable} HovSel:{_MIT.Hovered.IsSelected} Press:{_MIT.Hovered.OnPress.E()} HovChild:{_MIT.Hovered.Definition.IsChild} ToolManip:{_MIT.m_IsManipulateMode}");
            if (_MIT.MITState == MITStates.ToolActive)
            {
                if (_MIT.Hovered.IsNull) return;
                _MIT.ToolboxManager.ObjectClicked(_MIT.Hovered.MV);
                return;
            }

            if (_MIT.MITState != MITStates.Default) return;
            if (_MIT.Hovered.IsNull) return;
            if (_MIT.Hovered.MV is null) return;

            _MIT.MITState = MITStates.Default;
            _MIT.Hovered.MV.OnClick();

            if (_MIT.m_IsManipulateMode && !_MIT.Hovered.IsManipulatable)
            {
                Systems.MIT_ToolTipSystem.instance.Set($"Right-Click to exit Manipulation Mode", 1.5f);
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

        protected override void OnDragStart()
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

        protected override void OnDragEnd()
        {
            if (_MIT.MITState != MITStates.DrawingSelection) return;
            
            if (_MIT.Queue.Current is SelectMarqueeAction sma)
            {
                sma.AddMarqueeSelection(_MIT.m_Marquee, false);
            }
            else
            {
                MIT.Log.Debug($"Update DrawingSelection but current action is {_MIT.Queue.Current.Name}");
            }
        }

        protected override void OnHold()
        {
            //QLog.Bundle("ONHOLD", $"OnPress:{_MIT.Hover.TopPressed.E()}-Null:{_MIT.Hover.TopPressed.IsNull} :: {_MIT.Hover.Normal.OnPress.E()}/{_MIT.Hover.Child.OnPress.E()} status:{m_Status}, MITState:{_MIT.MITState}");

            if (m_Status == ButtonStatus.Down && _MIT.MITState == MITStates.Default && !_MIT.Hover.TopPressed.IsNull)
            {
                _MIT.MoveStart();
            }
            if (_MIT.MITState == MITStates.ApplyButtonHeld)
            {
                _MIT.UpdateSensitivityMode();
            }
        }

        protected override void OnHoldEnd()
        {
            if (m_Status == ButtonStatus.Down && _MIT.MITState == MITStates.ApplyButtonHeld)
            {
                _MIT.EndMove();
            }
            _MIT.ProcessSensitivityMode(false);
        }

        protected override void OnRelease()
        {
            m_Status = ButtonStatus.None;
            _MIT.Hovered.OnPress = new(true);
            if (_MIT.MITState == MITStates.DrawingSelection)
            {
                _MIT.MITState = MITStates.Default;
            }
            _MIT.m_Marquee?.Dispose();
            _MIT.m_Marquee = null;
        }
    }
}
