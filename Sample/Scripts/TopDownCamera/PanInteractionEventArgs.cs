namespace Urho.TopDownCamera
{
    public class PanInteractionEventArgs : SimpleInteractionEventArgs
    {
        private readonly IntVector2 _delta;

        public PanInteractionEventArgs(IntVector2 center, IntVector2 delta) : base(center)
        {
            _delta = delta;
        }

        public IntVector2 Delta
        {
            get { return _delta; }
        }
    }
}