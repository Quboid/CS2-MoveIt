using MoveIt.Actions.Toolbox;
using MoveIt.Tool;
using MoveIt.UI.Foldout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Managers
{
    internal class ToolboxManager : MIT_Manager
    {
        public enum Phases
        {
            None,
            Active,
            Fire,
            Processing,
            Finalise,
        }

        internal Phases Phase { get; set; } = Phases.None;
        private ToolBoxTool _ActiveTool { get; set; }

        private Moveables.Moveable _Clicked;

        internal ToolboxManager()
        { }

        internal bool IsActive(string id)
            => _ActiveTool is not null && _ActiveTool.m_Id.Equals(id);

        internal void ObjectClicked(Moveables.Moveable mv)
        {
            if (_ActiveTool is null) return;

            _Clicked = mv;

            if (Phase == Phases.Active)
            {
                Phase = Phases.Fire;
            }
        }

        internal void Update()
        {
            switch (Phase)
            {
                case Phases.Fire:
                    Fire();
                    break;
                case Phases.Finalise:
                    Deactivate();
                    break;
            }
        }

        internal bool Activate(string id)
        {
            try
            {
                // If the active tool is called, end it
                if (_ActiveTool is not null && _ActiveTool.m_Id.Equals(id))
                {
                    Deactivate();
                    return false;
                }

                Deactivate();

                _ActiveTool = ToolList.FirstOrDefault(t => t.m_Id.Equals(id));

                if (_ActiveTool == default)
                {
                    throw new Exception($"Tried to activate non-existent tool '{id}'!");
                }

                if (_ActiveTool.m_Settings.RequiresSelection && _MIT.Selection.Count == 0)
                {
                    return false;
                }

                _MIT.MITState = MITStates.ToolActive;

                if (_ActiveTool.m_Settings.FireOnCreation)
                {
                    Phase = Phases.Fire;
                }
                else
                {
                    Phase = Phases.Active;
                    if (_ActiveTool.m_Settings.UseOverlay)
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

        private bool Fire()
        {
            try
            {
                Phase = Phases.Processing;
                Actions.Transform.TransformToolbox action = (Actions.Transform.TransformToolbox)Activator.CreateInstance(_ActiveTool.m_ActionType);
                _MIT.Queue.Push(action);
                action.Moveable = _Clicked;
                _MIT.Queue.Do();
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed to activate tool '{_ActiveTool.m_Id}'.\n{ex}");
                return false;
            }

            return true;
        }

        private bool Deactivate()
        {
            if (_ActiveTool is null) return false;

            Phase = Phases.None;
            if (_ActiveTool.m_Settings.UseOverlay)
            {
                _MIT.Hover.SetToolFlagDisabled();
            }
            _MIT.MITState = MITStates.Default;
            _ActiveTool = null;
            _Clicked = null;
            return true;
        }

        internal Searcher.Filters GetMask()
        {
            if (_ActiveTool is null) return Searcher.Utils.FilterAll;

            return _ActiveTool.m_Filters;
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
        {
            //ToolList.Select(FoldoutEntry (t) => t.m_UI).ToList();

            List<FoldoutEntry> list = new();
            foreach (var t in ToolList)
            {
                list.Add(t.m_UI);
            }
            return list;
        }
    }
}
