using MoveIt.Tool;
using System.Collections.Generic;
using Unity.Entities;

namespace MoveIt.Actions
{
    internal class Queue
    {
        protected const int QUEUE_LENGTH = 100;
        protected static readonly MIT _Tool = MIT.m_Instance;

        private static readonly Action[] _Actions = new Action[QUEUE_LENGTH];
        /// <summary>
        /// The index of the currently active action.
        /// </summary>
        private static int Index
        {
            get => _CurrentIndex;
            set => _CurrentIndex = value % QUEUE_LENGTH;
        }
        private static int _CurrentIndex = 0;
        /// <summary>
        /// The index of the newest action.
        /// </summary>
        private static int _Head = 0;
        /// <summary>
        /// The index preceeding the oldest action. Never a valid action.
        /// </summary>
        private static int _Tail = 0;
        /// <summary>
        /// Has the current action finished its Do call (includes Redo)?
        /// </summary>
        private static bool _IsDone;

        //public void UpdateNodeIdInStateHistory(ushort oldId, ushort newId)
        //{
        //    foreach (Action action in _getPreviousAction())
        //    {
        //        if (action == null) continue;
        //        action.UpdateNodeIdInSegmentState(oldId, newId);
        //    }
        //}

        public static void Push(Action action)
        {
            if (_Tail == ((Index + 1) % QUEUE_LENGTH))
            {
                _Tail = (Index + 2) % QUEUE_LENGTH;
            }

            Index = (Index + 1) % QUEUE_LENGTH;
            _Head = Index;

            _IsDone = false;
            _Actions[Index] = action;

            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} QPush {Debug()}");
        }

        public static void FireAction(ToolActions action)
        {
            //if (action != ToolActions.None) MIT.Log.Bundle("FIRE", $"{UnityEngine.Time.frameCount} Queue.Fire {action}: {Debug()}");
            if (action == ToolActions.Do)
            {
                _Tool.ToolAction = ToolActions.None;
                Do();
            }
            else if (action == ToolActions.Undo)
            {
                _Tool.ToolAction = ToolActions.None;
                Undo();
            }
            else if (action == ToolActions.Redo)
            {
                _Tool.ToolAction = ToolActions.None;
                Redo();
            }
        }

        public static void Do()
        {
            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} QueueDo {Debug()}");
            _Actions[Index].Do();
            _IsDone = true;
        }

        public static void Undo()
        {
            if (Index == ((_Tail + 1) % QUEUE_LENGTH)) return;

            if (_IsDone)
            {
                _IsDone = false;
            }
            else
            {
                Index--;
            }

            _Actions[Index].Undo();

            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} QUndo {Debug()}");
        }

        public static void Redo()
        {
            if (_IsDone)
            {
                if (Index == _Head) return;
                Index++;
            }
            else
            {
                _IsDone = true;
            }

            _Actions[Index].Redo();

            //MIT.Log.Debug($"{UnityEngine.Time.frameCount} QRedo {Debug()}");
        }

        /// <summary>
        /// Remove any future actions from the queue to avoid a multiverse split time paradox
        /// </summary>
        public static void Invalidate()
        {
            //if (_Head != Index) MIT.Log.Debug($"ActionQueue Invalidate({Index}, {_Head})");
            _Head = Index;
        }

        public static void Clear()
        {
            Index = 0;
            _Head = 0;
            _Tail = 0;
        }

        public static void UpdateEntityReferencesForward(Dictionary<Entity, Entity> toReplace)
        {
            int action = Index;

            while (action != _Head)
            {
                action = (action + 1) % QUEUE_LENGTH;
                _Actions[action].UpdateEntityReferences(toReplace);
            }
        }

        public static void UpdateEntityReferencesBackward(Dictionary<Entity, Entity> toReplace)
        {
            int action = Index;

            if (action == 0)
            {
                action = QUEUE_LENGTH - 1;
            }
            else
            {
                action--;
            }

            while (action != _Tail)
            {
                _Actions[action].UpdateEntityReferences(toReplace);

                if (action == 0)
                {
                    action = QUEUE_LENGTH - 1;
                }
                else
                {
                    action--;
                }
            }
        }

        public static Action Current
        {
            get
            {
                if (Index == _Tail)
                {
                    Push(new SelectAction());
                }

                return _Actions[Index];
            }
        }

        public static string Debug()
        {
            string msg = $"{Index} {_Tail}/{_Head} ";
            int min = (_Tail + 1) % QUEUE_LENGTH;
            int max = _Head;
            if (max < _Tail) max += QUEUE_LENGTH;
            for (int i = min; i <= max; i++)
            {
                int idx = i % QUEUE_LENGTH;

                string manip = _Actions[idx].m_IsManipulate.from ? (_Actions[idx].m_IsManipulate.to ? "B": "F") : (_Actions[idx].m_IsManipulate.to ? "T" : "N" );
                if (!_IsDone && idx == Index) msg += "*";
                msg += $"{idx}{manip}:{_Actions[idx].Name}";
                if (_IsDone && idx == Index) msg += "*";
                msg += ", ";
            }

            return $"{msg} |{_Tool.CreationPhase}| {Current.GetActionState()}";
        }
    }
}
