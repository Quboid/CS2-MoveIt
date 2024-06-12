using MoveIt.Overlays;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVManipControlPoint : MVControlPoint
    {
        public override bool IsManipulatable => true;
        public override bool IsChild => true;

        public MVManipControlPoint(Entity e) : base(e, Identity.ControlPoint, ObjectType.Managed)
        {
            m_Overlay = Factory.Create<OverlayManipControlPoint>(this, OverlayTypes.MVManipControlPoint);
            Refresh();
        }

        internal override void DisposeIfUnused()
        {
            if (_Tool.Hover.Is(Definition))                 return;
            if (_Tool.Selection.HasFull(Definition))        return;
            if (_Tool.Selection.HasFull(NodeDefinition))    return;
            if (_Tool.Selection.HasFull(SegmentDefinition)) return;

            _Tool.Moveables.RemoveDo(this);
        }
    }
}
