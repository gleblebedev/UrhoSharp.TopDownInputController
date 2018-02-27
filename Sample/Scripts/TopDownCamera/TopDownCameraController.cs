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
            var ray = _camera.GetScreenRay(e.Position.X/ (float)_graphics.Width, e.Position.Y/(float)_graphics.Height);
            CameraNode.Position += ray.Direction * e.Zoom/120.0f;
        }

        private void HandleRotate(object sender, RotateInteractionEventArgs e)
        {
            //var ray = _camera.GetScreenRay(e.Position.X / (float)_graphics.Width, e.Position.Y / (float)_graphics.Height);
            var ray = _camera.GetScreenRay(0.5f, 0.5f);
            var distance = 3.0f;
            var center = CameraNode.Position + ray.Direction * distance;
            var r = Quaternion.FromAxisAngle(Vector3.Up, e.Degrees);
            CameraNode.Rotation = r*CameraNode.Rotation;
            var matrix = Matrix4.Rotate(CameraNode.Rotation);
            var forward = matrix.Row2.Xyz;
            CameraNode.Position = center - forward * distance;
        }

        private void HandlePanning(object sender, PanInteractionEventArgs e)
        {
            var matrix = Matrix4.Rotate(CameraNode.Rotation);
            var up = matrix.Row1.Xyz;
            var right = matrix.Row0.Xyz;
            CameraNode.Position += - right * e.Delta.X*0.01f
                + up * e.Delta.Y * 0.01f;
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
