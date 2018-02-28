namespace Urho.TopDownCamera
{
    internal class TouchState
    {
        public int ID { get; internal set; }
        public IntVector2 ScreenPosition { get; internal set; }
        public float Pressure { get; internal set; }
        public float Duration { get; internal set; }

        public override int GetHashCode()
        {
            return ID;
        }

        public void Update(TouchState touch)
        {
            ScreenPosition = touch.ScreenPosition;
            Pressure = touch.Pressure;
        }
    }
}