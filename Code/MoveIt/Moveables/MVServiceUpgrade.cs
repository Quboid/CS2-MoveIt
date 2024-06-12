using Unity.Entities;

namespace MoveIt.Moveables
{
    internal class MVServiceUpgrade : Moveable
    {
        public override bool IsChild => true;

        public MVServiceUpgrade(Entity e) : base(e, Identity.ServiceUpgrade, ObjectType.Normal)
        {
            m_Overlay = null;// Factory.Create<OverlayBuilding>(this, OverlayTypes.MVBuilding);
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
