namespace Urho.TopDownCamera
{
    public class RotateInteractionEventArgs : SimpleInteractionEventArgs
    {
        private readonly float _degrees;

        public RotateInteractionEventArgs(InputRaycastResult contact, float degrees) : base(contact)
        {
            _degrees = degrees;
        }

        public float Degrees
        {
            get { return _degrees; }
        }
    }
}