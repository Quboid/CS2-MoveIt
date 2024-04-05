using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Entities;

namespace MoveIt.Actions
{
    internal class SelectAction : SelectActionBase
    {
        public override string Name => "Select";
        private readonly Selection.Main _MarqueeStart;

        internal SelectAction(bool append = false) : base()
        {
            _OldSelection = new Selection.Main(_Tool.Selection);

            if (append && _Tool.Selection is not null)
            {
                _MarqueeStart = new Selection.Main(_Tool.Selection);
            }
            else
            {
                _MarqueeStart = new Selection.Main();
            }

            m_InitialFrame = UnityEngine.Time.frameCount;

            if (append && _Tool.Selection is not null)
            {
                _NewSelection = new Selection.Main(_Tool.Selection);
            }
            else
            {
                HashSet<Moveable> mvs = new(_Tool.Selection.Moveables);
                _NewSelection = new Selection.Main();
                _Tool.Selection = (Selection.Main)_NewSelection;
                foreach (Moveable mv in mvs)
                {
                    mv.OnDeselect();
                }
                mvs.Clear();
            }

            _Tool.Selection = (Selection.Main)_NewSelection;
        }

        public override bool IsHoveredValid()
        {
            return _Tool.Hover.IsNormal;
        }

        public override void Do()
        {
            _Tool.Selection = (Selection.Main)_NewSelection;

            base.Do();
        }

        public override void Undo()
        {
            HashSet<Moveable> selection = _Tool.Selection.Moveables.ToHashSet();
            HashSet<Moveable> oldSelection = _OldSelection.Moveables.ToHashSet();
            IEnumerable<Moveable> unSelected = selection.Except(oldSelection);
            IEnumerable<Moveable> reSelected = oldSelection.Except(selection);

            _Tool.Selection = (Selection.Main)_OldSelection;
            _Tool.Selection.Refresh();

            unSelected.ForEach(mv => mv.OnDeselect());
            reSelected.ForEach(mv => mv.OnSelect());

            base.Undo();
        }

        public override void Redo()
        {
            HashSet<Moveable> selection = _Tool.Selection.Moveables.ToHashSet();
            HashSet<Moveable> newSelection = _NewSelection.Moveables.ToHashSet();
            IEnumerable<Moveable> unSelected = selection.Except(newSelection);
            IEnumerable<Moveable> reSelected = newSelection.Except(selection);

            _Tool.Selection = (Selection.Main)_NewSelection;
            _Tool.Selection.Refresh();

            unSelected.ForEach(mv => mv.OnDeselect());
            reSelected.ForEach(mv => mv.OnSelect());

            base.Redo();
        }

        public void AddMarqueeSelection(Input.Marquee marquee)
        {
            HashSet<Entity> latest = new(_MarqueeStart.Entities);
            latest.UnionWith(marquee.m_Entities);

            foreach (Entity e in latest)
            {
                if (!_NewSelection.Has(e))
                {
                    _NewSelection.Add(e);
                }
            }

            foreach (Entity e in _NewSelection.Entities)
            {
                if (!latest.Contains(e))
                {
                    _NewSelection.Remove(e);
                }
            }

            HashSet<Entity> sanity = new(_NewSelection.Entities);
            sanity.IntersectWith(latest);
            if (sanity.Count != _NewSelection.Count)
            {
                MIT.Log.Error($"UNEQUAL on marquee selection update. Sanity:{sanity.Count}, SewSel:{_NewSelection.Count}");
            }
        }
    }

    internal abstract class SelectActionBase : Action
    {
        protected Selection.Base _OldSelection;
        protected Selection.Base _NewSelection;

        public SelectActionBase() : base()
        { }

        //public override void Do()
        //{
        //    base.Do();
        //    //QLog.Bundle("DO", $"{_OldSelection.Count},{_NewSelection.Count} " + Selection.DebugSelection());
        //}

        public void AddHovered(bool append)
        {
            if (!IsHoveredValid()) return;
            _NewSelection.ProcessAdd(_Tool.Hover.Entity, append);
            //Add(_Tool.Hover.Entity, append);
        }

        public abstract bool IsHoveredValid();

        //public void Add(Entity e, bool append)
        //{
        //    if (append)
        //    {
        //        if (_NewSelection.Has(e))
        //        {
        //            _NewSelection.Remove(e);
        //        }
        //        else
        //        {
        //            _NewSelection.Add(e);
        //        }
        //    }
        //    else if (!_Tool.ActiveSelection.Has(e))
        //    {
        //        _NewSelection.Clear();
        //        _NewSelection.Add(e);
        //    }

        //    //QLog.Debug($"Adding {e.D()} (S:{_Tool.Selection.Count}, M:{_Tool.SelectionManipulate.Count}, _N:{_NewSelection.Count} _O:{_OldSelection.Count} AS:{_Tool.ActiveSelection.Count})");
        //}

        public override void UpdateEntityReferences(Dictionary<Entity, Entity> toUpdate)
        {
            MIT.Log.Debug($"SelectAction.UpdateEntityReferences");
        }

        public string DebugSelectionHash(HashSet<Entity> entities, string prefix = "")
        {
            StringBuilder sb = new();
            sb.AppendFormat("{0} Entities [{1}]", prefix, entities.Count);
            foreach (var e in entities)
            {
                sb.AppendFormat(" {0},", e.D());
            }
            return sb.ToString();
        }
    }
}
