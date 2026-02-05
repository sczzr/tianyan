using Godot;
using System;
using System.Collections.Generic;
using TianYanShop.World.Sect;

namespace TianYanShop.World.Layer
{
    /// <summary>
    /// 宗门可视化图层 - 在地图上显示宗门势力范围
    /// </summary>
    public partial class SectMaskLayer : Node2D
    {
        // 势力范围显示配置
        private const float Opacity = 0.4f;
        private const float BorderOpacity = 0.7f;
        private const int BorderWidth = 2;
        
        // 宗门势力范围颜色（基于宗门类型）
        private readonly Dictionary<SectType, Color> _sectTypeColors = new()
        {
            { SectType.Sword, new Color(0.9f, 0.3f, 0.3f) },      // 剑修 - 红色
            { SectType.Spell, new Color(0.3f, 0.6f, 0.9f) },      // 法修 - 蓝色
            { SectType.Alchemy, new Color(0.9f, 0.7f, 0.3f) },    // 丹修 - 黄色
            { SectType.Artifact, new Color(0.7f, 0.7f, 0.7f) },   // 器修 - 灰色
            { SectType.Beast, new Color(0.4f, 0.8f, 0.4f) },      // 兽修 - 绿色
            { SectType.Formation, new Color(0.8f, 0.4f, 0.8f) },  // 阵修 - 紫色
            { SectType.Body, new Color(0.9f, 0.5f, 0.2f) },       // 体修 - 橙色
            { SectType.Ghost, new Color(0.3f, 0.3f, 0.3f) },      // 鬼修 - 深灰
            { SectType.Puppet, new Color(0.6f, 0.4f, 0.2f) },     // 傀儡 - 棕色
            { SectType.Music, new Color(0.5f, 0.8f, 0.9f) },      // 音修 - 青色
            { SectType.Mixed, new Color(0.5f, 0.5f, 0.5f) }       // 杂修 - 灰白
        };

        // 宗门等级大小配置
        private readonly Dictionary<SectLevel, SectDisplayConfig> _levelConfigs = new()
        {
            { SectLevel.Top, new SectDisplayConfig(15, 3.0f, true) },    // 顶级：半径15，图标3倍，显示名称
            { SectLevel.Large, new SectDisplayConfig(8, 2.0f, true) },   // 大型：半径8，图标2倍，显示名称
            { SectLevel.Small, new SectDisplayConfig(4, 1.0f, false) }   // 小型：半径4，图标1倍，不显示名称
        };

        private List<SectData> _sectDataList = new();
        private int _tileSize = 32;
        private Node2D _territoryContainer;
        private Node2D _iconContainer;
        private Node2D _labelContainer;

        public override void _Ready()
        {
            SetupContainers();
        }

        private void SetupContainers()
        {
            // 势力范围容器
            _territoryContainer = new Node2D();
            _territoryContainer.Name = "TerritoryContainer";
            AddChild(_territoryContainer);

            // 图标容器
            _iconContainer = new Node2D();
            _iconContainer.Name = "IconContainer";
            AddChild(_iconContainer);

            // 标签容器
            _labelContainer = new Node2D();
            _labelContainer.Name = "LabelContainer";
            AddChild(_labelContainer);
        }

        /// <summary>
        /// 更新宗门数据并刷新显示
        /// </summary>
        /// <param name="sects">宗门数据列表</param>
        /// <param name="tileSize">瓦片大小</param>
        public void UpdateSectData(List<SectData> sects, int tileSize)
        {
            _sectDataList = sects ?? new List<SectData>();
            _tileSize = tileSize;
            
            ClearDisplay();
            RenderSects();
        }

        /// <summary>
        /// 清空当前显示
        /// </summary>
        private void ClearDisplay()
        {
            // 清除所有子节点
            foreach (Node child in _territoryContainer.GetChildren())
            {
                child.QueueFree();
            }
            foreach (Node child in _iconContainer.GetChildren())
            {
                child.QueueFree();
            }
            foreach (Node child in _labelContainer.GetChildren())
            {
                child.QueueFree();
            }
        }

        /// <summary>
        /// 渲染所有宗门
        /// </summary>
        private void RenderSects()
        {
            // 按等级分组渲染（先渲染低级宗门，高级宗门在上面）
            var smallSects = _sectDataList.FindAll(s => s.Level == SectLevel.Small);
            var largeSects = _sectDataList.FindAll(s => s.Level == SectLevel.Large);
            var topSects = _sectDataList.FindAll(s => s.Level == SectLevel.Top);

            // 先渲染小型宗门
            foreach (var sect in smallSects)
            {
                RenderSect(sect);
            }

            // 再渲染大型宗门
            foreach (var sect in largeSects)
            {
                RenderSect(sect);
            }

            // 最后渲染顶级宗门
            foreach (var sect in topSects)
            {
                RenderSect(sect);
            }
        }

        /// <summary>
        /// 渲染单个宗门
        /// </summary>
        private void RenderSect(SectData sect)
        {
            if (!_levelConfigs.TryGetValue(sect.Level, out var config))
                return;

            // 获取颜色
            Color baseColor = GetSectColor(sect);
            Color fillColor = new Color(baseColor.R, baseColor.G, baseColor.B, Opacity);
            Color borderColor = new Color(baseColor.R, baseColor.G, baseColor.B, BorderOpacity);

            // 计算世界坐标
            Vector2 centerWorldPos = new Vector2(
                sect.CenterPosition.X * _tileSize + _tileSize / 2f,
                sect.CenterPosition.Y * _tileSize + _tileSize / 2f
            );

            // 使用宗门的实际势力范围半径（像素）
            float radiusPixels = sect.InfluenceRadius * _tileSize;

            // 创建势力范围显示
            RenderTerritory(centerWorldPos, radiusPixels, fillColor, borderColor);

            // 创建宗门图标
            RenderSectIcon(centerWorldPos, config.IconScale, baseColor, sect);

            // 显示宗门名称（大型和顶级宗门）
            if (config.ShowName)
            {
                RenderSectLabel(centerWorldPos, radiusPixels, sect, baseColor);
            }
        }

        /// <summary>
        /// 渲染势力范围圆形区域
        /// </summary>
        private void RenderTerritory(Vector2 center, float radius, Color fillColor, Color borderColor)
        {
            // 创建圆形区域 - 使用多边形近似
            int segments = 32;
            var polygon = new Polygon2D();
            polygon.Name = "TerritoryPolygon";
            polygon.Color = fillColor;
            
            Vector2[] points = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.Pi * 2;
                points[i] = new Vector2(
                    center.X + Mathf.Cos(angle) * radius,
                    center.Y + Mathf.Sin(angle) * radius
                );
            }
            polygon.Polygon = points;
            _territoryContainer.AddChild(polygon);

            // 添加边界线
            var line = new Line2D();
            line.Name = "TerritoryBorder";
            line.DefaultColor = borderColor;
            line.Width = BorderWidth;
            line.JointMode = Line2D.LineJointMode.Round;
            line.BeginCapMode = Line2D.LineCapMode.Round;
            line.EndCapMode = Line2D.LineCapMode.Round;
            
            Vector2[] linePoints = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.Pi * 2;
                linePoints[i] = new Vector2(
                    center.X + Mathf.Cos(angle) * radius,
                    center.Y + Mathf.Sin(angle) * radius
                );
            }
            line.Points = linePoints;
            _territoryContainer.AddChild(line);
        }

        /// <summary>
        /// 渲染宗门图标
        /// </summary>
        private void RenderSectIcon(Vector2 center, float scale, Color color, SectData sect)
        {
            // 创建一个简单的圆形图标作为宗门标记
            var iconContainer = new Node2D();
            iconContainer.Name = $"SectIcon_{sect.Id}";
            iconContainer.Position = center;
            iconContainer.ZIndex = 10;

            // 外圈（深色边框）
            var outerCircle = new Polygon2D();
            outerCircle.Name = "OuterCircle";
            outerCircle.Color = new Color(color.R * 0.5f, color.G * 0.5f, color.B * 0.5f, 1.0f);
            
            float outerRadius = 12 * scale;
            int segments = 16;
            Vector2[] outerPoints = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.Pi * 2;
                outerPoints[i] = new Vector2(
                    Mathf.Cos(angle) * outerRadius,
                    Mathf.Sin(angle) * outerRadius
                );
            }
            outerCircle.Polygon = outerPoints;
            iconContainer.AddChild(outerCircle);

            // 内圈（主色）
            var innerCircle = new Polygon2D();
            innerCircle.Name = "InnerCircle";
            innerCircle.Color = color;
            
            float innerRadius = 8 * scale;
            Vector2[] innerPoints = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.Pi * 2;
                innerPoints[i] = new Vector2(
                    Mathf.Cos(angle) * innerRadius,
                    Mathf.Sin(angle) * innerRadius
                );
            }
            innerCircle.Polygon = innerPoints;
            iconContainer.AddChild(innerCircle);

            // 根据等级添加装饰
            if (sect.Level == SectLevel.Top)
            {
                // 顶级宗门 - 添加星形装饰
                var star = new Polygon2D();
                star.Name = "StarDecoration";
                star.Color = new Color(1.0f, 1.0f, 0.8f, 1.0f);
                
                float starRadius = 4 * scale;
                Vector2[] starPoints = new Vector2[5];
                for (int i = 0; i < 5; i++)
                {
                    float angle = (float)i / 5 * Mathf.Pi * 2 - Mathf.Pi / 2;
                    starPoints[i] = new Vector2(
                        Mathf.Cos(angle) * starRadius,
                        Mathf.Sin(angle) * starRadius
                    );
                }
                star.Polygon = starPoints;
                iconContainer.AddChild(star);
            }
            else if (sect.Level == SectLevel.Large)
            {
                // 大型宗门 - 添加四角星装饰
                var diamond = new Polygon2D();
                diamond.Name = "DiamondDecoration";
                diamond.Color = new Color(1.0f, 1.0f, 1.0f, 0.8f);
                
                float diamondSize = 3 * scale;
                Vector2[] diamondPoints = new Vector2[]
                {
                    new Vector2(0, -diamondSize),
                    new Vector2(diamondSize, 0),
                    new Vector2(0, diamondSize),
                    new Vector2(-diamondSize, 0)
                };
                diamond.Polygon = diamondPoints;
                iconContainer.AddChild(diamond);
            }

            _iconContainer.AddChild(iconContainer);
        }

        /// <summary>
        /// 渲染宗门名称标签
        /// </summary>
        private void RenderSectLabel(Vector2 center, float radius, SectData sect, Color color)
        {
            var label = new Label();
            label.Name = $"SectLabel_{sect.Id}";
            label.Text = sect.Name;
            label.Modulate = new Color(color.R, color.G, color.B, 1.0f);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            
            // 设置字体大小和样式
            var font = new SystemFont();
            font.FontNames = new string[] { "Microsoft YaHei", "SimHei", "Arial" };
            label.AddThemeFontOverride("font", font);
            
            // 根据宗门等级设置字体大小
            int fontSize = sect.Level switch
            {
                SectLevel.Top => 16,
                SectLevel.Large => 12,
                _ => 10
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            
            // 添加文字描边/阴影效果
            label.AddThemeConstantOverride("outline_size", 2);
            label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
            
            // 设置位置（在势力范围下方）
            label.Position = new Vector2(
                center.X - 50,
                center.Y + radius + 10
            );
            label.CustomMinimumSize = new Vector2(100, 20);
            
            _labelContainer.AddChild(label);
        }

        /// <summary>
        /// 获取宗门颜色
        /// </summary>
        private Color GetSectColor(SectData sect)
        {
            switch (sect.Level)
            {
                case SectLevel.Top:
                    if (_sectTypeColors.TryGetValue(sect.PrimaryType, out Color topColor))
                    {
                        return topColor;
                    }
                    return new Color(0.8f, 0.6f, 0.4f);

                case SectLevel.Large:
                    return new Color(0.95f, 0.9f, 0.75f);

                case SectLevel.Small:
                default:
                    return new Color(0.6f, 0.6f, 0.6f);
            }
        }

        /// <summary>
        /// 势力范围显示配置结构
        /// </summary>
        private struct SectDisplayConfig
        {
            public int Radius;           // 势力范围半径（格）
            public float IconScale;      // 图标缩放
            public bool ShowName;        // 是否显示名称

            public SectDisplayConfig(int radius, float iconScale, bool showName)
            {
                Radius = radius;
                IconScale = iconScale;
                ShowName = showName;
            }
        }
    }
}
