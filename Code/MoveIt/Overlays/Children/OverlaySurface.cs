using Unity.Entities;

namespace MoveIt.Overlays
{
    public class OverlaySurface : Overlay
    {
        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Lines),
            });

        public static Entity Factory(Entity owner)
        {
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVSurface));
            _Tool.EntityManager.SetComponentData<MIO_Common>(e, new(owner));

            return e;
        }


        public OverlaySurface() : base(OverlayTypes.MVSurface) { }

        public override bool CreateOverlayEntity()
        {
            if (!base.CreateOverlayEntity()) return false;

            m_Entity = OverlaySurface.Factory(m_Moveable.m_Entity);
            EnqueueUpdate();

            return true;
        }
    }
}
