namespace Urho.TopDownCamera
{
    public class PanInteractionEventArgs : SimpleInteractionEventArgs
    {
        private readonly InputRaycastResult _newContact;

        public PanInteractionEventArgs(InputRaycastResult contact, InputRaycastResult newContact) : base(contact)
        {
            _newContact = newContact;
        }

        public IntVector2 Delta
        {
            get { return _newContact.Position-Position; }
        }

        public Vector3 Delta3D
        {
            get { return _newContact.ContactPoint - ContactPoint; }
        }
    }
}