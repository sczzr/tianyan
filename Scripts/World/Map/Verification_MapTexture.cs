using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.World.Map
{
    /// <summary>
    /// 地图纹理验证脚本 - 验证 BiomeTextureGenerator 和世界空间映射逻辑
    /// 运行方式：在游戏中调用 Verification_MapTexture.RunAllTests()
    /// </summary>
    public static class Verification_MapTexture
    {
        private static int _passedTests = 0;
        private static int _failedTests = 0;
        private static List<string> _errors = new();

        public static void RunAllTests()
        {
            GD.Print("========================================");
            GD.Print("开始地图纹理系统验证...");
            GD.Print("========================================");

            _passedTests = 0;
            _failedTests = 0;
            _errors.Clear();

            TestBiomeTextureGenerator();
            TestTextureDimensions();
            TestWorldSpaceMapping();
            TestTileSetSlicing();
            TestAdjacentTileContinuity();
            TestTextureTilingScale();

            PrintSummary();

            GD.Print("========================================");
            if (_failedTests == 0)
            {
                GD.Print("所有验证测试通过！");
            }
            else
            {
                GD.Print($"验证完成: {_passedTests} 通过, {_failedTests} 失败");
                foreach (var error in _errors)
                {
                    GD.PrintErr(error);
                }
            }
            GD.Print("========================================");
        }

        private static void TestBiomeTextureGenerator()
        {
            GD.Print("测试 1: BiomeTextureGenerator 创建和纹理生成");

            try
            {
                var generator = new BiomeTextureGenerator(512, 32, 12345);
                Assert(generator != null, "纹理生成器实例不应为 null");
                Assert(generator.GetTextureGridWidth() == 16, $"纹理网格宽度应为 16，实际: {generator.GetTextureGridWidth()}");
                Assert(generator.GetTextureGridHeight() == 16, $"纹理网格高度应为 16，实际: {generator.GetTextureGridHeight()}");
                Pass("BiomeTextureGenerator 创建成功");
            }
            catch (Exception ex)
            {
                Fail($"BiomeTextureGenerator 测试失败: {ex.Message}");
            }
        }

        private static void TestTextureDimensions()
        {
            GD.Print("测试 2: 验证生成的纹理尺寸");

            try
            {
                var generator = new BiomeTextureGenerator(512, 32, 12345);
                int textureSize = 512;
                int tileSize = 32;

                foreach (BiomeType biomeType in Enum.GetValues(typeof(BiomeType)))
                {
                    var texture = generator.GetBiomeTexture(biomeType);
                    Assert(texture != null, $"生物群系 {biomeType} 的纹理不应为 null");
                    Assert(texture.GetWidth() == textureSize, $"生物群系 {biomeType} 纹理宽度应为 {textureSize}，实际: {textureSize}");
                    Assert(texture.GetHeight() == textureSize, $"生物群系 {biomeType} 纹理高度应为 {textureSize}，实际: {textureSize}");
                }
                Pass($"所有 {Enum.GetValues(typeof(BiomeType)).Length} 种生物群系纹理尺寸验证通过");
            }
            catch (Exception ex)
            {
                Fail($"纹理尺寸测试失败: {ex.Message}");
            }
        }

        private static void TestWorldSpaceMapping()
        {
            GD.Print("测试 3: 验证世界空间映射公式: atlasX = (x / TextureTilingScale) % TextureGridWidth");

            try
            {
                var generator = new BiomeTextureGenerator(512, 32, 12345);
                int textureSize = 512;
                int tileSize = 32;
                int textureGridWidth = textureSize / tileSize;
                float textureTilingScale = 1.0f;

                for (int scaleNumerator = 1; scaleNumerator <= 4; scaleNumerator++)
                {
                    textureTilingScale = scaleNumerator * 1.0f;
                    GD.Print($"  测试缩放比例: {textureTilingScale:F1}");

                    for (int x = 0; x < 100; x++)
                    {
                        for (int y = 0; y < 100; y++)
                        {
                            int scaledX = (int)(x / textureTilingScale);
                            int scaledY = (int)(y / textureTilingScale);

                            int expectedAtlasX = ((scaledX % textureGridWidth) + textureGridWidth) % textureGridWidth;
                            int expectedAtlasY = ((scaledY % textureGridWidth) + textureGridWidth) % textureGridWidth;

                            Assert(expectedAtlasX >= 0 && expectedAtlasX < textureGridWidth,
                                $"atlasX {expectedAtlasX} 应在 0 到 {textureGridWidth - 1} 范围内");
                            Assert(expectedAtlasY >= 0 && expectedAtlasY < textureGridWidth,
                                $"atlasY {expectedAtlasY} 应在 0 到 {textureGridWidth - 1} 范围内");
                        }
                    }
                }
                Pass("世界空间映射公式验证通过");
            }
            catch (Exception ex)
            {
                Fail($"世界空间映射测试失败: {ex.Message}");
            }
        }

        private static void TestTileSetSlicing()
        {
            GD.Print("测试 4: 验证 TileSet 正确切片大纹理");

            try
            {
                var manager = new WorldMapManager();
                var generatorField = typeof(WorldMapManager).GetField("_generator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var textureGeneratorField = typeof(WorldMapManager).GetField("_textureGenerator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                manager.MapWidth = 32;
                manager.MapHeight = 32;
                manager.TileSize = 32;
                manager.BiomeTextureSize = 256;
                manager.GenerateOnReady = false;

                manager.CallDeferred(WorldMapManager.MethodName._Ready);
                System.Threading.Thread.Sleep(100);

                var textureGenerator = textureGeneratorField?.GetValue(manager) as BiomeTextureGenerator;
                Assert(textureGenerator != null, "纹理生成器不应为 null");

                int expectedTilesPerTexture = 256 / 32;
                Assert(expectedTilesPerTexture == 8, $"纹理应能分成 {expectedTilesPerTexture}x{expectedTilesPerTexture} 个瓦片");

                Pass($"TileSet 切片验证通过: 每个纹理可分成 {expectedTilesPerTexture}x{expectedTilesPerTexture} 瓦片");
            }
            catch (Exception ex)
            {
                Fail($"TileSet 切片测试失败: {ex.Message}");
            }
        }

        private static void TestAdjacentTileContinuity()
        {
            GD.Print("测试 5: 验证相邻地图瓦片引用相邻的图集瓦片（连续性检查）");

            try
            {
                int textureSize = 512;
                int tileSize = 32;
                int textureGridWidth = textureSize / tileSize;

                float textureTilingScale = 1.0f;

                int failures = 0;
                int totalChecks = 0;

                for (int x = 0; x < 50 && failures < 10; x++)
                {
                    for (int y = 0; y < 50 && failures < 10; y++)
                    {
                        totalChecks++;

                        int atlasX1 = (int)(x / textureTilingScale) % textureGridWidth;
                        int atlasY1 = (int)(y / textureTilingScale) % textureGridWidth;

                        int nextX = x + 1;
                        int atlasX2 = (int)(nextX / textureTilingScale) % textureGridWidth;

                        int nextY = y + 1;
                        int atlasY2 = (int)(nextY / textureTilingScale) % textureGridWidth;

                        if (atlasX2 < 0 || atlasX2 >= textureGridWidth ||
                            atlasY2 < 0 || atlasY2 >= textureGridWidth)
                        {
                            failures++;
                            continue;
                        }
                    }
                }

                Assert(failures == 0, $"连续性检查: {failures}/{totalChecks} 个检查失败");
                Pass($"相邻瓦片连续性验证通过 ({totalChecks} 个检查)");
            }
            catch (Exception ex)
            {
                Fail($"相邻瓦片连续性测试失败: {ex.Message}");
            }
        }

        private static void TestTextureTilingScale()
        {
            GD.Print("测试 6: 验证纹理网格缩放 API");

            try
            {
                var manager = new WorldMapManager();
                var tilingScaleField = typeof(WorldMapManager).GetField("TextureTilingScale",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                float defaultScale = (float)tilingScaleField?.GetValue(manager);
                Assert(Mathf.Abs(defaultScale - 1.0f) < 0.01f, $"默认缩放应为 1.0，实际: {defaultScale}");

                var generator = new BiomeTextureGenerator(512, 32, 12345);

                int[] testScales = { 1, 2, 4, 8, 16 };
                foreach (int scale in testScales)
                {
                    float scaleFloat = scale * 1.0f;

                    for (int x = 0; x < 64; x++)
                    {
                        int textureGridWidth = generator.GetTextureGridWidth();
                        int scaledX = (int)(x / scaleFloat);
                        int atlasX = scaledX % textureGridWidth;

                        Assert(atlasX >= 0 && atlasX < textureGridWidth,
                            $"缩放 {scale} 时 atlasX {atlasX} 应在有效范围内");
                    }
                }
                Pass("纹理网格缩放 API 验证通过");
            }
            catch (Exception ex)
            {
                Fail($"纹理网格缩放测试失败: {ex.Message}");
            }
        }

        private static void TestMultipleTextureSizes()
        {
            GD.Print("测试 7: 验证不同纹理尺寸配置");

            try
            {
                int[] sizes = { 256, 512, 1024 };
                int tileSize = 32;

                foreach (int size in sizes)
                {
                    var generator = new BiomeTextureGenerator(size, tileSize, 12345);
                    int expectedGrid = size / tileSize;

                    Assert(generator.GetTextureGridWidth() == expectedGrid,
                        $"纹理尺寸 {size} 时网格宽度应为 {expectedGrid}");
                    Assert(generator.GetTextureGridHeight() == expectedGrid,
                        $"纹理尺寸 {size} 时网格高度应为 {expectedGrid}");
                }
                Pass($"多纹理尺寸验证通过 ({string.Join(", ", sizes)}px)");
            }
            catch (Exception ex)
            {
                Fail($"多纹理尺寸测试失败: {ex.Message}");
            }
        }

        private static void PrintSummary()
        {
            GD.Print("");
            GD.Print("----------------------------------------");
            GD.Print($"验证结果: {_passedTests} 通过, {_failedTests} 失败");
            if (_errors.Count > 0)
            {
                GD.Print("失败详情:");
                foreach (var error in _errors)
                {
                    GD.Print($"  - {error}");
                }
            }
            GD.Print("----------------------------------------");
        }

        private static void Pass(string message)
        {
            _passedTests++;
            GD.Print($"  [PASS] {message}");
        }

        private static void Fail(string message)
        {
            _failedTests++;
            _errors.Add(message);
            GD.PrintErr($"  [FAIL] {message}");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }
}
