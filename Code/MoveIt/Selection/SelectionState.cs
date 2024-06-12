using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Selection
{
    public class SelectionState
    {
        protected static MIT _Tool = MIT.m_Instance;

        protected readonly List<MVDefinition> _Definitions;
        protected bool _IsManipulation;

        public List<MVDefinition> Definitions => _Definitions;
        public int Count => _Definitions.Count;
        public bool Any => Count > 0;

        public SelectionState(bool isManipulation, List<MVDefinition> definitions)
        {
            _Definitions = definitions;
            _IsManipulation = isManipulation;
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
        internal static List<MVDefinition> CleanDefinitions(List<MVDefinition> definitions)
        {
            //string msg = $"SelState.CleanDefinitions; OldDefs:{_Definitions.Count}";
            //string removing = "";
            HashSet<MVDefinition> toRemove = new();

            foreach (var mvd in definitions)
            {
                if (mvd.m_IsManaged)
                {
                    if (!_Tool.IsValid(mvd.m_Parent))
                    {
                        toRemove.Add(mvd);
                    }
                    continue;
                }

                if (!_Tool.IsValid(mvd.m_Entity))
                {
                    toRemove.Add(mvd);
                    //removing += $" [{mvd.m_Entity.D()}-{(mvd.m_IsManipulatable ? "M" : "n")}]";
                }
            }

            List<MVDefinition> newDefinitions = definitions.Except(toRemove).ToList();

            //MIT.Log.Info($"{msg}, NewDefs:{_Definitions.Count} {(removing.Length > 0 ? $"\n Removing: {removing}" : "")}");

            return newDefinitions;
        }

        internal static SelectionState SelectionToState(bool isManipulation, HashSet<MVDefinition> definitions = null)
        {
            definitions ??= new();
            SelectionState result = new(isManipulation, definitions.ToList());

            return result;
        }

        internal static SelectionBase StateToSelection(SelectionState state)
        {
            SelectionBase result;
            if (state._IsManipulation)
            {
                result = new SelectionManip(state);
            }
            else
            {
                result = new SelectionNormal(state);
            }
            return result;
        }


        public string Debug()
        {
            string msg = $"SelectionState Definitions:{_Definitions.Count}";
            foreach (MVDefinition mvd in _Definitions)
            {
                msg += $"\n    {mvd}";
            }
            return msg;
        }

        public void DebugDump(string prefix = "")
        {
            QLog.Debug(prefix + Debug());
        }
    }
}
