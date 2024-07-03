using Game.Input;
using MoveIt.Actions;
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
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.Hover.OnPress = _Tool.Hover.Definition;
                _Tool.m_ClickPositionAbs = _Tool.m_PointerPos;

                if (!_Tool.Hover.IsNull && !_Tool.UseMarquee)
                {
                    ObjectClicked();
                }
            }
        }

        internal override void OnClick()
        {
            if (_Tool.UseMarquee)
            {
                ObjectClicked();
            }
        }

        private void ObjectClicked()
        {
            if (_Tool.ToolState != ToolStates.Default) return;
            if (_Tool.Hover.IsNull) return;

            _Tool.ToolState = ToolStates.Default;
            _Tool.Hover.MV?.OnClick();

            if (_Tool.m_IsManipulateMode && !_Tool.Hover.IsManipulatable)
            {
                MIT_ToolTipSystem.instance.Set($"Right-Click to exit Manipulation Mode", 1.5f);
                return;
            }

            if (_Tool.IsManipulating && _Tool.Hover.IsManipulatable)
            {
                _Tool.SetManipulationMode(true);
                if (!_Tool.Hover.IsSelected)
                {
                    _Tool.Queue.Push(new SelectAction(true, QKeyboard.Shift, _Tool.Hover.MV.IsManipChild));
                    _Tool.Queue.Do();
                }
            }
            else
            {
                if (!_Tool.Hover.IsSelected || QKeyboard.Shift)
                {
                    _Tool.Queue.Push(new SelectAction(false, QKeyboard.Shift));
                    _Tool.Queue.Do();
                }
            }

            if (_Tool.Queue.Current is SelectAction selectAction)
            {
                selectAction.AddHovered(QKeyboard.Shift);
            }
        }

        internal override void OnDragStart()
        {
            if (_Tool.UseMarquee)
            {
                if (!_Tool.Hover.IsSelected)
                {
                    _Tool.ToolState = ToolStates.DrawingSelection;
                    _Tool.m_Marquee = new(_Tool.m_PointerPos);
                }
            }
        }

        internal override void OnDragEnd()
        {
            if (_Tool.ToolState == ToolStates.DrawingSelection)
            {
                if (_Tool.Queue.Current is SelectMarqueeAction sma)
                {
                    sma.AddMarqueeSelection(_Tool.m_Marquee, false);
                }
                else
                {
                    MIT.Log.Debug($"Update DrawingSelection but current action is {_Tool.Queue.Current.Name}");
                }
            }
        }

        internal override void OnHold()
        {
            if (m_Status == ButtonStatus.Down && _Tool.ToolState == ToolStates.Default && !_Tool.Hover.OnPress.IsNull)
            {
                _Tool.StartMove();
            }
        }

        internal override void OnHoldEnd()
        {
            if (m_Status == ButtonStatus.Down && _Tool.ToolState == ToolStates.ApplyButtonHeld)
            {
                _Tool.EndMove();
            }
        }

        internal override void OnRelease()
        {
            m_Status = ButtonStatus.None;
            _Tool.Hover.OnPress = new();
            if (_Tool.ToolState == ToolStates.DrawingSelection)
            {
                _Tool.ToolState = ToolStates.Default;
            }
            _Tool.m_Marquee?.Dispose();
            _Tool.m_Marquee = null;
        }
    }
}
