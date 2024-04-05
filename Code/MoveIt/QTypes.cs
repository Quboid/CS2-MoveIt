using Unity.Entities;

namespace MoveIt
{
    public class QTypes
    {
        public enum Identity
        {
            None,
            Building,
            ControlPoint,
            Node,
            Other,
            Plant,
            Roundabout,
            Segment,
            SubEntity,
            Invalid
        }

        public enum ObjectType
        {
            None,
            Normal,
            Managed
        }

        public enum Manipulate
        {
            Normal = 1,
            Parent = 2,
            Child = 4,
            All = 7,
        }

        public static Identity GetEntityIdentity(Entity e)
        {
            EntityManager EM = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (e.Equals(Entity.Null) ||
                EM.HasComponent<Game.Common.Deleted>(e) ||
                EM.HasComponent<Game.Common.Terrain>(e))
            {
                return Identity.Invalid;
            }

            if (EM.HasComponent<Game.Objects.Plant>(e))
            {
                return Identity.Plant;
            }
            else if (EM.HasComponent<Game.Buildings.Building>(e))
            {
                return Identity.Building;
            }
            else if (EM.HasComponent<Game.Buildings.Extension>(e))
            {
                return Identity.Building;
            }
            else if (EM.HasComponent<Game.Net.Edge>(e))
            {
                return Identity.Segment;
            }
            else if (EM.HasComponent<Game.Net.Node>(e))
            {
                return Identity.Node;
            }
            else if (EM.HasComponent<Components.MIT_ControlPoint>(e))
            {
                return Identity.ControlPoint;
            }
            else if (EM.HasComponent<Game.Objects.Static>(e) && EM.HasComponent<Game.Objects.NetObject>(e))
            {
                return Identity.Roundabout;
            }
            else
            {
                return Identity.Other;
            }
        }
    }
}
