using Colossal.Mathematics;
using MoveIt.Tool;
using Unity.Entities;

namespace MoveIt.Overlays
{
    public class OverlayMarquee : Overlay
    {
        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Quad),
            });

        public static Entity Factory(Entity owner)
        {
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Flags = InteractionFlags.Static,
                m_OutlineColor = Colors.Get(ColorData.Contexts.Hovering),
                m_Owner = owner,
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.Marquee));
            _Tool.EntityManager.SetComponentData(e, common);

            return e;
        }

        public static OverlayMarquee HandlerFactory(int index, int version = 1)
        {
            Entity owner = new() { Index = index, Version = version };
            Entity e = OverlayMarquee.Factory(owner);

            OverlayMarquee overlay = new()
            {
                m_Owner = owner,
                m_Entity = e
            };

            return overlay;
        }


        public OverlayMarquee() : base(OverlayTypes.Marquee) { }

        public override bool CreateOverlayEntity()
        {
            MIT.Log.Error($"Trying to automatically make OverlayMarquee.", "MIT02");
            return false;
        }

        public void Update(Quad3 quad)
        {
            _Tool.EntityManager.SetComponentData<MIO_Quad>(m_Entity, new(quad));
        }
    }
}
