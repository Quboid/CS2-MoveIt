using MoveIt.Overlays.Children;
using Unity.Entities;

namespace MoveIt.Moveables
{
    internal class MVServiceUpgrade : Moveable
    {
        public override bool IsManipChild => true;

        public MVServiceUpgrade(Entity e) : base(e, Identity.ServiceUpgrade)
        {
            if (_MIT.EntityManager.HasComponent<Game.Common.Owner>(e))
            {
                m_Overlay = new OverlayNone(this);
            }
            else
            {
                m_Overlay = new OverlayBuilding(this);
            }
            RefreshFromAbstract();
        }

        //internal override bool Refresh()
        //{
        //    if (!IsValid) return false;
        //    if (!IsOverlayValid) return false;

        //    //m_Overlay.EnqueueUpdate();
        //    return true;
        //}
    }
}
