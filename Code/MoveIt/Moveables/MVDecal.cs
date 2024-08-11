using MoveIt.Overlays;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVDecal : Moveable
    {
        public MVDecal(Entity e) : base(e, Identity.Decal)
        {
            m_Overlay = Factory.Create<OverlayDecal>(this, OverlayTypes.MVDecal);
            Refresh();
        }
    }
}
