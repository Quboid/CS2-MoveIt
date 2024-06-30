using MoveIt.Actions;
using MoveIt.Selection;
using MoveIt.Tool;

namespace MoveIt.Managers
{
    internal class QueueManager
    {
        protected const int QUEUE_LENGTH = 100;
        protected static readonly MIT _Tool = MIT.m_Instance;
        protected Action _CreationAction;

        internal QueueManager()
        {
            Index++;
            _Head = Index;

            _Tool.Selection ??= new SelectionNormal();
            _Actions[Index] = new SelectAction();
            _CreationAction = null;
            Do();
        }

        private readonly Action[] _Actions = new Action[QUEUE_LENGTH];
        /// <summary>
        /// The index of the currently active action.
        /// </summary>
        internal int Index
        {
            get => _CurrentIndex;
            set
            {
                _CurrentIndex = value % QUEUE_LENGTH;
                if (_CurrentIndex < 0)
                {
                    _CurrentIndex = QUEUE_LENGTH + _CurrentIndex;
                }
            }
        }
        private int _CurrentIndex = 0;

        private int IndexPrev => Index == 0 ? QUEUE_LENGTH - 1 : Index - 1;
        private int IndexNext => Index >= QUEUE_LENGTH - 1 ? 0 : Index + 1;

        /// <summary>
        /// The index of the newest action.
        /// </summary>
        private int _Head = 0;

        /// <summary>
        /// The index preceeding the oldest action. Never a valid action.
        /// </summary>
        private int _Tail = 0;

        /// <summary>
        /// The currently active action
        /// Creates a SelectAction if there none, which should only happen when tool is first enabled.
        /// </summary>
        public Action Current
        {
            get
            {
                //if (Index == _Tail) Push();

                return _Actions[Index];
            }
        }

        public Action PrevAction => IndexPrev == _Tail ? new NullAction() : _Actions[IndexPrev];
        public Action NextAction => IndexNext == _Head ? new NullAction() : _Actions[IndexNext];

        /// <summary>
        /// Get the action for Creation engine, or Current if there isn't one set
        /// </summary>
        public Action CreationAction
        {
            get => _CreationAction ?? Current;
            set => _CreationAction = value;
        }
        public bool HasCreationAction => _CreationAction != null;

        //public void UpdateNodeIdInStateHistory(ushort oldId, ushort newId)
        //{
        //    foreach (Action action in _getPreviousAction())
        //    {
        //        if (action == null) continue;
        //        action.UpdateNodeIdInSegmentState(oldId, newId);
        //    }
        //}

        public void Push(Action action)
        {
            if (_Tail == (Index + 1) % QUEUE_LENGTH)
            {
                _Tail = (Index + 2) % QUEUE_LENGTH;
            }

            Current.Archive(ToolActions.None, Index);
            if (Index != _Head)
            {
                Invalidate();
            }

            Index++;
            _Head = Index;

            _Actions[Index] = action;

            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} Push {Debug()}");
        }

        public void FireAction()
        {
            //if (action != ToolActions.None) MIT.Log.Bundle("FIRE", $"{UnityEngine.Time.frameCount} Queue.Fire {action}: {Debug()}");
            if (_Tool.ToolAction == ToolActions.Do)
            {
                _Tool.ToolAction = ToolActions.None;
                Do();
            }
            else if (_Tool.ToolAction == ToolActions.Undo)
            {
                _Tool.ToolAction = ToolActions.None;
                Undo();
            }
            else if (_Tool.ToolAction == ToolActions.Redo)
            {
                _Tool.ToolAction = ToolActions.None;
                Redo();
            }
        }

        public void Do()
        {
            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} Do {Debug()}");
            _Actions[Index].Do();
        }

        public bool CanUndo()
        {
            return Index != (_Tail + 1) % QUEUE_LENGTH;
        }

        public void Undo()
        {
            if (!CanUndo()) return;

            _Actions[Index].Archive(ToolActions.Undo, Index);
            _Actions[IndexPrev].Unarchive(ToolActions.Undo, IndexPrev);

            _Actions[Index].Undo();
            Index--;

            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} Undo (Can Redo:{CanRedo()}) {Debug()}");
        }

        public bool CanRedo()
        {
            return Index != _Head;
        }

        public void Redo()
        {
            if (!CanRedo()) return;

            _Actions[Index].Archive(ToolActions.Redo, Index);
            _Actions[IndexNext].Unarchive(ToolActions.Redo, IndexNext);

            Index++;

            _Actions[Index].Redo();

            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} Redo (Can Undo:{CanUndo()},Redo:{CanRedo()}) {Debug()}");
        }

        public T GetPrevious<T>() where T : Action
        {
            T previous = null;
            int idx;
            int miniumum = (_Tail + 1) % QUEUE_LENGTH;
            if (_Tail > Index) miniumum -= QUEUE_LENGTH;

            int i = Index - 1;
            do
            {
                // i is negative if _Tail is higher than Index, convert it to be in the actual _Actions range
                idx = (i + QUEUE_LENGTH) % QUEUE_LENGTH;

                if (_Actions[idx] is DeselectAllAction)
                {
                    previous = null;
                    break;
                }

                if (_Actions[idx] is T result)
                {
                    previous = result;
                    break;
                }

                i--;
            }
            while (i >= miniumum);

            return previous;
        }

        /// <summary>
        /// Remove any future actions from the queue to avoid a multiverse split time paradox
        /// </summary>
        public void Invalidate()
        {
            int start = IndexNext;
            int end = IndexNext > _Head ? _Head + QUEUE_LENGTH : _Head;
            int idx = -1;
            int i;

            for (i = start; i <= end; i++)
            {
                idx = i % QUEUE_LENGTH;
                _Actions[idx] = null;
            }

            MIT.Log.Debug($"QM.Invalidate ({_Tail}-{Index}-{_Head}) {start}-{end} lastIdx:{idx}/{i}");
            _Head = Index;
        }

        public void Clear()
        {
            Index = 0;
            _Head = 0;
            _Tail = 0;
        }

        public string GetQueueIndexes() => $"{_Tail}-**{Index}**-{_Head}";

        public string Debug()
        {
            string msg = $"{Index} {_Tail}/{_Head} ";
            int min = (_Tail + 1) % QUEUE_LENGTH;
            int max = _Head;
            if (max < _Tail) max += QUEUE_LENGTH;
            for (int i = min; i <= max; i++)
            {
                int idx = i % QUEUE_LENGTH;

                //string manip = _Actions[idx].m_IsManipulate.from ? (_Actions[idx].m_IsManipulate.to ? "B": "F") : (_Actions[idx].m_IsManipulate.to ? "T" : "N" );
                string manip = _Actions[idx].m_IsManipulationMode ? "M" : "n";
                msg += $"{idx}{manip}:{_Actions[idx].Name}";
                msg += ", ";
            }

            return $"{msg} |{_Tool.CreationPhase}| {Current.GetActionState()}";
        }

        //public static void UpdateEntityReferencesForward(Dictionary<Entity, Entity> toReplace)
        //{
        //    int action = Index;

        //    while (action != _Head)
        //    {
        //        action = (action + 1) % QUEUE_LENGTH;
        //        _Actions[action].UpdateEntityReferences(toReplace);
        //    }
        //}

        //public static void UpdateEntityReferencesBackward(Dictionary<Entity, Entity> toReplace)
        //{
        //    int action = Index;

        //    if (action == 0)
        //    {
        //        action = QUEUE_LENGTH - 1;
        //    }
        //    else
        //    {
        //        action--;
        //    }

        //    while (action != _Tail)
        //    {
        //        _Actions[action].UpdateEntityReferences(toReplace);

        //        if (action == 0)
        //        {
        //            action = QUEUE_LENGTH - 1;
        //        }
        //        else
        //        {
        //            action--;
        //        }
        //    }
        //}
    }
}
