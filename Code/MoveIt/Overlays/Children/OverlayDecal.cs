using Unity.Entities;

namespace MoveIt.Overlays
{
    internal class OverlayDecal : Overlay
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

            MIO_Common common = new(owner);

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVDecal));
            _Tool.EntityManager.SetComponentData(e, common);

            return e;
        }


        public OverlayDecal() : base(OverlayTypes.MVDecal) { }

        public override bool CreateOverlayEntity()
        {
            if (!base.CreateOverlayEntity()) return false;

            m_Entity = OverlayDecal.Factory(m_Moveable.m_Entity);
            EnqueueUpdate();

            return true;
        }
    }
}
