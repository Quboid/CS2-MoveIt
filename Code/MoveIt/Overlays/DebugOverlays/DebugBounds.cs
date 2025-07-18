using Colossal.Mathematics;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.DebugOverlays
{
    public class DebugBounds
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_SingleFrame),
                typeof(MIO_Debug),
                typeof(MIO_Bounds),
            });

        private static readonly EntityArchetype _ArchetypeTTL = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_TTL),
                typeof(MIO_Debug),
                typeof(MIO_Bounds),
            });

        public static Entity Factory(Bounds2 bounds, int ttl = 0, UnityEngine.Color color = default, int index = 4, int version = 1)
        {
            if (_MIT.m_OverlaySystem.DebugFreeze) return Entity.Null;

            float terrainHeight = _MIT.GetTerrainHeight(new(bounds.min.x, 0f, bounds.min.y));
            Bounds3 b = new(new float3(bounds.min.x, terrainHeight, bounds.min.y), new float3(bounds.max.x, terrainHeight, bounds.max.y));
            return Factory(b, ttl, color, index, version);
        }

        public static Entity Factory(Bounds3 bounds, int ttl = 0, UnityEngine.Color color = default, int index = 4, int version = 1)
        {
            if (_MIT.m_OverlaySystem.DebugFreeze) return Entity.Null;

            Entity owner = new() { Index = index, Version = version };
            Entity e = _MIT.EntityManager.CreateEntity(ttl == 0 ? _Archetype : _ArchetypeTTL);

            MIO_Common common = new(true)
            {
                m_Flags = InteractionFlags.Static,
                m_Owner = owner,
                m_OutlineColor = color.Equals(default) ? Colors.Get(ColorData.Contexts.Hovering) : color,
                m_TerrainHeight = bounds.Center().y,
                m_Transform = new(bounds.Center(), default),
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.Bounds));
            _MIT.EntityManager.SetComponentData(e, common);
            _MIT.EntityManager.SetComponentData<MIO_Bounds>(e, new(bounds));
            if (ttl > 0)
            {
                _MIT.EntityManager.SetComponentData<MIO_TTL>(e, new(ttl));
            }

            return e;
        }
    }
}
