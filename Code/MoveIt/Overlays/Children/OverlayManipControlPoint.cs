using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.Children
{
    internal class OverlayManipControlPoint : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Circle),
            });


        public OverlayManipControlPoint(Moveable mv) : base(OverlayTypes.MVManipControlPoint, mv)
        { }

        protected override bool CreateOverlayEntity()
        {
            if (_Moveable is not MVManipControlPoint mcp) throw new System.Exception($"CP ManipOverlay is not for ManipControlPoint! (mv:{_Moveable.m_Entity.DX(true)}-{_Moveable.Name})");
            if (mcp.m_Overlay is not null)
            {
                if (mcp.m_Overlay != this) throw new System.Exception($"CP ManipOverlay is not own Moveable's overlay! (mv:{mcp.m_Entity.DX()}, mv.cp:{mcp.m_Overlay.E()}, this:{E()})");
                if (!mcp.m_Overlay.m_Entity.Equals(Entity.Null)) { MIT.Log.Debug($"CP ManipOverlay creating but already exists {m_Entity.D()}/{mcp.m_Overlay.m_Entity.D()}\n{QCommon.GetStackTrace()}"); return true; }
            }

            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner = mcp.m_Entity,
                m_IsManipulatable = mcp.IsManipulatable,
                m_IsManipChild = mcp.IsManipChild,
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVManipControlPoint));
            _MIT.EntityManager.SetComponentData(m_Entity, common);
            _MIT.EntityManager.SetComponentData<MIO_Circle>(m_Entity, new(new(CP_RADIUS, mcp.Transform.m_Position, quaternion.identity)));

            //QLog.Debug($"OlayMCP-CreateOlayEnt {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name} {E()}");
            EnqueueUpdate();

            return true;
        }

        public override void EnqueueUpdate()
        {
#if IS_DEBUG
            m_Caller = QCommon.GetStackTrace(15);
#endif
            if (_Moveable is not MVManipControlPoint cp) throw new System.Exception($"CP ManipOverlay is not for ControlPoint! (mv:{_Moveable.m_Entity.DX(true)}-{_Moveable.Name})");

            if (m_Owner.Exists(_MIT.EntityManager))
            {
                _MIT.QueueOverlayUpdate(this);
            }
            if (_MIT.Moveables.TryGet(cp.ParentDefinition, out MVManipSegment seg)) seg.UpdateOverlayDeferred();
        }
    }
}
