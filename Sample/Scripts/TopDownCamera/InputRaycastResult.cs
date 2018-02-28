namespace Urho.TopDownCamera
{
    public class InputRaycastResult
    {
        private readonly IntVector2 _position;
        private readonly Ray _ray;
        private readonly Vector3 _contactPoint;
        private readonly object _userContext;

        public InputRaycastResult(IntVector2 position, Ray ray, Vector3 contactPoint, object userContext)
        {
            _position = position;
            _ray = ray;
            _contactPoint = contactPoint;
            _userContext = userContext;
        }

        public IntVector2 Position
        {
            get { return _position; }
        }

        public Ray Ray
        {
            get { return _ray; }
        }

        public Vector3 ContactPoint
        {
            get { return _contactPoint; }
        }

        public object UserContext
        {
            get { return _userContext; }
        }
    }
}