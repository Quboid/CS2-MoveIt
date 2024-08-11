using MoveIt.Managers;

namespace MoveIt.Searcher
{
    internal class FilterManager : MIT_Manager
    {
        /// <summary>
        /// Do filters apply at all?
        /// </summary>
        internal bool Active
        {
            get => _Active;
            set => _Active = value;
        }
        private bool _Active;

        internal Filters GetMask()
        {
            return _MIT.m_UISystem.GetFilterState().GetMask() & _MIT.ToolboxManager.GetMask();
        }
    }
}
