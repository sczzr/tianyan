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
        [Export] public float ZoomSpeed = 0.1f;
        [Export] public float MinZoom = 0.2f;
        [Export] public float MaxZoom = 3.0f;
        [Export] public bool EnableSmoothZoom = true;
        [Export] public float SmoothZoomSpeed = 10.0f;

        // 地图边界
        private float _mapWidth = 8192;
        private float _mapHeight = 8192;
        private bool _hasBounds = false;

        // 目标缩放值 (用于平滑缩放)
        private Vector2 _targetZoom;

        // 鼠标状态
        private bool _isDragging = false;
        private Vector2 _dragStartPos;
        private Vector2 _dragStartCameraPos;

        // 鼠标在世界中的位置 (用于缩放时保持指向)
        private Vector2 _mouseWorldPosBeforeZoom;

        public override void _Ready()
        {
            // 启用相机2D
            Enabled = true;

            // 初始化缩放
            _targetZoom = Zoom;

            // 确保进程模式始终运行
            ProcessMode = ProcessModeEnum.Always;

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

            // 应用边界限制
            if (_hasBounds)
            {
                ApplyBounds();
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
            Position += direction * speed * delta / Zoom.X;
        }

        /// <summary>
        /// 处理鼠标按钮事件 (滚轮缩放)
        /// </summary>
        private void HandleMouseButton(InputEventMouseButton mouseButton)
        {
            // 滚轮缩放
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                // 记录缩放前的鼠标世界坐标
                Vector2 mousePos = GetViewport().GetMousePosition();
                _mouseWorldPosBeforeZoom = ToGlobal(mousePos);

                // 放大
                ZoomIn();

                // 调整位置保持鼠标指向的坐标不变
                AdjustPositionForZoom(mousePos);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                // 记录缩放前的鼠标世界坐标
                Vector2 mousePos = GetViewport().GetMousePosition();
                _mouseWorldPosBeforeZoom = ToGlobal(mousePos);

                // 缩小
                ZoomOut();

                // 调整位置保持鼠标指向的坐标不变
                AdjustPositionForZoom(mousePos);
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
            }
        }

        /// <summary>
        /// 处理平滑缩放
        /// </summary>
        private void HandleSmoothZoom(float delta)
        {
            if (Zoom != _targetZoom)
            {
                Zoom = Zoom.Lerp(_targetZoom, SmoothZoomSpeed * delta);

                // 接近目标时直接设置
                if (Zoom.DistanceTo(_targetZoom) < 0.001f)
                {
                    Zoom = _targetZoom;
                }
            }
        }

        /// <summary>
        /// 调整相机位置以保持缩放时鼠标指向的坐标不变
        /// 核心算法：newPos = mouseWorldPos - (mouseScreenPos - newPos) / newZoom
        /// 推导：我们希望 mouseWorldPos 保持不变
        /// 即: (mouseScreenPos - newPos) / newZoom = (mouseScreenPos - oldPos) / oldZoom
        /// </summary>
        private void AdjustPositionForZoom(Vector2 mouseScreenPos)
        {
            // 计算新的相机位置，使得鼠标指向的世界坐标保持不变
            // newPos = mouseWorldPos * newZoom - mouseScreenPos
            // 但这需要反向思考...

            // 正确的公式：
            // 我们希望: (mouseScreenPos - newPos) / Zoom = mouseWorldPos
            // 因此: newPos = mouseScreenPos - mouseWorldPos * Zoom

            Vector2 newPos = mouseScreenPos - _mouseWorldPosBeforeZoom * Zoom.X;

            // 添加平滑插值使缩放更自然
            GlobalPosition = newPos;
        }

        /// <summary>
        /// 设置地图边界
        /// </summary>
        public void SetMapBounds(float width, float height)
        {
            _mapWidth = width;
            _mapHeight = height;
            _hasBounds = true;
        }

        /// <summary>
        /// 应用边界限制
        /// </summary>
        private void ApplyBounds()
        {
            Vector2 viewportSize = GetViewportRect().Size;

            // 计算可见区域在世界坐标中的大小
            Vector2 visibleWorldSize = viewportSize / Zoom.X;

            // 计算允许的位置范围
            float minX = visibleWorldSize.X / 2 - 100; // 允许稍微超出边界
            float maxX = _mapWidth - visibleWorldSize.X / 2 + 100;
            float minY = visibleWorldSize.Y / 2 - 100;
            float maxY = _mapHeight - visibleWorldSize.Y / 2 + 100;

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
        /// 将屏幕坐标转换为世界坐标
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return ToGlobal(screenPos);
        }

        /// <summary>
        /// 将世界坐标转换为地图瓦片坐标
        /// </summary>
        public Vector2I WorldToTile(Vector2 worldPos)
        {
            return new Vector2I(
                Mathf.FloorToInt(worldPos.X / 32),
                Mathf.FloorToInt(worldPos.Y / 32)
            );
        }
    }
}
