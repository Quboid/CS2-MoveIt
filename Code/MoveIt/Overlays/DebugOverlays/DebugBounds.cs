using Colossal.Mathematics;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public class DebugBounds
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_SingleFrame),
                typeof(MIO_Debug),
                typeof(MIO_Bounds),
            });

        private static EntityArchetype _ArchetypeTTL = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_TTL),
                typeof(MIO_Debug),
                typeof(MIO_Bounds),
            });

        public static Entity Factory(Bounds2 bounds, int ttl = 0, UnityEngine.Color color = default, int index = 4, int version = 1)
        {
            float terrainHeight = _Tool.GetTerrainHeight(new(bounds.min.x, 0f, bounds.min.y));
            Bounds3 b = new(new float3(bounds.min.x, terrainHeight, bounds.min.y), new float3(bounds.max.x, terrainHeight, bounds.max.y));
            return Factory(b, ttl, color, index, version);
        }

        public static Entity Factory(Bounds3 bounds, int ttl = 0, UnityEngine.Color color = default, int index = 4, int version = 1)
        {
            Entity owner = new() { Index = index, Version = version };
            Entity e = _Tool.EntityManager.CreateEntity(ttl == 0 ? _Archetype : _ArchetypeTTL);

            MIO_Common common = new()
            {
                m_Flags = InteractionFlags.Static,
                m_Owner = owner,
                m_OutlineColor = color.Equals(default) ? Colors.Get(ColorData.Contexts.Hovering) : color,
                m_TerrainHeight = bounds.Center().y,
                m_Transform = new(bounds.Center(), default),
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.Bounds));
            _Tool.EntityManager.SetComponentData(e, common);
            _Tool.EntityManager.SetComponentData<MIO_Bounds>(e, new(bounds));
            if (ttl > 0)
            {
                _Tool.EntityManager.SetComponentData<MIO_TTL>(e, new(ttl));
            }

            return e;
        }
    }
}
