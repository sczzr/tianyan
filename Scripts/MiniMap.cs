using Godot;
using System;

namespace TianYanShop
{
	/// <summary>
	/// 小地图系统 - 显示整个地图的缩略图，并标记当前视野范围
	/// </summary>
	public partial class MiniMap : Control
	{
		[Export] public float MiniMapScale = 0.15f;          // 小地图缩放比例
		[Export] public int MiniMapWidth = 80;              // 小地图宽度
		[Export] public int MiniMapHeight = 80;             // 小地图高度
		[Export] public Color MiniMapBackgroundColor = Colors.DarkGray; // 背景色
		[Export] public Color ViewportIndicatorColor = Colors.Red;       // 视野指示器颜色
		[Export] public Color ViewportBorderColor = Colors.White;        // 视野边框颜色
		
		private TextureRect _miniMapTexture;                 // 小地图纹理显示控件
		private ColorRect _viewportIndicator;                // 表示当前视野范围的矩形
		private Camera2D _mainCamera;                        // 主相机引用
		private Texture2D _mapTexture;                       // 地图纹理（可选）
		private Viewport _miniMapViewPort;                   // 用于渲染小地图的视口

		// 地图边界信息
		private float _mapWidth = 80;
		private float _mapHeight = 80;
		private bool _hasMapBounds = false;
		
		public override void _Ready()
		{
			InitializeMiniMap();
		}

		public override void _Process(double delta)
		{
			UpdateViewportIndicator();
		}

		/// <summary>
		/// 初始化小地图界面元素
		/// </summary>
		private void InitializeMiniMap()
		{
			// 设置锚点为左上角
			AnchorLeft = 0.0f;
			AnchorTop = 0.0f;
			AnchorRight = 0.0f;
			AnchorBottom = 0.0f;

			// 设置位置和大小（左上角，带有一些边距）
			var size = new Vector2(MiniMapWidth, MiniMapHeight);
			var position = new Vector2(20, 20);
			CustomMinimumSize = size;
			Size = size;
			Position = position;

			// 创建背景色块
			var background = new ColorRect();
			background.Color = MiniMapBackgroundColor;
			background.Size = size;
			background.AnchorLeft = 0.0f;
			background.AnchorTop = 0.0f;
			background.AnchorRight = 1.0f;
			background.AnchorBottom = 1.0f;
			background.OffsetLeft = 0.0f;
			background.OffsetTop = 0.0f;
			background.OffsetRight = 0.0f;
			background.OffsetBottom = 0.0f;
			AddChild(background);

			// 创建小地图纹理显示控件
			_miniMapTexture = new TextureRect();
			_miniMapTexture.Size = size;
			_miniMapTexture.AnchorLeft = 0.0f;
			_miniMapTexture.AnchorTop = 0.0f;
			_miniMapTexture.AnchorRight = 1.0f;
			_miniMapTexture.AnchorBottom = 1.0f;
			_miniMapTexture.OffsetLeft = 0.0f;
			_miniMapTexture.OffsetTop = 0.0f;
			_miniMapTexture.OffsetRight = 0.0f;
			_miniMapTexture.OffsetBottom = 0.0f;
			_miniMapTexture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
			AddChild(_miniMapTexture);

			// 创建视野指示器容器
			_viewportIndicator = new ColorRect();
			_viewportIndicator.Color = new Color(ViewportIndicatorColor, 0.3f); // 半透明
			AddChild(_viewportIndicator);

			// 创建边框（四个细长的ColorRect组成）
			CreateViewportBorder();

			// 查找主相机
			FindMainCamera();
		}

		/// <summary>
		/// 设置主相机
		/// </summary>
		public void SetMainCamera(Camera2D camera)
		{
			_mainCamera = camera;
		}

		/// <summary>
		/// 查找主相机
		/// </summary>
		private void FindMainCamera()
		{
			// 如果已经设置了主相机，则直接返回
			if (_mainCamera != null)
			{
				return;
			}

			// 尝试从 WorldMapManager 获取相机引用
			var worldMapManager = GetTree().Root.GetNodeOrNull<WorldMapManager>("WorldMapScene/MainMap");
			if (worldMapManager != null)
			{
				_mainCamera = worldMapManager.MapCamera;
				if (_mainCamera != null)
				{
					return;
				}
			}

			// 如果没找到，遍历场景树寻找
			_mainCamera = FindCameraInTree(GetTree().Root);
		}

		/// <summary>
		/// 在场景树中递归查找相机
		/// </summary>
		private Camera2D FindCameraInTree(Node node)
		{
			if (node is Camera2D camera)
			{
				return camera;
			}

			foreach (Node child in node.GetChildren())
			{
				Camera2D foundCamera = FindCameraInTree(child);
				if (foundCamera != null)
				{
					return foundCamera;
				}
			}

			return null;
		}

		/// <summary>
		/// 更新视野指示器位置和大小
		/// </summary>
		private void UpdateViewportIndicator()
		{
			if (_mainCamera == null)
			{
				FindMainCamera();
				return;
			}

			// 获取主视口信息
			Viewport mainViewport = GetViewport();
			if (mainViewport == null) return;

			// 获取主视口的实际尺寸
			var visibleRect = mainViewport.GetVisibleRect();
			Vector2 viewportSize = visibleRect.Size;

			if (!_hasMapBounds || _mapWidth <= 0 || _mapHeight <= 0) return;

			// 计算小地图缩放比例（小地图尺寸 / 实际地图尺寸）
			float miniMapScaleX = MiniMapWidth / _mapWidth;
			float miniMapScaleY = MiniMapHeight / _mapHeight;

			// 视野在小地图上的尺寸 = 视口尺寸 / 相机缩放 * 小地图缩放比例
			// 缩放倍数越大，视野越小 → 指示器越小
			float cameraZoom = _mainCamera.Zoom.X;
			float viewWidthInMiniMap = viewportSize.X / cameraZoom * miniMapScaleX;
			float viewHeightInMiniMap = viewportSize.Y / cameraZoom * miniMapScaleY;

			// 计算相机在地图中的相对位置（0-1之间）
			float cameraRelativeX = Mathf.Clamp(_mainCamera.GlobalPosition.X / _mapWidth, 0, 1);
			float cameraRelativeY = Mathf.Clamp(_mainCamera.GlobalPosition.Y / _mapHeight, 0, 1);

			// 计算视野指示器在小地图上的位置（中心对齐）
			float indicatorX = (cameraRelativeX * MiniMapWidth) - (viewWidthInMiniMap / 2);
			float indicatorY = (cameraRelativeY * MiniMapHeight) - (viewHeightInMiniMap / 2);

			// 确保视野指示器在小地图范围内
			indicatorX = Mathf.Clamp(indicatorX, 0, MiniMapWidth - viewWidthInMiniMap);
			indicatorY = Mathf.Clamp(indicatorY, 0, MiniMapHeight - viewHeightInMiniMap);

			// 应用位置和大小
			_viewportIndicator.Position = new Vector2(indicatorX, indicatorY);
			_viewportIndicator.Size = new Vector2(viewWidthInMiniMap, viewHeightInMiniMap);

			// 更新边框
			UpdateViewportBorder();
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
		/// 创建视野指示器边框
		/// </summary>
		private void CreateViewportBorder()
		{
			// 创建四个边框线
			// 顶边
			var topBorder = new ColorRect();
			topBorder.Color = ViewportBorderColor;
			topBorder.Name = "TopBorder";
			AddChild(topBorder);

			// 底边
			var bottomBorder = new ColorRect();
			bottomBorder.Color = ViewportBorderColor;
			bottomBorder.Name = "BottomBorder";
			AddChild(bottomBorder);

			// 左边
			var leftBorder = new ColorRect();
			leftBorder.Color = ViewportBorderColor;
			leftBorder.Name = "LeftBorder";
			AddChild(leftBorder);

			// 右边
			var rightBorder = new ColorRect();
			rightBorder.Color = ViewportBorderColor;
			rightBorder.Name = "RightBorder";
			AddChild(rightBorder);
		}

		/// <summary>
		/// 更新边框位置和大小
		/// </summary>
		private void UpdateViewportBorder()
		{
			if (_viewportIndicator == null) return;

			float borderWidth = 1; // 边框宽度
			Vector2 pos = _viewportIndicator.Position;
			Vector2 size = _viewportIndicator.Size;

			// 更新四个边框的位置和大小
			var topBorder = GetNode<ColorRect>("TopBorder");
			if (topBorder != null)
			{
				topBorder.Position = new Vector2(pos.X, pos.Y);
				topBorder.Size = new Vector2(size.X, borderWidth);
			}

			var bottomBorder = GetNode<ColorRect>("BottomBorder");
			if (bottomBorder != null)
			{
				bottomBorder.Position = new Vector2(pos.X, pos.Y + size.Y - borderWidth);
				bottomBorder.Size = new Vector2(size.X, borderWidth);
			}

			var leftBorder = GetNode<ColorRect>("LeftBorder");
			if (leftBorder != null)
			{
				leftBorder.Position = new Vector2(pos.X, pos.Y);
				leftBorder.Size = new Vector2(borderWidth, size.Y);
			}

			var rightBorder = GetNode<ColorRect>("RightBorder");
			if (rightBorder != null)
			{
				rightBorder.Position = new Vector2(pos.X + size.X - borderWidth, pos.Y);
				rightBorder.Size = new Vector2(borderWidth, size.Y);
			}
		}

		/// <summary>
		/// 设置小地图纹理
		/// </summary>
		public void SetMapTexture(Texture2D texture)
		{
			_mapTexture = texture;
			_miniMapTexture.Texture = texture;
		}

		/// <summary>
		/// 设置小地图视口（用于渲染整个地图的缩略图）
		/// </summary>
		public void SetMiniMapViewPort(Viewport mapViewPort)
		{
			_miniMapViewPort = mapViewPort;

			// 创建一个ViewportTexture来显示地图视口的内容
			if (_miniMapViewPort != null)
			{
				var viewportTexture = new ViewportTexture();
				viewportTexture.ViewportPath = _miniMapViewPort.GetPath();

				// 设置纹理到TextureRect
				_miniMapTexture.Texture = viewportTexture;
			}
		}

		/// <summary>
		/// 处理小地图点击事件 - 点击小地图任意位置跳转到对应的大地图位置
		/// </summary>
		public override void _GuiInput(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
			{
				// 获取点击位置相对于小地图的位置
				Vector2 localClickPos = mouseEvent.Position;
				
				// 计算点击位置在地图上的相对坐标 (0-1)
				float relativeX = Mathf.Clamp(localClickPos.X / Size.X, 0, 1);
				float relativeY = Mathf.Clamp(localClickPos.Y / Size.Y, 0, 1);
				
				// 转换为实际地图坐标
				Vector2 mapPosition = new Vector2(relativeX * _mapWidth, relativeY * _mapHeight);
				
				// 通知主相机跳转到该位置
				if (_mainCamera != null)
				{
					_mainCamera.GlobalPosition = mapPosition;
				}
				
			}
		}
	}
}
