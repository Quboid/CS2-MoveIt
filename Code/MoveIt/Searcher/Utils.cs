using Colossal.Entities;
using Colossal.Mathematics;
using MoveIt.QAccessor;
using QCommonLib;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Searcher
{
    internal enum Filters
    {
        None = 0,
        Buildings = 1,
        Plants = 2,
        Props = 4,
        Decals = 8,
        Nodes = 16,
        Segments = 32,
        ControlPoints = 64,
        Surfaces = 128,
    }

    internal enum SearchTypes
    {
        Point,
        Marquee,
        Bounds,
        Ray,
    }

    internal record struct Result : IComparable<Result>
    {
        public Entity m_Entity;
        public Identity m_Identity;
        public float m_Distance;

        public Result(Entity e, Identity identity, float distance)
        {
            m_Entity = e;
            m_Identity = identity;
            m_Distance = distance;
        }

        public readonly int CompareTo(Result other)
        {
            int priority = Utils.IdentityPriority(m_Identity) - Utils.IdentityPriority(other.m_Identity);
            if (priority != 0) return priority;
            return (int)(m_Distance - other.m_Distance);
        }
    }

    internal static class Utils
    {
        internal const Filters FilterAllStatics = Filters.Buildings | Filters.Plants | Filters.Props | Filters.Decals;
        internal const Filters FilterAllNetworks = Filters.Nodes | Filters.Segments | Filters.ControlPoints;
        internal const Filters FilterAll = Filters.Buildings | Filters.Plants | Filters.Props | Filters.Decals | Filters.Nodes | Filters.Segments | Filters.ControlPoints | Filters.Surfaces;

        internal static int IdentityPriority(Identity id)
            => id switch
            {
                Identity.ControlPoint       => 0,
                Identity.Plant              => 1,
                Identity.Prop               => 2,
                Identity.Decal              => 3,
                Identity.Node               => 4,
                Identity.NetLane            => 5,
                Identity.Segment            => 6,
                Identity.Extension          => 7,
                Identity.ServiceUpgrade     => 8,
                Identity.Building           => 9,
                Identity.Surface            => 10,
                _                           => 11,
            };

        internal static Circle2 GetCircle(EntityManager manager, Entity e, Game.Net.Node node)
        {
            if (manager.TryGetComponent(e, out Game.Net.NodeGeometry geoData))
            {
                return Moveables.MVNode.GetCircle(geoData);
            }
            return Moveables.MVNode.GetCircle(node);
        }

        internal static Quad2 CalculateBuildingCorners(EntityManager manager, ref QObject obj, Entity prefab, float expand = 0f)
        {
            return CalculateBuildingCorners(manager, obj.m_Parent.Position.XZ(), obj.m_Parent.Rotation, prefab, expand);
        }

        internal static Quad2 CalculateBuildingCorners(EntityManager manager, ref QObjectSimple obj, Entity prefab, float expand = 0f)
        {
            return CalculateBuildingCorners(manager, obj.m_Parent.Position.XZ(), obj.m_Parent.Rotation, prefab, expand);
        }

        internal static Quad2 CalculateBuildingCorners(EntityManager manager, float2 position, quaternion rotation, Entity prefab, float expand = 0f)
        {
            int2 lotSize = manager.GetComponentData<Game.Prefabs.BuildingData>(prefab).m_LotSize;
            float offX = lotSize.x * 4 + expand;
            float offZ = lotSize.y * 4 + expand;

            Quad2 result = new(
                RotateAroundPivot(position, rotation, new(-offX, 0, -offZ)),
                RotateAroundPivot(position, rotation, new(offX, 0, -offZ)),
                RotateAroundPivot(position, rotation, new(offX, 0, offZ)),
                RotateAroundPivot(position, rotation, new(-offX, 0, offZ)));

            return result;
        }

        internal static float2 RotateAroundPivot(float2 position, quaternion q, float3 offset)
        {
            float3 newPos = math.mul(q, offset);
            return position + new float2(newPos.x, newPos.z);
        }

        //internal static (Entity e, float d)[] CalculateDistances(EntityManager manager, ref QLookup lookup, NativeList<Entity> results, float3 center)
        //{
        //    (Entity e, float d)[] data = new (Entity e, float d)[results.Length];
        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        QObjectSimple obj = new(manager, ref lookup, results[i]);
        //        float distance = obj.m_Parent.Position.DistanceXZ(center);
        //        data[i] = (results[i], distance);
        //    }
        //    results.Dispose();
        //    var result = data.OrderBy(pair => pair.d).ToArray();
        //    //DebugDumpCalculateDistance(result);
        //    return result;
        //}
    }
}
