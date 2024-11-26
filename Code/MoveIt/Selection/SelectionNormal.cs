using MoveIt.Moveables;
using MoveIt.Tool;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Selection
{
    public class SelectionNormal : SelectionBase
    {
        public override bool IsActive => !_MIT.IsManipulating;
        public override bool Any => Count > 0;

        internal override string Name => "SelNormal";

        public SelectionNormal() : base() { }
        public SelectionNormal(SelectionBase old) : base(old) { }
        public SelectionNormal(SelectionState states) : base(states) { }

        public override void ProcessAdd(MVDefinition mvd, bool append)
        {
            var mv = _MIT.Moveables.GetOrCreate<Moveable>(mvd);

            if (mv.IsManipulatable)
            {
                MIT.Log.Error($"Attempted to add manipulatable Moveable of type {mv.m_Identity} to Normal selection");
            }

            if (append && Has(mvd))
            {
                Remove(mvd);
            }
            else
            {
                Add(mvd);
            }
        }

        protected override HashSet<MVDefinition> GetObjectsToTransform()
        {
            bool isSegmentMove = _Buffer.Count(mvd => mvd.m_Identity != Identity.Segment && mvd.m_Identity != Identity.NetLane) == 0;

            return _Buffer.Where(mvd =>
                (mvd.m_Identity == Identity.Segment || mvd.m_Identity == Identity.NetLane) == isSegmentMove)
                .ToHashSet();
        }

        internal override HashSet<MVDefinition> GetObjectsToTransformFull()
        {
            bool isSegmentMove = _Buffer.Count(mvd => mvd.m_Identity != Identity.Segment && mvd.m_Identity != Identity.NetLane) == 0;

            var definitions = _BufferFull.Where(mvd =>
                (mvd.m_Identity == Identity.Segment || mvd.m_Identity == Identity.NetLane) == isSegmentMove && mvd.m_IsManaged == false)
                .ToList();

            int max = definitions.Count;

            for (var i = 0; i < max; i++)
            {
                Moveable mv = GetMV(definitions[i]);
                List<MVDefinition> list = mv.GetChildrenToTransform();

                foreach (var child in list)
                {
                    if (!definitions.Contains(child))
                    {
                        definitions.Add(child);
                    }
                }
            }

            //MIT.DebugDumpDefinitions(definitions, "GetObjectsToTransformFull: ");
            return definitions.ToHashSet();
        }
    }
}
