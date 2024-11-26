using MoveIt.Actions.Transform;
using MoveIt.Overlays.Children;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Moveables
{
    public class MVManipControlPoint : MVControlPoint
    {
        public override bool IsManipulatable    => true;
        public override bool IsManipChild       => true;
        public override bool IsNormalChild      => false;

        public MVManipControlPoint(MVDefinition mvd) : base(mvd.m_Entity, Identity.ControlPoint)
        {
            m_Parent = mvd.m_Parent;
            m_ParentKey = mvd.m_ParentKey;
            m_Entity = Managers.ControlPointManager.CreateEntity(mvd);
            m_Overlay = new OverlayManipControlPoint(this);
            RefreshFromAbstract();
        }

        internal override void DisposeIfUnused()
        {
            if (_MIT.Hover.Is(Definition))                 return;
            if (_MIT.Hover.Is(ParentDefinition))           return;
            if (_MIT.Selection.HasFull(Definition))        return;
            if (_MIT.Selection.HasFull(NodeDefinition))    return;
            if (_MIT.Selection.HasFull(ParentDefinition))  return;

            _MIT.Moveables.RemoveDo(this);
        }

        internal override void MoveIt(TransformBase action, State state, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            if (QTypes.GetEntityIdentity(m_Parent) == Identity.NetLane)
            {
                if (!_MIT.EntityManager.HasComponent<Game.Net.Elevation>(m_Parent))
                {
                    QLog.Debug($"Adding Elevation to NetLane segment {m_Parent.DX(true)}");
                    _MIT.EntityManager.AddComponentData<Game.Net.Elevation>(m_Parent, new(new(0f, 0f)));
                }
                Game.Net.Edge edge = _MIT.EntityManager.GetComponentData<Game.Net.Edge>(m_Parent);
                if (!_MIT.EntityManager.HasComponent<Game.Net.Elevation>(edge.m_Start))
                {
                    QLog.Debug($"Adding Elevation to NetLane start node {edge.m_Start.DX(true)}");
                    _MIT.EntityManager.AddComponentData<Game.Net.Elevation>(edge.m_Start, new(new(0f, 0f)));
                }
                if (!_MIT.EntityManager.HasComponent<Game.Net.Elevation>(edge.m_End))
                {
                    QLog.Debug($"Adding Elevation to NetLane end node {edge.m_End.DX(true)}");
                    _MIT.EntityManager.AddComponentData<Game.Net.Elevation>(edge.m_End, new(new(0f, 0f)));
                }
            }

            base.MoveIt(action, state, move, rotate);
        }
    }
}
