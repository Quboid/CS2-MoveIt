using MoveIt.Moveables;
using MoveIt.Tool;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Selection
{
    public class SelectionManip : SelectionBase
    {
        public override bool IsActive => _Tool.IsManipulating;
        public override bool Any => _Buffer.Count(mvd => GetMV(mvd).IsManipChild) > 0;

        internal override string Name => "SelManip";

        public SelectionManip() : base() { }
        public SelectionManip(SelectionBase old) : base(old) { }
        public SelectionManip(SelectionState states) : base(states) { }

        public override void ProcessAdd(MVDefinition mvd, bool append)
        {
            Moveable mv = _Tool.Moveables.GetOrCreate<Moveable>(mvd);

            if (!mv.IsManipulatable)
            {
                MIT.Log.Error($"Attempted to add non-manipulatable Moveable of type {mv.m_Identity} to Manipulating selection");
            }

            if (append)
            {
                if (Has(mvd))
                {
                    Remove(mvd);
                }
                else
                {
                    Add(mvd);
                }
            }
            else if (!_Tool.Selection.Has(mvd))
            {
                if (mv.IsManipChild)
                {
                    // If the to-be-added object is a child, only clear other children
                    HashSet<MVDefinition> toRemove = new();
                    foreach (MVDefinition mvd1 in _Buffer)
                    {
                        Moveable mv1 = GetMV(mvd1);
                        if (mv1.IsManipChild)
                        {
                            toRemove.Add(mvd1);
                        }
                    }
                    foreach (MVDefinition mvd1 in toRemove)
                    {
                        _Buffer.Remove(mvd1);
                    }
                }
                else
                {
                    Clear();
                }
                Add(mvd);
            }
        }

        internal override HashSet<MVDefinition> GetObjectsToTransform()
        {
            IEnumerable<MVDefinition> candidates = _Buffer.Where(mvd => mvd.m_IsManipulatable && Has(mvd));

            HashSet<MVDefinition> result = new();
            foreach (MVDefinition mvd in candidates)
            {
                Moveable mv = GetMV(mvd);
                if (mv.IsManipChild)
                {
                    result.Add(mvd);
                }
            }
            return result;
        }

        internal override HashSet<MVDefinition> GetObjectsToTransformFull()
        {
            IEnumerable<MVDefinition> candidates = _BufferFull.Where(mvd => mvd.m_IsManipulatable && Has(mvd));

            HashSet<MVDefinition> result = new();
            foreach (MVDefinition mvd in candidates)
            {
                Moveable mv = GetMV(mvd);
                if (mv.IsManipChild)
                {
                    result.Add(mvd);
                }
            }
            return result;
        }
    }
}
