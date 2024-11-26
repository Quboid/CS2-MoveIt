using MoveIt.Overlays.Children;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVDecal : Moveable
    {
        public MVDecal(Entity e) : base(e, Identity.Decal)
        {
            m_Overlay = new OverlayDecal(this);
            RefreshFromAbstract();
        }
    }
}
