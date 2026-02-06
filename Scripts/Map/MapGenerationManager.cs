using System;
using Godot;
using FantasyMapGenerator.Scripts.Core;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;
using FantasyMapGenerator.Scripts.Map.Heightmap;

namespace FantasyMapGenerator.Scripts.Map;

/// <summary>
/// 地图生成管理器，提供高级地图生成控制功能
/// </summary>
public class MapGenerationManager
{
    private MapGenerator _mapGenerator;
    
    public event Action<MapData> OnMapGenerated;
    public event Action<float> OnProgressUpdated;
    
    public MapData CurrentMapData => _mapGenerator?.Data;
    public AleaPRNG CurrentPRNG => _mapGenerator?.PRNG;

    public MapGenerationManager()
    {
        _mapGenerator = new MapGenerator();
    }

    /// <summary>
    /// 生成新地图（使用随机种子）
    /// </summary>
    public void GenerateNewMap(int cellCount = 500, float waterLevel = 0.35f)
    {
        string seed = GenerateRandomSeed();
        GenerateMap(seed, cellCount, waterLevel);
    }

    /// <summary>
    /// 使用指定种子生成地图
    /// </summary>
    public void GenerateMap(string seed, int cellCount = 500, float waterLevel = 0.35f)
    {
        _mapGenerator.WaterLevel = waterLevel;
        
        // 触发进度更新
        OnProgressUpdated?.Invoke(0.1f);
        
        _mapGenerator.Generate(seed, cellCount);
        
        OnProgressUpdated?.Invoke(1.0f);
        OnMapGenerated?.Invoke(_mapGenerator.Data);
    }

    /// <summary>
    /// 使用当前PRNG重新生成地图
    /// </summary>
    public void RegenerateCurrentMap()
    {
        if (_mapGenerator.Data != null && _mapGenerator.PRNG != null)
        {
            var currentCellCount = _mapGenerator.CellCount;
            var currentWaterLevel = _mapGenerator.WaterLevel;
            
            _mapGenerator.Regenerate();
            OnMapGenerated?.Invoke(_mapGenerator.Data);
        }
    }

    /// <summary>
    /// 生成随机种子
    /// </summary>
    private string GenerateRandomSeed()
    {
        // 使用时间戳和随机数生成唯一种子
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomNum = new Random().Next(10000, 99999);
        return $"seed_{timestamp}_{randomNum}";
    }

    /// <summary>
    /// 生成具有特定地形类型的随机地图
    /// </summary>
    public void GenerateThemedMap(MapTheme theme, int cellCount = 500, float waterLevel = 0.35f)
    {
        _mapGenerator.UseTemplate = true;
        _mapGenerator.RandomTemplate = true;
        _mapGenerator.TemplateType = ConvertThemeToTemplate(theme);
        _mapGenerator.WaterLevel = waterLevel;

        string seed = GenerateRandomSeed();
        _mapGenerator.Generate(seed, cellCount);
        
        OnMapGenerated?.Invoke(_mapGenerator.Data);
    }

    /// <summary>
    /// 将地图主题转换为模板类型
    /// </summary>
    private HeightmapTemplateType ConvertThemeToTemplate(MapTheme theme)
    {
        switch (theme)
        {
            case MapTheme.Continental:
                return HeightmapTemplateType.HighIsland;
            case MapTheme.Island:
                return HeightmapTemplateType.LowIsland;
            case MapTheme.Archipelago:
                return HeightmapTemplateType.Archipelago;
            case MapTheme.Pangea:
                return HeightmapTemplateType.Pangea;
            case MapTheme.ContinentAndIslands:
                return HeightmapTemplateType.Continents;
            case MapTheme.Mediterranean:
                return HeightmapTemplateType.Mediterranean;
            case MapTheme.Peninsula:
                return HeightmapTemplateType.Peninsula;
            case MapTheme.Custom:
            default:
                return HeightmapTemplateType.HighIsland;
        }
    }

    /// <summary>
    /// 获取当前地图信息
    /// </summary>
    public MapInfo GetCurrentMapInfo()
    {
        if (_mapGenerator.Data == null) return null;

        return new MapInfo
        {
            Seed = _mapGenerator.Data.Seed,
            CellCount = _mapGenerator.CellCount,
            MapSize = _mapGenerator.MapSize,
            WaterLevel = _mapGenerator.WaterLevel,
            FeatureCount = _mapGenerator.Data.Features?.Length ?? 0,
            RiverCount = _mapGenerator.Data.Rivers?.Length ?? 0
        };
    }
}

/// <summary>
/// 地图主题枚举
/// </summary>
public enum MapTheme
{
    Continental,          // 大陆型
    Island,               // 岛屿型
    Archipelago,          // 群岛型
    Pangea,               // 盘古大陆型
    ContinentAndIslands,  // 大陆加岛屿型
    Mediterranean,        // 地中海型
    Peninsula,            // 半岛型
    Custom                // 自定义
}

/// <summary>
/// 地图信息类
/// </summary>
public class MapInfo
{
    public int Seed { get; set; }
    public int CellCount { get; set; }
    public Vector2 MapSize { get; set; }
    public float WaterLevel { get; set; }
    public int FeatureCount { get; set; }
    public int RiverCount { get; set; }
}