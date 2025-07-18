using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Selection;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace MoveIt.Actions
{
    public enum Phases
    {
        None,
        Initialise,
        Do,
        Undo,
        Redo,
        Finalise,
        Cleanup,
        Complete,
    }

    public struct Neighbour
    {
        internal Entity m_Entity;
        internal Identity m_Identity;
        internal Bezier4x3 m_InitialCurve;

        internal readonly MVDefinition Definition => new(m_Identity, m_Entity, false);
    }

    internal class ActionState : IDisposable
    {
        protected readonly MIT _MIT = MIT.m_Instance;

        public virtual void Dispose() { }

        public override string ToString()
        {
            return "(normal)";
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
        /// <summary>
        /// The Action.Phases phase the current action is in
        /// </summary>
        public static Phases Phase { get => _Phase; set => _Phase = value; }
        protected static Phases _Phase = Phases.None;

        protected static readonly MIT _MIT = MIT.m_Instance;

        public abstract string Name { get; }
        public int m_InitialFrame;

        /// <summary>
        /// Outer bounds of rectangle that this action affects on creation (e.g. for network updates)
        /// </summary>
        internal Bounds3 m_InitialBounds;

        /// <summary>
        /// Outer bounds of rectangle that this action affects on completion (e.g. for network updates)
        /// </summary>
        internal Bounds3 m_FinalBounds;

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

        /// <summary>
        /// Can this action use low sensitivty mode? (Slower mouse movement, no overlays)
        /// </summary>
        internal bool m_CanUseLowSensitivity = false;

        public Action()
        {
            m_IsManipulationMode = _MIT.m_IsManipulateMode;
            m_InitialFrame = UnityEngine.Time.frameCount;
            Phase = Phases.Initialise;
        }

        //public virtual HashSet<Overlays.Overlay> GetOverlays(Overlays.ToolFlags toolFlags)
        //{
        //    return new();
        //}

        public virtual ActionState GetActionState() => new();

        /// <summary>
        /// Initialise the action, runs immediately after action's ctor
        /// </summary>
        public virtual void Initialise() { }
        /// <summary>
        /// Run the action, called once per frame as long as needed
        /// </summary>
        public virtual void Do() { }
        /// <summary>
        /// Undo the action
        /// </summary>
        public virtual void Undo() { Phase = Phases.Complete; }
        /// <summary>
        /// Redo the action
        /// </summary>
        public virtual void Redo() { Phase = Phases.Complete; }
        /// <summary>
        /// Finish the action, runs the frame after when the action defines it, if at all
        /// </summary>
        public virtual void Finalise() { Phase = Phases.Cleanup; }
        /// <summary>
        /// Cleanup the action, runs the frame after Finalise(), if at all
        /// </summary>
        public virtual void Cleanup() { Phase = Phases.Complete; }

        /// <summary>
        /// This action is no longer the current one
        /// Runs directly before Unarchive(), Undo(), Redo(), and new action Pushed
        /// </summary>
        /// <param name="toolState">The tool action at the time of archiving</param>
        /// <param name="idx">This action's queue index</param>
        public virtual void Archive(Phases phase, int idx)
        {
            string oldSelState = _SelectionState is null ? "<null>" : _SelectionState.Debug();
            int old = _MIT.Selection.Count;
            int oldFull = _MIT.Selection.CountFull;
            string moveables = _MIT.Selection.DebugSelection();
            _SelectionState = SelectionState.SelectionToState(_MIT.m_IsManipulateMode, _MIT.Selection.Definitions);
            string newSelection = _SelectionState.Debug();
            MIT.Log.Debug($"ARCHIVE {idx}:{_MIT.Queue.Current.Name} OldPhase:{phase} Definitions:{old}/{oldFull}->{_SelectionState.Count}\nOld: {oldSelState}\nNew: {newSelection}\nAll Moveables: {moveables}");
        }

        /// <summary>
        /// This action will be restored to the current one
        /// Runs directly after Archive(), before Undo() and Redo(), not on a newly Pushed action
        /// </summary>
        /// <param name="toolAction">The tool action at the time of unarchiving</param>
        /// <param name="idx">This action's queue index</param>
        public virtual void Unarchive(Phases phase, int idx)
        {
            string oldSelState = _SelectionState is null ? "<null>" : _SelectionState.Debug();
            int oldSelStateC = _SelectionState.Count;
            string moveables = _MIT.Selection.DebugSelection();
            _SelectionState = _SelectionState.CleanDefinitions();
            string newSelection = _SelectionState.Debug();
            MIT.Log.Debug($"UNARCHIVE {idx}:{_MIT.Queue.Current.Name} OldPhase:{phase} Definitions:{oldSelStateC}->{_SelectionState.Count}\nOld: {oldSelState}\nNew: {newSelection}\nAll Moveables: {moveables}");
        }

        /// <summary>
        /// Get this action's select states without modification
        /// </summary>
        public virtual List<MVDefinition> GetSelectionStates() => SelectionState.CleanDefinitions(_SelectionState);

        /// <summary>
        /// Does this action use the defined object? (Excluding used in selection or hover)
        /// </summary>
        /// <param name="mvd">The object to check</param>
        /// <returns>Is this object used?</returns>
        internal virtual bool Uses(MVDefinition mvd)
            => false;

        /// <summary>
        /// Clean up unselected Moveables. Must be run AFTER removing from actual Selection or unneeded Moveables will be kept
        /// </summary>
        /// <param name="definitions"></param>
        protected void Deselect(IEnumerable<MVDefinition> definitions)
        {
            foreach (var mvd in definitions)
            {
                if (_MIT.Moveables.TryGet(mvd, out Moveable mv))
                {
                    mv.OnDeselect();
                }
                else
                {
                    MIT.Log.Warning($"Tried to Deselect {mvd}, but no Moveable found.\n{_MIT.Moveables.DebugFull()}\n{QCommon.GetStackTrace()}");
                }
            }
        }

        public override string ToString()
        {
            return $"{Name}/{m_InitialFrame}";
        }
    }
}
