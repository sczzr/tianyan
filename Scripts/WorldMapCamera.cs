using Godot;
using System;

namespace TianYanShop
{
	/// <summary>
	/// 世界地图相机 - 支持WASD移动、鼠标拖拽、滚轮缩放
	/// 缩放时保持鼠标指向的地图坐标不变
	/// </summary>
	public partial class WorldMapCamera : Camera2D
	{
		[Export] public float MoveSpeed = 500.0f;
		[Export] public float FastMoveSpeed = 1000.0f;
		[Export] public float ZoomSpeed = 0.2f;        // 增加缩放速度，一次滚轮可以跨越较大范围
		[Export] public float MinZoom = 0.05f;
		[Export] public float MaxZoom = 1.5f;
		[Export] public bool EnableSmoothZoom = true;
		[Export] public float SmoothZoomSpeed = 13.0f;   // 如果启用平滑，也加快速度
		[Export] public int TileSize = 64;              // 瓦片尺寸，应与 WorldMapManager.TileSize 匹配

		// 地图边界
		private float _mapWidth = 0;
		private float _mapHeight = 0;
		private bool _hasMapBounds = false;

		// 目标缩放值 (用于平滑缩放)
		private Vector2 _targetZoom;

		// 鼠标状态
		private bool _isDragging = false;
		private Vector2 _dragStartPos;
		private Vector2 _dragStartCameraPos;

		// 缩放时保持鼠标指向的世界坐标不变
		private Vector2 _targetMouseWorldPos; // 缩放时要保持的目标世界坐标
		private Vector2 _zoomMouseScreenPos;  // 缩放时鼠标在屏幕上的位置
		private Vector2 _mouseWorldPosBeforeZoom; // 缩放前鼠标指向的世界坐标

		public override void _Ready()
		{
			// 启用相机2D
			Enabled = true;

			// 初始化缩放
			_targetZoom = Zoom;

			// 确保进程模式始终运行
			ProcessMode = ProcessModeEnum.Always;

			// 相机初始位置保持为 (0, 0)，地图从左上角开始显示
			// 不再自动设置为地图中心

			GD.Print("世界地图相机已初始化");
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
			Vector2 newPosition = GlobalPosition + movement;

			GlobalPosition = newPosition;
		}

		/// <summary>
		/// 处理鼠标按钮事件 (滚轮缩放)
		/// </summary>
		private void HandleMouseButton(InputEventMouseButton mouseButton)
		{
			// 滚轮缩放
			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				// 记录缩放前的鼠标世界坐标 (使用正确的转换)
				Vector2 mousePos = GetViewport().GetMousePosition();
				_mouseWorldPosBeforeZoom = GetMouseWorldPosition(mousePos);

				// 记录鼠标屏幕位置用于后续平滑缩放调整
				_zoomMouseScreenPos = mousePos;

				// 放大
				ZoomIn();
			}
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				// 记录缩放前的鼠标世界坐标 (使用正确的转换)
				Vector2 mousePos = GetViewport().GetMousePosition();
				_mouseWorldPosBeforeZoom = GetMouseWorldPosition(mousePos);

				// 记录鼠标屏幕位置用于后续平滑缩放调整
				_zoomMouseScreenPos = mousePos;

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
		private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
		{
            if (_isDragging)
            {
                Vector2 currentMousePos = GetViewport().GetMousePosition();
                Vector2 delta = _dragStartPos - currentMousePos;
                GlobalPosition = _dragStartCameraPos + delta / Zoom.X;
            }
            GD.Print(mouseMotion.Position);
		}

		/// <summary>
		/// 放大
		/// </summary>
		public void ZoomIn()
		{
			float newZoomX = Mathf.Min(Zoom.X + ZoomSpeed, MaxZoom);
			_targetZoom = new Vector2(newZoomX, newZoomX);

			if (!EnableSmoothZoom)
			{
				Zoom = _targetZoom;
				// 如果不是平滑缩放，在缩放后调整位置以保持鼠标指向的世界坐标不变
				if (_zoomMouseScreenPos != Vector2.Zero)
				{
					AdjustPositionForZoom(_zoomMouseScreenPos);
				}
			}
		}

		/// <summary>
		/// 缩小
		/// </summary>
		public void ZoomOut()
		{
			float newZoomX = Mathf.Max(Zoom.X - ZoomSpeed, MinZoom);
			_targetZoom = new Vector2(newZoomX, newZoomX);

			if (!EnableSmoothZoom)
			{
				Zoom = _targetZoom;
				// 如果不是平滑缩放，在缩放后调整位置以保持鼠标指向的世界坐标不变
				if (_zoomMouseScreenPos != Vector2.Zero)
				{
					AdjustPositionForZoom(_zoomMouseScreenPos);
				}
			}
		}

		/// <summary>
		/// 处理平滑缩放
		/// 关键：在每一帧中都根据当前 Zoom 值重新计算相机位置，保持鼠标指向坐标不变
		/// </summary>
		private void HandleSmoothZoom(float delta)
		{
			if (Zoom != _targetZoom)
			{
				// 记录上一帧的缩放值
				float prevZoom = Zoom.X;

				// 更新缩放值
				Zoom = Zoom.Lerp(_targetZoom, SmoothZoomSpeed * delta);

				// 接近目标时直接设置
				if (Zoom.DistanceTo(_targetZoom) < 0.001f)
				{
					Zoom = _targetZoom;
				}

				// 关键：根据缩放变化调整位置，保持鼠标指向坐标不变
				// 缩放比例变化时，鼠标相对视口中心的偏移对应的世界坐标会变化
				// 我们需要调整相机位置来补偿这个变化
				if (prevZoom != Zoom.X && _zoomMouseScreenPos != Vector2.Zero)
				{
					AdjustPositionDuringZoom(prevZoom, Zoom.X);
				}
			}
		}

		/// <summary>
		/// 在平滑缩放过程中调整相机位置
		/// 保持缩放前鼠标指向的世界坐标不变
		/// </summary>
		private void AdjustPositionDuringZoom(float prevZoom, float currentZoom)
		{
			// 获取当前鼠标屏幕位置
			Vector2 mouseScreenPos = _zoomMouseScreenPos;

			// 计算视口中心
			Vector2 viewportCenter = GetViewportRect().Size / 2;

			// 计算鼠标相对于视口中心的偏移
			Vector2 offsetFromCenter = mouseScreenPos - viewportCenter;

			// 为了保持鼠标指向的世界坐标不变：
			// 缩放前：worldPos = GlobalPosition + offsetFromCenter / prevZoom
			// 缩放后：worldPos = newGlobalPosition + offsetFromCenter / currentZoom
			// 因此：newGlobalPosition = GlobalPosition + offsetFromCenter * (1/prevZoom - 1/currentZoom)
			Vector2 positionDelta = offsetFromCenter * (1.0f / prevZoom - 1.0f / currentZoom);

			// 应用位置调整
			GlobalPosition += positionDelta;
		}

		/// <summary>
		/// 调整相机位置以保持缩放时鼠标指向的坐标不变
		/// 核心思想：缩放前后，鼠标指向的世界坐标应该相同
		///
		/// 世界坐标 = 相机位置 + (屏幕坐标 - 视口中心) / 缩放
		/// 为了保持世界坐标不变：
		/// cameraPos1 + (mouseScreen - viewportCenter) / zoom1 =
		/// cameraPos2 + (mouseScreen - viewportCenter) / zoom2
		/// </summary>
		private void AdjustPositionForZoom(Vector2 mouseScreenPos)
		{
			Vector2 viewportCenter = GetViewportRect().Size / 2;

			// 计算鼠标相对于视口中心的偏移
			Vector2 offsetFromCenter = mouseScreenPos - viewportCenter;

			// 为了保持鼠标指向的世界坐标不变：
			// 缩放前：worldPos = GlobalPosition + offsetFromCenter / Zoom.X
			// 缩放后：worldPos = newGlobalPosition + offsetFromCenter / _targetZoom.X
			// 因此：newGlobalPosition = GlobalPosition + offsetFromCenter * (1/Zoom.X - 1/_targetZoom.X)
			Vector2 positionDelta = offsetFromCenter * (1.0f / Zoom.X - 1.0f / _targetZoom.X);

			// 应用位置调整
			GlobalPosition += positionDelta;
		}

		/// <summary>
		/// 设置地图边界
		/// </summary>
		public void SetMapBounds(float width, float height)
		{
			_mapWidth = width;
			_mapHeight = height;
			_hasMapBounds = true;

			// 相机位置保持不变，地图从左上角开始显示
		}

		/// <summary>
		/// 应用边界限制
		/// </summary>
		private void ApplyBounds()
		{
			// 计算允许的位置范围
			// 相机中心可以在 0 到 mapWidth/mapHeight 之间自由移动
			// 添加少量边距允许轻微超出
			float borderPadding = 100.0f / Zoom.X;
			float minX = -borderPadding;
			float maxX = _mapWidth + borderPadding;
			float minY = -borderPadding;
			float maxY = _mapHeight + borderPadding;
			// 应用限制
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
			Vector2 viewportSize = GetViewportRect().Size;
			GlobalPosition = viewportSize / 2.0f / Zoom.X;
		}

		/// <summary>
		/// 将屏幕坐标转换为世界坐标
		/// </summary>
		public Vector2 ScreenToWorld(Vector2 screenPos)
		{
			// 世界坐标 = 相机位置 + (屏幕坐标坐标 - 视口中心) / 缩放
			Vector2 viewportCenter = GetViewportRect().Size / 2;
			return GlobalPosition + (screenPos - viewportCenter) / Zoom.X;
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
		/// </summary>
		private Vector2 GetMouseWorldPosition(Vector2 mouseScreenPos)
		{
			// 世界坐标 = 相机位置 + (屏幕坐标 - 视口中心) / 缩放
			Vector2 viewportCenter = GetViewportRect().Size / 2;
			return GlobalPosition + (mouseScreenPos - viewportCenter) / Zoom.X;
		}
	}
}
