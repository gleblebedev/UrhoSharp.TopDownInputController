using System;

namespace Urho.TopDownCamera
{
    public class SimpleInteractionEventArgs:EventArgs
    {
        private IntVector2 _position;

        public SimpleInteractionEventArgs(IntVector2 position)
        {
            Position = position;
        }

        public IntVector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }
    }
}