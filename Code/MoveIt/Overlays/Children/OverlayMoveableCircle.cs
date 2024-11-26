using Colossal.Mathematics;
using MoveIt.Moveables;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.Children
{
    public abstract class OverlayMoveableCircle : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Circle),
            });


        public OverlayMoveableCircle(Moveable mv) : base(OverlayTypes.MVCircle, mv) { }

        protected override bool CreateOverlayEntity()
        {
            Circle3 circle = new(_Moveable.GetRadius(), Transform.m_Position, quaternion.identity);
            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVCircle));
            _MIT.EntityManager.SetComponentData<MIO_Common>(m_Entity, new(_Moveable.m_Entity));
            _MIT.EntityManager.SetComponentData<MIO_Circle>(m_Entity, new(circle));

            EnqueueUpdate();

            return true;
        }
    }
}
