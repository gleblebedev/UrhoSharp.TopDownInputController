using System;

namespace Urho.TopDownCamera
{
    public class SimpleInteractionEventArgs:EventArgs
    {
        private IntVector2 _position;

        public SimpleInteractionEventArgs(IntVector2 position)
        {
            _position = position;
        }
    }
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