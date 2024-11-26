using MoveIt.Moveables;
using Unity.Entities;

namespace MoveIt.Overlays.Children
{
    public class OverlaySurface : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Lines),
            });


        public OverlaySurface(Moveable mv) : base(OverlayTypes.MVSurface, mv) { }

        protected override bool CreateOverlayEntity()
        {
            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVSurface));
            _MIT.EntityManager.SetComponentData<MIO_Common>(m_Entity, new(_Moveable.m_Entity));
            EnqueueUpdate();

            return true;
        }
    }
}
