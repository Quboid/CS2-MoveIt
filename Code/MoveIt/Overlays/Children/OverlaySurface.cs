using Unity.Entities;

namespace MoveIt.Overlays
{
    public class OverlaySurface : Overlay
    {
        private static EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Lines),
            });

        public static Entity Factory(Entity owner)
        {
            Entity e = _MIT.EntityManager.CreateEntity(_Archetype);

            _MIT.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVSurface));
            _MIT.EntityManager.SetComponentData<MIO_Common>(e, new(owner));

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
