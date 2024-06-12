using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    public struct QReferenceBufferType
    {
        /// <summary>
        /// The type of the buffer component
        /// </summary>
        public Type tParentComponent;
        /// <summary>
        /// The FieldInfo of the buffer component's Entity field
        /// </summary>
        public FieldInfo m_FieldInfo;
    }

    /// <summary>
    /// Primary accessor for entities, including children
    /// </summary>
    public struct QObject : IDisposable, INativeDisposable
    {
        private readonly EntityManager Manager => World.DefaultGameObjectInjectionWorld.EntityManager;

        internal Entity m_Entity;
        internal QEntity m_Parent;
        internal NativeList<QEntity> m_Children;
        internal Identity m_Identity;

        internal QObject(Entity e, SystemBase system, Identity identity = Identity.None)
        {
            if (e == Entity.Null) throw new ArgumentNullException("Creating QObject with null entity");

            m_Entity = e;
            m_Identity = identity == Identity.None ? QTypes.GetEntityIdentity(e) : identity;
            m_Parent = new(system, e, m_Identity);
            m_Children = new(0, Allocator.Persistent);

            var subEntities = GetSubEntities(e, m_Identity);

            if (subEntities.Count > 0)
            {
                for (int i = 0; i < subEntities.Count; i++)
                {
                    if (subEntities[i] == Entity.Null) throw new NullReferenceException($"Creating child for {e.D()} with null entity");

                    if (system.EntityManager.HasComponent<Game.Net.ConnectionLane>(subEntities[i])) continue;

                    Identity subType = QTypes.GetEntityIdentity(subEntities[i]);
                    m_Children.Add(new(system, subEntities[i], subType));
                }
            }
        }

        public void Dispose()
        {
            m_Children.Dispose();
        }

        public JobHandle Dispose(JobHandle handle)
        {
            handle = m_Children.Dispose(handle);
            return handle;
        }

        #region Transforming
        public void Transform(State state, float3 position, quaternion rotation, bool move, bool rotate)
        {
            if (move || rotate) MoveTo(state, position);
            if (rotate) RotateTo(state, rotation);
        }

        public void MoveTo(State state, float3 newPosition)
        {
            //QLog.Debug($"QObj.MoveTo {m_Entity.D()} {newPosition.DX()}  CPs:{m_ChildCPs.Length}, nodes:{m_ChildNodes.Length}, other:{m_Children.Length}");
            MoveBy(state, newPosition, newPosition - m_Parent.Position);
        }

        public void MoveBy(State state, float3 newPosition, float3 delta)
        {
            //QLog.Debug($"QObj.MoveBy {m_Entity.D()} {delta.DX()}  CPs:{m_ChildCPs.Length}, nodes:{m_ChildNodes.Length}, other:{m_Children.Length}");
            m_Parent.MoveBy(state, newPosition, delta);

            for (int i = 0; i < m_Children.Length; i++)
            {
                m_Children[i].MoveBy(state, newPosition, delta);
            }
        }

        public void RotateTo(State state, quaternion newRotation)
        {
            float delta = newRotation.Y() - m_Parent.Angle;
            float3 origin = m_Parent.Position;
            GetMatrix(delta, origin, out Matrix4x4 matrix);

            m_Parent.RotateTo(state, newRotation, ref matrix, origin);

            for (int i = 0; i < m_Children.Length; i++)
            {
                m_Children[i].RotateBy(state, delta, ref matrix, origin);
            }
        }

        public readonly void UpdateAll()
        {
            Manager.AddComponent<Game.Common.Updated>(m_Entity);
            Manager.AddComponent<Game.Common.BatchesUpdated>(m_Entity);

            foreach (var child in m_Children)
            {
                Manager.AddComponent<Game.Common.Updated>(child.m_Entity);
                Manager.AddComponent<Game.Common.BatchesUpdated>(child.m_Entity);
            }
        }

        private readonly void GetMatrix(float delta, float3 origin, out Matrix4x4 matrix)
        {
            matrix = default;
            matrix.SetTRS(origin, Quaternion.Euler(0f, delta, 0f), Vector3.one);
        }
        #endregion

        #region Load children
        private static List<Entity> GetSubEntities(Entity e, Identity identity)
        {
            List<Entity> entities = IterateSubEntities(e, e, 0, identity, identity);

            return entities;
        }

        private static List<Entity> IterateSubEntities(Entity top, Entity e, int depth, Identity identity, Identity parentIdentity)
        {
            if (depth > 3) throw new Exception($"Moveable.IterateSubEntities depth ({depth}) too deep for {top.D()}/{e.D()}");
            depth++;

            List<QReferenceBufferType> referenceBufferTypes = identity switch
            {
                Identity.ControlPoint   => new(),
                Identity.Node           => GetReferenceTypesNode(),
                Identity.Segment        => new(),
                _ => GetReferenceTypesOther()
            };

            List<Entity> entities = new();

            foreach (QReferenceBufferType type in referenceBufferTypes)
            {
                if (QByType.HasBuffer(type.tParentComponent, e))
                {
                    QByType.GetRefBufferComponents(type.tParentComponent, e, out List<IBufferElementData> buffer, true);
                    foreach (IBufferElementData element in buffer)
                    {
                        Entity sub = (Entity)type.m_FieldInfo.GetValue(element);
                        if (!entities.Contains(sub) && IsValidChild(parentIdentity, sub))
                        {
                            entities.Add(sub);
                            entities.AddRange(IterateSubEntities(top, sub, depth, Identity.None, parentIdentity));
                        }
                    }
                }
            }

            return entities;
        }

        private static bool IsValidChild(Identity parentIdentity, Entity e)
        {
            switch (parentIdentity)
            {
                case Identity.Node:
                    EntityManager EM = World.DefaultGameObjectInjectionWorld.EntityManager;
                    if (EM.HasComponent<Game.Objects.Attached>(e)) return true;
                    return false;

                default:
                    return true;
            }
        }

        private static List<QReferenceBufferType> GetReferenceTypesNode()
        {
            return new()
            {
                new() { tParentComponent = typeof(Game.Objects.SubObject),          m_FieldInfo = GetEntityReferenceField(typeof(Game.Objects.SubObject)) },
            };
        }

        private static List<QReferenceBufferType> GetReferenceTypesOther()
        {
            return new()
            {
                new() { tParentComponent = typeof(Game.Areas.SubArea),              m_FieldInfo = GetEntityReferenceField(typeof(Game.Areas.SubArea)) },
                new() { tParentComponent = typeof(Game.Net.SubNet),                 m_FieldInfo = GetEntityReferenceField(typeof(Game.Net.SubNet)) },
                new() { tParentComponent = typeof(Game.Net.SubLane),                m_FieldInfo = GetEntityReferenceField(typeof(Game.Net.SubLane)) },
                //new() { tParentComponent = typeof(Game.Buildings.InstalledUpgrade), m_FieldInfo = GetEntityReferenceField(typeof(Game.Buildings.InstalledUpgrade)) },
                //new() { tParentComponent = typeof(Game.Objects.SubObject),  m_FieldInfo = GetEntityReferenceField(typeof(Game.Objects.SubObject)) },
            };
        }
        #endregion

        #region Low level entity access
        /// <summary>
        /// Get the field in an IBufferElementData reference buffer component that holds the actual entity reference
        /// For example for Game.Areas.SubArea.m_area, it returns m_area
        /// </summary>
        /// <param name="type">The IBufferElementData struct type to search</param>
        /// <param name="index">How many entity fields to skip over</param>
        /// <returns>FieldInfo of this field</returns>
        /// <exception cref="Exception">If no such field is found</exception>
        public static FieldInfo GetEntityReferenceField(Type type, int index = 0)
        {
            int c = 0;
            FieldInfo field = null;
            foreach (FieldInfo f in type.GetFields())
            {
                if (f.FieldType == typeof(Entity))
                {
                    if (c == index)
                    {
                        field = f;
                        break;
                    }
                    else
                    {
                        c++;
                    }
                }
            }
            if (field == null) throw new Exception($"Entity field not found for type {type}");
            return field;
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            return m_Parent.TryGetComponent<T>(out component);
        }

        public readonly bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return m_Parent.TryGetBuffer<T>(out buffer, isReadOnly);
        }
        #endregion

        public readonly override string ToString()
        {
            return $"{m_Identity}/{m_Entity.DX()}  Children: {(m_Children.IsCreated ? m_Children.Length : "Not Created!")}";
        }


        internal readonly string DebugFullObject(bool forceAll = false)
        {
            StringBuilder sb = new();
            sb.AppendFormat("{0}", this);

            if (m_Children.IsCreated)
            {
                int max = forceAll ? m_Children.Length : math.min(m_Children.Length, 20);
                for (int i = 0; i < max; i++)
                {
                    sb.AppendFormat("\n    {0}: {1}", i, m_Children[i].m_Entity.DX(true));
                }
                if (!forceAll && max < m_Children.Length)
                {
                    sb.AppendFormat("\n    {0} more truncated", m_Children.Length - max);
                }
            }
            return sb.ToString();
        }

        internal readonly void DebugDumpFullObject(bool forceAll = false, string prefix = "")
        {
            QLog.Debug(prefix + DebugFullObject(forceAll));
        }
    }
}
