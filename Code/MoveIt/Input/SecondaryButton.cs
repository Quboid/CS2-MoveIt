using Game.Input;
using MoveIt.Actions;
using MoveIt.Tool;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace MoveIt.Input
{
    internal class SecondaryButton : InputButton
    {
        internal SecondaryButton(ProxyAction action) : base(action) { }
        internal SecondaryButton(string mapName, string actionName) : base(mapName, actionName) { }

        internal override void OnPress()
        {
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.m_MouseStartX = Mouse.current.position.x.ReadValue();
            }
        }

        internal override void OnClick()
        {
            if (_Tool.ToolState != ToolStates.Default) return;

            if (_Tool.Manipulating)
            {
                EndManipulationAction action = new();
                Queue.Push(action);
                action.Do();

                return;
            }

            if (Queue.Current is not SelectAction)
            {
                SelectAction action = new();
                Queue.Push(action);
            }
            else
            {
                _Tool.Selection.Clear();
                _Tool.ControlPointManager.Clear();
                Queue.Invalidate();
            }

            _Tool.CreationPhase = CreationPhases.Cleanup;

            //MIT.Log.Debug($"SecondaryButton.OnClick {Queue.Debug()}");
        }

        internal override void OnHold()
        {
            //MIT.Log.Debug($"SecondaryButton.OnDrag");
            if (_Tool.ToolState == ToolStates.Default)
            {
                if (_Tool.Hover.LastValid == Entity.Null) return;

                _Tool.StartRotation();
            }
        }

        internal override void OnHoldEnd()
        {
            //MIT.Log.Debug($"SecondaryButton.OnHoldEnd ts:{ToolState}");
            if (Queue.Current is TransformAction ta && _Tool.ToolState == ToolStates.SecondaryButtonHeld)
            {
                _Tool.EndRotation();
            }
        }

        internal override void OnRelease()
        {
            //MIT.Log.Debug($"SecondaryButton.OnRelease");
        }
    }
}
