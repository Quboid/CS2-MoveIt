using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace MoveIt.Actions
{
    internal class SelectManipulateAction : SelectActionBase
    {
        public override string Name => "SelectManipulate";

        /// <summary>
        /// Constructor for SelectManipulateAction
        /// </summary>
        /// <param name="append">Should this be added to existing selection, or should new one be made?</param>
        /// <param name="isForChild">Is the object being added a manipulatable child?</param>
        internal SelectManipulateAction(bool append = false, bool isForChild = false) : base()
        {
            _OldSelection = new Selection.Manipulating(_Tool.Manipulation);

            if (append && _Tool.Manipulation is not null)
            {
                _NewSelection = new Selection.Manipulating(_Tool.Manipulation);
            }
            else if (isForChild)
            {
                _NewSelection = new Selection.Manipulating(_Tool.Manipulation);
                HashSet<Moveable> toRemove = new();
                foreach (Moveable mv in _NewSelection.Moveables)
                {
                    if ((mv.m_Manipulatable & QTypes.Manipulate.Parent) == 0)
                    {
                        toRemove.Add(mv);
                    }
                }
                _NewSelection.Remove(toRemove);
            }
            else
            {
                HashSet<Moveable> mvs = new(_Tool.Manipulation.Moveables);
                _NewSelection = new Selection.Manipulating();
                _Tool.Manipulation = (Selection.Manipulating)_NewSelection;
                foreach (Moveable mv in mvs)
                {
                    mv.OnDeselect();
                }
                mvs.Clear();
            }

            _Tool.Manipulation = (Selection.Manipulating)_NewSelection;
        }

        public override bool IsHoveredValid()
        {
            return _Tool.Hover.IsManipulatable;
        }

        public override void Do()
        {
            _Tool.Manipulation = (Selection.Manipulating)_NewSelection;
            
            base.Do();
        }

        public override void Undo()
        {
            HashSet<Moveable> selection = _Tool.Manipulation.Moveables.ToHashSet();
            HashSet<Moveable> oldSelection = _OldSelection.Moveables.ToHashSet();
            IEnumerable<Moveable> unSelected = selection.Except(oldSelection);
            IEnumerable<Moveable> reSelected = oldSelection.Except(selection);

            _Tool.Manipulation = (Selection.Manipulating)_OldSelection;
            _Tool.Manipulation.Refresh();

            unSelected.ForEach(mv => mv.OnDeselect());
            reSelected.ForEach(mv => mv.OnSelect());
            base.Undo();
        }

        public override void Redo()
        {
            HashSet<Moveable> selection = _Tool.Manipulation.Moveables.ToHashSet();
            HashSet<Moveable> newSelection = _NewSelection.Moveables.ToHashSet();
            IEnumerable<Moveable> unSelected = selection.Except(newSelection);
            IEnumerable<Moveable> reSelected = newSelection.Except(selection);

            _Tool.Manipulation = (Selection.Manipulating)_NewSelection;
            _Tool.Manipulation.Refresh();

            unSelected.ForEach(mv => mv.OnDeselect());
            reSelected.ForEach(mv => mv.OnSelect());
            base.Redo();
        }
    }


    internal class EndManipulationAction : SelectManipulateAction
    {
        public override string Name => "EndManipulationAction";

        internal EndManipulationAction() : base(false)
        { }

        public override void Do()
        {
            base.Do();

            _Tool.Manipulation.Clear();
            _Tool.SetManipulationMode(false);
            m_IsManipulate.to = false; // Overwrite base ctor's assignment
            //QLog.Debug($"ToggleManipulation.Do (mode at start:{_IsManipulation}, switching to {m_NewManipulation})");
        }

        public override void Undo()
        {
            _Tool.SetManipulationMode(true);

            base.Undo();
        }

        public override void Redo()
        {
            _Tool.SetManipulationMode(false);

            base.Redo();
        }

        public override void UpdateEntityReferences(Dictionary<Entity, Entity> toUpdate)
        { }

        public override bool IsHoveredValid() => false;
    }
}
