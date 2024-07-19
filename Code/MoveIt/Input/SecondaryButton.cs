using Game.Input;
using MoveIt.Actions;
using MoveIt.Tool;
using UnityEngine.InputSystem;

namespace MoveIt.Input
{
    internal class SecondaryButton : InputButton
    {
        internal SecondaryButton(ProxyAction action) : base(action) { }
        internal SecondaryButton(string mapName, string actionName) : base(mapName, actionName) { }

        internal override void OnPress()
        {
            m_Status = ButtonStatus.Down;
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.m_MouseStartX = Mouse.current.position.x.ReadValue();
            }
        }

        internal override void OnClick()
        {
            if (_Tool.ToolState != ToolStates.Default) return;

            if (_Tool.IsManipulating && _Tool.Selection.Count == 0)
            {
                _Tool.SetManipulationMode(false);

                return;
            }

            _Tool.Queue.Push(new SelectAction());
            _Tool.Queue.Do();

            _Tool.CreationPhase = CreationPhases.Cleanup;

            //MIT.Log.Debug($"SecondaryButton.OnClick {Queue.Debug()}");
        }

        internal override void OnHold()
        {
            //MIT.Log.Debug($"SecondaryButton.OnDrag");
            if (m_Status == ButtonStatus.Down && _Tool.ToolState == ToolStates.Default)
            {
                if (_Tool.Hover.LastValid.IsNull) return;

                _Tool.RotationStart();
            }
        }

        internal override void OnHoldEnd()
        {
            //MIT.Log.Debug($"SecondaryButton.OnHoldEnd ts:{ToolState}");
            if (m_Status == ButtonStatus.Down && _Tool.Queue.Current is TransformAction ta && _Tool.ToolState == ToolStates.SecondaryButtonHeld)
            {
                _Tool.RotationEnd();
            }
        }

        internal override void OnRelease()
        {
            m_Status = ButtonStatus.None;
            //MIT.Log.Debug($"SecondaryButton.OnRelease");
        }
    }
}
