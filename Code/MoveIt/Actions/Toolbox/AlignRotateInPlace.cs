using MoveIt.Actions.Transform;
using MoveIt.Moveables;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Actions.Toolbox
{
    internal class AlignRotateInPlace : TransformToolbox
    {
        public override string Name => "AlignRotationIndividual";
        protected override bool ToolDo()
        {
            for (int i = 0; i < m_Old.Count; i++)
            {
                State old = m_Old.m_States[i];
                float3 position = old.m_Position;
                var angle = Moveable.m_Identity switch
                {
                    Identity.Segment or Identity.NetLane => ((MVSegment)Moveable).GetAngleRelative(position),
                    _ => Moveable.Transform.m_Rotation.Y(),
                };

                float3 oldAngles = old.m_InitialRotation.ToEulerDegrees();
                quaternion q = Quaternion.Euler(oldAngles.x, angle, oldAngles.z);

                m_New.m_States[i].Dispose();
                State state = m_Old.m_States[i].GetCopy(_MIT.EntityManager, ref QLookupFactory.Get());
                state.m_Rotation = q;
                state.m_AngleDelta = angle - oldAngles.y;
                m_New.m_States[i] = state;
            }

            m_UpdateRotate = true;
            _MIT.CreationPhase = CreationPhases.Create;
            _MIT.ToolboxManager.Phase = Managers.Phases.Finalize;
            return true;
        }
    }
}
