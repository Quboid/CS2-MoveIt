using MoveIt.Overlays;
using System.Collections.Generic;

namespace MoveIt.Systems
{
    internal partial class MIT_PostToolSystem : MIT_System
    {
        internal HashSet<Overlay> m_UpdateOverlayQueue = new();
        internal HashSet<Overlay> m_UpdateOverlayQueueDeferred = new();

        internal void QueueOverlayUpdate(Overlay overlay)
        {
            if (m_UpdateOverlayQueue.Contains(overlay)) return;

            m_UpdateOverlayQueue.Add(overlay);
        }

        internal void QueueOverlayUpdateDeferred(Overlay overlay)
        {
            if (m_UpdateOverlayQueueDeferred.Contains(overlay)) return;

            m_UpdateOverlayQueueDeferred.Add(overlay);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            // Update overlays
            foreach (Overlay overlay in m_UpdateOverlayQueue)
            {
                overlay.Update();
            }
            m_UpdateOverlayQueue.Clear();
            m_UpdateOverlayQueue = new(m_UpdateOverlayQueueDeferred);
            m_UpdateOverlayQueueDeferred.Clear();
        }

        //private void UpdateOverlays()
        //{
        //    //bool isOnlyCenter = m_UpdateOverlayQueue.Count == 1 && m_UpdateOverlayQueue.First() is OverlaySelectionCenter;

        //    string msg = $"Updating overlays:{m_UpdateOverlayQueue.Count} (deferred:{m_UpdateOverlayQueueDeferred.Count})";
        //    foreach (Overlay overlay in m_UpdateOverlayQueue)
        //    {
        //        if (overlay.Update()) msg += $"\n    {overlay}";
        //    }
        //    m_UpdateOverlayQueue.Clear();
        //    m_UpdateOverlayQueue = new (m_UpdateOverlayQueueDeferred);
        //    m_UpdateOverlayQueueDeferred.Clear();
        //    //if (isOnlyCenter) return;
        //    QLog.XDebug(msg);
        //}
    }
}
