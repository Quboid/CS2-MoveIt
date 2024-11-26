using MoveIt.Moveables;
using Unity.Entities;

namespace MoveIt.Overlays.Children
{
    internal class OverlayDecal : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Lines),
            });


        public OverlayDecal(Moveable mv) : base(OverlayTypes.MVDecal, mv) { }

        protected override bool CreateOverlayEntity()
        {
            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new(_Moveable.m_Entity);

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVDecal));
            _MIT.EntityManager.SetComponentData(m_Entity, common);

            EnqueueUpdate();

            return true;
        }
    }
}
