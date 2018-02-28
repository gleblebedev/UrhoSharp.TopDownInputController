using System;

namespace Urho.TopDownCamera
{
    public class SimpleInteractionEventArgs:EventArgs
    {
        private readonly InputRaycastResult _contact;

        public SimpleInteractionEventArgs(InputRaycastResult contact)
        {
            _contact = contact;
        }

        public IntVector2 Position
        {
            get { return _contact.Position; }
        }

        public Ray Ray
        {
            get { return _contact.Ray; }
        }

        public Vector3 ContactPoint
        {
            get { return _contact.ContactPoint; }
        }
    }
}