using Colossal.Mathematics;
using MoveIt.Tool;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public class DebugLine
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_SingleFrame),
                typeof(MIO_Line),
            });

        public static Entity Factory(Line3.Segment line, UnityEngine.Color color = default, int index = 4, int version = 1)
        {
            Entity owner = new() { Index = index, Version = version };
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            float3 position = line.a + (line.ab / 2);

            MIO_Common common = new()
            {
                m_Flags = InteractionFlags.Static,
                m_OutlineColor = Colors.Get(ColorData.Contexts.Deselect),
                m_Owner = owner,
                m_TerrainHeight = position.y,
                m_Transform = new(position, default),
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.Line));
            _Tool.EntityManager.SetComponentData(e, common);
            _Tool.EntityManager.SetComponentData<MIO_Line>(e, new(line));

            return e;
        }
    }
}
