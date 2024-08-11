using Colossal.Mathematics;
using MoveIt.Overlays;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVSurface : Moveable
    {
        public override Game.Objects.Transform Transform
        {
            get
            {
                Circle3 circle = QAccessor.QEntity.GetSurfaceCircle(_MIT.EntityManager, m_Entity);
                return new(circle.position, circle.rotation);
            }
        }

        public MVSurface(Entity e) : base(e, Identity.Surface)
        {
            m_Overlay = Factory.Create<OverlaySurface>(this, OverlayTypes.MVSurface);
            Refresh();
        }

        internal override float GetRadius()
        {
            return QAccessor.QEntity.GetSurfaceCircle(_MIT.EntityManager, m_Entity).radius;
        }
    }
}
