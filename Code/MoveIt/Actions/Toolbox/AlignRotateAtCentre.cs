using MoveIt.Actions.Transform;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Actions.Toolbox
{
    internal class AlignRotateAtCentre : TransformToolbox
    {
        public override string Name => "AlignRotationGroup";
        protected override bool ToolDo()
        {
            int seniorIdx = 0;
            for (int i = 0; i < m_Old.Count; i++)
            {
                State old = m_Old.m_States[i];
                if (old.m_Identity == Identity.Building)
                {
                    seniorIdx = i;
                    break;
                }
            }

            float angle = Moveable.m_Identity switch
            {
                Identity.Segment or Identity.NetLane => ((MVSegment)Moveable).GetAngleRelative(m_Center),
                _ => Moveable.Transform.m_Rotation.Y(),
            };
            MoveDelta = new(0f, 0f, 0f);
            AngleDelta = angle - m_Old.m_States[seniorIdx].m_InitialRotation.Y();

            DoFromAngleAndMoveDeltas();

            m_UpdateMove = true;
            m_UpdateRotate = true;
            _MIT.CreationPhase = CreationPhases.Create;
            _MIT.ToolboxManager.Phase = Managers.Phases.Finalize;
            return true;
        }
    }
}
