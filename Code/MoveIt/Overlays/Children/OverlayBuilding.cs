using MoveIt.Moveables;
using Unity.Entities;

namespace MoveIt.Overlays.Children
{
    public class OverlayBuilding : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Lines),
                typeof(MIO_DashedLines),
            });


        public OverlayBuilding(Moveable mv) : base(OverlayTypes.MVBuilding, mv) { }

        protected override bool CreateOverlayEntity()
        {
            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVBuilding));
            _MIT.EntityManager.SetComponentData<MIO_Common>(m_Entity, new(_Moveable.m_Entity)); 
            EnqueueUpdate();

            return true;
        }
    }
}
