using MoveIt.Actions.Toolbox;
using MoveIt.Tool;
using MoveIt.UI.Foldout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Managers
{
    public enum Phases
    {
        None,
        Active,
        Fire,
        Processing,
        Finalize,
    }

    internal class ToolboxManager : MIT_Manager
    {
        internal Phases Phase { get; set; } = Phases.None;
        internal ToolBoxTool Active => _Active;
        private ToolBoxTool _Active;
        private Moveables.Moveable _Clicked;

        internal ToolboxManager()
        { }

        internal bool IsActive(string id)
            => _Active is not null && _Active.m_Id.Equals(id);

        internal void ObjectClicked(Moveables.Moveable mv)
        {
            if (_Active is null) return;

            _Clicked = mv;

            if (Phase == Phases.Active)
            {
                Phase = Phases.Fire;
            }
        }

        internal void Update()
        {
            if (Phase == Phases.Fire)
            {
                Fire();
            }
            else if (Phase == Phases.Finalize)
            {
                Deactivate();
            }
        }

        internal bool Activate(string id)
        {
            try
            {
                // If the active tool is called, end it
                if (_Active is not null && Active.m_Id.Equals(id))
                {
                    Deactivate();
                    return false;
                }

                Deactivate();

                _Active = ToolList.FirstOrDefault(t => t.m_Id.Equals(id));

                if (_Active == default)
                {
                    throw new Exception($"Tried to activate non-existent tool '{id}'!");
                }

                if (_Active.m_Settings.RequiresSelection && _MIT.Selection.Count == 0)
                {
                    return false;
                }

                _MIT.MITState = MITStates.ToolActive;

                if (_Active.m_Settings.FireOnCreation)
                {
                    Phase = Phases.Fire;
                }
                else
                {
                    Phase = Phases.Active;
                    if (_Active.m_Settings.UseOverlay)
                    {
                        _MIT.Hover.SetToolFlagEnabled();
                    }
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed to prepare tool '{id}'.\n{ex}");
                return false;
            }
            return true;
        }

        internal bool Fire()
        {
            try
            {
                Phase = Phases.Processing;
                Actions.Transform.TransformToolbox action = (Actions.Transform.TransformToolbox)Activator.CreateInstance(_Active.m_ActionType);
                _MIT.Queue.Push(action);
                action.Moveable = _Clicked;
                _MIT.Queue.Do();
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed to activate tool '{_Active.m_Id}'.\n{ex}");
                return false;
            }

            return true;
        }

        internal bool Deactivate()
        {
            if (_Active is null) return false;

            Phase = Phases.None;
            if (_Active.m_Settings.UseOverlay)
            {
                _MIT.Hover.SetToolFlagDisabled();
            }
            _MIT.MITState = MITStates.Default;
            _Active = null;
            _Clicked = null;
            return true;
        }

        internal Searcher.Filters GetMask()
        {
            if (_Active is null) return Searcher.Utils.FilterAll;

            return _Active.m_Filters;
        }


        private static List<ToolBoxTool> _ToolList = null;
        internal static List<ToolBoxTool> ToolList
        {
            get
            {
                _ToolList ??= new List<ToolBoxTool>()
                {
                    new TerrainHeight(),
                    new ObjectHeight(),
                    new RotateAtCentre(),
                    new RotateInPlace(),
                };
                return _ToolList;
            }
        }
        internal static List<FoldoutEntry> GetUIEntries()
            => ToolList.Select(t => (FoldoutEntry)t.m_UI).ToList();
    }
}
