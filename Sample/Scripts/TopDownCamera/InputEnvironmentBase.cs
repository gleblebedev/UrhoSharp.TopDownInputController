namespace Urho.TopDownCamera
{
    public abstract class InputEnvironmentBase : IInputEnvironment
    {
        readonly Camera _camera;
        private readonly Graphics _graphics;

        public InputEnvironmentBase(Camera camera, Graphics graphics)
        {
            _camera = camera;
            _graphics = graphics;
        }

        public InputRaycastResult Raycast(IntVector2 position)
        {
            var ray = _camera.GetScreenRay(position.X/ (float)_graphics.Width, position.Y/(float)_graphics.Height);
            Vector3 contactPoint;
            object userContext;
            Raycast(ray, out contactPoint, out userContext);

            return new InputRaycastResult(position, ray, contactPoint, userContext);
        }

        protected abstract void Raycast(Ray ray, out Vector3 contactPoint, out object userContext);

        public InputRaycastResult Raycast(Ray ray)
        {
            Vector3 contactPoint;
            object userContext;
            Raycast(ray, out contactPoint, out userContext);
            var point = _camera.WorldToScreenPoint(contactPoint);
            IntVector2 position = new IntVector2((int)(point.X*_graphics.Width), (int)(point.Y * _graphics.Height));
            return new InputRaycastResult(position, ray, contactPoint, userContext);

        }

        public IntVector2 SceenSize { get {return new IntVector2(_graphics.Width, _graphics.Height);} }
        public virtual InputRaycastResult RaycastToPlane(IntVector2 screenPosition, Plane plane)
        {
            var ray = _camera.GetScreenRay(screenPosition.X / (float)_graphics.Width, screenPosition.Y / (float)_graphics.Height);
            var dist = ray.HitDistance(plane);
            if (float.IsNaN(dist) || float.IsInfinity(dist))
            {
                return new InputRaycastResult(screenPosition,ray, ray.Origin,null);
            }
            return new InputRaycastResult(screenPosition, ray, ray.Origin+ray.Direction*dist, null);
        }
    }
}