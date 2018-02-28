namespace Urho.TopDownCamera
{
    public interface IInputEnvironment
    {
        InputRaycastResult Raycast(IntVector2 position);
        InputRaycastResult Raycast(Ray ray);
        IntVector2 SceenSize { get;  }
        InputRaycastResult RaycastToPlane(IntVector2 screenPosition, Plane plane, object userContext);
    }
}