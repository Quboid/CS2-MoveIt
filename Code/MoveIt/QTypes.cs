﻿using Unity.Entities;

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
        Surface,
        Overlay,
        Invalid
    }

    public static class QTypes
    {
        public static bool IsManipulationPredict(Identity identity, bool isToolManipulating)
        {
            if (identity is Identity.Segment or Identity.NetLane or Identity.ControlPoint)
            {
                return isToolManipulating;
            }

            return false;
        }

        public static bool IsManipChildPredict(Identity identity, bool isToolManipulating)
            => identity == Identity.ControlPoint && isToolManipulating;

        public static bool IsManagedPredict(Identity identity)
            => identity == Identity.ControlPoint;

        public static string GetIdentityCode(Identity identity)
            => identity switch
            {
                Identity.None           => "###",
                Identity.Building       => "BLD",
                Identity.ControlPoint   => "CPT",
                Identity.Node           => "NOD",
                Identity.Plant          => "PLT",
                Identity.Segment        => "SEG",
                Identity.NetLane        => "NLN",
                Identity.Extension      => "XTN",
                Identity.ServiceUpgrade => "SER",
                Identity.Prop           => "PRP",
                Identity.Decal          => "DEC",
                Identity.Surface        => "SUR",
                Identity.Overlay        => "OVR",
                Identity.Other          => "***",
                Identity.Invalid        => "???",
                _ => throw new System.NotImplementedException(),
            };

        public static string GetIdentityCode(Entity e)
            => GetIdentityCode(GetEntityIdentity(e));

        public static Identity GetEntityIdentity(Entity e)
            => GetEntityIdentity(World.DefaultGameObjectInjectionWorld.EntityManager, e);

        public static Identity GetEntityIdentity(EntityManager manager, Entity e)
        {
            if (e.Equals(Entity.Null) ||
                manager.HasComponent<Game.Common.Deleted>(e) ||
                manager.HasComponent<Game.Common.Terrain>(e))
            {
                return Identity.Invalid;
            }

            if (manager.HasComponent<Overlays.MIO_Type>(e))
            {
                return Identity.Overlay;
            }
            else if (manager.HasComponent<Game.Objects.Plant>(e))
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
            else if (manager.HasComponent<Game.Net.AreaLane>(e))
            {
                return Identity.NetLane;
            }
            else if (manager.HasComponent<Game.Net.Node>(e))
            {
                return Identity.Node;
            }
            else if (manager.HasComponent<Game.Areas.Surface>(e) || manager.HasComponent<Game.Areas.Space>(e))
            {
                return Identity.Surface;
            }
            else if (manager.HasComponent<Components.MIT_ControlPoint>(e))
            {
                return Identity.ControlPoint;
            }
            else if (manager.HasComponent<Game.Objects.ObjectGeometry>(e))// && !manager.HasComponent<Game.Objects.Pillar>(e))
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
