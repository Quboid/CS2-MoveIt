using MoveIt.Overlays;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVProp : Moveable
    {
        public MVProp(Entity e) : base(e, Identity.Prop, ObjectType.Normal)
        {
            m_Overlay = Factory.Create<OverlayProp>(this, OverlayTypes.MVCircle);
            Refresh();
        }
    }
}
