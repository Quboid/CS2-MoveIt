using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.Children
{
    internal class OverlayControlPoint : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Circle),
            });


        public OverlayControlPoint(Moveable mv) : base(OverlayTypes.MVControlPoint, mv) { }

        protected override bool CreateOverlayEntity()
        {
            if (_Moveable is not MVControlPoint cp) throw new System.Exception($"CP Overlay is not for ControlPoint! (mv:{_Moveable.m_Entity.DX(true)}-{_Moveable.Name})");
            if (cp.m_Overlay is not null)
            {
                if (cp.m_Overlay != this) throw new System.Exception($"CP Overlay is not own Moveable's overlay! (mv:{cp.m_Entity.DX()}, mv.cp:{cp.m_Overlay.E()}, this:{E()})");
                if (!cp.m_Overlay.m_Entity.Equals(Entity.Null)) { MIT.Log.Debug($"CP Overlay creating but already exists {m_Entity.D()}/{cp.m_Overlay.m_Entity.D()}\n{QCommon.GetStackTrace()}"); return true; }
            }

            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner = cp.m_Entity,
                m_IsManipulatable = cp.IsManipulatable,
                m_IsManipChild = cp.IsManipChild,
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVControlPoint));
            _MIT.EntityManager.SetComponentData(m_Entity, common);
            _MIT.EntityManager.SetComponentData<MIO_Circle>(m_Entity, new(new(CP_RADIUS, cp.Transform.m_Position, quaternion.identity)));

            //QLog.Debug($"OlayCP-CreateOlayEnt {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name} {E()}");
            EnqueueUpdate();

            return true;
        }
    }
}
