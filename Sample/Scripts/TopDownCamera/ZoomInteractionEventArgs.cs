namespace Urho.TopDownCamera
{
    public class ZoomInteractionEventArgs : SimpleInteractionEventArgs
    {
        private readonly int _zoom;

        public ZoomInteractionEventArgs(InputRaycastResult contact, int zoom):base(contact)
        {
            _zoom = zoom;
        }

        public int Zoom
        {
            get { return _zoom; }
        }
    }
}