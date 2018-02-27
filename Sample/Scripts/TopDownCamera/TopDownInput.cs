using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Urho.TopDownCamera
{
    public class TopDownInput : IDisposable
    {
        private const int MouseTouchID = int.MinValue;
        private readonly List<TouchState> _activeTouches = new List<TouchState>(4);
        private readonly Graphics _graphics;

        private bool _isEnabled;

        private TouchState _mouseTouch;
        private float _panningAngle;

        private IntVector2 _panningCenter;
        private int _panningDistance;
        private bool _potentialClick;

        private bool _potentialRightClick;
        private bool _prevMouseGrabbed;
        private MouseMode _prevMouseMode;
        private bool _prevMouseVisible;
        private IntVector2 _rightClickStart;

        private TouchState _rigthClick;

        private readonly float AxisDeadZone = 0.1f;
        private readonly float JoystickRotateSpeed = 180;

        private readonly float JoystickScrollSpeed = 0.25f;

        public TopDownInput(Input input, Graphics graphics)
        {
            _graphics = graphics;
            Input = input;
        }

        public Input Input { get; }

        public int ClickThresholdSquared { get; set; } = 4;

        public float AltClickDuration { get; set; } = 1;


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
                Position = new IntVector2(args.X, args.Y),
                Pressure = args.Pressure
            });
        }

        private void MoveTouch(TouchState touch)
        {
            var index = IndexOfTouch(touch);
            if (index < 0) return;
            if (_activeTouches.Count == 1)
            {
                if (_potentialClick)
                {
                    var dist = touch.Position - _activeTouches[0].Position;
                    if (dist.LengthSquared < ClickThresholdSquared) return;
                    _potentialClick = false;
                    ClickCanceled?.Invoke(this, new SimpleInteractionEventArgs(touch.Position));
                }

                Pan?.Invoke(this,
                    new PanInteractionEventArgs(touch.Position, touch.Position - _activeTouches[0].Position));
            }
            else if (_activeTouches.Count >= 2)
            {
                var center = _panningCenter;
                var dist = _panningDistance;
                var angle = _panningAngle;
                CalculatePanning(out center, out dist, ref angle);
                if (center != _panningCenter)
                {
                    Pan?.Invoke(this, new PanInteractionEventArgs(center, center - _panningCenter));
                    _panningCenter = center;
                }

                if (angle != _panningAngle)
                {
                    Rotate?.Invoke(this, new RotateInteractionEventArgs(center, angle - _panningAngle));
                    _panningAngle = angle;
                }

                if (dist != _panningDistance)
                {
                    Zoom?.Invoke(this, new ZoomInteractionEventArgs(center, dist - _panningDistance));
                    _panningDistance = dist;
                }
            }

            _activeTouches[index].Update(touch);
        }

        private void HandleTouchEnd(TouchEndEventArgs args)
        {
            EndTouch(new TouchState
            {
                ID = args.TouchID,
                Position = new IntVector2(args.X, args.Y),
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
                    if (duration > AltClickDuration)
                    {
                        ClickCanceled?.Invoke(this, new SimpleInteractionEventArgs(touch.Position));
                        AltClickComplete?.Invoke(this, new SimpleInteractionEventArgs(touch.Position));
                    }
                    else
                    {
                        ClickComplete?.Invoke(this, new SimpleInteractionEventArgs(touch.Position));
                    }

                    _potentialClick = true;
                }
            }
            else if (_activeTouches.Count >= 2)
            {
                CalculatePanning(out _panningCenter, out _panningDistance, ref _panningAngle);
            }
        }

        private void HandleTouchBegin(TouchBeginEventArgs args)
        {
            StartTouch(new TouchState
            {
                ID = args.TouchID,
                Position = new IntVector2(args.X, args.Y),
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
            return new SimpleInteractionEventArgs(new IntVector2(_graphics.Width / 2, _graphics.Height / 2));
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
                _mouseTouch = new TouchState {ID = MouseTouchID, Position = Input.MousePosition, Pressure = 0.0f};
                MoveTouch(_mouseTouch);
            }

            if (_rigthClick != null)
            {
                var position = Input.MousePosition;
                var diff = (position - _rigthClick.Position).LengthSquared;
                if (diff > 2) _potentialRightClick = false;

                if (!_potentialRightClick)
                {
                    Rotate?.Invoke(this,
                        new RotateInteractionEventArgs(_rightClickStart, position.X - _rigthClick.Position.X));
                    _rigthClick.Position = position;
                }
            }
        }

        private void HandleMouseWheel(MouseWheelEventArgs args)
        {
            Zoom?.Invoke(this, new ZoomInteractionEventArgs(Input.MousePosition, args.Wheel * 120));
        }

        private void HandleMouseButtonDown(MouseButtonDownEventArgs args)
        {
            if (args.Button == 1)
                if (_mouseTouch == null)
                {
                    _mouseTouch = new TouchState {ID = MouseTouchID, Position = Input.MousePosition, Pressure = 1.0f};
                    StartTouch(_mouseTouch);
                }

            if (args.Button == 4)
            {
                _rigthClick = new TouchState {ID = MouseTouchID, Position = Input.MousePosition, Pressure = 1.0f};
                _rightClickStart = _rigthClick.Position;
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
                ProbablyClick?.Invoke(this, new SimpleInteractionEventArgs(touch.Position));
                _potentialClick = true;
            }
            else if (_activeTouches.Count == 2)
            {
                if (_potentialClick)
                {
                    ClickCanceled?.Invoke(this, new SimpleInteractionEventArgs(_activeTouches[0].Position));
                    _potentialClick = false;
                }

                CalculatePanning(out _panningCenter, out _panningDistance, ref _panningAngle);
            }
        }

        private void CalculatePanning(out IntVector2 panningCenter, out int panningDistance, ref float panningAngle)
        {
            var ab = _activeTouches[1].Position - _activeTouches[0].Position;
            panningCenter = _activeTouches[0].Position + ab / 2;
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
                    _mouseTouch = new TouchState {ID = MouseTouchID, Position = Input.MousePosition, Pressure = 0.0f};
                    EndTouch(_mouseTouch);
                    _mouseTouch = null;
                }

            if (args.Button == 4)
            {
                if (_potentialRightClick)
                {
                    AltClickComplete?.Invoke(this, new SimpleInteractionEventArgs(Input.MousePosition));
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
                    var pan = Vector2.Zero;
                    var numAxises = state.Axes.Size;
                    if (numAxises > 0) pan.X = -ApplyDeadZoone(state.GetAxisPosition(0));
                    if (numAxises > 1) pan.Y = -ApplyDeadZoone(state.GetAxisPosition(1));
                    if (pan != Vector2.Zero)
                        Pan?.Invoke(this,
                            new PanInteractionEventArgs(new IntVector2(_graphics.Width / 2, _graphics.Height / 2),
                                new IntVector2((int) (pan.X * timeStep * JoystickScrollSpeed * _graphics.Width),
                                    (int) (pan.Y * timeStep * JoystickScrollSpeed * _graphics.Height))));
                    if (numAxises > 2)
                    {
                        var angle = ApplyDeadZoone(state.GetAxisPosition(2));
                        if (angle != 0.0f)
                            Rotate?.Invoke(this,
                                new RotateInteractionEventArgs(
                                    new IntVector2(_graphics.Width / 2, _graphics.Height / 2),
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
                if (pan != Vector2.Zero)
                    Pan?.Invoke(this,
                        new PanInteractionEventArgs(new IntVector2(_graphics.Width / 2, _graphics.Height / 2),
                            new IntVector2((int) (pan.X * timeStep * JoystickScrollSpeed * _graphics.Width),
                                (int) (pan.Y * timeStep * JoystickScrollSpeed * _graphics.Height))));
            }
        }
    }
}