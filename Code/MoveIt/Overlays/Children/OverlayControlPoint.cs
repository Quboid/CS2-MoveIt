using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    internal class OverlayControlPoint : Overlay
    {
        private static EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Circle),
            });

        public static Entity Factory(Entity owner, float3 position)
        {
            Entity e = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner = owner,
                m_IsManipulatable = false,
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVControlPoint));
            _MIT.EntityManager.SetComponentData(e, common);
            _MIT.EntityManager.SetComponentData<MIO_Circle>(e, new(new(CP_RADIUS, position, quaternion.identity)));
            return e;
        }

        public OverlayControlPoint() : base(OverlayTypes.MVControlPoint) { }

        public bool CreateOverlayEntityIfNoneExists()
        {
            if (m_Moveable.m_Overlay != this) throw new System.Exception($"CP Overlay is not own Moveable's overlay! (mv:{m_Moveable.m_Entity.DX(true)})");
            if (m_Moveable is not MVControlPoint cp) throw new System.Exception($"CP Overlay is not for ControlPoint! (mv:{m_Moveable.m_Entity.DX(true)}-{m_Moveable.Name})");
            if (!m_Entity.Equals(Entity.Null)) return false;

            m_Entity = OverlayControlPoint.Factory(cp.m_Entity, cp.Transform.m_Position);
            EnqueueUpdate();

            return true;
        }

        public override bool CreateOverlayEntity()
        {
            if (m_Moveable.m_Overlay != this) throw new System.Exception($"CP Overlay is not own Moveable's overlay! (mv:{m_Moveable.m_Entity.DX(true)})");
            if (m_Moveable is not MVControlPoint cp) throw new System.Exception($"CP Overlay is not for ControlPoint! (mv:{m_Moveable.m_Entity.DX(true)}-{m_Moveable.Name})");
            if (!cp.m_Overlay.m_Entity.Equals(Entity.Null)) { MIT.Log.Debug($"CP Overlay creating but already exists {m_Entity.D()}/{cp.m_Overlay.m_Entity.D()}\n{QCommon.GetStackTrace()}"); return true; }

            m_Entity = OverlayControlPoint.Factory(cp.m_Entity, cp.Transform.m_Position);
            EnqueueUpdate();

            return true;
        }
    }
}
