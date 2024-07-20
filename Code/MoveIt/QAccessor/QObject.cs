using Colossal.Entities;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    /// <summary>
    /// Primary accessor for entities, including children
    /// </summary>
    public struct QObject : IDisposable, INativeDisposable
    {
        internal static EntityManager m_Manager;
        internal Entity m_Entity;
        internal QEntity m_Parent;
        internal NativeList<QEntity> m_Children;
        internal Identity m_Identity;

        internal QObject(EntityManager manager, ref QLookup lookup, Entity e, Identity identity = Identity.None)
        {
            if (e == Entity.Null) throw new ArgumentNullException("Creating QObject with null entity");

            m_Manager       = manager;
            m_Entity        = e;
            m_Identity      = identity == Identity.None ? QTypes.GetEntityIdentity(manager, e) : identity;
            m_Parent        = new(m_Manager, ref lookup, e, m_Identity);
            m_Children      = new(0, Allocator.Persistent);

            using NativeArray<Entity> subEntities = GetSubEntities(e, m_Identity);

            if (subEntities.Length > 0)
            {
                for (int i = 0; i < subEntities.Length; i++)
                {
                    if (subEntities[i] == Entity.Null) throw new NullReferenceException($"Creating child for {e.D()} with null entity");

                    if (m_Manager.HasComponent<Game.Net.ConnectionLane>(subEntities[i]) && !m_Manager.HasComponent<Game.Net.AreaLane>(subEntities[i])) continue;

                    Identity subType = QTypes.GetEntityIdentity(manager, subEntities[i]);
                    m_Children.Add(new(m_Manager, ref lookup, subEntities[i], subType, m_Entity));
                }
            }

            //DebugDumpFullObject(new(Allocator.Temp) { (int)Identity.Node, (int)Identity.Segment, (int)Identity.NetLane }, true, $"QObject.Ctor {m_Entity.D()}: ");
            //DebugDumpFullObject(new(0, Allocator.Temp), true, $"QObject.Ctor {m_Entity.D()} (subEntities:{subEntities.Length}) - ");
        }

        public NativeArray<Entity> GetAllEntities()
        {
            NativeArray<Entity> all = new(m_Children.Length + 1, Allocator.Temp);
            all[0] = m_Entity;
            for (int i = 0; i < m_Children.Length; i++)
            {
                all[i + 1] = m_Children[i].m_Entity;
            }
            return all;
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

        public void TransformEnd(NativeArray<Entity> all)
        {
            m_Parent.TransformEnd(all);
            for (int i = 0; i < m_Children.Length; i++)
            {
                m_Children[i].TransformEnd(all);
            }
        }

        #region Transforming
        public void Transform(State state, float3 position, quaternion rotation, bool move, bool rotate)
        {
            if (move || rotate) MoveTo(state, position);
            if (rotate) RotateTo(state, rotation);
        }

        public void MoveTo(State state, float3 newPosition)
        {
            //QLog.Debug($"QObj.MoveTo ({state.m_Entity.D()})  {m_Entity.D()} parent:{m_Parent.m_Entity.DX()} children:{m_Children.Length} {newPosition.DX()}");
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

        public readonly int UpdateAll()
        {
            //m_Manager.AddComponent<Game.Common.Updated>(m_Entity);
            //m_Manager.AddComponent<Game.Common.BatchesUpdated>(m_Entity);

            foreach (var child in m_Children)
            {
                m_Manager.AddComponent<Game.Common.Updated>(child.m_Entity);
                m_Manager.AddComponent<Game.Common.BatchesUpdated>(child.m_Entity);

                //if (m_Manager.TryGetBuffer<Game.Net.ConnectedEdge>(child.m_Entity, true, out var edgeBuffer))
                //{
                //    QLog.Debug($"Child of {m_Parent.m_Entity.DX()} :: {child.m_Entity.DX()} :: {edgeBuffer.Length}");
                //    for (int i = 0; i < edgeBuffer.Length; i++)
                //    {
                //        m_Manager.AddComponent<Game.Common.Updated>(edgeBuffer[i].m_Edge);
                //        m_Manager.AddComponent<Game.Common.BatchesUpdated>(edgeBuffer[i].m_Edge);
                //    }
                //}
            }

            m_Parent.SetUpdated();

            return m_Children.Length + 1;
        }

        private readonly void GetMatrix(float delta, float3 origin, out Matrix4x4 matrix)
        {
            matrix = default;
            matrix.SetTRS(origin, Quaternion.Euler(0f, delta, 0f), Vector3.one);
        }
        #endregion

        #region Load children
        private static NativeArray<Entity> GetSubEntities(Entity e, Identity identity)
        {
            NativeArray<Entity> entities = IterateSubEntities(e, e, 0, identity, identity);

            return entities;
        }

        private static NativeArray<Entity> IterateSubEntities(Entity top, Entity e, int depth, Identity identity, Identity parentIdentity)
        {
            if (depth > 3) throw new Exception($"Moveable.IterateSubEntities depth ({depth}) too deep for {top.D()}/{e.D()}");
            depth++;

            NativeList<Entity> entities = new(Allocator.Temp);

            // Handle Control Points, Segments, and Netlanes
            if (identity == Identity.ControlPoint || identity == Identity.Segment || identity == Identity.NetLane)
            {
                // Do nothing
            }

            // Handle Nodes
            else if (identity == Identity.Node)
            {
                if (m_Manager.TryGetBuffer(e, true, out DynamicBuffer<Game.Objects.SubObject> subObjects))
                {
                    for (int i = 0; i < subObjects.Length; i++)
                    {
                        Entity sub = subObjects[i].m_SubObject;
                        if (!entities.Contains(sub) && IsValidChild(parentIdentity, sub))
                        {
                            entities.Add(sub);
                            entities.AddRange(IterateSubEntities(top, sub, depth, Identity.None, parentIdentity));
                        }
                    }
                }
            }

            // Handle everything else
            else
            {
                if (m_Manager.TryGetBuffer(e, true, out DynamicBuffer<Game.Objects.SubObject> subObjects))
                {
                    // Add pillars so elevated metro stations can be moved
                    for (int i = 0; i < subObjects.Length; i++)
                    {
                        Entity sub = subObjects[i].m_SubObject;
                        if (!entities.Contains(sub) && IsValidChild(parentIdentity, sub) && m_Manager.HasComponent<Game.Objects.Pillar>(sub))
                        {
                            entities.Add(sub);
                            entities.AddRange(IterateSubEntities(top, sub, depth, Identity.None, parentIdentity));
                        }
                    }
                }

                if (m_Manager.TryGetBuffer(e, true, out DynamicBuffer<Game.Areas.SubArea> subAreas))
                {
                    for (int i = 0; i < subAreas.Length; i++)
                    {
                        Entity sub = subAreas[i].m_Area;
                        if (!entities.Contains(sub) && IsValidChild(parentIdentity, sub))
                        {
                            entities.Add(sub);
                            entities.AddRange(IterateSubEntities(top, sub, depth, Identity.None, parentIdentity));
                        }
                    }
                }

                if (m_Manager.TryGetBuffer(e, true, out DynamicBuffer<Game.Net.SubNet> subNets))
                {
                    for (int i = 0; i < subNets.Length; i++)
                    {
                        Entity sub = subNets[i].m_SubNet;
                        if (!entities.Contains(sub) && IsValidChild(parentIdentity, sub))
                        {
                            entities.Add(sub);
                            entities.AddRange(IterateSubEntities(top, sub, depth, Identity.None, parentIdentity));
                        }
                    }
                }

                if (m_Manager.TryGetBuffer(e, true, out DynamicBuffer<Game.Net.SubLane> subLanes))
                {
                    for (int i = 0; i < subLanes.Length; i++)
                    {
                        Entity sub = subLanes[i].m_SubLane;
                        if (!entities.Contains(sub) && IsValidChild(parentIdentity, sub))
                        {
                            entities.Add(sub);
                            entities.AddRange(IterateSubEntities(top, sub, depth, Identity.None, parentIdentity));
                        }
                    }
                }

                //        //new() { tParentComponent = typeof(Game.Buildings.InstalledUpgrade), m_FieldInfo = GetEntityReferenceField(typeof(Game.Buildings.InstalledUpgrade)) },
                //        //new() { tParentComponent = typeof(Game.Objects.SubObject),  m_FieldInfo = GetEntityReferenceField(typeof(Game.Objects.SubObject)) },
            }

            return entities.ToArray(Allocator.Temp);
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
        #endregion

        public readonly override string ToString()
        {
            return $"{m_Identity}/{m_Entity.D()}  Children: {(m_Children.IsCreated ? m_Children.Length : "Not Created!")}";
        }

#if USE_BURST
        // Do nothing if in burst mode

        internal readonly void DebugDumpFullObject(NativeList<int> ids, bool forceAll = false, string prefix = "")
        { }

        internal readonly string DebugFullObject() => ""; 
#else
        internal readonly string DebugFullObject(NativeList<int> ids, bool forceAll = false)
        {
            StringBuilder sb = new();
            sb.AppendFormat("{0}", this);

            if (m_Children.IsCreated)
            {
                int max = forceAll ? m_Children.Length : math.min(m_Children.Length, 20);
                int idMatch = 0;
                int idMismatch = 0;
                for (int i = 0; i < m_Children.Length; i++)
                {
                    if (ids.Length == 0 || ids.Contains((int)m_Children[i].m_Identity))
                    {
                        if (!m_Children[i].m_Owner.Equals(m_Children[i].m_Parent) && !m_Children[i].m_Owner.Equals(Entity.Null))
                        {
                            sb.AppendFormat("\n    {0}: {1} ({2}, {3})", i, m_Children[i].m_Entity.DX(true), m_Children[i].m_Parent.D(), m_Children[i].m_Owner.D());
                        }
                        else
                        {
                            sb.AppendFormat("\n    {0}: {1} ({2})", i, m_Children[i].m_Entity.DX(true), m_Children[i].m_Parent.D());
                        }

                        if (idMatch++ > max) break;
                    }
                    else
                    {
                        idMismatch++;
                    }
                }
                sb.Append("\n    ");
                if (forceAll)
                {
                    sb.AppendFormat("Showing {0}/{1} children ", idMatch, m_Children.Length);
                }
                else
                {
                    sb.AppendFormat("Showing {0}/{1} children ({2} more truncated) ", idMatch, m_Children.Length, m_Children.Length - idMatch);
                }
                if (ids.Length == 0)
                {
                    sb.Append("of all identities.");
                }
                else
                {
                    sb.AppendFormat("of {0} identities, {1} not matched.", ids.Length, idMismatch);
                }
            }
            return sb.ToString();
        }

        internal readonly void DebugDumpFullObject(NativeList<int> ids, bool forceAll = false, string prefix = "")
        {
            QLog.Debug(prefix + DebugFullObject(ids, forceAll));
        }
#endif
    }
}
