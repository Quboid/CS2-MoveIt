using MoveIt.Overlays;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVPlant : Moveable
    {
        public MVPlant(Entity e) : base(e, Identity.Plant, ObjectType.Normal)
        {
            m_Overlay = Factory.Create<OverlayPlant>(this, OverlayTypes.MVCircle);
            Refresh();
        }
    }
}
