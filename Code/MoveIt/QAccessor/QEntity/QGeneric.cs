using MoveIt.Tool;
using QCommonLib;
using System.Text;
using System;
using Unity.Mathematics;
using MoveIt.Moveables;
using UnityEngine;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        private readonly float3 Generic_Position
        {
            get
            {
                try
                {
                    StringBuilder sb = new($"Pos.GET " + m_Entity.DX() + ": ");
                    float3 result;

                    if (m_Lookup.goTransform.HasComponent(m_Entity))
                    {
                        sb.Append($"goTransform");
                        result = m_Lookup.goTransform.GetRefRO(m_Entity).ValueRO.m_Position;
                    }
                    //else if (m_Lookup.gnNode.HasComponent(m_Entity))
                    //{
                    //    sb.Append($"gnNode");
                    //    result = m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position;
                    //}

                    else
                    {
                        sb.Append($"notFound");
                        result = float3.zero;
                    }

                    sb.AppendFormat(" ({0})", result.DX());

                    //QLog.Bundle("GET", sb.ToString());

                    return result;
                }
                catch (Exception ex)
                {
                    bool exists = Manager.Exists(m_Entity);
                    string has = " (detail failed)";
                    try
                    {
                        if (exists)
                        {
                            has = " (";
                            if (m_Lookup.goTransform.HasComponent(m_Entity)) has += "goTransform";
                            else if (m_Lookup.gnNode.HasComponent(m_Entity)) has += "gnNode";
                            else has += "notFound";
                            has += ")";
                        }
                    }
                    catch { }
                    MIT.Log.Error($"Position.Get failed for {m_Entity.D()} (exists:{exists}){has}\n{ex}");

                    return default;
                }
            }
        }

        private readonly float Generic_Angle => Rotation.Y();

        private readonly quaternion Generic_Rotation => m_Lookup.goTransform.HasComponent(m_Entity) ? m_Lookup.goTransform.GetRefRO(m_Entity).ValueRO.m_Rotation : quaternion.identity;


        private readonly bool Generic_SetUpdated()
        {
            TryAddUpdate(m_Entity);
            return true;
        }


        private readonly bool Generic_MoveBy(State state, float3 newPosition, float3 delta)
        {
            return Generic_MoveTo(state, newPosition, delta);
        }

        private readonly bool Generic_MoveTo(State state, float3 newPosition, float3 delta)
        {
            //if (!_Manager.Exists(m_Entity)) return false;

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

            Generic_SetUpdated();

            //QLog.Debug(sb.ToString());

            return true;
        }

        private readonly bool Generic_RotateBy(State state, float delta, ref Matrix4x4 matrix, float3 origin)
        {
            float3 oldAngles = Generic_Rotation.ToEulerDegrees();
            return Generic_RotateTo(state, Quaternion.Euler(oldAngles.x, oldAngles.y + delta, oldAngles.z), ref matrix, origin);
        }

        private readonly bool Generic_RotateTo(State state, quaternion newRotation, ref Matrix4x4 matrix, float3 origin)
        {
            //StringBuilder sb = new();
            //sb.AppendFormat("Rotation.Set for {0}: ", m_Entity.DX());

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

            Generic_SetUpdated();

            //QLog.Debug(sb.ToString());
            return true;
        }
    }
}
