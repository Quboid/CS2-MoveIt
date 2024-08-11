using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public abstract class OverlayMoveableCircle : Overlay
    {
        private static EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Circle),
            });

        public static Entity Factory(Entity owner, float radius, float3 position)
        {
            return Factory(owner, new Circle3(radius, position, quaternion.identity));
        }

        public static Entity Factory(Entity owner, Circle3 circle)
        {
            Entity e = _MIT.EntityManager.CreateEntity(_Archetype);

            _MIT.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVCircle));
            _MIT.EntityManager.SetComponentData<MIO_Common>(e, new(owner));
            _MIT.EntityManager.SetComponentData<MIO_Circle>(e, new(circle));

            return e;
        }


        public OverlayMoveableCircle() : base(OverlayTypes.MVCircle) { }

        public override bool CreateOverlayEntity()
        {
            if (m_Moveable is null) return false;
            if (!base.CreateOverlayEntity()) return false;

            m_Entity = Factory(m_Moveable.m_Entity, m_Moveable.GetRadius(), Transform.m_Position);
            EnqueueUpdate();

            return true;
        }
    }
}
