using Game.Prefabs;
using MoveIt.Actions;
using MoveIt.Managers;
using MoveIt.Moveables;
using QCommonLib;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Tool
{
    public partial class MIT : Game.Tools.ObjectToolBaseSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_InputDeps = base.OnUpdate(inputDeps);
            //ClearDebugOverlays();

            if (!m_HasFocus) return inputDeps;
            UpdateUIHasFocus();

            if (m_SelectionDirty && Selection is not null)
            {
                Selection.CalculateCenter();
                m_SelectionDirty = false;
            }

            m_PointerPos = m_RaycastTerrain.HitPosition;

            Hover.Process(m_ToolRaycastSystem);

            m_ApplyAction.Update();
            m_SecondaryAction.Update();
            HotkeyManager.ProcessHotkeys();

            switch (ToolState)
            {
                case ToolStates.Default:
                case ToolStates.ToolActive:
                    {
                        break;
                    }

                case ToolStates.ApplyButtonHeld:
                case ToolStates.SecondaryButtonHeld:
                    {
                        ToolAction = ToolActions.Do;

                        break;
                    }

                case ToolStates.DrawingSelection:
                    {
                        if (m_Marquee is null)
                        {
                            Log.Warning($"Drawing selection but m_Marquee is null");
                            break;
                        }

                        if (!m_Marquee.CheckIfMoved(m_PointerPos))
                        {
                            break;
                        }

                        if (Queue.Current is not SelectAction sa)
                        {
                            Log.Debug($"Update DrawingSelection but current action is {Queue.Current.Name}");
                            break;
                        }

                        UpdateMarqueeList(m_Marquee);
                        sa.AddMarqueeSelection(m_Marquee);
                        ToolAction = ToolActions.Do;

                        break;
                    }
            }

            Queue.FireAction();

            ManageCreation(Queue.Current);

            //DebugDumpSelections();

            return m_InputDeps;
        }

        internal PrefabInfo GetPrefabInfo(Entity e)
        {
            PrefabInfo result = new();
            if (e == Entity.Null)
            {
                throw new Exception("Tool.GetPrefab: Entity is null");
            }
            if (!EntityManager.HasComponent<PrefabRef>(e))
            {
                throw new Exception($"Tool.GetPrefab: Entity {e.D()} has no PrefabRef");
            }

            PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(e);
            result.m_Name = QCommon.GetPrefabName(EntityManager, e);
            result.m_Entity = prefabRef.m_Prefab;
            return result;
        }

    }
}
