﻿using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Selection
{
    public abstract class ActionState
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

        protected readonly bool _IsManipulation;

        protected ActionState(bool isManipulation)
        {
            _IsManipulation = isManipulation;
        }
    }

    // public class TransformState : ActionState
    // {
    //     protected readonly List<Neighbour> _Neighbours;
    //     public List<Neighbour> Neighbours => _Neighbours;
    //
    //     public TransformState(bool isManipulation, List<Neighbour> neighbours) : base(isManipulation)
    //     {
    //         _Neighbours = neighbours;
    //     }
    // }

    public class SelectionState : ActionState
    {
        private readonly List<MVDefinition> _Definitions;

        public List<MVDefinition> Definitions => _Definitions;
        public int Count => _Definitions.Count;
        public bool Any => Count > 0;

        public SelectionState(bool isManipulation, List<MVDefinition> definitions) : base(isManipulation)
        {
            _Definitions = definitions;
        }

        /// <summary>
        /// Get new SelectionState with invalid entries removed. Ignores Control Points.
        /// </summary>
        /// <returns>SelectionState without invalid entries</returns>
        internal SelectionState CleanDefinitions() => new(_IsManipulation, CleanDefinitions(_Definitions));

        /// <summary>
        /// Get list of MVDefinitions with invalid entries removed. Ignores Control Points.
        /// </summary>
        /// <param name="selectionState">SelectionState to clean</param>
        /// <returns>List of MVDefinitions without invalid entries</returns>
        internal static List<MVDefinition> CleanDefinitions(SelectionState selectionState)
        {
            return CleanDefinitions(selectionState.Definitions);
        }

        /// <summary>
        /// Get list of MVDefinitions with invalid entries removed. Ignores Control Points.
        /// </summary>
        /// <param name="definitions">MVDefinitions to clean</param>
        /// <returns>List of MVDefinitions without invalid entries</returns>
        private static List<MVDefinition> CleanDefinitions(List<MVDefinition> definitions)
        {
            var msg = $"SelState.CleanDefinitions; OldDefs:{definitions.Count}";
            var removing = "";
            HashSet<MVDefinition> toRemove = new();

            foreach (var mvd in definitions)
            {
                if (mvd.m_IsManaged)
                {
                    if (!_MIT.IsValid(mvd.m_Parent))
                    {
                        toRemove.Add(mvd);
                    }
                    continue;
                }

                if (!_MIT.IsValid(mvd.m_Entity))
                {
                    toRemove.Add(mvd);
                    removing += $" [{mvd.m_Entity.D()}-{(mvd.m_IsManipulatable ? "M" : "n")}]";
                }
            }

            List<MVDefinition> newDefinitions = definitions.Except(toRemove).ToList();

            MIT.Log.Info($"{msg}, NewDefs:{newDefinitions.Count} {(removing.Length > 0 ? $"\n Removing: {removing}" : "")}");

            return newDefinitions;
        }

        internal static SelectionState SelectionToState(bool isManipulation, HashSet<MVDefinition> definitions = null)
        {
            definitions ??= new();
            string old = MIT.DebugDefinitions(definitions);
            SelectionState result = new(isManipulation, definitions.ToList());

            QLog.Debug($"SS.SelectionToState {QCommon.GetCallerDebug()}\nOld: {old}\nNew: {MIT.DebugDefinitions(result.Definitions)}");

            return result;
        }

        //internal static SelectionBase StateToSelection(SelectionState state)
        //{
        //    SelectionBase result;
        //    if (state._IsManipulation)
        //    {
        //        result = new SelectionManip(state);
        //    }
        //    else
        //    {
        //        result = new SelectionNormal(state);
        //    }
        //    return result;
        //}


        public string Debug()
        {
            var msg = $"SelectionState Definitions:{_Definitions.Count} {QCommon.GetCallerDebug()}";
            return _Definitions.Aggregate(msg, (current, mvd) => current + $"\n    {mvd}");
        }

        public void DebugDump(string prefix = "")
        {
            MIT.Log.Debug(prefix + Debug());
        }
    }
}
