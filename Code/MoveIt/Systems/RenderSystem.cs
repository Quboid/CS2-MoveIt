using MoveIt.Tool;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace MoveIt.Systems
{
    internal partial class MIT_RenderSystem : MIT_System
    {
        internal List<Overlays.Widget> m_Widgets;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Widgets = new();
        }

        internal override void Start()
        {
            base.Start();

            m_Widgets.ForEach(w => w.Active = true);
        }

        internal override void End()
        {
            m_Widgets.ForEach(w => w.Active = false);

            base.End();
        }

        internal bool Has(string name)
        {
            return m_Widgets.Any(w => w.m_Name.Equals(name));
        }

        internal void Add(Overlays.Widget w)
        {
            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;

            if (Has(w.m_Name))
            {
                MIT.Log.Warning($"Adding '{w.m_Name}' but key already exists ({m_Widgets.Count} entries)");
                return;
            }
            m_Widgets.Add(w);
        }

        internal void Update(string name, float3 position)
        {
            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;

            if (!Has(name))
            {
                MIT.Log.Warning($"Updating '{name}' but key not found ({m_Widgets.Count} entries)");
                return;
            }

            Get<Overlays.Widget>(name).SetPosition(position);
        }

        internal T Get<T>(string name) where T : Overlays.Widget
        {
            return (T)m_Widgets.FirstOrDefault(w => w.m_Name.Equals(name));
        }

        internal bool TryGet<T>(string name, out T widget) where T : Overlays.Widget
        {
            if (!Has(name))
            {
                widget = null;
                return false;
            }

            widget = (T)m_Widgets.FirstOrDefault(w => w.m_Name.Equals(name));
            return true;
        }

        internal void Remove(string name)
        {
            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;

            if (!Has(name))
            {
                MIT.Log.Warning($"Removing '{name}' but key not found ({m_Widgets.Count} entries)");
                return;
            }
            m_Widgets.Remove(Get<Overlays.Widget>(name));
        }

        internal void Remove(Overlays.Widget widget)
        {
            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;
            if (!Has(widget.m_Name))
            {
                MIT.Log.Warning($"Removing '{widget.m_Name}' but key not found ({m_Widgets.Count} entries)");
                return;
            }
            m_Widgets.Remove(widget);
        }

        internal void RemoveAll()
        {
            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;

            foreach (Overlays.Widget w in m_Widgets)
            {
                w.Dispose();
            }
            m_Widgets.Clear();
        }

        internal void Clear()
        {
            if (_Tool.m_OverlaySystem.m_DebugFreeze) return;

            foreach (var w in m_Widgets)
            {
                w.Dispose();
            }
            m_Widgets.Clear();
        }

        protected override void OnUpdate()
        {
            //MIT_Tooltip.instance.Text = "";
            //if (m_Widgets.Count > 0 )
            //{
            //    string msg = $"[{m_Widgets.Count}]";
            //    foreach (var w in m_Widgets)
            //    {
            //        string t;
            //        if (w is Overlays.Cuboid) t = "Q";
            //        else if (w is Overlays.Cylinder) t = "C";
            //        else if (w is Overlays.Point) t = "P";
            //        else t = "?";
            //        msg += $" {t}:{w.m_Name},";
            //    }
            //    msg = msg.Substring(0, msg.Length - 1);
            //    MIT_Tooltip.instance.Text = msg;
            //}

            //if (m_Widgets.Count > 0)
            //{
            //    foreach (Camera camera in Camera.allCameras)
            //    {
            //        if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
            //        {
            //            foreach (var widget in m_Widgets)
            //            {
            //                widget.Draw(camera);
            //            }
            //            break;
            //        }
            //    }
            //}
        }

        protected override void OnDestroy()
        {
            foreach (var widget in m_Widgets)
            {
                widget.Dispose();
            }
            m_Widgets = null;

            base.OnDestroy();
        }
    }
}
