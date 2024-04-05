using Colossal.Entities;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Overlays;
using QCommonLib;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        private void DebugDumpSelections()
        {
            StringBuilder sb = new();

            sb.Append("Dumping Selections\nNormal Selection:");
            sb.Append(Selection.DebugSelection());
            sb.Append("\nManipulation Selection:");
            sb.Append(Manipulation.DebugSelection());

            QLog.Bundle("SEL", sb.ToString());
        }

        private void DebugDumpStateData(ref QNativeArray<Moveables.State> stateData)
        {
            StringBuilder sb = new();

            sb.AppendFormat("State Data {0}", stateData.Length);
            for (int i = 0; i < stateData.Length; i++)
            {
                sb.AppendFormat("\n    {0}", stateData[i]);
            }

            QLog.Debug(sb.ToString());
        }

        private void DebugDumpTempEntities(bool evenIfNone = false)
        {
            using var tempEntities = m_TempQuery.ToEntityArray(Allocator.Temp);

            if (m_TempQuery.IsEmptyIgnoreFilter && !evenIfNone)
            {
                return;
            }

            StringBuilder sb = new();
            sb.AppendFormat("Temp objects: {0} (C:{1},BAM:{2})", m_TempQuery.CalculateEntityCount(), CreationPhase, BaseApplyMode);
            for (int i = 0; i < tempEntities.Length; i++)
            {
                sb.AppendFormat("\n{0}", tempEntities[i].GetDump());
            }
            QLog.Bundle("Temps", sb.ToString());
        }

        private float3 _prevTempPos = new();
        private void DebugCompareTempToSelection()
        {
            if (ToolState == ToolStates.ApplyButtonHeld || ToolState == ToolStates.SecondaryButtonHeld)
            {
                using var tempEntities = m_TempQuery.ToEntityArray(Allocator.Temp);
                using var tempTemps = m_TempQuery.ToComponentDataArray<Temp>(Allocator.Temp);
                using var tempTransforms = m_TempQuery.ToComponentDataArray<Game.Objects.Transform>(Allocator.Temp);
                if (!tempTransforms[0].m_Position.Equals(_prevTempPos))
                {
                    StringBuilder sb = new();
                    sb.AppendFormat("[{0}] Comparing Temps ({1}) to Selection ({2})", UnityEngine.Time.frameCount, tempEntities.Length, Selection.Count);
                    for (int i = 0; i < tempEntities.Length; i++)
                    {
                        var original = tempTemps[i].m_Original;
                        if (!Selection.Has(original) && !Manipulation.Has(original))
                        {
                            sb.AppendFormat("\n    No Selection match for {0", original.D());
                            continue;
                        }
                        Moveables.Moveable mv;
                        if (Selection.Has(original)) mv = Selection.Get(original);
                        else mv = Manipulation.Get(original);
                        var tempPos = tempTransforms[i].m_Position;
                        using Utils.IOverlay overlay = mv.GetOverlay(OverlayFlags.None);
                        var mvPos = overlay.Common.Transform.m_Position;

                        sb.AppendFormat("\n    {0}  Temp:{1},  mvPos:{2}  Temp:mvPos:{3}", original.D(), tempPos.D(), mvPos.D(), (tempPos - mvPos).D());
                    }
                    Log.Debug(sb.ToString());
                    _prevTempPos = tempTransforms[0].m_Position;
                }
            }
        }

        private string DebugOverlay()
        {
            var c = ActiveSelection.Count;

            StringBuilder sb = new();
            sb.AppendFormat("Selection {0} ({1})", Manipulating ? "Manipulation" : "Selection", c);
            if (c > 0) sb.Append(':');

            foreach (Moveables.Moveable mv in ActiveSelection.Moveables)
            {
                using Utils.IOverlay overlay = mv.GetOverlay(OverlayFlags.None);
                sb.AppendFormat("\n  [{0}:{1}]", mv.m_Entity.D(), overlay);
            }

            if (c > 0) sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        private void DebugCreationDefinition(CreationDefinition def)
        {
            StringBuilder sb = new();
            sb.AppendFormat("CreationDefinition:{0}", def);
            sb.AppendFormat("\n       m_Attached:{0}", def.m_Attached);
            sb.AppendFormat("\n          m_Flags:{0}", def.m_Flags);
            sb.AppendFormat("\n       m_Original:{0}", def.m_Original);
            sb.AppendFormat("\n          m_Owner:{0}", def.m_Owner);
            sb.AppendFormat("\n         m_Prefab:{0}", def.m_Prefab);
            sb.AppendFormat("\n     m_RandomSeed:{0}", def.m_RandomSeed);
            sb.AppendFormat("\n      m_SubPrefab:{0}", def.m_SubPrefab);
            MIT.Log.Debug(sb.ToString());
        }

        private void DebugObjectDefinition(ObjectDefinition def)
        {
            StringBuilder sb = new();
            sb.AppendFormat("ObjectDefinition:{0}", def);
            sb.AppendFormat("\n            m_Age:{0}", def.m_Age);
            sb.AppendFormat("\n      m_Elevation:{0}", def.m_Elevation);
            sb.AppendFormat("\n     m_GroupIndex:{0}", def.m_GroupIndex);
            sb.AppendFormat("\n      m_Intensity:{0}", def.m_Intensity);
            sb.AppendFormat("\n  m_LocalPosition:{0}", def.m_LocalPosition);
            sb.AppendFormat("\n  m_LocalRotation:{0}", def.m_LocalRotation);
            sb.AppendFormat("\n     m_ParentMesh:{0}", def.m_ParentMesh);
            sb.AppendFormat("\n       m_Position:{0}", def.m_Position);
            sb.AppendFormat("\n m_PrefabSubIndex:{0}", def.m_PrefabSubIndex);
            sb.AppendFormat("\n    m_Probability:{0}", def.m_Probability);
            sb.AppendFormat("\n       m_Rotation:{0}", def.m_Rotation);
            sb.AppendFormat("\n          m_Scale:{0}", def.m_Scale);
            Log.Debug(sb.ToString());
        }

        private void DebugControlPoint(ControlPoint cp, bool hasHit, bool justthetooltip = true, bool log = true)
        {
            if (justthetooltip)
            {
                DebugControlPointTooltip(cp, hasHit);
            }
            if (log && hasHit && !Hover.Is(cp.m_OriginalEntity))
            {
                DebugControlPointLog(cp);
            }
        }

        private void DebugControlPointTooltip(ControlPoint cp, bool hasHit)
        {
            string hitDesc = "(none)";
            if (hasHit && cp.m_OriginalEntity != Entity.Null)
            {
                bool isTerrain = EntityManager.HasComponent<Game.Common.Terrain>(cp.m_OriginalEntity);
                if (isTerrain)
                {
                    hitDesc = "Terrain";
                }
                else
                {
                    PrefabInfo prefabInfo = GetPrefabInfo(cp.m_OriginalEntity);
                    hitDesc = $"{cp.m_OriginalEntity.D()} \"{prefabInfo.m_Name}\"";
                }
            }
            MIT_ToolTipSystem.instance.Set($"{hasHit} {cp.m_Position.D()}, {cp.m_HitPosition.D()} {hitDesc}");
        }

        private void DebugControlPointLog(ControlPoint cp)
        {
            StringBuilder sb = new();
            sb.AppendFormat("ControlPoint:{0}", cp);
            sb.AppendFormat("\n   m_CurvePosition:{0}", cp.m_CurvePosition);
            sb.AppendFormat("\n       m_Direction:{0}", cp.m_Direction);
            sb.AppendFormat("\n    m_ElementIndex:{0}", cp.m_ElementIndex);
            sb.AppendFormat("\n       m_Elevation:{0}", cp.m_Elevation);
            sb.AppendFormat("\n    m_HitDirection:{0}", cp.m_HitDirection);
            sb.AppendFormat("\n     m_HitPosition:{0}", cp.m_HitPosition.DX());
            sb.AppendFormat("\n  m_OriginalEntity:{0}", cp.m_OriginalEntity.D());
            sb.AppendFormat("\n        m_Position:{0}", cp.m_Position.DX());
            sb.AppendFormat("\n        m_Rotation:{0}", cp.m_Rotation);
            sb.AppendFormat("\n    m_SnapPriority:{0}", cp.m_SnapPriority);
            MIT.Log.Debug(sb.ToString());
        }
    }

    public static class DebugExtensions
    {
        public static string DX(this Entity e, bool incPrefab = false)
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (!em.Exists(e))
            {
                return $"E[null]";
            }

            char id;
            if (em.HasComponent<Game.Buildings.Building>(e)) id = 'B';
            else if (em.HasComponent<Game.Buildings.Extension>(e)) id = 'X';
            else if (em.HasComponent<Game.Objects.Plant>(e)) id = 'P';
            else if (em.HasComponent<Game.Net.Node>(e)) id = 'N';
            else if (em.HasComponent<Game.Net.Edge>(e)) id = 'S';
            else if (em.HasComponent<MIT_ControlPoint>(e)) id = 'C';
            else if (em.HasComponent<Game.Objects.Static>(e)
                && em.HasComponent<Game.Objects.NetObject>(e)) id = 'R';
            else id = '?';

            return $"E{e.Index}.{e.Version}{id}{(incPrefab ? $" (\"{QCommon.GetPrefabName(em, e)}\")" : "")}";
        }

        public static string GetDump(this Entity e)
        {
            StringBuilder sb = new();
            sb.AppendFormat("Entity: {0}", e.D());
            EntityManager EM = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (EM.TryGetComponent<Game.Objects.Transform>(e, out var transform))
            {
                sb.AppendFormat(" ({0})", transform.D());
            }

            var compTypes = EM.GetComponentTypes(e);
            int compCount = compTypes.Where(c => c.IsBuffer == false && !c.IsZeroSized).Count();
            int buffCount = compTypes.Where(c => c.IsBuffer).Count();
            int tagsCount = compTypes.Where(c => c.IsBuffer == false && c.IsZeroSized).Count();
            StringBuilder compStr = new();
            StringBuilder buffStr = new();
            StringBuilder tagsStr = new();

            if (compCount > 0)
            {
                foreach (var compType in compTypes.Where(c => c.IsBuffer == false && !c.IsZeroSized))
                {
                    compStr.AppendFormat("{0}, ", compType.GetManagedType());
                }
                compStr.Remove(compStr.Length - 2, 2);
            }
            if (buffCount > 0)
            {
                foreach (var compType in compTypes.Where(c => c.IsBuffer))
                {
                    int count = QByType.GetRefBufferLength(compType.GetManagedType(), e);
                    buffStr.AppendFormat("{0}({1}), ", compType.GetManagedType(), count);
                }
                buffStr.Remove(buffStr.Length - 2, 2);
            }
            if (tagsCount > 0)
            {
                foreach (var compType in compTypes.Where(c => c.IsBuffer == false && c.IsZeroSized))
                {
                    tagsStr.AppendFormat("{0}, ", compType.GetManagedType());
                }
                tagsStr.Remove(tagsStr.Length - 2, 2);
            }

            sb.AppendFormat("\n Components:{0} - {1}", compCount, compStr);
            sb.AppendFormat("\n    Buffers:{0} - {1}", buffCount, buffStr);
            sb.AppendFormat("\n       Tags:{0} - {1}", tagsCount, tagsStr);
            if (EM.TryGetComponent<Temp>(e, out var temp))
            {
                sb.AppendFormat("\n      <Temp> orig:{0}, flags:{1}", temp.m_Original.D(), temp.m_Flags);
            }
            return sb.ToString();
        }

        public static void Dump(this Entity e)
        {
            MIT.Log.Debug(e.GetDump());
        }
    }
}
