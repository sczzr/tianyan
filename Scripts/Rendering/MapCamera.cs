using Godot;

namespace FantasyMapGenerator.Scripts.Rendering;

/// <summary>
/// 地图相机：通过调整 MapView.ViewScale 实现缩放
/// </summary>
public partial class MapCamera : Camera2D
{
	private const string ConfigPath = "user://settings.cfg";
	private const string ConfigSection = "controls";

	[Export] public float MinViewScale { get; set; } = 0.05f;
	[Export] public float MaxViewScale { get; set; } = 5.0f;
	[Export] public float ZoomStep { get; set; } = 0.05f;
	[Export] public float FineZoomStep { get; set; } = 0.01f;
	[Export] public float CoarseZoomStep { get; set; } = 0.1f;
	[Export] public float ZoomOutMinFactor { get; set; } = 0.2f;
	[Export] public float ZoomOutMaxFactor { get; set; } = 2.0f;
	[Export] public float ZoomOutCurvePower { get; set; } = 1.6f;
	[Export] public bool UseSmoothing { get; set; } = true;
	[Export] public float ZoomSpeed { get; set; } = 10f;
	[Export] public float ZoomImpulse { get; set; } = 6f;
	[Export] public float ZoomDamping { get; set; } = 12f;
	[Export] public float MaxZoomVelocity { get; set; } = 3f;
	[Export] public bool EnableKeyboardPan { get; set; } = true;
	[Export] public float MoveSpeed { get; set; } = 240f;
	[Export] public float MoveAcceleration { get; set; } = 1200f;
	[Export] public float MoveDamping { get; set; } = 1600f;
	[Export] public bool ScaleMoveSpeedByZoom { get; set; } = true;

	private MapView _mapView;
	private float _targetViewScale = 1f;
	private float _zoomVelocity = 0f;
	private Vector2 _moveVelocity = Vector2.Zero;
	private bool _hasZoomFocus = false;
	private Vector2 _zoomFocusScreenPos = Vector2.Zero;
	private Vector2 _zoomFocusMapPos = Vector2.Zero;
	private bool _isMiddleDragging = false;
	private Vector2 _lastDragScreenPos = Vector2.Zero;

	public override void _Ready()
	{
		SetProcessInput(true);
		SetProcess(true);

		_mapView = GetNodeOrNull<MapView>("../MapView")
			?? GetParent()?.GetNodeOrNull<MapView>("MapView")
			?? GetTree()?.CurrentScene?.FindChild("MapView", true, false) as MapView;

		if (_mapView == null)
		{
			GD.PrintErr("MapCamera: MapView node not found.");
		}

		LoadControlSettings();
		if (_mapView != null)
		{
			var effectiveMin = GetEffectiveMinViewScale();
			var effectiveMax = Mathf.Max(MaxViewScale, effectiveMin);
			_targetViewScale = Mathf.Clamp(_mapView.ViewScale, effectiveMin, effectiveMax);
			if (!UseSmoothing)
			{
				_mapView.ViewScale = _targetViewScale;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		HandlePointerInput(@event);
	}

	private void HandlePointerInput(InputEvent @event)
	{
		if (_mapView == null)
		{
			return;
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Middle)
			{
				if (mouseButton.Pressed && IsPointerOverMapView())
				{
					_isMiddleDragging = true;
					_lastDragScreenPos = mouseButton.Position;
				}
				else if (!mouseButton.Pressed)
				{
					_isMiddleDragging = false;
				}
			}

			if (!mouseButton.Pressed)
			{
				return;
			}

			if (!IsPointerOverMapView())
			{
				return;
			}

			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				CaptureZoomFocus();
				AdjustZoom(1, GetZoomStep(mouseButton));
			}
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				CaptureZoomFocus();
				AdjustZoom(-1, GetZoomStep(mouseButton));
			}

			return;
		}

		if (@event is InputEventMouseMotion mouseMotion && _isMiddleDragging)
		{
			var previousMapPos = _mapView.ScreenToMap(_lastDragScreenPos);
			var currentMapPos = _mapView.ScreenToMap(mouseMotion.Position);
			_mapView.CameraMapOffset += previousMapPos - currentMapPos;
			_lastDragScreenPos = mouseMotion.Position;
			return;
		}

		if (@event is InputEventMagnifyGesture magnifyGesture)
		{
			if (!IsPointerOverMapView())
			{
				return;
			}

			var direction = magnifyGesture.Factor >= 1f ? 1 : -1;
			CaptureZoomFocus();
			AdjustZoom(direction, ZoomStep, Mathf.Abs(magnifyGesture.Factor - 1f));
		}
	}

	public override void _Process(double delta)
	{
		if (_mapView == null)
		{
			return;
		}

		var dt = (float)delta;

		if (EnableKeyboardPan && _mapView != null)
		{
			UpdateMoveInertia(dt);
		}

		UpdateZoomInertia(dt);

		if (!UseSmoothing || _mapView == null)
		{
			return;
		}

		var current = _mapView.ViewScale;
		if (Mathf.IsEqualApprox(current, _targetViewScale))
		{
			return;
		}

		var lerpT = 1f - Mathf.Exp(-ZoomSpeed * dt);
		_mapView.ViewScale = Mathf.Lerp(current, _targetViewScale, lerpT);
		ApplyZoomFocus(_mapView.ViewScale);
	}

	private void AdjustZoom(int direction, float step, float factor = 1f)
	{
		if (direction == 0)
		{
			return;
		}

		var baseScale = UseSmoothing ? _targetViewScale : _mapView.ViewScale;
		if (direction < 0)
		{
			step *= GetZoomOutFactor(baseScale);
		}

		var impulse = direction * step * factor * ZoomImpulse;
		_zoomVelocity = Mathf.Clamp(_zoomVelocity + impulse, -MaxZoomVelocity, MaxZoomVelocity);
	}

	private float GetZoomStep(InputEventWithModifiers inputEvent)
	{
		if (inputEvent != null && inputEvent.ShiftPressed)
		{
			return CoarseZoomStep;
		}

		if (inputEvent != null && inputEvent.CtrlPressed)
		{
			return FineZoomStep;
		}

		return ZoomStep;
	}

	private float GetZoomOutFactor(float currentScale)
	{
		if (MaxViewScale <= MinViewScale)
		{
			return 1f;
		}

		var t = Mathf.Clamp((currentScale - MinViewScale) / (MaxViewScale - MinViewScale), 0f, 1f);
		var curved = Mathf.Pow(t, Mathf.Max(0.01f, ZoomOutCurvePower));
		return Mathf.Lerp(ZoomOutMinFactor, ZoomOutMaxFactor, curved);
	}

	private Vector2 GetMoveDirection()
	{
		float x = 0f;
		float y = 0f;

		if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D))
		{
			x += 1f;
		}

		if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A))
		{
			x -= 1f;
		}

		if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S))
		{
			y += 1f;
		}

		if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W))
		{
			y -= 1f;
		}

		var direction = new Vector2(x, y);
		return direction.LengthSquared() > 1f ? direction.Normalized() : direction;
	}

	private void UpdateMoveInertia(float dt)
	{
		var direction = GetMoveDirection();
		var speed = MoveSpeed;
		if (ScaleMoveSpeedByZoom && _mapView.ViewScale > 0.001f)
		{
			speed /= _mapView.ViewScale;
		}

		if (direction != Vector2.Zero)
		{
			var targetVelocity = direction * speed;
			_moveVelocity = _moveVelocity.MoveToward(targetVelocity, MoveAcceleration * dt);
		}
		else
		{
			_moveVelocity = _moveVelocity.MoveToward(Vector2.Zero, MoveDamping * dt);
		}

		if (_moveVelocity.LengthSquared() > 0.000001f)
		{
			_mapView.CameraMapOffset += _moveVelocity * dt;
		}
	}

	private void UpdateZoomInertia(float dt)
	{
		if (Mathf.Abs(_zoomVelocity) > 0.000001f)
		{
			var effectiveMin = GetEffectiveMinViewScale();
			var effectiveMax = Mathf.Max(MaxViewScale, effectiveMin);
			_targetViewScale = Mathf.Clamp(_targetViewScale + _zoomVelocity * dt, effectiveMin, effectiveMax);
			_zoomVelocity = Mathf.MoveToward(_zoomVelocity, 0f, ZoomDamping * dt);
		}
		else
		{
			_zoomVelocity = 0f;
		}

		if (!UseSmoothing)
		{
			_mapView.ViewScale = _targetViewScale;
			ApplyZoomFocus(_mapView.ViewScale);
		}
		else if (Mathf.Abs(_mapView.ViewScale - _targetViewScale) > 0.0001f && Mathf.Abs(_zoomVelocity) < 0.000001f)
		{
			_targetViewScale = _mapView.ViewScale;
		}
	}

	private void LoadControlSettings()
	{
		var config = new ConfigFile();
		if (config.Load(ConfigPath) != Error.Ok)
		{
			return;
		}

		UseSmoothing = ReadBool(config, "use_smoothing", UseSmoothing);
		EnableKeyboardPan = ReadBool(config, "enable_keyboard_pan", EnableKeyboardPan);
		ScaleMoveSpeedByZoom = ReadBool(config, "scale_move_speed_by_zoom", ScaleMoveSpeedByZoom);

		ZoomSpeed = Mathf.Max(0.1f, ReadFloat(config, "zoom_speed", ZoomSpeed));
		ZoomImpulse = Mathf.Max(0f, ReadFloat(config, "zoom_impulse", ZoomImpulse));
		ZoomDamping = Mathf.Max(0f, ReadFloat(config, "zoom_damping", ZoomDamping));
		MaxZoomVelocity = Mathf.Max(0.01f, ReadFloat(config, "max_zoom_velocity", MaxZoomVelocity));

		ZoomStep = Mathf.Max(0.001f, ReadFloat(config, "zoom_step", ZoomStep));
		FineZoomStep = Mathf.Max(0.001f, ReadFloat(config, "fine_zoom_step", FineZoomStep));
		CoarseZoomStep = Mathf.Max(0.001f, ReadFloat(config, "coarse_zoom_step", CoarseZoomStep));

		MoveSpeed = Mathf.Max(0f, ReadFloat(config, "move_speed", MoveSpeed));
		MoveAcceleration = Mathf.Max(0f, ReadFloat(config, "move_acceleration", MoveAcceleration));
		MoveDamping = Mathf.Max(0f, ReadFloat(config, "move_damping", MoveDamping));
	}

	private float ReadFloat(ConfigFile config, string key, float defaultValue)
	{
		if (!config.HasSectionKey(ConfigSection, key))
		{
			return defaultValue;
		}

		var value = config.GetValue(ConfigSection, key, defaultValue);
		var text = value.AsString();
		if (float.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (bool.TryParse(text, out var parsedBool))
		{
			return parsedBool ? 1f : 0f;
		}

		return defaultValue;
	}

	private bool ReadBool(ConfigFile config, string key, bool defaultValue)
	{
		if (!config.HasSectionKey(ConfigSection, key))
		{
			return defaultValue;
		}

		var value = config.GetValue(ConfigSection, key, defaultValue);
		var text = value.AsString();
		if (bool.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (int.TryParse(text, out var parsedInt))
		{
			return parsedInt != 0;
		}

		if (float.TryParse(text, out var parsedFloat))
		{
			return Mathf.Abs(parsedFloat) > 0.0001f;
		}

		return defaultValue;
	}

	private bool IsPointerOverMapView()
	{
		if (_mapView == null)
		{
			return true;
		}

		var viewport = GetViewport();
		if (viewport == null)
		{
			return true;
		}

		var pointerPos = viewport.GetMousePosition();
		if (!_mapView.GetGlobalRect().HasPoint(pointerPos))
		{
			return false;
		}

		var hovered = viewport.GuiGetHoveredControl();
		if (hovered == null)
		{
			return true;
		}

		if (hovered == _mapView || _mapView.IsAncestorOf(hovered) || hovered.IsAncestorOf(_mapView))
		{
			return true;
		}

		return hovered.MouseFilter != Control.MouseFilterEnum.Stop;
	}
	private float GetEffectiveMinViewScale()
	{
		if (_mapView == null)
		{
			return MinViewScale;
		}

		return Mathf.Max(MinViewScale, _mapView.GetMinViewScaleForArea());
	}

	private void CaptureZoomFocus()
	{
		if (_mapView == null)
		{
			return;
		}

		_zoomFocusScreenPos = _mapView.GetLocalMousePosition();
		_zoomFocusMapPos = _mapView.ScreenToMap(_zoomFocusScreenPos);
		_hasZoomFocus = true;
	}

	private void ApplyZoomFocus(float viewScale)
	{
		if (!_hasZoomFocus || _mapView == null)
		{
			return;
		}

		var newOffset = _mapView.GetCameraOffsetForZoomFocus(_zoomFocusMapPos, _zoomFocusScreenPos, viewScale);
		_mapView.CameraMapOffset = newOffset;

		if (Mathf.Abs(_zoomVelocity) < 0.000001f && Mathf.Abs(_mapView.ViewScale - _targetViewScale) < 0.0001f)
		{
			_hasZoomFocus = false;
		}
	}
}
