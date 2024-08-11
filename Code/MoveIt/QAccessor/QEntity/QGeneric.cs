using Colossal.Entities;
using MoveIt.Moveables;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.QAccessor
{
    internal partial struct QEntity
    {
        private readonly float3 Generic_Position
        {
            get
            {
                //try
                {
                    //StringBuilder sb = new($"Pos.GET " + m_Entity.D() + ": ");
                    float3 result;

                    if (m_Lookup.goTransform.HasComponent(m_Entity))
                    {
                        //sb.Append($"goTransform");
                        result = m_Lookup.goTransform.GetRefRO(m_Entity).ValueRO.m_Position;
                    }
                    //else if (m_Lookup.gnNode.HasComponent(m_Entity))
                    //{
                    //    sb.Append($"gnNode");
                    //    result = m_Lookup.gnNode.GetRefRO(m_Entity).ValueRO.m_Position;
                    //}

                    else
                    {
                        //sb.Append($"notFound");
                        result = float3.zero;
                    }

                    //sb.AppendFormat(" ({0})", result.DX());

                    //QLog.Bundle("GET", sb.ToString());

                    return result;
                }
                //catch (Exception ex)
                //{
                //    bool exists = Manager.Exists(m_Entity);
                //    string has = " (detail failed)";
                //    try
                //    {
                //        if (exists)
                //        {
                //            has = " (";
                //            if (m_Lookup.goTransform.HasComponent(m_Entity)) has += "goTransform";
                //            else if (m_Lookup.gnNode.HasComponent(m_Entity)) has += "gnNode";
                //            else has += "notFound";
                //            has += ")";
                //        }
                //    }
                //    catch { }
                //    MIT.Log.Error($"Position.Get failed for {m_Entity.D()} (exists:{exists}){has}\n{ex}");

                //    return default;
                //}
            }
        }

        private readonly float Generic_Angle => Rotation.Y();

        private readonly quaternion Generic_Rotation => m_Lookup.goTransform.HasComponent(m_Entity) ? m_Lookup.goTransform.GetRefRO(m_Entity).ValueRO.m_Rotation : quaternion.identity;

        private readonly bool Generic_TryGetElevation(out float elevation)
        {
            elevation = 0f;
            if (!m_Lookup.goElevation.HasComponent(m_Entity)) return false;

            elevation = m_Lookup.goElevation.GetRefRO(m_Entity).ValueRO.m_Elevation;
            return true;
        }

        private readonly bool Generic_TrySetElevation(float elevation)
        {
            if (!m_Lookup.goElevation.HasComponent(m_Entity)) return false;

            m_Lookup.goElevation.GetRefRW(m_Entity).ValueRW.m_Elevation = elevation;
            return true;
        }


        private readonly bool Generic_SetUpdated()
        {
            TryAddUpdate(m_Entity);
            return true;
        }

        private readonly void Generic_TransformEnd(NativeArray<Entity> allEntities)
        {
            // Some objects (e.g. elevated stations) are attached to the nearby road; break that connection
            if (m_Lookup.goAttached.HasComponent(m_Entity))
            {
                Entity parent = m_Lookup.goAttached[m_Entity].m_Parent;
                if (!parent.Equals(Entity.Null) && !allEntities.Contains(parent))
                {
                    m_Lookup.goAttached.GetRefRW(m_Entity).ValueRW.m_Parent = Entity.Null;

                    if (m_Manager.TryGetBuffer(parent, true, out DynamicBuffer<Game.Objects.SubObject> buffer))
                    {
                        using NativeList<Game.Objects.SubObject> newSubs = new(0, Allocator.Temp);
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (!buffer[i].m_SubObject.Equals(m_Entity))
                            {
                                newSubs.Add(buffer[i]);
                            }
                        }
                        buffer.Clear();
                        for (int i = 0; i < newSubs.Length; i++)
                        {
                            buffer.Add(newSubs[i]);
                        }
                    }
                }
            }
        }


        private readonly bool Generic_MoveBy(State state, float3 newPosition, float3 delta)
        {
            return Generic_MoveTo(state, newPosition, delta);
        }

        private readonly bool Generic_MoveTo(State state, float3 newPosition, float3 delta)
        {
            if (m_Lookup.goTransform.HasComponent(m_Entity))
            {
                m_Lookup.goTransform.GetRefRW(m_Entity).ValueRW.m_Position = newPosition;
            }

            if (m_Lookup.grCullingInfo.HasComponent(m_Entity))
            {
                m_Lookup.grCullingInfo.GetRefRW(m_Entity).ValueRW.m_Bounds = MoveBounds3(m_Lookup.grCullingInfo.GetRefRO(m_Entity).ValueRO.m_Bounds, delta);
            }

            SetElevation(state, delta);

            Generic_SetUpdated();

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

            Generic_SetUpdated();

            //QLog.XDebug(sb.ToString());
            return true;
        }
    }
}
