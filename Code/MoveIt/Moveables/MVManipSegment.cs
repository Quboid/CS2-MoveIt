using MoveIt.Actions;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVManipSegment : MVSegment
    {
        public override bool IsManipulatable => true;

        public MVManipSegment(Entity e) : base(e, Identity.Segment, ObjectType.Normal)
        {
            m_Overlay = Factory.Create<OverlayManipSegment>(this, OverlayTypes.MVManipSegment);
            Refresh();
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            foreach (var child in GetChildMoveablesForOverlays<MVManipControlPoint>())
            {
                _Tool.Selection.RemoveIfExists(child.Definition);
            }
        }

        internal override void MoveIt(TransformAction action, State state, bool move, bool rotate)
        {
            MIT.Log.Error($"Attempted to move ManipulateSegment {m_Entity.D()}");
        }
    }
}
