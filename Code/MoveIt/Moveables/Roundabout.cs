using MoveIt.Overlays;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Moveables
{
    public class Roundabout : Moveable
    {
        public Roundabout(Entity e) : base(e, QTypes.Identity.Roundabout, QTypes.ObjectType.Normal)
        { }

        internal override Utils.IOverlay GetOverlay(OverlayFlags flags = OverlayFlags.None)
        {
            Game.Prefabs.PrefabRef prefab = _Tool.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(m_Entity);
            Game.Prefabs.ObjectGeometryData geoData = _Tool.EntityManager.GetComponentData<Game.Prefabs.ObjectGeometryData>(prefab);
            float diameter = math.max(math.cmax(new float2(geoData.m_Size.x, geoData.m_Size.z)), 3f);

            return new Utils.Circle(OverlayTransform, diameter, flags);
        }
    }
}
