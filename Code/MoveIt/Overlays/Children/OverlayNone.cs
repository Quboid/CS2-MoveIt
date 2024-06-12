namespace MoveIt.Overlays
{
    public class OverlayNone : Overlay
    {
        public OverlayNone() : base(OverlayTypes.None) { }

        public override bool CreateOverlayEntity() => false;
        public override bool DestroyOverlayEntity() => false;
    }
}
