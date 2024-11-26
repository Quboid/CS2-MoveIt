using MoveIt.Overlays.Children;
using System.Collections.Generic;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVBuilding : Moveable
    {
        private readonly List<MVDefinition> _UpgradeDefinitions;

        public MVBuilding(Entity e) : base(e, Identity.Building)
        {
            _UpgradeDefinitions = new();
            short c = 0;

            if (TryGetBuffer<Game.Buildings.InstalledUpgrade>(out var buffer, true))
            {
                foreach (Game.Buildings.InstalledUpgrade upgradeComponent in buffer)
                {
                    Identity id = _MIT.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(upgradeComponent.m_Upgrade) ? Identity.ServiceUpgrade : Identity.Extension;
                    MVDefinition mvd = new(id, upgradeComponent.m_Upgrade, IsManipulatable, IsManaged, m_Entity, Identity.Building, c);
                    Moveable mv = _MIT.Moveables.GetOrCreate<Moveable>(mvd);
                    _UpgradeDefinitions.Add(mv.Definition);
                    c++;
                }
            }

            m_Overlay = new OverlayBuilding(this);
            RefreshFromAbstract();
        }

        internal override bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            m_Overlay.EnqueueUpdate();
            return true;
        }

        internal override List<MVDefinition> GetAllChildren() => _UpgradeDefinitions;

        internal override List<MVDefinition> GetChildrenToTransform() => _UpgradeDefinitions;

        public override void Dispose()
        {
            //QLog.Debug($"MVBuilding.Dispose {E()} upgrades:{m_UpgradeDefinitions.Count}");
            foreach (MVDefinition mvd in _UpgradeDefinitions)
            {
                _MIT.Moveables.TryRemove(mvd);
            }

            base.Dispose();
        }
    }
}
