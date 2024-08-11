using MoveIt.Overlays;
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
                m_Overlay = Factory.Create<OverlayNone>(this, OverlayTypes.None);
            }
            else
            {
                m_Overlay = Factory.Create<OverlayBuilding>(this, OverlayTypes.MVBuilding);
            }
            Refresh();
        }

        internal override bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            //m_Overlay.EnqueueUpdate();
            return true;
        }

        public override void Dispose()
        { }
    }
}
