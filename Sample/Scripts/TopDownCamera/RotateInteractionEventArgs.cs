namespace Urho.TopDownCamera
{
    public class RotateInteractionEventArgs : SimpleInteractionEventArgs
    {
        private readonly float _degrees;

        public RotateInteractionEventArgs(IntVector2 center, float degrees) : base(center)
        {
            _degrees = degrees;
        }

        public float Degrees
        {
            get { return _degrees; }
        }
    }
}