using MoveIt.Actions.Transform;
using MoveIt.Overlays.Children;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVManipSegment : MVSegment
    {
        public override bool IsManipulatable => true;

        public MVManipSegment(Entity e) : base(e, Identity.Segment)
        {
            m_CPDefinitions = new();
            for (short i = 0; i < CURVE_CPS; i++)
            {
                MVDefinition mvd = new(Identity.ControlPoint, Entity.Null, IsManipulatable, true, m_Entity, m_Identity, i);
                //QLog.Debug($"{i} MVMSeg.ctor1 cp:{mvd}");
                MVControlPoint cp = _MIT.ControlPointManager.GetOrCreateMoveable(mvd);
                m_CPDefinitions.Add(cp.Definition);
                //QLog.Debug($"{i} MVMSeg.ctor2 cp:{mvd}");
            }

            m_Overlay = new OverlayManipSegment(this);
            RefreshFromAbstract();
        }

        internal override void MoveIt(TransformBase action, State state, bool move, bool rotate)
        {
            MIT.Log.Error($"Attempted to move ManipulateSegment {m_Entity.D()}");
        }
    }
}
