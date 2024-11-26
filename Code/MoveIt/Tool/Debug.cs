using Colossal.Entities;
using Game.Objects;
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
            IEnumerable<MVDefinition> mvDefinitions = definitions as MVDefinition[] ?? definitions.ToArray();
            sb.AppendFormat("Definitions: {0}", mvDefinitions.Count());
            foreach (MVDefinition mvd in mvDefinitions)
            {
                sb.AppendFormat("\n        {0}", mvd);
            }
            return sb.ToString();
        }

        internal static void DebugDumpDefinitions(IEnumerable<MVDefinition> definitions, string prefix = "", bool stack = false)
        {
            Log.Debug(prefix + DebugDefinitions(definitions) + (stack ? "\n" + QCommon.GetStackTrace() : ""));
        }


        internal string DebugMoveables(IEnumerable<Moveable> moveables)
        {
            StringBuilder sb = new();
            IEnumerable<Moveable> enumerable = moveables as Moveable[] ?? moveables.ToArray();
            sb.AppendFormat("Moveables: {0}", enumerable.Count());
            foreach (Moveable mv in enumerable)
            {
                sb.AppendFormat("\n        {0} - {1}", mv, mv.Definition);
            }
            return sb.ToString();
        }

        internal void DebugDumpMoveables(IEnumerable<Moveable> moveables, string prefix = "")
        {
            Log.Debug(prefix + DebugMoveables(moveables));
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
            Log.Debug(prefix + DebugStateData(ref stateData));
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
            Systems.MIT_ToolTipSystem.instance.Set($"{hasHit} {cp.m_Position.D()}, {cp.m_HitPosition.D()} {hitDesc}");
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
            if (!manager.Exists(e)) return $"E![{e.Index}.{e.Version}]";

            Identity id = QTypes.GetEntityIdentity(e);
            string idCode = QTypes.GetIdentityCode(id);

            var ent = $"E{e.Index}.{e.Version}{idCode}";
            if (align) ent = $"{ent,13}";
            return $"{ent}{(incPrefab ? $" (\"{QCommon.GetPrefabName(manager, e)}\")" : "")}";
        }

        public static string DebugEntity(this Entity e)
        {
            StringBuilder sb = new();
            sb.AppendFormat("Entity: {0}", e.D());
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (manager.TryGetComponent<Game.Objects.Transform>(e, out Transform transform))
            {
                sb.AppendFormat(" ({0})", transform.D());
            }

            NativeArray<ComponentType> compTypes = manager.GetComponentTypes(e);
            int sharCount = compTypes.Count(IsSharedComponent);
            int compCount = compTypes.Count(IsNormalComponent);
            int buffCount = compTypes.Count(IsBufferComponent);
            int tagsCount = compTypes.Count(IsTagComponent);
            StringBuilder sharStr = new();
            StringBuilder compStr = new();
            StringBuilder buffStr = new();
            StringBuilder tagsStr = new();

            if (sharCount > 0)
            {
                foreach (ComponentType compType in compTypes.Where(IsSharedComponent))
                {
                    sharStr.AppendFormat("{0},  ", compType.GetManagedType());
                }
                sharStr.Remove(sharStr.Length - 2, 2);
            }
            if (compCount > 0)
            {
                foreach (ComponentType compType in compTypes.Where(IsNormalComponent))
                {
                    compStr.AppendFormat("{0},  ", compType.GetManagedType());
                }
                compStr.Remove(compStr.Length - 2, 2);
            }
            if (buffCount > 0)
            {
                foreach (ComponentType compType in compTypes.Where(IsBufferComponent))
                {
                    int count = QByType.GetRefBufferLength(compType.GetManagedType(), e);
                    buffStr.AppendFormat("{0}({1}),  ", compType.GetManagedType(), count);
                }
                buffStr.Remove(buffStr.Length - 2, 2);
            }
            if (tagsCount > 0)
            {
                foreach (ComponentType compType in compTypes.Where(IsTagComponent))
                {
                    tagsStr.AppendFormat("{0},  ", compType.GetManagedType());
                }
                tagsStr.Remove(tagsStr.Length - 2, 2);
            }

            sb.AppendFormat("\n     Shared:{0} - {1}", sharCount, sharStr);
            sb.AppendFormat("\n Components:{0} - {1}", compCount, compStr);
            sb.AppendFormat("\n    Buffers:{0} - {1}", buffCount, buffStr);
            sb.AppendFormat("\n       Tags:{0} - {1}", tagsCount, tagsStr);
            if (manager.TryGetComponent<Temp>(e, out Temp temp))
            {
                sb.AppendFormat("\n      <Temp> orig:{0}, flags:{1}", temp.m_Original.D(), temp.m_Flags);
            }
            return sb.ToString();
        }

        private static bool IsSharedComponent(ComponentType c)  => c.IsSharedComponent;
        private static bool IsNormalComponent(ComponentType c)  => c is { IsSharedComponent: false, IsBuffer: false, IsZeroSized: false };
        private static bool IsBufferComponent(ComponentType c)  => c is { IsSharedComponent: false, IsBuffer: true };
        private static bool IsTagComponent(ComponentType c)     => c is { IsSharedComponent: false, IsBuffer: false, IsZeroSized: true };

        public static void DebugDumpEntity(this Entity e, string prefix = "")
        {
            MIT.Log.Debug(prefix + e.DebugEntity());
        }
    }
}
