using MoveIt.Actions;
using MoveIt.Actions.Select;
using MoveIt.Selection;
using MoveIt.Tool;
using System.Text;

namespace MoveIt.Managers
{
    internal class QueueManager
    {
        protected const int QUEUE_LENGTH = 100;
        protected static readonly MIT _MIT = MIT.m_Instance;
        protected Action _CreationAction;

        internal QueueManager()
        {
            Index++;
            _Head = Index;

            _MIT.Selection ??= new SelectionNormal();
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

            MIT.Log.Info("--- QueueManager.Push");
            Current.Archive(MITActions.None, Index);
            if (Index != _Head)
            {
                Invalidate();
            }

            Index++;
            _Head = Index;

            _Actions[Index] = action;

            MIT.Log.Info($"Push {Index}:{_Actions[Index].Name}");
        }

        public void FireAction()
        {
            if (_MIT.MITAction == MITActions.Do)
            {
                _MIT.MITAction = MITActions.None;
                Do();
            }
            else if (_MIT.MITAction == MITActions.Undo)
            {
                _MIT.MITAction = MITActions.None;
                Undo();
            }
            else if (_MIT.MITAction == MITActions.Redo)
            {
                _MIT.MITAction = MITActions.None;
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

            MIT.Log.Debug("--- QueueManager.Undo");
            _Actions[Index].Archive(MITActions.Undo, Index);
            _Actions[IndexPrev].Unarchive(MITActions.Undo, IndexPrev);

            _Actions[Index].Undo();
            Index--;

            //MIT.Log.Debug($"Undo (Can Redo:{CanRedo()}) {Debug()}");
        }

        public bool CanRedo()
        {
            return Index != _Head;
        }

        public void Redo()
        {
            if (!CanRedo()) return;

            MIT.Log.Debug("--- QueueManager.Redo");
            _Actions[Index].Archive(MITActions.Redo, Index);
            _Actions[IndexNext].Unarchive(MITActions.Redo, IndexNext);

            Index++;

            _Actions[Index].Redo();

            //MIT.Log.Debug($"Redo (Can Undo:{CanUndo()},Redo:{CanRedo()}) {Debug()}");
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
            string prewipeDebug = DebugQueue();

            for (i = start; i <= end; i++)
            {
                idx = i % QUEUE_LENGTH;
                _Actions[idx] = null;
            }

            MIT.Log.Info($"QM.Invalidate ({_Tail}-{Index}-{_Head}) {start}-{end} lastIdx:{idx}/{i}\n{prewipeDebug}");
            _Head = Index;
        }

        public void Clear()
        {
            Index = 0;
            _Head = 0;
            _Tail = 0;
        }

        public string UI_GetQueueIndexes()
            => $"{_Tail}-**{Index}**-{_Head}";


        public string DebugQueue()
        {
            StringBuilder sb = new();
            sb.AppendFormat("Idx:{0} {1}/{2} Creation:{3} Current:{4}-{5}", Index, _Tail, _Head, _MIT.CreationPhase, Index, Current.Name);
            int min = (_Tail + 1) % QUEUE_LENGTH;
            int max = _Head;
            if (max < _Tail) max += QUEUE_LENGTH;

            int c = 0;
            for (int i = min; i <= max; i++)
            {
                if (c % 5 == 0) sb.Append("\n");
                c++;

                int idx = i % QUEUE_LENGTH;

                sb.AppendFormat("{0}: ", idx);
                if (_Actions[idx].m_IsManipulationMode)
                {
                    sb.Append("M-");
                }
                sb.AppendFormat("{0},  ", idx == Index ? _Actions[idx].Name.ToUpper() : _Actions[idx].Name);
            }

            return sb.ToString();
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
