using Godot;

namespace TianYanShop
{
	/// <summary>
	/// 世界地图相机 - 支持WASD移动、鼠标拖拽、滚轮缩放
	/// 使用 anchor_mode = Fixed Top Left
	/// 缩放时保持鼠标（屏幕坐标）指向的世界坐标不变
	/// MainMap 的坐标始终相对于左上角不变，只改变相机视角
	/// </summary>
	public partial class WorldMapCamera : Camera2D
	{
		[Export] public float MoveSpeed = 500.0f;
		[Export] public float FastMoveSpeed = 1000.0f;
		[Export] public float ZoomSpeed = 0.2f;
		[Export] public float MinZoom = 0.05f;
		[Export] public float MaxZoom = 1.5f;
		[Export] public bool EnableSmoothZoom = true;
		[Export] public float SmoothZoomSpeed = 13.0f;
		[Export] public int TileSize = 64;

		// 地图边界
		private float _mapWidth = 0;
		private float _mapHeight = 0;
		private bool _hasMapBounds = false;

		// 目标缩放值 (用于平滑缩放)
		private Vector2 _targetZoom;

		// 鼠标拖拽状态
		private bool _isDragging = false;
		private Vector2 _dragStartPos;
		private Vector2 _dragStartCameraPos;

		// 缩放时保持鼠标指向的单元格位置不变
		private Vector2 _zoomMouseScreenPos;
		private Vector2 _cameraPosBeforeZoom;
		private Vector2I _zoomTargetTile;      // 缩放前鼠标指向的单元格坐标
		private Vector2 _zoomTileOffset;       // 鼠标在单元格内的偏移 (0~TileSize)

		public override void _Ready()
		{
			// 确保 anchor_mode = Fixed Top Left (0)
			AnchorMode = AnchorModeEnum.FixedTopLeft;

			// 启用相机2D
			Enabled = true;

			// 初始化缩放
			_targetZoom = Zoom;

			// 确保进程模式始终运行
			ProcessMode = ProcessModeEnum.Always;

			GD.Print("世界地图相机已初始化 (Fixed Top Left 模式)");
		}

		public override void _Process(double delta)
		{
			float dt = (float)delta;

			// 处理移动
			HandleMovement(dt);

			// 处理平滑缩放
			if (EnableSmoothZoom)
			{
				HandleSmoothZoom(dt);
			}
		}

		public override void _Input(InputEvent @event)
		{
			// 处理鼠标滚轮缩放
			if (@event is InputEventMouseButton mouseButton)
			{
				HandleMouseButton(mouseButton);
			}

			// 处理鼠标拖拽
			if (@event is InputEventMouseMotion mouseMotion)
			{
				HandleMouseMotion(mouseMotion);
			}
		}

		/// <summary>
		/// 处理 WASD 移动
		/// 在 Fixed Top Left 模式下：GlobalPosition 表示屏幕左上角显示的世界坐标
		/// </summary>
		private void HandleMovement(float delta)
		{
			Vector2 direction = Vector2.Zero;

			// WASD 移动
			if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A))
				direction.X -= 1;
			if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D))
				direction.X += 1;
			if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W))
				direction.Y -= 1;
			if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S))
				direction.Y += 1;

			// 归一化方向
			if (direction != Vector2.Zero)
			{
				direction = direction.Normalized();
			}

			// 检查是否加速
			float speed = Input.IsKeyPressed(Key.Shift) ? FastMoveSpeed : MoveSpeed;

			// 应用移动 (考虑缩放级别)
			Vector2 movement = direction * speed * delta / Zoom.X;
			GlobalPosition += movement;
		}

		/// <summary>
		/// 处理鼠标按钮事件 (滚轮缩放)
		/// </summary>
		private void HandleMouseButton(InputEventMouseButton mouseButton)
		{
			// 滚轮缩放
			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				// 记录缩放前的状态
				Vector2 mousePos = GetViewport().GetMousePosition();
				_zoomMouseScreenPos = mousePos;
				_cameraPosBeforeZoom = GlobalPosition;
				RecordZoomTargetTile(mousePos);

				// 放大
				ZoomIn();
			}
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				// 记录缩放前的状态
				Vector2 mousePos = GetViewport().GetMousePosition();
				_zoomMouseScreenPos = mousePos;
				_cameraPosBeforeZoom = GlobalPosition;
				RecordZoomTargetTile(mousePos);

				// 缩小
				ZoomOut();
			}

			// 中键拖拽
			if (mouseButton.ButtonIndex == MouseButton.Middle)
			{
				if (mouseButton.Pressed)
				{
					_isDragging = true;
					_dragStartPos = GetViewport().GetMousePosition();
					_dragStartCameraPos = GlobalPosition;
				}
				else
				{
					_isDragging = false;
				}
			}
		}

		/// <summary>
		/// 处理鼠标移动事件 (拖拽)
		/// </summary>
		private void HandleMouseMotion(InputEventMouseMotion _)
		{
			if (_isDragging)
			{
				Vector2 currentMousePos = GetViewport().GetMousePosition();
				Vector2 delta = _dragStartPos - currentMousePos;
				// 在 Fixed Top Left 模式下，拖拽方向就是相机移动方向
				GlobalPosition = _dragStartCameraPos + delta / Zoom.X;
			}
		}

		/// <summary>
		/// 放大
		/// </summary>
		public void ZoomIn()
		{
			float prevZoom = Zoom.X;
			float newZoomX = Mathf.Min(prevZoom + ZoomSpeed, MaxZoom);
			_targetZoom = new Vector2(newZoomX, newZoomX);

			if (!EnableSmoothZoom)
			{
				Zoom = _targetZoom;
				if (_zoomMouseScreenPos != Vector2.Zero)
				{
					AdjustPositionForZoom(_zoomMouseScreenPos, prevZoom, newZoomX);
				}
			}
		}

		/// <summary>
		/// 缩小
		/// </summary>
		public void ZoomOut()
		{
			float prevZoom = Zoom.X;
			float newZoomX = Mathf.Max(prevZoom - ZoomSpeed, MinZoom);
			_targetZoom = new Vector2(newZoomX, newZoomX);

			if (!EnableSmoothZoom)
			{
				Zoom = _targetZoom;
				if (_zoomMouseScreenPos != Vector2.Zero)
				{
					AdjustPositionForZoom(_zoomMouseScreenPos, prevZoom, newZoomX);
				}
			}
		}

		/// <summary>
		/// 处理平滑缩放
		/// </summary>
		private void HandleSmoothZoom(float delta)
		{
			if (Zoom != _targetZoom)
			{
				float prevZoom = Zoom.X;

				// 更新缩放值
				Zoom = Zoom.Lerp(_targetZoom, SmoothZoomSpeed * delta);

				// 接近目标时直接设置
				if (Zoom.DistanceTo(_targetZoom) < 0.001f)
				{
					Zoom = _targetZoom;
				}

				// 根据缩放变化调整位置，保持鼠标指向坐标不变
				if (prevZoom != Zoom.X && _zoomMouseScreenPos != Vector2.Zero)
				{
					AdjustPositionForZoom(_zoomMouseScreenPos, prevZoom, Zoom.X);
				}
			}
		}

		/// <summary>
		/// 记录缩放前鼠标指向的单元格位置
		/// </summary>
		private void RecordZoomTargetTile(Vector2 mouseScreenPos)
		{
			// 计算鼠标指向的世界坐标
			Vector2 worldPos = GlobalPosition + mouseScreenPos / Zoom.X;
			// 记录单元格坐标（左上角的世界坐标）
			Vector2 tileWorldPos = new Vector2(
				Mathf.Floor(worldPos.X / TileSize) * TileSize,
				Mathf.Floor(worldPos.Y / TileSize) * TileSize
			);
			_zoomTargetTile = new Vector2I((int)tileWorldPos.X / TileSize, (int)tileWorldPos.Y / TileSize);
			// 记录鼠标在单元格内的偏移
			_zoomTileOffset = worldPos - tileWorldPos;
		}

		/// <summary>
		/// 调整相机位置以保持缩放时鼠标指向的单元格位置不变
		///
		/// 在 Fixed Top Left 模式下：
		/// 目标：缩放后，鼠标指向的单元格与单元格内偏移点不变
		/// worldPos = tileWorldPosTopLeft + offset
		/// worldPos = cameraPos + screenPos / zoom
		/// </summary>
		private void AdjustPositionForZoom(Vector2 mouseScreenPos, float prevZoom, float currentZoom)
		{
			// 计算目标单元格的世界坐标（左上角）
			Vector2 targetTileWorldPos = new Vector2(_zoomTargetTile.X * TileSize, _zoomTargetTile.Y * TileSize);
			// 目标鼠标应该指向的世界坐标 = 单元格左上角 + 偏移
			Vector2 targetMouseWorldPos = targetTileWorldPos + _zoomTileOffset;
			// 计算需要的相机位置：targetMouseWorldPos = cameraPos + mouseScreenPos / currentZoom
			// => cameraPos = targetMouseWorldPos - mouseScreenPos / currentZoom
			Vector2 newCameraPos = targetMouseWorldPos - mouseScreenPos / currentZoom;
			GlobalPosition = newCameraPos;
		}

		/// <summary>
		/// 设置地图边界
		/// </summary>
		public void SetMapBounds(float width, float height)
		{
			_mapWidth = width;
			_mapHeight = height;
			_hasMapBounds = true;
		}

		/// <summary>
		/// 应用边界限制
		/// </summary>
		private void ApplyBounds()
		{
			float borderPadding = 100.0f / Zoom.X;
			float minX = -borderPadding;
			float maxX = _mapWidth + borderPadding;
			float minY = -borderPadding;
			float maxY = _mapHeight + borderPadding;

			GlobalPosition = new Vector2(
				Mathf.Clamp(GlobalPosition.X, minX, maxX),
				Mathf.Clamp(GlobalPosition.Y, minY, maxY)
			);
		}

		/// <summary>
		/// 将相机移动到指定位置
		/// </summary>
		public void CenterOnPosition(Vector2 worldPos)
		{
			GlobalPosition = worldPos;
		}

		/// <summary>
		/// 将世界坐标的左上角 (0,0) 对齐到屏幕左上角
		/// </summary>
		public void AlignTopLeftToZero()
		{
			GlobalPosition = Vector2.Zero;
		}

		/// <summary>
		/// 将屏幕坐标转换为世界坐标
		/// Fixed Top Left 模式：worldPos = cameraPos + screenPos / zoom
		/// </summary>
		public Vector2 ScreenToWorld(Vector2 screenPos)
		{
			return GlobalPosition + screenPos / Zoom.X;
		}

		/// <summary>
		/// 将世界坐标转换为屏幕坐标
		/// Fixed Top Left 模式：screenPos = (worldPos - cameraPos) * zoom
		/// </summary>
		public Vector2 WorldToScreen(Vector2 worldPos)
		{
			return (worldPos - GlobalPosition) * Zoom.X;
		}

		/// <summary>
		/// 将世界坐标转换为地图瓦片坐标
		/// </summary>
		public Vector2I WorldToTile(Vector2 worldPos)
		{
			return new Vector2I(
				Mathf.FloorToInt(worldPos.X / TileSize),
				Mathf.FloorToInt(worldPos.Y / TileSize)
			);
		}

		/// <summary>
		/// 获取鼠标在世界中的坐标
		/// Fixed Top Left 模式：worldPos = cameraPos + mouseScreenPos / zoom
		/// </summary>
		public Vector2 GetMouseWorldPosition()
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			return GlobalPosition + mousePos / Zoom.X;
		}
	}
}
