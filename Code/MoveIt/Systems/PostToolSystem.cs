using MoveIt.Overlays;
using System.Collections.Generic;

namespace MoveIt.Systems
{
    internal partial class MIT_PostToolSystem : MIT_System
    {
        internal HashSet<Overlay> m_UpdateOverlayQueue = new();

        internal void QueueOverlayUpdate(Overlay overlay)
        {
            if (m_UpdateOverlayQueue.Contains(overlay)) return;

            m_UpdateOverlayQueue.Add(overlay);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            UpdateOverlays();
        }

        private void UpdateOverlays()
        {
            if (m_UpdateOverlayQueue.Count == 0) return;

            foreach (Overlay overlay in m_UpdateOverlayQueue)
            {
                overlay.Update();
            }
            m_UpdateOverlayQueue.Clear();
        }

        //private void UpdateOverlays()
        //{
        //    if (m_UpdateOverlayQueue.Count == 0) return;
        //    //bool isOnlyCenter = m_UpdateOverlayQueue.Count == 1 && m_UpdateOverlayQueue.First() is OverlaySelectionCenter;

        //    string msg = $"Updating overlays:{m_UpdateOverlayQueue.Count}";
        //    foreach (Overlay overlay in m_UpdateOverlayQueue)
        //    {
        //        if (overlay.Update()) msg += $"\n    {overlay}";
        //    }
        //    m_UpdateOverlayQueue.Clear();
        //    //if (isOnlyCenter) return;
        //    QLog.Debug(msg);
        //}
    }
}
