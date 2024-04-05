using Game.Prefabs;
using Game.Tools;
using MoveIt.Actions;
using QCommonLib;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_InputDeps = base.OnUpdate(inputDeps);
            ClearDebugOverlays();

            if (!m_HasFocus) return inputDeps;

            NativeArray<Game.Common.RaycastResult> vanillaRaycastResults = _RaycastSystem.GetResult(m_ToolRaycastSystem);
            
            m_PointerPos = m_RaycastTerrain.HitPosition;

            QTypes.Manipulate f = QTypes.Manipulate.Parent | (_IsManipulateMode ? QTypes.Manipulate.Child : QTypes.Manipulate.Normal);
            Searcher.Ray searcher = new(Searcher.Filters.All, vanillaRaycastResults, f);
            (Entity e, float d)[] RaycastResults = searcher.OnLine(m_RaycastTerrain.Line, m_PointerPos);

            if (RaycastResults.Length > 0)
            {
                Hover.Process(RaycastResults[0].e);
            }
            else
            {
                Hover.Process(Entity.Null);
            }

            m_ApplyAction.Update();
            m_SecondaryAction.Update();
            HotkeyManager.ProcessHotkeys();

            //MIT_Tooltip.instance.Set("");
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

            Queue.FireAction(ToolAction);

            //MIT_Tooltip.instance.Set($"{Selection.Count}/{Selection.FullSelection.Count}, {Manipulation.Count}/{Manipulation.FullSelection.Count}");

            ManageCreation(Queue.Current);


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
