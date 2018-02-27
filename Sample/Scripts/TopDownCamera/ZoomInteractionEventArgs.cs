namespace Urho.TopDownCamera
{
    public class ZoomInteractionEventArgs : SimpleInteractionEventArgs
    {
        private readonly int _zoom;

        public ZoomInteractionEventArgs(IntVector2 center, int zoom):base(center)
        {
            _zoom = zoom;
        }

        public int Zoom
        {
            get { return _zoom; }
        }
    }
}