using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVOther : Moveable
    {
        public MVOther(Entity e) : base(e, Identity.Other)
        {
            MIT.Log.Debug($"Other.Ctor {e.DX()}\n{QCommon.GetStackTrace(8)}");
            m_Overlay = Factory.Create<OverlayOther>(this, OverlayTypes.MVCircle);
            Refresh();
        }
    }
}
