using MoveIt.Overlays;
using System.Collections.Generic;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVBuilding : Moveable
    {
        internal List<MVDefinition> m_UpgradeDefinitions;

        public MVBuilding(Entity e) : base(e, Identity.Building)
        {
            m_Overlay = Factory.Create<OverlayBuilding>(this, OverlayTypes.MVBuilding);
            Refresh();
        }

        internal override bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            m_UpgradeDefinitions = new();
            short c = 0;

            if (TryGetBuffer<Game.Buildings.InstalledUpgrade>(out var buffer, true))
            {
                foreach (var upgradeComponent in buffer)
                {
                    var id = _MIT.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(upgradeComponent.m_Upgrade) ? Identity.ServiceUpgrade : Identity.Extension;
                    MVDefinition mvd = new(id, upgradeComponent.m_Upgrade, IsManipulatable, IsManaged, m_Entity, c);
                    Moveable mv = _MIT.Moveables.GetOrCreate(mvd);
                    m_UpgradeDefinitions.Add(mv.Definition);
                    c++;
                }
            }

            m_Overlay.EnqueueUpdate();
            return true;
        }

        internal override List<MVDefinition> GetAllChildren() => m_UpgradeDefinitions;

        internal override List<MVDefinition> GetChildrenToTransform() => m_UpgradeDefinitions;
    }
}
