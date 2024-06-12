using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Selection;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;

namespace MoveIt.Actions
{
    internal class ActionState : IDisposable
    {
        protected readonly MIT _Tool = MIT.m_Instance;

        public virtual void Dispose() { }

        public override string ToString()
        {
            return string.Empty;
        }
    }

    internal class NullAction : Action
    {
        public override string Name => "NullAction";

        public NullAction()
        {
            _SelectionState = new(false, new());
        }
    }

    internal abstract class Action
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        public abstract string Name { get; }
        public int m_InitialFrame;
        internal Bounds3 m_UpdateArea;

        /// <summary>
        /// Was the player in Manipulation Mode when action was created?
        /// </summary>
        internal bool m_IsManipulationMode;

        /// <summary>
        /// The selected moveables' definitions at the end of this action, saved in Archive()
        /// </summary>
        protected SelectionState _SelectionState;

        /// <summary>
        /// Has anything moved for the creation engine to deal with this frame?
        /// </summary>
        internal bool m_UpdateMove = false;

        /// <summary>
        /// Has anything rotated for the creation engine to deal with this frame?
        /// </summary>
        internal bool m_UpdateRotate = false;

        public Action()
        {
            m_IsManipulationMode = _Tool.m_IsManipulateMode;
            m_InitialFrame = UnityEngine.Time.frameCount;
        }

        //public virtual HashSet<Overlays.Overlay> GetOverlays(Overlays.ToolFlags toolFlags)
        //{
        //    return new();
        //}

        public virtual ActionState GetActionState() => new();

        public virtual void Do() { }
        public virtual void Undo() { }
        public virtual void Redo() { }

        /// <summary>
        /// This action is no longer the current one
        /// Runs directly before Unarchive(), Undo(), Redo(), and new action Pushed
        /// </summary>
        /// <param name="toolState">The tool action at the time of archiving</param>
        /// <param name="idx">This action's queue index</param>
        public void Archive(ToolActions toolAction, int idx)
        {
            int old = _Tool.Selection.Count;
            int oldFull = _Tool.Selection.CountFull;
            _Tool.Selection.DebugDumpSelection();
            _SelectionState = SelectionState.SelectionToState(_Tool.m_IsManipulateMode, _Tool.Selection.Definitions);
            MIT.Log.Debug($"Archive {idx}:{_Tool.Queue.Current.Name} ToolAction:{toolAction} Definitions:{old}/{oldFull}->{_SelectionState.Count}");
        }

        /// <summary>
        /// This action will be restored to the current one
        /// Runs directly after Archive(), before Undo() and Redo(), not on a newly Pushed action
        /// </summary>
        /// <param name="toolAction">The tool action at the time of unarchiving</param>
        /// <param name="idx">This action's queue index</param>
        public virtual void Unarchive(ToolActions toolAction, int idx)
        {
            int old = _SelectionState.Count;
            _SelectionState = _SelectionState.CleanDefinitions();
            MIT.Log.Debug($"Unarchive {idx}:{_Tool.Queue.Current.Name} ToolAction:{toolAction} Definitions:{old}->{_SelectionState.Count}");
        }

        /// <summary>
        /// Get this action's select states without modification
        /// </summary>
        public virtual List<MVDefinition> GetSelectionStates() => SelectionState.CleanDefinitions(_SelectionState);

        internal virtual void OnHold() { }
        internal virtual void OnHoldEnd() { }

        /// <summary>
        /// Clean up unselected Moveables. Must be run AFTER removing from actual Selection or unneeded Moveables will be kept
        /// </summary>
        /// <param name="definitions"></param>
        protected void Deselect(IEnumerable<MVDefinition> definitions)
        {
            MIT.Log.Debug($"Action.Deselect {MIT.DebugDefinitions(definitions)}" +
                $"\n{_Tool.Moveables.DebugFull()}" +
                $"\n{QCommon.GetStackTrace(3)}");
            foreach (var mvd in definitions)
            {
                if (_Tool.Moveables.TryGet(mvd, out Moveable mv))
                {
                    mv.OnDeselect();
                }
                else
                {
                    MIT.Log.Warning($"Tried to Deselect {mvd}, but no Moveable found.\n{_Tool.Moveables.DebugFull()}\n{QCommon.GetStackTrace()}");
                }
            }
        }

        public override string ToString()
        {
            return $"{Name}/{m_InitialFrame}";
        }
    }
}
