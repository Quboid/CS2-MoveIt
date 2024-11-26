using MoveIt.Overlays.Children;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVPlant : Moveable
    {
        public MVPlant(Entity e) : base(e, Identity.Plant)
        {
            m_Overlay = new OverlayPlant(this);
            RefreshFromAbstract();
        }
    }
}
