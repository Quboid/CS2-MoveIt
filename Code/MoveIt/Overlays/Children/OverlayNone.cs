using MoveIt.Moveables;

namespace MoveIt.Overlays.Children
{
    public class OverlayNone : Overlay
    {
        public OverlayNone(Moveable mv) : base(OverlayTypes.None, mv) { }

        protected override bool CreateOverlayEntity() => false;
        protected override bool DestroyOverlayEntity() => false;
    }
}
