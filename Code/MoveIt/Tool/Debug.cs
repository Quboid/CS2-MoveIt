using Colossal.Entities;
using Game.Tools;
using MoveIt.Moveables;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        internal static string DebugDefinitions(IEnumerable<MVDefinition> definitions)
        {
            StringBuilder sb = new();
            sb.AppendFormat("Definitions: {0}", definitions.Count());
            foreach (MVDefinition mvd in definitions)
            {
                sb.AppendFormat("\n        {0}", mvd);
            }
            return sb.ToString();
        }

        internal static void DebugDumpDefinitions(IEnumerable<MVDefinition> definitions, string prefix = "")
        {
            QLog.Debug(prefix + DebugDefinitions(definitions));
        }


        internal string DebugMoveables(IEnumerable<Moveable> moveables)
        {
            StringBuilder sb = new();
            sb.AppendFormat("Moveables: {0}", moveables.Count());
            foreach (Moveable mv in moveables)
            {
                sb.AppendFormat("\n        {0} - {1}", mv, mv.Definition);
            }
            return sb.ToString();
        }

        internal void DebugDumpMoveables(IEnumerable<Moveable> moveables, string prefix = "")
        {
            QLog.Debug(prefix + DebugMoveables(moveables));
        }


        internal string DebugStateData(ref NativeArray<State> stateData)
        {
            StringBuilder sb = new();
            sb.AppendFormat("State Data {0}", stateData.Length);
            for (int i = 0; i < stateData.Length; i++)
            {
                sb.AppendFormat("\n        {0}", stateData[i]);
            }
            return sb.ToString();
        }

        internal void DebugDumpStateData(ref NativeArray<State> stateData, string prefix = "")
        {
            QLog.Debug(prefix + DebugStateData(ref stateData));
        }


        internal void DebugCreationDefinition(CreationDefinition def)
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
            Log.Debug(sb.ToString());
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
            Log.Debug(sb.ToString());
        }
    }

    public static class DebugExtensions
    {
        public static string DX(this Entity e, bool incPrefab = false, bool align = false)
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (e.Equals(Entity.Null)) return $"E[null]";
            if (!manager.Exists(e)) return $"E[!{e.Index}.{e.Version}]";

            Identity id = QTypes.GetEntityIdentity(e);
            string idCode = QTypes.GetIdentityCode(id);

            string ent = $"E{e.Index}.{e.Version}{idCode}";
            if (align) ent = $"{ent,13}";
            return $"{ent}{(incPrefab ? $" (\"{QCommon.GetPrefabName(manager, e)}\")" : "")}";
        }

        public static string DebugEntity(this Entity e)
        {
            StringBuilder sb = new();
            sb.AppendFormat("Entity: {0}", e.D());
            EntityManager EM = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (EM.TryGetComponent<Game.Objects.Transform>(e, out var transform))
            {
                sb.AppendFormat(" ({0})", transform.D());
            }

            var compTypes = EM.GetComponentTypes(e);
            int sharCount = compTypes.Where(c => IsSharedComponent(c)).Count();
            int compCount = compTypes.Where(c => IsNormalComponent(c)).Count();
            int buffCount = compTypes.Where(c => IsBufferComponent(c)).Count();
            int tagsCount = compTypes.Where(c => IsTagComponent(c)).Count();
            StringBuilder sharStr = new();
            StringBuilder compStr = new();
            StringBuilder buffStr = new();
            StringBuilder tagsStr = new();

            if (sharCount > 0)
            {
                foreach (var compType in compTypes.Where(c => IsSharedComponent(c)))
                {
                    sharStr.AppendFormat("{0},  ", compType.GetManagedType());
                }
                sharStr.Remove(sharStr.Length - 2, 2);
            }
            if (compCount > 0)
            {
                foreach (var compType in compTypes.Where(c => IsNormalComponent(c)))
                {
                    compStr.AppendFormat("{0},  ", compType.GetManagedType());
                }
                compStr.Remove(compStr.Length - 2, 2);
            }
            if (buffCount > 0)
            {
                foreach (var compType in compTypes.Where(c => IsBufferComponent(c)))
                {
                    int count = QByType.GetRefBufferLength(compType.GetManagedType(), e);
                    buffStr.AppendFormat("{0}({1}),  ", compType.GetManagedType(), count);
                }
                buffStr.Remove(buffStr.Length - 2, 2);
            }
            if (tagsCount > 0)
            {
                foreach (var compType in compTypes.Where(c => IsTagComponent(c)))
                {
                    tagsStr.AppendFormat("{0},  ", compType.GetManagedType());
                }
                tagsStr.Remove(tagsStr.Length - 2, 2);
            }

            sb.AppendFormat("\n     Shared:{0} - {1}", sharCount, sharStr);
            sb.AppendFormat("\n Components:{0} - {1}", compCount, compStr);
            sb.AppendFormat("\n    Buffers:{0} - {1}", buffCount, buffStr);
            sb.AppendFormat("\n       Tags:{0} - {1}", tagsCount, tagsStr);
            if (EM.TryGetComponent<Temp>(e, out var temp))
            {
                sb.AppendFormat("\n      <Temp> orig:{0}, flags:{1}", temp.m_Original.D(), temp.m_Flags);
            }
            return sb.ToString();
        }

        private static bool IsSharedComponent(ComponentType c)  => c.IsSharedComponent;
        private static bool IsNormalComponent(ComponentType c)  => !c.IsSharedComponent && c.IsBuffer == false && !c.IsZeroSized;
        private static bool IsBufferComponent(ComponentType c)  => !c.IsSharedComponent && c.IsBuffer;
        private static bool IsTagComponent(ComponentType c)     => !c.IsSharedComponent && c.IsBuffer == false && c.IsZeroSized;

        public static void DebugDumpEntity(this Entity e, string prefix = "")
        {
            MIT.Log.Debug(prefix + e.DebugEntity());
        }
    }
}
