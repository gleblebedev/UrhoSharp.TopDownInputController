namespace Urho.TopDownCamera
{
    public class SimpleInputEnvironment: InputEnvironmentBase
    {
        public SimpleInputEnvironment(Camera camera, Graphics graphics) :base(camera, graphics)
        {
        }

        protected override void Raycast(Ray ray, out Vector3 contactPoint, out object userContext)
        {
            userContext = null;
            var dy = ray.Direction.Y;
            if (dy <= 0.01f && dy >= -0.01f)
            {
                contactPoint = ray.Origin + ray.Direction;
                return;
            }

            var dist = ray.Origin.Y / -dy;
            if (dist < 0.0f)
            {
                contactPoint = ray.Origin + ray.Direction;
                return;
            }

            contactPoint = ray.Origin + ray.Direction * dist;
        }
    }
}