using Colossal.Mathematics;
using MoveIt.Tool;
using Unity.Entities;

namespace MoveIt.Overlays.Children
{
    public class OverlayMarquee : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                typeof(MIO_Type),
                typeof(MIO_Common),
                typeof(MIO_Updateable),
                typeof(MIO_Quad),
            });


        public OverlayMarquee(int index, int version = 1) : base(OverlayTypes.None, null)
        {
            Entity owner = new() { Index = index, Version = version };
            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Flags         = InteractionFlags.Static,
                m_OutlineColor  = Colors.Get(ColorData.Contexts.Hovering),
                m_Owner         = owner,
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.Marquee));
            _MIT.EntityManager.SetComponentData(m_Entity, common);

            m_Owner = owner;
            m_Type = OverlayTypes.Marquee;
            _Moveable = null;
        }

        protected override bool CreateOverlayEntity()
        {
            MIT.Log.Error($"Trying to automatically make OverlayMarquee.", "MIT02");
            return false;
        }

        public void Update(Quad3 quad)
        {
            _MIT.EntityManager.SetComponentData<MIO_Quad>(m_Entity, new(quad));
        }
    }
}
