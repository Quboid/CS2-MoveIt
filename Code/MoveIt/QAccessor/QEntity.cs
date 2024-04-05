using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    /// <summary>
    /// Actual accessor for entities, excluding nodes
    /// </summary>
    public struct QEntity : IQEntity
    {
        internal readonly EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Entity m_Entity { get; }
        public bool m_IsTopLevel { get; }
        internal float3 m_OriginPosition;
        internal QLookup m_Lookup;
        internal QTypes.Identity m_Type;

        internal QEntity(Entity e, SystemBase system, QTypes.Identity type, bool isTopLevel = true)
        {
            m_Lookup = QLookup.Get(system);

            m_Entity = e;
            m_IsTopLevel = isTopLevel;
            m_OriginPosition = float.MaxValue;
            m_Type = type;
        }

        public readonly float3 Position
        {
            get
            {
                StringBuilder sb = new($"Pos.GET " + m_Entity.DX() + ": ");
                float3 result;

                if (m_Lookup.goTransform.HasComponent(m_Entity))
                {
                    sb.Append($"goTransform");
                    result = m_Lookup.goTransform.GetRefRO(m_Entity).ValueRO.m_Position;
                }
                else if (m_Lookup.gnNode.HasComponent(m_Entity))
                {
                    sb.Append($"gnNode");
                    result = m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position;
                }

                else
                {
                    sb.Append($"notFound");
                    result = float3.zero;
                }

                sb.AppendFormat(" ({0})", result.DX());

                //QLog.Bundle("GET", sb.ToString());

                return result;
            }
        }

        public readonly float Angle
        {
            get
            {
                return ((Quaternion)Rotation).eulerAngles.y;
            }
        }

        public readonly quaternion Rotation
        {
            get
            {
                //StringBuilder sb = new($"Rotation.Get for {m_Entity.D()} '{QCommon.GetPrefabName(EntityManager, m_Entity)}': ");
                quaternion result;

                if (m_Lookup.goTransform.HasComponent(m_Entity))
                {
                    //sb.Append($"goTransform");
                    result = m_Lookup.goTransform.GetRefRO(m_Entity).ValueRO.m_Rotation;
                }
                else
                {
                    //sb.Append($"notFound");
                    result = quaternion.identity;
                }

                //QLog.Debug(sb.ToString());
                return result;
            }
        }


        public readonly bool MoveBy(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            return Move(data, newPosition, delta);
        }

        public readonly bool Move(StateDataWrapper data, float3 newPosition, float3 delta)
        {
            if (!EntityManager.Exists(m_Entity)) return false;

            //StringBuilder sb = new();
            //sb.AppendFormat("Pos.Set {0} ({1}, delta:{2}, old:{3}): ", m_Entity.D(), newPosition.DX(), delta.DX(), Position.DX());

            if (m_Lookup.gaGeometry.HasComponent(m_Entity))
            {
                //sb.Append($"gaGeo, ");
                m_Lookup.gaGeometry.GetRefRW(m_Entity).ValueRW.m_CenterPosition = newPosition;
                m_Lookup.gaGeometry.GetRefRW(m_Entity).ValueRW.m_Bounds = MoveBounds3(m_Lookup.gaGeometry.GetRefRO(m_Entity).ValueRO.m_Bounds, delta);
            }

            if (m_Lookup.goTransform.HasComponent(m_Entity))
            {
                //sb.Append($"goTransform, ");
                m_Lookup.goTransform.GetRefRW(m_Entity).ValueRW.m_Position = newPosition;
            }

            if (m_Lookup.gaNode.HasBuffer(m_Entity))
            {
                //sb.Append("gaNode");
                if (m_Lookup.gaNode.TryGetBuffer(m_Entity, out var buffer))
                {
                    //sb.AppendFormat("({0})", buffer.Length);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var b = buffer[i];
                        b.m_Position += delta;
                        buffer[i] = b;
                    }
                }
                //sb.Append(", ");
            }

            EntityManager.AddComponent<Game.Common.Updated>(m_Entity);
            EntityManager.AddComponent<Game.Common.BatchesUpdated>(m_Entity);

            //QLog.Debug(sb.ToString());

            return true;
        }

        internal static Bounds3 MoveBounds3(Bounds3 input, float3 delta)
        {
            input.min += delta;
            input.max += delta;
            return input;
        }


        public readonly bool RotateBy(StateDataWrapper data, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return RotateTo(data, ((Quaternion)Rotation).eulerAngles.y + delta, ref matrix, origin);
        }

        public readonly bool RotateTo(StateDataWrapper data, float angle, ref Matrix4x4 matrix, float3 origin)
        {
            //StringBuilder sb = new();
            //sb.AppendFormat("Rotation.Set for {0}: ", m_Entity.DX());

            quaternion newRotation = Quaternion.Euler(0f, angle, 0f);
            if (m_Lookup.goTransform.HasComponent(m_Entity))
            {
                //sb.Append($"goTransform, ");
                m_Lookup.goTransform.GetRefRW(m_Entity).ValueRW.m_Rotation = newRotation;
                m_Lookup.goTransform.GetRefRW(m_Entity).ValueRW.m_Position = matrix.MultiplyPoint(m_Lookup.goTransform.GetRefRO(m_Entity).ValueRO.m_Position - origin);
            }

            if (m_Lookup.gaNode.HasBuffer(m_Entity))
            {
                //sb.Append("gaNode, ");
                if (m_Lookup.gaNode.TryGetBuffer(m_Entity, out var buffer))
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        Game.Areas.Node node = buffer[i];
                        node.m_Position = (float3)matrix.MultiplyPoint(node.m_Position - origin);
                        buffer[i] = node;
                    }
                }
            }

            //QLog.Debug(sb.ToString());
            return true;
        }



        public readonly T GetComponent<T>() where T : unmanaged, IComponentData
        {
            return EntityManager.GetComponentData<T>(m_Entity);
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponentData
        {
            if (!EntityManager.HasComponent<T>(m_Entity))
            {
                component = default;
                return false;
            }

            component = EntityManager.GetComponentData<T>(m_Entity);
            return true;
        }

        public readonly DynamicBuffer<T> GetBuffer<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return EntityManager.GetBuffer<T>(m_Entity, isReadOnly);
        }

        public readonly bool TryGetBuffer<T>(out DynamicBuffer<T> buffer, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            if (!EntityManager.HasBuffer<T>(m_Entity))
            {
                buffer = default;
                return false;
            }

            buffer = EntityManager.GetBuffer<T>(m_Entity, isReadOnly);
            return true;
        }
    }
}
