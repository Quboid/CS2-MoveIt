using MoveIt.Overlays;
using System.Collections.Generic;

namespace MoveIt.Systems
{
    internal partial class MIT_PostToolSystem : MIT_System
    {
        private HashSet<Overlay> _UpdateOverlayQueue = new();
        private readonly HashSet<Overlay> _UpdateOverlayQueueDeferred = new();

        internal void QueueOverlayUpdate(Overlay overlay)
            => _UpdateOverlayQueue.Add(overlay);

        internal void QueueOverlayUpdateDeferred(Overlay overlay)
            => _UpdateOverlayQueueDeferred.Add(overlay);

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            // Update overlays
            foreach (Overlay overlay in _UpdateOverlayQueue)
            {
                overlay.Update();
            }
            _UpdateOverlayQueue.Clear();
            _UpdateOverlayQueue = new(_UpdateOverlayQueueDeferred);
            _UpdateOverlayQueueDeferred.Clear();
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
