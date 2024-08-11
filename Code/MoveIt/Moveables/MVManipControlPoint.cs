using MoveIt.Overlays;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVManipControlPoint : MVControlPoint
    {
        public override bool IsManipulatable    => true;
        public override bool IsManipChild       => true;
        public override bool IsNormalChild      => false;

        public MVManipControlPoint(Entity e) : base(e, Identity.ControlPoint)
        {
            m_Overlay = Factory.Create<OverlayManipControlPoint>(this, OverlayTypes.MVManipControlPoint);
            Refresh();
        }

        internal override void DisposeIfUnused()
        {
            if (_MIT.Hover.Is(Definition))                 return;
            if (_MIT.Hover.Is(ParentDefinition))           return;
            if (_MIT.Selection.HasFull(Definition))        return;
            if (_MIT.Selection.HasFull(NodeDefinition))    return;
            if (_MIT.Selection.HasFull(ParentDefinition))  return;

            _MIT.Moveables.RemoveDo(this);
        }
    }
}
