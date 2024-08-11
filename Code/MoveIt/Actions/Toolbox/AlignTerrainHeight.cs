using MoveIt.Actions.Transform;
using MoveIt.Moveables;
using MoveIt.QAccessor;
using MoveIt.Tool;
using Unity.Mathematics;

namespace MoveIt.Actions.Toolbox
{
    internal class AlignTerrainHeight : TransformToolbox
    {
        public override string Name => "AlignTerrainHeight";

        protected override bool ToolDo()
        {
            for (int i = 0; i < m_Old.Count; i++)
            {
                State old = m_Old.m_States[i];
                float3 position = old.m_Position;
                position.y = _MIT.GetTerrainHeight(position);

                m_New.m_States[i].Dispose();
                State state = m_Old.m_States[i].GetCopy(_MIT.EntityManager, ref QLookupFactory.Get());
                state.m_Position = position;
                state.m_MoveDelta = position - old.m_Position;
                m_New.m_States[i] = state;
            }

            m_UpdateMove = true;
            _MIT.CreationPhase = CreationPhases.Create;
            _MIT.ToolboxManager.Phase = Managers.Phases.Finalize;
            return true;
        }
    }
}
