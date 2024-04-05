using MoveIt.Overlays;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Moveables
{
    public class Plant : Moveable
    {
        public Plant(Entity e) : base (e, QTypes.Identity.Plant, QTypes.ObjectType.Normal)
        { }

        internal override Utils.IOverlay GetOverlay(OverlayFlags flags = OverlayFlags.None)
        {
            Game.Prefabs.PrefabRef prefab = _Tool.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(m_Entity);
            Game.Prefabs.ObjectGeometryData geoData = _Tool.EntityManager.GetComponentData<Game.Prefabs.ObjectGeometryData>(prefab);
            float diameter = math.max(math.cmax(new float2(geoData.m_Size.x, geoData.m_Size.z)), 3f);

            return new Utils.Circle(OverlayTransform, diameter, flags);
        }

        ~Plant()
        { }
    }
}
