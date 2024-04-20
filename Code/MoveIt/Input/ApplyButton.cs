using Game.Input;
using MoveIt.Actions;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;

namespace MoveIt.Input
{
    internal class ApplyButton : InputButton
    {
        internal ApplyButton(ProxyAction action) : base(action) { }
        internal ApplyButton(string mapName, string actionName) : base(mapName, actionName) { }

        internal override void OnPress()
        {
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.Hover.OnPress = _Tool.Hover.Entity;
                _Tool.m_ClickPositionAbs = _Tool.m_PointerPos;

                if (_Tool.UseMarquee)
                {
                    if (!_Tool.Hover.IsSelected)
                    {
                        _Tool.ToolState = ToolStates.DrawingSelection;
                        _Tool.m_Marquee = new(_Tool.m_PointerPos);
                    }
                }
                else if (!_Tool.Hover.IsNull)
                {
                    ObjectClicked();
                }
            }

            //MIT.Log.Debug($"ApplyButton.OnPress ts:{_Tool.ToolState}");
        }

        internal override void OnClick()
        {
            if (_Tool.UseMarquee)
            {
                ObjectClicked();
            }
            //MIT.Log.Debug($"ApplyButton.OnClick {Selection.Debug()}");
        }

        private void ObjectClicked()
        {
            if (_Tool.ToolState != ToolStates.Default && _Tool.ToolState != ToolStates.DrawingSelection) return;
            if (_Tool.Hover.IsNull) return;

            _Tool.ToolState = ToolStates.Default;
            _Tool.Hover.Moveable?.OnClick();

            //QLog.Debug($"ObjClick {_Tool.Hover} man:{_Tool.m_IsManipulateMode}");

            if (_Tool.Manipulating && !_Tool.Hover.IsManipulatable)
            {
                MIT_ToolTipSystem.instance.Set($"Right-Click to exit Manipulation Mode", 1.5f);
                return;
            }

            if (_Tool.Hover.IsManipulatable && (_Tool.Manipulating || QKeyboard.Alt))
            {
                if (!_Tool.Hover.IsSelected || Queue.Current is not SelectManipulateAction)
                {
                    Queue.Push(new SelectManipulateAction(QKeyboard.Shift, (_Tool.Hover.Moveable.m_Manipulatable & QTypes.Manipulate.Child) > 0));
                }

                _Tool.SetManipulationMode(true);
            }
            else
            {
                if (!_Tool.Hover.IsSelected || Queue.Current is not SelectAction)
                {
                    Queue.Push(new SelectAction(QKeyboard.Shift));
                }
            }

            if (Queue.Current is SelectActionBase selectAction)
            {
                selectAction.AddHovered(QKeyboard.Shift);
            }
            _Tool.ToolAction = ToolActions.Do;
        }

        internal override void OnHold()
        {
            if (_Tool.ToolState == ToolStates.Default && !_Tool.Hover.OnPress.Equals(Entity.Null))
            {
                _Tool.StartMove();
            }
            //MIT.Log.Debug($"ApplyButton.OnHold ts:{_Tool.ToolState}, prev:{_Tool.Hovered},");
        }

        internal override void OnHoldEnd()
        {
            if (_Tool.ToolState == ToolStates.ApplyButtonHeld)
            {
                _Tool.EndMove();
            }
            //MIT.Log.Debug($"ApplyButton.OnHoldEnd ts:{_Tool.ToolState}");
        }

        internal override void OnRelease()
        {
            _Tool.Hover.OnPress = Entity.Null;
            if (_Tool.ToolState == ToolStates.DrawingSelection)
            {
                _Tool.ToolState = ToolStates.Default;
                _Tool.m_Marquee = null;
            }
            //MIT.Log.Debug($"ApplyButton.OnRelease");
        }
    }
}
