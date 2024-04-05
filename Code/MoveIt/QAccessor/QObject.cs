using System.Collections.Generic;
using System;
using Unity.Collections;
using Unity.Entities;
using System.Reflection;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using QCommonLib;
using MoveIt.Tool;
using MoveIt.Moveables;
using Unity.Jobs;

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

    public interface IQEntity
    {
#pragma warning disable IDE1006 // Naming Styles
        public Entity m_Entity { get; }
        public bool m_IsTopLevel { get; }
#pragma warning restore IDE1006 // Naming Styles

        public float3 Position { get; }
        public float Angle { get; }
        public quaternion Rotation { get; }

        public bool MoveBy(StateDataWrapper data, float3 newPosition, float3 delta);
        public bool Move(StateDataWrapper data, float3 newPosition, float3 delta);
        public bool RotateBy(StateDataWrapper data, float delta, ref Matrix4x4 matrix, float3 origin);
        public bool RotateTo(StateDataWrapper data, float angle, ref Matrix4x4 matrix, float3 origin);

        public T GetComponent<T>() where T : unmanaged, IComponentData;
        public bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData;
        public DynamicBuffer<T> GetBuffer<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData;
        public bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData;
    }

    /// <summary>
    /// Primary accessor for entities, including children
    /// </summary>
    public struct QObject : IDisposable, INativeDisposable
    {
        internal readonly EntityManager EM => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Entity m_Entity;
        public QEntity m_Parent;
        public QNode m_ParentNode;
        public QControlPoint m_ParentCP;
        public QSegment m_ParentSegment;
        internal NativeList<QEntity> m_Children;
        internal NativeList<QNode> m_ChildNodes;
        internal NativeList<QControlPoint> m_ChildCPs;
        internal NativeList<QSegmentChild> m_ChildSegments;
        internal QTypes.Identity m_Identity;

        internal QObject(Entity e, SystemBase system, QTypes.Identity identity = QTypes.Identity.None)
        {
            if (e == Entity.Null) throw new ArgumentNullException("Creating QObject with null entity");

            m_Entity = e;
            m_Identity = identity == QTypes.Identity.None ? QTypes.GetEntityIdentity(e) : identity;
            m_Parent = new(e, system, m_Identity);
            m_ParentNode = new(e, system, m_Identity);
            m_ParentCP = new(e, system, m_Identity);
            m_ParentSegment = new(e, system, m_Identity);
            m_Children = new(0, Allocator.Persistent);
            m_ChildNodes = new(0, Allocator.Persistent);
            m_ChildCPs = new(0, Allocator.Persistent);
            m_ChildSegments = new(0, Allocator.Persistent);

            var subEntities = GetSubEntities(e, m_Identity);

            if (subEntities.Count > 0)
            {
                for (int i = 0; i < subEntities.Count; i++)
                {
                    if (subEntities[i] == Entity.Null) throw new NullReferenceException($"Creating child for {e.D()} with null entity");

                    if (system.EntityManager.HasComponent<Game.Net.ConnectionLane>(subEntities[i])) continue;

                    QTypes.Identity subType = QTypes.GetEntityIdentity(subEntities[i]);
                    switch (subType)
                    {
                        case QTypes.Identity.Segment:
                            m_ChildSegments.Add(new(subEntities[i], system, subType));
                            break;

                        case QTypes.Identity.ControlPoint:
                            m_ChildCPs.Add(new(subEntities[i], system, subType, false));
                            break;

                        case QTypes.Identity.Node:
                            m_ChildNodes.Add(new(subEntities[i], system, subType, false));
                            break;

                        default:
                            m_Children.Add(new(subEntities[i], system, subType, false));
                            break;
                    }
                }
            }

            //DebugDumpAll();
        }

        public readonly IQEntity Parent => m_Identity switch
        {
            QTypes.Identity.Node => m_ParentNode,
            QTypes.Identity.ControlPoint => m_ParentCP,
            QTypes.Identity.Segment => m_ParentSegment,
            _ => m_Parent,
        };

        public void Dispose()
        {
            DisposeElements();

            m_ChildCPs.Dispose();
            m_Children.Dispose();
            m_ChildNodes.Dispose();
            m_ChildSegments.Dispose();
        }

        public JobHandle Dispose(JobHandle handle)
        {
            DisposeElements();

            handle = m_ChildCPs.Dispose(handle);
            handle = m_Children.Dispose(handle);
            handle = m_ChildNodes.Dispose(handle);
            handle = m_ChildSegments.Dispose(handle);
            return handle;
        }

        private void DisposeElements()
        {
            if (m_ChildNodes.IsCreated)
            {
                for (int i = 0; i < m_ChildNodes.Length; i++)
                {
                    m_ChildNodes[i].Dispose();
                }
            }
            if (m_ChildCPs.IsCreated)
            {
                for (int i = 0; i < m_ChildCPs.Length; i++)
                {
                    m_ChildCPs[i].Dispose();
                }
            }
            if (m_ChildSegments.IsCreated)
            {
                for (int i = 0; i < m_ChildSegments.Length; i++)
                {
                    m_ChildSegments[i].Dispose();
                }
            }
        }

        #region Transforming
        public void Transform(StateDataWrapper data, float3 position, float angle, bool move, bool rotate)
        {
            if (move || rotate) MoveTo(data, position);
            if (rotate) RotateTo(data, angle);
        }

        public void MoveTo(StateDataWrapper data, float3 newPosition)
        {
            //QLog.Debug($"QObj.MoveTo {m_Entity.D()} {newPosition.DX()}  CPs:{m_ChildCPs.Length}, nodes:{m_ChildNodes.Length}, other:{m_Children.Length}");
            MoveBy(data, newPosition, newPosition - Parent.Position);
        }

        public void MoveBy(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            //QLog.Debug($"QObj.MoveBy {m_Entity.D()} {delta.DX()}  CPs:{m_ChildCPs.Length}, nodes:{m_ChildNodes.Length}, other:{m_Children.Length}");
            Parent.MoveBy(data, newPosition, delta);

            for (int i = 0; i < m_ChildCPs.Length; i++)
            {
                m_ChildCPs[i].MoveBy(data, newPosition, delta);
            }

            for (int i = 0; i < m_ChildNodes.Length; i++)
            {
                m_ChildNodes[i].MoveBy(data, newPosition, delta);
            }

            for (int i = 0; i < m_ChildSegments.Length; i++)
            {
                m_ChildSegments[i].MoveBy(data, newPosition,delta);
            }

            for (int i = 0; i < m_Children.Length; i++)
            {
                m_Children[i].MoveBy(data, newPosition, delta);
            }
        }

        public void RotateTo(StateDataWrapper data, float newAngle)
        {
            float delta = newAngle - Parent.Angle;
            float3 origin = Parent.Position;
            GetMatrix(delta, origin, out Matrix4x4 matrix);

            Parent.RotateTo(data, newAngle, ref matrix, origin);

            for (int i = 0; i < m_ChildNodes.Length; i++)
            {
                m_ChildNodes[i].RotateBy(data, delta, ref matrix, origin);
            }

            for (int i = 0; i < m_ChildCPs.Length; i++)
            {
                m_ChildCPs[i].RotateBy(data, delta, ref matrix, origin);
            }

            for (int i = 0; i < m_ChildSegments.Length; i++)
            {
                m_ChildSegments[i].RotateBy(data, delta, ref matrix, origin);
            }

            for (int i = 0; i < m_Children.Length; i++)
            {
                m_Children[i].RotateBy(data, delta, ref matrix, origin);
            }
        }

        public readonly void UpdateAll()
        {
            EM.AddComponent<Game.Common.Updated>(m_Entity);
            EM.AddComponent<Game.Common.BatchesUpdated>(m_Entity);

            foreach (var child in m_ChildNodes)
            {
                EM.AddComponent<Game.Common.Updated>(child.m_Entity);
                EM.AddComponent<Game.Common.BatchesUpdated>(child.m_Entity);
            }
            foreach (var child in m_ChildCPs)
            {
                EM.AddComponent<Game.Common.Updated>(child.m_Entity);
                EM.AddComponent<Game.Common.BatchesUpdated>(child.m_Entity);
            }
            foreach (var child in m_ChildSegments)
            {
                EM.AddComponent<Game.Common.Updated>(child.m_Entity);
                EM.AddComponent<Game.Common.BatchesUpdated>(child.m_Entity);
            }
            foreach (var child in m_Children)
            {
                EM.AddComponent<Game.Common.Updated>(child.m_Entity);
                EM.AddComponent<Game.Common.BatchesUpdated>(child.m_Entity);
            }
        }

        private readonly void GetMatrix(float delta, float3 origin, out Matrix4x4 matrix)
        {
            matrix = default;
            matrix.SetTRS(origin, Quaternion.Euler(0f, delta, 0f), Vector3.one);
        }
        #endregion

        #region Load children
        private static List<Entity> GetSubEntities(Entity e, QTypes.Identity identity)
        {
            List<Entity> entities = IterateSubEntities(e, e, 0, identity);

            return entities;
        }

        private static List<Entity> IterateSubEntities(Entity top, Entity e, int depth, QTypes.Identity identity)
        {
            if (depth > 3) throw new Exception($"Moveable.IterateSubEntities depth ({depth}) too deep for {top.D()}/{e.D()}");
            depth++;

            List<QReferenceBufferType> referenceBufferTypes = identity switch
            {
                QTypes.Identity.ControlPoint    => new(),
                QTypes.Identity.Node            => new(),
                QTypes.Identity.Segment         => new(),
                _ => GetReferenceTypes()
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
                        if (!entities.Contains(sub))
                        {
                            entities.Add(sub);
                            entities.AddRange(IterateSubEntities(top, sub, depth, QTypes.Identity.None));
                        }
                        //else
                        //{
                        //    QLog.Debug($"Duplicate subEntity found: {sub.D()}");
                        //}
                    }
                }
            }

            return entities;
        }

        private static List<QReferenceBufferType> GetReferenceTypes()
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

        public readonly T GetComponent<T>() where T : unmanaged, IComponentData
        {
            return Parent.GetComponent<T>();
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            return Parent.TryGetComponent<T>(out component);
        }

        public readonly DynamicBuffer<T> GetBuffer<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return Parent.GetBuffer<T>(isReadOnly);
        }

        public readonly bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return Parent.TryGetBuffer<T>(out buffer, isReadOnly);
        }
        #endregion

        public readonly override string ToString()
        {
            return "Parent:" + m_Entity.D() + ", children: " + (m_Children.IsCreated ? m_Children.Length : "Not Created!");
        }

        internal void DebugDumpAll()
        {
            StringBuilder sb = new();
            sb.AppendFormat("Parent: {0}, children: {1}, nodes: {2}, CPs: {3}, segs:{4}", m_Entity.D(),
                m_Children.IsCreated ? m_Children.Length : "Not Created!", 
                m_ChildNodes.IsCreated ? m_ChildNodes.Length : "Not Created!", 
                m_ChildCPs.IsCreated ? m_ChildCPs.Length : "Not Created!",
                m_ChildSegments.IsCreated ? m_ChildSegments.Length : "Not Created!");

            if (m_Children.IsCreated)
            {
                for (int i = 0; i < m_Children.Length; i++)
                {
                    sb.AppendFormat("\n    {0}:{1}", i, m_Children[i].m_Entity.DX(true));
                }
            }

            if (m_ChildNodes.IsCreated)
            {
                for (int i = 0; i < m_ChildNodes.Length; i++)
                {
                    sb.AppendFormat("\n    {0}:{1}", i, m_ChildNodes[i].m_Entity.DX(true));
                }
            }

            if (m_ChildCPs.IsCreated)
            {
                for (int i = 0; i < m_ChildCPs.Length; i++)
                {
                    sb.AppendFormat("\n    {0}:{1}", i, m_ChildCPs[i].m_Entity.DX(true));
                }
            }

            if (m_ChildSegments.IsCreated)
            {
                for (int i = 0; i < m_ChildSegments.Length; i++)
                {
                    sb.AppendFormat("\n    {0}:{1}", i, m_ChildSegments[i].m_Entity.DX(true));
                }
            }

            QLog.Debug(sb.ToString());
        }
    }
}
