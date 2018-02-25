using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace Urho.TopDownCamera
{
    public class TopDownCameraController
    {
        private TopDownInput _input;
        private Node _cameraNode;
        private Camera _camera;

        public TopDownCameraController(TopDownInput input, Node cameraNode)
        {
            _input = input;
            CameraNode = cameraNode;
            _input.Pan += HandlePanning;
            _input.Rotate += HandleRotate;
            _input.Zoom += HandleZoom;
        }

        private void HandleZoom(object sender, ZoomInteractionEventArgs e)
        {
            var matrix = Matrix4.Rotate(CameraNode.Rotation);
            var forward =  matrix.Row2.Xyz;
            CameraNode.Position += forward*e.Zoom/120.0f;
        }

        private void HandleRotate(object sender, RotateInteractionEventArgs e)
        {
            var r= Quaternion.FromAxisAngle(Vector3.Up, e.Degrees);
            CameraNode.Rotation = r*CameraNode.Rotation;
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
