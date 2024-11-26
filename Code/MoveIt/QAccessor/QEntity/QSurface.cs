using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using MoveIt.Moveables;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor.QEntity
{
    internal partial struct QEntity
    {
        private readonly float3 Surface_Position => GetSurfaceCircle(_Manager, m_Entity).position;

        private float Surface_Angle => Rotation.Y();

        private readonly quaternion Surface_Rotation => GetSurfaceRotation(_Manager, m_Entity);


        private bool Surface_SetUpdated()
        {
            TryAddUpdate(m_Entity);
            return true;
        }

        private readonly void Surface_TransformEnd()
        { }


        private bool Surface_MoveBy(State state, float3 newPosition, float3 delta)
        {
            return Surface_MoveTo(state, newPosition, delta);
        }

        private bool Surface_MoveTo(State state, float3 newPosition, float3 delta)
        {
            if (_Lookup.gaGeometry.HasComponent(m_Entity))
            {
                _Lookup.gaGeometry.GetRefRW(m_Entity).ValueRW.m_CenterPosition = newPosition;
                _Lookup.gaGeometry.GetRefRW(m_Entity).ValueRW.m_Bounds = MoveBounds3(_Lookup.gaGeometry.GetRefRO(m_Entity).ValueRO.m_Bounds, delta);
            }

            //string msg = $"Surface_MoveTo {m_Entity.DX(true)} {newPosition.DX()}, delta:{delta.DX()}";
            if (_Lookup.gaNode.TryGetBuffer(m_Entity, out var gaNodes))
            {
                //msg += $" gaNodes:{gaNodes.Length}";
                for (int i = 0; i < gaNodes.Length; i++)
                {
                    //float3 old = gaNodes[i].m_Position;
                    Node b = gaNodes[i];
                    b.m_Position += delta;
                    gaNodes[i] = b;
                    //msg += $"\n   {i,2}: {old.DX()} -> {gaNodes[i].m_Position.DX()}";
                }
            }
            //msg += $"\n{m_Entity.DebugEntity()}";
            //QLog.XDebug(msg);

            Surface_SetUpdated();
            return true;
        }

        private bool Surface_RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            return Surface_RotateTo(state, quaternion.identity, ref matrix, origin);
        }

        private bool Surface_RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            if (_Lookup.gaGeometry.HasComponent(m_Entity))
            {
                float3 centre = _Lookup.gaGeometry.GetRefRO(m_Entity).ValueRO.m_CenterPosition;
                _Lookup.gaGeometry.GetRefRW(m_Entity).ValueRW.m_CenterPosition = matrix.MultiplyPoint(centre - origin);
            }

            if (_Lookup.gaNode.TryGetBuffer(m_Entity, out var buffer))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    Node node = buffer[i];
                    node.m_Position = (float3)matrix.MultiplyPoint(node.m_Position - origin);
                    buffer[i] = node;
                }
            }

            Surface_SetUpdated();
            return true;
        }


        internal static Circle3 GetSurfaceCircle(EntityManager manager, Entity e)
        {
            if (!manager.TryGetBuffer(e, true, out DynamicBuffer<Node> nodes))
            {
                throw new System.Exception($"Failed to get surface nodes for {e}");
            }
            if (nodes.Length < 1)
            {
                throw new System.Exception($"No nodes found for surface {e}");
            }
            if (nodes.Length == 1)
            {
                return new(0f, nodes[0].m_Position, quaternion.identity);
            }

            float y = nodes[0].m_Position.y;

            quaternion q = GetSurfaceRotation(nodes);

            using NativeList<float2> nodePoses = new(nodes.Length, Allocator.Temp);
            for (int i = 0; i < nodes.Length; i++)
            {
                nodePoses.Add(nodes[i].m_Position.XZ());
            }
            Circle2 c = QMinimumEnclosingCircle.Welzl(nodePoses);
            Circle3 circle = new(c.radius, new(c.position.x, y, c.position.y), q);

            return circle;
        }

        internal static quaternion GetSurfaceRotation(EntityManager manager, Entity e)
        {
            if (!manager.TryGetBuffer(e, true, out DynamicBuffer<Node> nodes)) return quaternion.identity;
            if (nodes.Length < 2) return quaternion.identity;
            return GetSurfaceRotation(nodes);
        }

        /// <summary>
        /// Calculate the Y angle by using the angle from node 0 to node 1
        /// </summary>
        private static quaternion GetSurfaceRotation(DynamicBuffer<Node> nodes)
        {
            float2 mag = nodes[1].m_Position.XZ() - nodes[0].m_Position.XZ();
            float radians = math.atan2(mag.x, mag.y);
            return Quaternion.Euler(0f, math.degrees(radians), 0f);
        }
    }
}
