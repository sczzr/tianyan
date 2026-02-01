using Godot;
using System;
using TianYanShop.Scripts;

namespace TianYanShop.Examples
{
    /// <summary>
    /// NoiseGenerator 使用示例
    /// 演示如何使用 AdvancedNoiseGenerator 生成各种类型的噪声
    /// </summary>
    public partial class NoiseGeneratorExample : Node2D
    {
        [Export] public int MapWidth = 256;
        [Export] public int MapHeight = 256;
        [Export] public float NoiseScale = 100.0f;

        // 显示用 Sprite
        private Sprite2D _displaySprite;
        private Label _infoLabel;

        // 噪声生成器
        private AdvancedNoiseGenerator _noiseGenerator;

        // 当前显示的噪声类型
        private int _currentNoiseType = 0;
        private string[] _noiseTypeNames = new[]
        {
            "基础高度图",
            "山脉图",
            "洞穴密度图",
            "侵蚀效果图",
            "完整合成高度"
        };

        public override void _Ready()
        {
            // 创建显示 Sprite
            _displaySprite = new Sprite2D();
            _displaySprite.Centered = false;
            AddChild(_displaySprite);

            // 创建信息标签
            _infoLabel = new Label();
            _infoLabel.Position = new Vector2(10, 10);
            _infoLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
            AddChild(_infoLabel);

            // 初始化噪声生成器
            InitializeNoiseGenerator();

            // 生成并显示噪声
            GenerateAndDisplayNoise();

            GD.Print("[NoiseGeneratorExample] 按空格键切换噪声类型，按 R 重新生成");
        }

        /// <summary>
        /// 初始化噪声生成器
        /// </summary>
        private void InitializeNoiseGenerator()
        {
            _noiseGenerator = new AdvancedNoiseGenerator
            {
                MapWidth = MapWidth,
                MapHeight = MapHeight,
                NoiseScale = NoiseScale,
                Octaves = 4,
                Persistence = 0.5f,
                Lacunarity = 2.0f,
                // 山脉参数
                MountainScale = 80.0f,
                MountainHeight = 0.4f,
                MountainThreshold = 0.65f,
                MountainOctaves = 6,
                // 洞穴参数
                CaveScale = 60.0f,
                CaveThreshold = 0.45f,
                // 侵蚀参数
                EnableErosion = true,
                ErosionStrength = 0.3f
            };

            // 使用固定种子以便复现
            _noiseGenerator.Initialize(12345);
        }

        /// <summary>
        /// 生成并显示当前类型的噪声
        /// </summary>
        private void GenerateAndDisplayNoise()
        {
            float[,] noiseData;
            string title = _noiseTypeNames[_currentNoiseType];

            switch (_currentNoiseType)
            {
                case 0: // 基础高度图
                    noiseData = GenerateBaseHeightMap();
                    break;
                case 1: // 山脉图
                    noiseData = GenerateMountainMap();
                    break;
                case 2: // 洞穴密度图
                    noiseData = _noiseGenerator.GenerateCaveMap();
                    break;
                case 3: // 侵蚀效果图
                    noiseData = GenerateErosionMap();
                    break;
                case 4: // 完整合成高度
                default:
                    noiseData = _noiseGenerator.GenerateHeightMap();
                    break;
            }

            // 创建纹理并显示
            ImageTexture texture = CreateTextureFromNoise(noiseData);
            _displaySprite.Texture = texture;

            // 更新信息标签
            float minVal = float.MaxValue, maxVal = float.MinValue, avgVal = 0;
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    float v = noiseData[x, y];
                    minVal = Mathf.Min(minVal, v);
                    maxVal = Mathf.Max(maxVal, v);
                    avgVal += v;
                }
            }
            avgVal /= (MapWidth * MapHeight);

            _infoLabel.Text = $"{title}\n" +
                             $"最小值: {minVal:F3}\n" +
                             $"最大值: {maxVal:F3}\n" +
                             $"平均值: {avgVal:F3}\n" +
                             $"按空格切换类型，R重新生成";
        }

        /// <summary>
        /// 生成基础高度图（仅基础噪声）
        /// </summary>
        private float[,] GenerateBaseHeightMap()
        {
            float[,] data = new float[MapWidth, MapHeight];

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    data[x, y] = _noiseGenerator.GetBaseHeightAt(x, y);
                }
            }

            return data;
        }

        /// <summary>
        /// 生成山脉图
        /// </summary>
        private float[,] GenerateMountainMap()
        {
            float[,] data = new float[MapWidth, MapHeight];

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    data[x, y] = _noiseGenerator.GetMountainValueAt(x, y);
                }
            }

            return data;
        }

        /// <summary>
        /// 生成侵蚀效果图
        /// </summary>
        private float[,] GenerateErosionMap()
        {
            float[,] data = new float[MapWidth, MapHeight];

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    data[x, y] = _noiseGenerator.GetErosionValueAt(x, y);
                }
            }

            return data;
        }

        /// <summary>
        /// 从噪声数据创建纹理
        /// </summary>
        private ImageTexture CreateTextureFromNoise(float[,] noiseData)
        {
            Image image = Image.CreateEmpty(MapWidth, MapHeight, false, Image.Format.Rgba8);

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    float value = noiseData[x, y];
                    // 使用灰度显示，值越大越亮
                    Color color = new Color(value, value, value);
                    image.SetPixel(x, y, color);
                }
            }

            return ImageTexture.CreateFromImage(image);
        }

        public override void _Input(InputEvent @event)
        {
            // 空格键切换噪声类型
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (keyEvent.Keycode == Key.Space)
                {
                    _currentNoiseType = (_currentNoiseType + 1) % _noiseTypeNames.Length;
                    GenerateAndDisplayNoise();
                }
                else if (keyEvent.Keycode == Key.R)
                {
                    // R键重新生成
                    _noiseGenerator.Initialize(new Random().Next());
                    GenerateAndDisplayNoise();
                }
            }
        }
    }
}
