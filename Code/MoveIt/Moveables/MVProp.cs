using MoveIt.Overlays.Children;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVProp : Moveable
    {
        public MVProp(Entity e) : base(e, Identity.Prop)
        {
            m_Overlay = new OverlayProp(this);
            RefreshFromAbstract();
        }
    }
}
