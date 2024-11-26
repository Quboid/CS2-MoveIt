using MoveIt.Overlays.Children;
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
            m_Overlay = new OverlayOther(this);
            RefreshFromAbstract();
        }
    }
}
