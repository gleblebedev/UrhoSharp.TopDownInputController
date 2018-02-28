using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Urho.TopDownCamera
{
    public class TopDownInput : IDisposable
    {
        private const int MouseTouchID = int.MinValue;
        private readonly List<TouchState> _activeTouches = new List<TouchState>(4);
        private readonly IInputEnvironment _environment;
        private Camera _camera;

        private bool _isEnabled;

        private TouchState _mouseTouch;
        private float _panningAngle;

        private IntVector2 _multitouchCenter;
        private int _panningDistance;
        private bool _potentialClick;

        private bool _potentialRightClick;
        private bool _prevMouseGrabbed;
        private MouseMode _prevMouseMode;
        private bool _prevMouseVisible;
        private InputRaycastResult _rightClickStart;

        private TouchState _rigthClick;

        private readonly float AxisDeadZone = 0.1f;
        private readonly float JoystickRotateSpeed = 180;

        private readonly float JoystickScrollSpeed = 0.25f;

        public TopDownInput(Input input, IInputEnvironment environment)
        {
            _environment = environment;
            Input = input;
        }

        public Input Input { get; }

        public int ClickThresholdSquared { get; set; } = 4;

        public float AltClickDuration { get; set; } = 1;

        public Camera Camera
        {
            get { return _camera; }
            set { _camera = value; }
        }


        public void Dispose()
        {
            Disable();
        }

        public event EventHandler<SimpleInteractionEventArgs> ProbablyClick;
        public event EventHandler<SimpleInteractionEventArgs> ClickCanceled;
        public event EventHandler<SimpleInteractionEventArgs> ClickComplete;
        public event EventHandler<SimpleInteractionEventArgs> AltClickComplete;
        public event EventHandler<PanInteractionEventArgs> Pan;
        public event EventHandler<RotateInteractionEventArgs> Rotate;
        public event EventHandler<ZoomInteractionEventArgs> Zoom;

        public void Enable()
        {
            if (_isEnabled)
                return;
            _isEnabled = true;
            _prevMouseMode = Input.MouseMode;
            _prevMouseVisible = Input.MouseVisible;
            _prevMouseGrabbed = Input.MouseGrabbed;
            Input.Enabled = true;
            Input.SetMouseMode(MouseMode.Free);
            Input.SetMouseGrabbed(false);
            Input.SetMouseVisible(true);

            Input.MouseButtonUp += HandleMouseButtonUp;
            Input.MouseButtonDown += HandleMouseButtonDown;
            Input.MouseWheel += HandleMouseWheel;
            Input.MouseMoved += HandleMouseMoved;
            Input.KeyUp += HandleKeyUp;
            Input.KeyDown += HandleKeyDown;
            Input.JoystickButtonDown += HandleJoystickButtonDown;
            Input.JoystickButtonUp += HandleJoystickButtonUp;
            Input.JoystickAxisMove += HandleJoystickAxisMove;
            Input.TouchBegin += HandleTouchBegin;
            Input.TouchEnd += HandleTouchEnd;
            Input.TouchMove += HandleTouchMove;
        }

        private void HandleJoystickAxisMove(JoystickAxisMoveEventArgs args)
        {
            var d = ApplyDeadZoone(args.Position);
            if (d != 0) Trace.WriteLine(args.Button + " " + d);
        }

        private float ApplyDeadZoone(float position)
        {
            if (position > 0)
            {
                if (position < AxisDeadZone)
                    return 0.0f;
                return (position - AxisDeadZone) / (1.0f - AxisDeadZone);
            }

            if (position > -AxisDeadZone)
                return 0.0f;
            return (position + AxisDeadZone) / (1.0f - AxisDeadZone);
        }

        private void HandleTouchMove(TouchMoveEventArgs args)
        {
            MoveTouch(new TouchState
            {
                ID = args.TouchID,
                ScreenPosition = new IntVector2(args.X, args.Y),
                Pressure = args.Pressure
            });
        }

        private void MoveTouch(TouchState touch)
        {
            var index = IndexOfTouch(touch);
            if (index < 0) return;
            if (_activeTouches.Count == 1)
            {
                var dist = touch.ScreenPosition - _activeTouches[index].ScreenPosition;
                var contact = _environment.Raycast(touch.ScreenPosition);
                if (_potentialClick)
                {
                    if (dist.LengthSquared < ClickThresholdSquared) return;
                    _potentialClick = false;
                    ClickCanceled?.Invoke(this, new SimpleInteractionEventArgs(contact));
                }

                Pan?.Invoke(this, new PanInteractionEventArgs(Move(contact,_activeTouches[index].ScreenPosition), contact));
            }
            else if (_activeTouches.Count >= 2)
            {
                var center = _multitouchCenter;
                var dist = _panningDistance;
                var angle = _panningAngle;
                CalculatePanning(out center, out dist, ref angle);
                if (center != _multitouchCenter)
                {
                    var oldContact = _environment.Raycast(_multitouchCenter);
                    var newContact = _environment.Raycast(center); 
                    Pan?.Invoke(this, new PanInteractionEventArgs(oldContact, newContact));
                    _multitouchCenter = center;
                }

                if (angle != _panningAngle)
                {
                    var oldContact = _environment.Raycast(_multitouchCenter);
                    Rotate?.Invoke(this, new RotateInteractionEventArgs(oldContact, angle - _panningAngle));
                    _panningAngle = angle;
                }

                if (dist != _panningDistance)
                {
                    var oldContact = _environment.Raycast(_multitouchCenter);
                    Zoom?.Invoke(this, new ZoomInteractionEventArgs(oldContact, dist - _panningDistance));
                    _panningDistance = dist;
                }
            }

            _activeTouches[index].Update(touch);
        }

        private InputRaycastResult Move(InputRaycastResult contact, IntVector2 screenPosition)
        {
            InputRaycastResult res = _environment.RaycastToPlane(screenPosition, new Plane(Vector3.Up, contact.ContactPoint));
            return new InputRaycastResult(res.Position,res.Ray,res.ContactPoint,contact.UserContext);
        }

        private void HandleTouchEnd(TouchEndEventArgs args)
        {
            EndTouch(new TouchState
            {
                ID = args.TouchID,
                ScreenPosition = new IntVector2(args.X, args.Y),
                Pressure = 0.0f
            });
        }

        private void EndTouch(TouchState touch)
        {
            var index = IndexOfTouch(touch);
            if (index < 0) return;
            var duration = _activeTouches[index].Duration;
            _activeTouches.RemoveAt(index);
            if (_activeTouches.Count == 0)
            {
                if (_potentialClick)
                {
                    var contact = _environment.Raycast(touch.ScreenPosition);
                    if (duration > AltClickDuration)
                    {
                        ClickCanceled?.Invoke(this, new SimpleInteractionEventArgs(contact));
                        AltClickComplete?.Invoke(this, new SimpleInteractionEventArgs(contact));
                    }
                    else
                    {
                        ClickComplete?.Invoke(this, new SimpleInteractionEventArgs(contact));
                    }

                    _potentialClick = true;
                }
            }
            else if (_activeTouches.Count >= 2)
            {
                UpdateMultitouch();
            }
        }

        private void HandleTouchBegin(TouchBeginEventArgs args)
        {
            StartTouch(new TouchState
            {
                ID = args.TouchID,
                ScreenPosition = new IntVector2(args.X, args.Y),
                Pressure = args.Pressure
            });
        }

        private void HandleJoystickButtonUp(JoystickButtonUpEventArgs args)
        {
            switch (args.Button)
            {
                case 0:
                    ClickComplete?.Invoke(this, GetSimleEventArg());
                    break;
                case 1:
                    AltClickComplete?.Invoke(this, GetSimleEventArg());
                    break;
            }
        }

        private SimpleInteractionEventArgs GetSimleEventArg()
        {
            var contact = _environment.Raycast(_environment.SceenSize/2);
            return new SimpleInteractionEventArgs(contact);
        }

        private void HandleJoystickButtonDown(JoystickButtonDownEventArgs args)
        {
            switch (args.Button)
            {
                case 0:
                    ProbablyClick?.Invoke(this, GetSimleEventArg());
                    break;
            }
        }

        private void HandleMouseMoved(MouseMovedEventArgs args)
        {
            if (_mouseTouch != null)
            {
                _mouseTouch = new TouchState {ID = MouseTouchID, ScreenPosition = Input.MousePosition, Pressure = 0.0f};
                MoveTouch(_mouseTouch);
            }

            if (_rigthClick != null)
            {
                var position = Input.MousePosition;
                var diff = (position - _rigthClick.ScreenPosition).LengthSquared;
                if (diff > 2) _potentialRightClick = false;

                if (!_potentialRightClick)
                {
                    var contact = _environment.Raycast(_environment.SceenSize / 2);
                    Rotate?.Invoke(this, new RotateInteractionEventArgs(contact, position.X - _rigthClick.ScreenPosition.X));
                    _rigthClick.ScreenPosition = Input.MousePosition;
                }
            }
        }

        private void HandleMouseWheel(MouseWheelEventArgs args)
        {
            var contact = _environment.Raycast(Input.MousePosition);
            Zoom?.Invoke(this, new ZoomInteractionEventArgs(contact, args.Wheel * 120));
        }

        private void HandleMouseButtonDown(MouseButtonDownEventArgs args)
        {
            if (args.Button == 1)
                if (_mouseTouch == null)
                {
                    _mouseTouch = new TouchState {ID = MouseTouchID, ScreenPosition = Input.MousePosition, Pressure = 1.0f};
                    StartTouch(_mouseTouch);
                }

            if (args.Button == 4)
            {
                _rigthClick = new TouchState {ID = MouseTouchID, ScreenPosition = Input.MousePosition, Pressure = 1.0f};
                _rightClickStart = _environment.Raycast(Input.MousePosition);
                _potentialRightClick = true;
            }
        }

        private void StartTouch(TouchState touch)
        {
            var index = IndexOfTouch(touch);
            if (index >= 0)
            {
                MoveTouch(touch);
                return;
            }

            _activeTouches.Add(touch);
            if (_activeTouches.Count == 1)
            {
                ProbablyClick?.Invoke(this, new SimpleInteractionEventArgs(_environment.Raycast(touch.ScreenPosition)));
                _potentialClick = true;
            }
            else if (_activeTouches.Count == 2)
            {
                if (_potentialClick)
                {
                    ClickCanceled?.Invoke(this, new SimpleInteractionEventArgs(_environment.Raycast(_activeTouches[0].ScreenPosition)));
                    _potentialClick = false;
                }

                UpdateMultitouch();
            }
        }

        private void UpdateMultitouch()
        {
            CalculatePanning(out _multitouchCenter, out _panningDistance, ref _panningAngle);
        }

        private void CalculatePanning(out IntVector2 panningCenter, out int panningDistance, ref float panningAngle)
        {
            var ab = _activeTouches[1].ScreenPosition - _activeTouches[0].ScreenPosition;
            panningCenter = _activeTouches[0].ScreenPosition + ab / 2;
            panningDistance = ab.Length;
            if (panningDistance > 0)
            {
                var abVec = new Vector2(ab.X, ab.Y);
                abVec.Normalize();
                panningAngle = MathHelper.RadiansToDegrees((float) Math.Acos(abVec.X));
                if (abVec.Y > 0)
                    panningAngle = 360 - panningAngle;
            }
        }

        private int IndexOfTouch(TouchState touch)
        {
            for (var i = 0; i < _activeTouches.Count; i++)
                if (_activeTouches[i].ID == touch.ID)
                    return i;

            return -1;
        }

        private void HandleKeyDown(KeyDownEventArgs args)
        {
        }

        private void HandleKeyUp(KeyUpEventArgs args)
        {
        }

        private void HandleMouseButtonUp(MouseButtonUpEventArgs args)
        {
            if (args.Button == 1)
                if (_mouseTouch != null)
                {
                    _mouseTouch = new TouchState {ID = MouseTouchID, ScreenPosition = Input.MousePosition, Pressure = 0.0f};
                    EndTouch(_mouseTouch);
                    _mouseTouch = null;
                }

            if (args.Button == 4)
            {
                if (_potentialRightClick)
                {
                    AltClickComplete?.Invoke(this, new SimpleInteractionEventArgs(_environment.Raycast(Input.MousePosition)));
                    _potentialRightClick = false;
                }

                _rigthClick = null;
            }
        }

        public void Disable()
        {
            if (!_isEnabled)
                return;
            _isEnabled = false;

            Input.MouseButtonUp -= HandleMouseButtonUp;
            Input.MouseButtonDown -= HandleMouseButtonDown;
            Input.MouseWheel -= HandleMouseWheel;
            Input.MouseMoved -= HandleMouseMoved;
            Input.KeyUp -= HandleKeyUp;
            Input.KeyDown -= HandleKeyDown;
            Input.JoystickButtonDown -= HandleJoystickButtonDown;
            Input.JoystickButtonUp -= HandleJoystickButtonUp;
            Input.JoystickAxisMove -= HandleJoystickAxisMove;
            Input.TouchBegin -= HandleTouchBegin;
            Input.TouchEnd -= HandleTouchEnd;
            Input.TouchMove -= HandleTouchMove;

            Input.SetMouseMode(_prevMouseMode);
            Input.SetMouseGrabbed(_prevMouseGrabbed);
            Input.SetMouseVisible(_prevMouseVisible);
        }

        public void Update(float timeStep)
        {
            foreach (var activeTouch in _activeTouches) activeTouch.Duration += timeStep;
            Input.Update();
            for (uint i = 0; i < Input.NumJoysticks; ++i)
            {
                JoystickState state;
                if (Input.TryGetJoystickState(i, out state))
                {
                    if (state.Name == "Android Accelerometer")
                        continue;
                    var pan = Vector2.Zero;
                    var numAxises = state.Axes.Size;
                    if (numAxises > 0) pan.X = -ApplyDeadZoone(state.GetAxisPosition(0));
                    if (numAxises > 1) pan.Y = -ApplyDeadZoone(state.GetAxisPosition(1));
                    KeyboardPanning(timeStep, pan);

                    if (numAxises > 2)
                    {
                        var angle = ApplyDeadZoone(state.GetAxisPosition(2));
                        if (angle != 0.0f)
                            Rotate?.Invoke(this,
                                new RotateInteractionEventArgs(
                                    _environment.Raycast(_environment.SceenSize / 2),
                                    angle * timeStep * JoystickRotateSpeed));
                    }
                }
            }

            {
                var pan = Vector2.Zero;
                if (Input.GetKeyDown(Key.Up)) pan.Y += 1;
                if (Input.GetKeyDown(Key.Down)) pan.Y -= 1;
                if (Input.GetKeyDown(Key.Right)) pan.X -= 1;
                if (Input.GetKeyDown(Key.Left)) pan.X += 1;
                KeyboardPanning(timeStep, pan);
            }
        }

        private void KeyboardPanning(float timeStep, Vector2 pan)
        {
            if (pan == Vector2.Zero)
                return;
            var delta = new IntVector2(
                (int)(pan.X * timeStep * JoystickScrollSpeed * _environment.SceenSize.X),
                (int)(pan.Y * timeStep * JoystickScrollSpeed * _environment.SceenSize.Y));
            if (delta == IntVector2.Zero)
                return;

            var screenCenter = _environment.SceenSize / 2;
            var centralContact = _environment.Raycast(screenCenter);
            var newContact =
                _environment.RaycastToPlane(screenCenter + delta, new Plane(Vector3.Up, centralContact.ContactPoint));
            Pan?.Invoke(this, new PanInteractionEventArgs(centralContact, newContact));
        }
    }
}