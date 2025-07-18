using Colossal.Mathematics;
using MoveIt.Tool;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.DebugOverlays
{
    public class DebugLine
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_SingleFrame),
                typeof(MIO_Debug),
                typeof(MIO_Line),
            });

        private static readonly EntityArchetype _ArchetypeTTL = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_TTL),
                typeof(MIO_Debug),
                typeof(MIO_Line),
            });

        public static Entity Factory(Line3.Segment line, int ttl = 0, UnityEngine.Color color = default, int index = 6, int version = 1)
        {
            if (_MIT.m_OverlaySystem.DebugFreeze) return Entity.Null;

            Entity owner = new() { Index = index, Version = version };
            Entity e = _MIT.EntityManager.CreateEntity(ttl == 0 ? _Archetype : _ArchetypeTTL);

            float3 position = line.a + (line.ab / 2);

            MIO_Common common = new(true)
            {
                m_Flags = InteractionFlags.Static,
                m_OutlineColor = color.Equals(default) ? Colors.Get(ColorData.Contexts.Hovering) : color,
                m_Owner = owner,
                m_TerrainHeight = position.y,
                m_Transform = new(position, default),
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.Line));
            _MIT.EntityManager.SetComponentData(e, common);
            _MIT.EntityManager.SetComponentData<MIO_Line>(e, new(line));
            if (ttl > 0)
            {
                _MIT.EntityManager.SetComponentData<MIO_TTL>(e, new(ttl));
            }

            return e;
        }
    }
}
