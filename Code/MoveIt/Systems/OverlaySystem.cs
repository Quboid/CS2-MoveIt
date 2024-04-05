using Game.Rendering;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Systems
{
    internal partial class MIT_OverlaySystem : MIT_System
    {
        private OverlayRenderSystem _OverlayRenderSystem;
        internal static readonly float s_DefaultLineWidth = Utils.s_DefaultLineWidth;

        internal bool m_DebugFreeze = false;
        private List<Utils.IOverlay> _DebugOverlays;

        private bool ShowHoveredOverlay => _Tool.ToolState != ToolStates.DrawingSelection && !_Tool.Hover.IsNull;

        internal void DebugAdd(Utils.IOverlay overlay)
        {
            if (m_DebugFreeze) return;

            _DebugOverlays.Add(overlay);
        }

        internal void DebugClear()
        {
            if (m_DebugFreeze) return;

            //if (_DebugOverlays.Count > 0) QLog.Debug($"DebugClear {_DebugOverlays.Count}");
            for (int i = 0; i < _DebugOverlays.Count; i++)
            {
                _DebugOverlays[i].Dispose();
            }
            _DebugOverlays.Clear();
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            _DebugOverlays = new();
            Enabled = false;
        }

        internal override void Start()
        {
            base.Start();
        }

        internal override void End()
        {
            DebugClear();
            base.End();
        }

        protected override void OnUpdate()
        {
            OverlayCollection overlays = new();

            try
            {
                // Selection overlays
                if (_Tool.Selection.Count > 0)
                {
                    foreach ((Entity e, Moveables.Moveable mv) in _Tool.Selection)
                    {
                        if (_Tool.Manipulating && _Tool.Manipulation.Has(e)) continue;
                        if (ShowHoveredOverlay && _Tool.Hover.Is(e)) continue;

                        OverlayFlags f = OverlayFlags.Selected;
                        if (_Tool.m_Marquee is not null && _Tool.m_Marquee.m_Entities is not null && _Tool.m_Marquee.m_Entities.Contains(e))
                        {
                            f |= OverlayFlags.Hovering;
                        }
                        overlays.Add(mv.GetOverlays(f));
                    }

                    if (!_Tool.Manipulating) overlays.Add(new Utils.Circle(_Tool.Selection.Center, 1.5f, OverlayFlags.Selected));
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on Selection Overlays:\n{ex}");
            }

            try
            {
                // Manipulation overlays
                if (_Tool.Manipulating && _Tool.Manipulation.Count > 0)
                {
                    foreach ((Entity e, Moveables.Moveable mv) in _Tool.Manipulation)
                    {
                        if (ShowHoveredOverlay && _Tool.Hover.Is(e)) continue;

                        OverlayFlags f = OverlayFlags.Selected;
                        if (_Tool.m_Marquee is not null && _Tool.m_Marquee.m_Entities is not null && _Tool.m_Marquee.m_Entities.Contains(e))
                        {
                            f |= OverlayFlags.Hovering;
                        }
                        overlays.Add(mv.GetManipulationOverlays(f));
                    }

                    if (_Tool.Manipulation.GetCountOfType(QTypes.Manipulate.Child) > 0)
                    {
                        Utils.OverlayCommon common = new()
                        {
                            Flags = OverlayFlags.Selected,
                            Manipulatable = QTypes.Manipulate.Child,
                        };
                        overlays.Add(new Utils.Circle(_Tool.Manipulation.Center, 1.5f, Colors.Get(common, Colors.Styles.Foreground), OverlayRenderSystem.StyleFlags.Projected));
                    }
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on Selection Overlays:\n{ex}");
            }

            try
            {
                // Hovered Object
                if (ShowHoveredOverlay)
                {
                    OverlayFlags f = OverlayFlags.Hovering;
                    if (_Tool.Hover.IsSelected)
                    {
                        f |= OverlayFlags.Selected;
                        if (QKeyboard.Shift)
                        {
                            f |= OverlayFlags.Unselect;
                        }
                    }

                    if (_Tool.Manipulating)
                    {
                        overlays.Add(_Tool.Hover.Moveable.GetManipulationOverlays(f));
                    }
                    else
                    {
                        overlays.Add(_Tool.Hover.Moveable.GetOverlays(f));
                    }
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on Hovered Overlay:\n{ex}");
            }

            try
            {
                // Selection rectangle
                if (_Tool.ToolState == ToolStates.DrawingSelection && !(_Tool.m_Marquee.m_SelectArea.a.Equals(0f) && _Tool.m_Marquee.m_SelectArea.c.Equals(0f)))
                {
                    Utils.Rectangle overlay = new(_Tool.m_Marquee.m_SelectArea, OverlayFlags.Hovering);
                    overlays.Add(overlay);
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on Marquee Overlay:\n{ex}");
            }

            try
            {
                // Action overlays
                overlays.Add(Actions.Queue.Current.GetOverlays());
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on Action Overlays:\n{ex}");
            }

            try
            {
                // Debugging overlays
                foreach (Utils.IOverlay overlay in _DebugOverlays)
                {
                    overlays.Add(overlay);
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on Debug Overlays:\n{ex}");
            }

            try
            {
                OverlayRenderSystem.Buffer buffer = _OverlayRenderSystem.GetBuffer(out JobHandle overlayRenderBufferHandle);
                overlayRenderBufferHandle.Complete();
                DrawTools.m_OverlayRenderBuffer = buffer;

                ToolFlags flags = ToolFlags.None;
                if (_Tool.Manipulating) flags |= ToolFlags.Manipulation;

                DrawMoveableJob drawMoveable = new()
                {
                    m_Overlays = overlays,
                    m_OverlayRenderBuffer = buffer,
                    m_Flags = flags,
                };
                JobHandle drawMoveableHandle = drawMoveable.Schedule(overlays.Count, overlayRenderBufferHandle);

                _OverlayRenderSystem.AddBufferWriter(drawMoveableHandle);
                JobHandle overlayHandle = overlays.Dispose(drawMoveableHandle);
                drawMoveableHandle.Complete();
                Dependency = overlayHandle;
            }
            catch (Exception ex)
            {
                MIT.Log.Error($"Failed on Overlay job management:\n{ex}");
            }
        }

        protected struct DrawMoveableJob : IJobFor
        {
            public OverlayCollection m_Overlays;
            public OverlayRenderSystem.Buffer m_OverlayRenderBuffer;
            public ToolFlags m_Flags;

            public readonly void Execute(int index)
            {
                Utils.IOverlay overlay = m_Overlays.Get(index);
                overlay.Draw(m_Flags);
            }
        }
    }
}
