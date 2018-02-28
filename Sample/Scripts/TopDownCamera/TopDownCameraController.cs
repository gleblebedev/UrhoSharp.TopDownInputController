using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Urho;

namespace Urho.TopDownCamera
{
    
    public class TopDownCameraController
    {
        private TopDownInput _input;
        private readonly Graphics _graphics;
        private Node _cameraNode;
        private Camera _camera;

        public TopDownCameraController(TopDownInput input, Graphics graphics, Node cameraNode)
        {
            _input = input;
            _graphics = graphics;
            CameraNode = cameraNode;
            _input.Pan += HandlePanning;
            _input.Rotate += HandleRotate;
            _input.Zoom += HandleZoom;
        }

        private void HandleZoom(object sender, ZoomInteractionEventArgs e)
        {
            if (_camera == null)
                return;
            var maxDistance = Vector3.Dot(e.ContactPoint - CameraNode.Position, e.Ray.Direction) - 0.1f;
            var offset = Vector3.Dot(e.Ray.Origin - CameraNode.Position, e.Ray.Direction);
            maxDistance -= offset * 2.0f;
            var distance = e.Zoom / 120.0f;
            if (distance > maxDistance)
                distance = maxDistance;
            CameraNode.Position += e.Ray.Direction * distance;
        }

        private void HandleRotate(object sender, RotateInteractionEventArgs e)
        {
            var r = Quaternion.FromAxisAngle(Vector3.Up, e.Degrees);
            CameraNode.Rotation = r*CameraNode.Rotation;
            var matrix = Matrix4.CreateTranslation(-e.ContactPoint)* Matrix4.Rotate(r)* Matrix4.CreateTranslation(e.ContactPoint);
            CameraNode.Position = Vector3.TransformPosition(CameraNode.Position, matrix);
        }

        private void HandlePanning(object sender, PanInteractionEventArgs e)
        {
            CameraNode.Position -= e.Delta3D;
            //var matrix = Matrix4.Rotate(CameraNode.Rotation);
            //var up = matrix.Row1.Xyz;
            //var right = matrix.Row0.Xyz;
            //CameraNode.Position += - right * e.Delta.X*0.01f
            //    + up * e.Delta.Y * 0.01f;
        }

        public Node CameraNode
        {
            get { return _cameraNode; }
            set
            {
                _cameraNode = value;
                _camera = _cameraNode?.GetComponent<Camera>();
            }
        }
    }
}
