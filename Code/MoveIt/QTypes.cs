using Unity.Entities;

namespace MoveIt
{
    /// <summary>
    /// The type of object that a Moveable, State, or QAccessor is for
    /// </summary>
    public enum Identity
    {
        None,
        Building,
        ControlPoint,
        Node,
        Other,
        Plant,
        Segment,
        NetLane,
        Extension,
        ServiceUpgrade,
        Prop,
        Decal,
        Invalid
    }

    /// <summary>
    /// Whether a Moveable or State refers to a normal vanilla object or something managed by Move It
    /// </summary>
    public enum ObjectType
    {
        None,
        Normal,
        Managed
    }

    public class QTypes
    {
        public static bool IsManipulationPredict(Identity identity, bool isToolManipulating)
        {
            if (identity == Identity.Segment || identity == Identity.NetLane || identity == Identity.ControlPoint)
            {
                return isToolManipulating;
            }

            return false;
        }

        public static string GetIdentityCode(Identity identity) => identity switch
            {
                Identity.None           => "##",
                Identity.Building       => "BL",
                Identity.ControlPoint   => "CP",
                Identity.Node           => "ND",
                Identity.Other          => "**",
                Identity.Plant          => "PL",
                Identity.Segment        => "SG",
                Identity.NetLane        => "LN",
                Identity.Extension      => "XT",
                Identity.ServiceUpgrade => "SU",
                Identity.Prop           => "PR",
                Identity.Decal          => "DE",
                Identity.Invalid        => "??",
                _ => throw new System.NotImplementedException(),
            };

        public static Identity GetEntityIdentity(Entity e)
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (e.Equals(Entity.Null) ||
                manager.HasComponent<Game.Common.Deleted>(e) ||
                manager.HasComponent<Game.Common.Terrain>(e))
            {
                return Identity.Invalid;
            }

            if (manager.HasComponent<Game.Objects.Plant>(e))
            {
                return Identity.Plant;
            }
            else if (manager.HasComponent<Game.Buildings.Extension>(e))
            {
                return Identity.Extension;
            }
            else if (manager.HasComponent<Game.Buildings.ServiceUpgrade>(e))
            {
                return Identity.ServiceUpgrade;
            }
            else if (manager.HasComponent<Game.Buildings.Building>(e))
            {
                return Identity.Building;
            }
            else if (manager.HasComponent<Game.Net.Edge>(e))
            {
                if (manager.HasComponent<Game.Net.EdgeGeometry>(e))
                {
                    return Identity.Segment;
                }
                else
                {
                    return Identity.NetLane;
                }
            }
            else if (manager.HasComponent<Game.Net.Node>(e))
            {
                return Identity.Node;
            }
            else if (manager.HasComponent<Components.MIT_ControlPoint>(e))
            {
                return Identity.ControlPoint;
            }
            else if (manager.HasComponent<Game.Objects.ObjectGeometry>(e))
            {
                if (manager.HasComponent<Game.Objects.Surface>(e))
                {
                    return Identity.Prop;
                }
                else
                {
                    return Identity.Decal;
                }
            }
            else
            {
                return Identity.Other;
            }
        }
    }
}
