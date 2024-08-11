using Colossal.Mathematics;
using MoveIt.Tool;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public class DebugCircle
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

        private static EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_SingleFrame),
                typeof(MIO_Debug),
                typeof(MIO_Circle),
            });

        private static EntityArchetype _ArchetypeTTL = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_TTL),
                typeof(MIO_Debug),
                typeof(MIO_Circle),
            });

        public static Entity Factory(float3 position, float radius, int ttl = 0, UnityEngine.Color color = default, int index = 5, int version = 1)
        {
            if (_MIT.m_OverlaySystem.DebugFreeze) return Entity.Null;

            Entity owner = new() { Index = index, Version = version };
            Entity e = _MIT.EntityManager.CreateEntity(ttl == 0 ? _Archetype : _ArchetypeTTL);

            MIO_Common common = new()
            {
                m_Flags = InteractionFlags.Static,
                m_OutlineColor = color.Equals(default) ? Colors.Get(ColorData.Contexts.Hovering) : color,
                m_Owner = owner,
                m_TerrainHeight = position.y,
                m_Transform = new(position, default),
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.Circle));
            _MIT.EntityManager.SetComponentData(e, common);
            _MIT.EntityManager.SetComponentData<MIO_Circle>(e, new(new Circle3(radius, position, quaternion.identity)));
            if (ttl > 0)
            {
                _MIT.EntityManager.SetComponentData<MIO_TTL>(e, new(ttl));
            }

            return e;
        }
    }
}
