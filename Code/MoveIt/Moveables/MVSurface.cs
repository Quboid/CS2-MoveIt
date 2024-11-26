using Colossal.Mathematics;
using MoveIt.Overlays.Children;
using MoveIt.QAccessor.QEntity;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVSurface : Moveable
    {
        public override Game.Objects.Transform Transform
        {
            get
            {
                Circle3 circle = QEntity.GetSurfaceCircle(_MIT.EntityManager, m_Entity);
                return new(circle.position, circle.rotation);
            }
        }

        public MVSurface(Entity e) : base(e, Identity.Surface)
        {
            m_Overlay = new OverlaySurface(this);
            RefreshFromAbstract();
        }

        internal override float GetRadius()
        {
            return QEntity.GetSurfaceCircle(_MIT.EntityManager, m_Entity).radius;
        }
    }
}
