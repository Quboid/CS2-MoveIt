using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    internal class OverlayManipControlPoint : Overlay
    {
        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Circle),
            });

        public static Entity Factory(MVManipControlPoint mcp)
        {
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner             = mcp.m_Entity,
                m_IsManipulatable   = mcp.IsManipulatable,
                m_IsManipChild      = mcp.IsManipChild,
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVManipControlPoint));
            _Tool.EntityManager.SetComponentData(e, common);
            _Tool.EntityManager.SetComponentData<MIO_Circle>(e, new(new(CP_RADIUS, mcp.Transform.m_Position, quaternion.identity)));
            return e;
        }

        public OverlayManipControlPoint() : base(OverlayTypes.MVControlPoint) { }

        public bool CreateOverlayEntityIfNoneExists()
        {
            if (m_Moveable.m_Overlay != this) throw new System.Exception($"CP ManipOverlay is not own Moveable's overlay! (mv:{m_Moveable.m_Entity.DX(true)})");
            if (m_Moveable is not MVManipControlPoint mcp) throw new System.Exception($"CP ManipOverlay is not for ControlPoint! (mv:{m_Moveable.m_Entity.DX(true)}-{m_Moveable.Name})");
            if (!m_Entity.Equals(Entity.Null)) return false;

            m_Entity = OverlayManipControlPoint.Factory(mcp);
            EnqueueUpdate();

            return true;
        }

        public override bool CreateOverlayEntity()
        {
            if (m_Moveable.m_Overlay != this) throw new System.Exception($"CP ManipOverlay is not own Moveable's overlay! (mv:{m_Moveable.m_Entity.DX(true)})");
            if (m_Moveable is not MVManipControlPoint mcp) throw new System.Exception($"CP ManipOverlay is not for ControlPoint! (mv:{m_Moveable.m_Entity.DX(true)}-{m_Moveable.Name})");
            if (!mcp.m_Overlay.m_Entity.Equals(Entity.Null)) { MIT.Log.Debug($"CP ManipOverlay creating but already exists {m_Entity.D()}/{mcp.m_Overlay.m_Entity.D()}\n{QCommon.GetStackTrace()}"); return true; }

            m_Entity = OverlayManipControlPoint.Factory(mcp);
            EnqueueUpdate();

            return true;
        }

        public override void EnqueueUpdate()
        {
            if (m_Moveable is not MVManipControlPoint cp) throw new System.Exception($"CP ManipOverlay is not for ControlPoint! (mv:{m_Moveable.m_Entity.DX(true)}-{m_Moveable.Name})");

            _Tool.QueueOverlayUpdate(this);
            if (_Tool.Moveables.TryGet(cp.SegmentDefinition, out MVManipSegment seg)) seg.UpdateOverlayDeferred();
        }
    }
}
