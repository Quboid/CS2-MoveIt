using MoveIt.Overlays;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVProp : Moveable
    {
        public MVProp(Entity e) : base(e, Identity.Prop)
        {
            m_Overlay = Factory.Create<OverlayProp>(this, OverlayTypes.MVCircle);
            Refresh();
        }
    }
}
