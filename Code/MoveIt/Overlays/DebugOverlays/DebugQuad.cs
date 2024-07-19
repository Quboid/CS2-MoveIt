using Colossal.Mathematics;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public class DebugQuad
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Quad),
                typeof(MIO_SingleFrame),
                typeof(MIO_Debug),
            });

        private static EntityArchetype _ArchetypeTTL = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Quad),
                typeof(MIO_TTL),
                typeof(MIO_Debug),
            });

        public static Entity Factory(Quad2 quad, int ttl = 0, UnityEngine.Color color = default, int index = 7, int version = 2)
        {
            if (_Tool.m_OverlaySystem.DebugFreeze) return Entity.Null;

            float2 center2d = (quad.a + quad.b + quad.c + quad.d) / 4;
            float3 center = new(center2d.x, 0f, center2d.y);
            float terrain = _Tool.GetTerrainHeight(center);

            Quad3 quad3 = new(
                new float3(quad.a.x, terrain, quad.a.y),
                new float3(quad.b.x, terrain, quad.b.y),
                new float3(quad.c.x, terrain, quad.c.y),
                new float3(quad.d.x, terrain, quad.d.y)
                );

            return Factory(quad3, ttl, color, index, version);
        }

        public static Entity Factory(Quad3 quad, int ttl = 0, UnityEngine.Color color = default, int index = 7, int version = 3)
        {
            if (_Tool.m_OverlaySystem.DebugFreeze) return Entity.Null;

            Entity owner = new() { Index = index, Version = version };
            Entity e = _Tool.EntityManager.CreateEntity(ttl == 0 ? _Archetype : _ArchetypeTTL);

            float3 center = quad.Center();
            MIO_Common common = new()
            {
                m_Flags = InteractionFlags.Static,
                m_Owner = owner,
                m_OutlineColor = color.Equals(default) ? Colors.Get(ColorData.Contexts.Hovering) : color,
                m_TerrainHeight = center.y,
                m_Transform = new(center, default),
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.Quad));
            _Tool.EntityManager.SetComponentData(e, common);
            _Tool.EntityManager.SetComponentData<MIO_Quad>(e, new(quad));
            if (ttl > 0)
            {
                _Tool.EntityManager.SetComponentData<MIO_TTL>(e, new(ttl));
            }

            return e;
        }
    }
}
