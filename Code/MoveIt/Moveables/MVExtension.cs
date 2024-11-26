using MoveIt.Overlays.Children;
using Unity.Entities;

namespace MoveIt.Moveables
{
    internal class MVExtension : Moveable
    {
        public override bool IsManipChild => true;

        public MVExtension(Entity e) : base(e, Identity.Extension)
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

        //public override void Dispose()
        //{ }
    }
}
