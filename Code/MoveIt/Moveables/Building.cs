using Colossal.Mathematics;
using MoveIt.Overlays;
using MoveIt.Tool;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public class Building : Moveable
    {
        private NativeArray<Line3.Segment> _OverlayLines;
        private Game.Objects.Transform _OverlayTransformOld;

        public Building(Entity e) : base(e, QTypes.Identity.Building, QTypes.ObjectType.Normal, QTypes.Manipulate.Normal)
        {
            Refresh();
        }

        internal override bool Refresh()
        {
            if (!base.Refresh()) return false;

            UpdateLines();
            _OverlayTransformOld = OverlayTransform;
            return true;
        }

        internal List<Moveable> GetInstalledUpgrades()
        {
            List<Moveable> result = new();

            if (TryGetBuffer<Game.Buildings.InstalledUpgrade>(out var buffer, true))
            {
                foreach (var upgradeComponent in buffer)
                {
                    Moveable upgrade = GetOrCreate(upgradeComponent.m_Upgrade);
                    result.Add(upgrade);
                }
            }

            return result;
        }

        internal override Utils.IOverlay GetOverlay(OverlayFlags flags = OverlayFlags.None)
        {
            if (!OverlayTransform.Equals(_OverlayTransformOld))
            {
                UpdateLines();
                _OverlayTransformOld = OverlayTransform;
            }
             
            OverlayBuilding overlay = new(_OverlayLines, flags);
            return overlay;
        }

        private void UpdateLines()
        {
            _OverlayLines.Dispose();
            Game.Prefabs.PrefabRef prefab = _Tool.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(m_Entity);
            Game.Prefabs.BuildingData buildingData = _Tool.EntityManager.GetComponentData<Game.Prefabs.BuildingData>(prefab);
            float3 lotHalfSize = new(buildingData.m_LotSize.x * 4, 0f, buildingData.m_LotSize.y * 4);
            _OverlayLines = DrawTools.CalculateBuildingRectangleLines(OverlayTransform, Utils.s_DefaultLineWidth, lotHalfSize, Allocator.Persistent);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            UpdateLines();
        }

        public struct OverlayBuilding : Utils.IOverlay
        {
            public Utils.OverlayCommon Common { get; set; }

            public float m_Width = Utils.s_DefaultLineWidth;
            public NativeArray<Line3.Segment> m_Lines;

            public OverlayBuilding(NativeArray<Line3.Segment> lines, OverlayFlags flags)
            {
                Common = new()
                {
                    Flags = flags,
                };

                m_Lines = lines;
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                if (m_Lines.Length != 5)
                {
                    MIT.Log.Warning($"Overlay failed, rectangle only has {m_Lines.Length} lines defined.");
                    return;
                }

                Color c = Colors.Get(Common, Colors.Styles.Foreground);

                try
                {
                    Common.DrawTool.LineSimple(Common, c, m_Lines[0]);
                    Common.DrawTool.LineSimple(Common, c, m_Lines[1]);
                    Common.DrawTool.LineSimple(Common, c, m_Lines[2]);
                    Common.DrawTool.LineSimple(Common, c, m_Lines[3]);
                    Common.DrawTool.LineDashedSimple(Common, c, m_Lines[4]);
                }
                catch (NullReferenceException e)
                {
                    MIT.Log.Warning(e);
                }
            }

            public readonly void Dispose() { }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return handle;
            }
        }

        public override void Dispose()
        {
            _OverlayLines.Dispose();
        }

        ~Building()
        {
            _OverlayLines.Dispose();
        }
    }
}
