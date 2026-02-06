using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Core;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Utils;

/// <summary>
/// 随机种子管理器，用于管理和保存地图生成种子
/// </summary>
public class SeedManager
{
    private static SeedManager _instance;
    public static SeedManager Instance => _instance ??= new SeedManager();

    private List<string> _recentSeeds;
    private const int MaxRecentSeeds = 20; // 最大保存最近使用的种子数量

    private SeedManager()
    {
        _recentSeeds = new List<string>();
    }

    /// <summary>
    /// 生成新的随机种子
    /// </summary>
    public string GenerateNewSeed()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomNum = new Random().Next(100000, 999999);
        string newSeed = $"seed_{timestamp}_{randomNum}";
        
        AddSeedToHistory(newSeed);
        
        return newSeed;
    }

    /// <summary>
    /// 生成具有描述性的种子（基于时间戳和随机词）
    /// </summary>
    public string GenerateDescriptiveSeed()
    {
        var random = new Random();
        string[] adjectives = { "ancient", "mystic", "frozen", "volcanic", "fertile", "barren", "magical", "warrior", "peaceful", "chaotic" };
        string[] nouns = { "forest", "desert", "mountain", "ocean", "plains", "swamp", "tundra", "jungle", "island", "valley" };
        
        string adjective = adjectives[random.Next(adjectives.Length)];
        string noun = nouns[random.Next(nouns.Length)];
        string number = random.Next(100, 999).ToString();
        
        string newSeed = $"{adjective}_{noun}_{number}";
        
        AddSeedToHistory(newSeed);
        
        return newSeed;
    }

    /// <summary>
    /// 将种子添加到历史记录
    /// </summary>
    private void AddSeedToHistory(string seed)
    {
        if (!_recentSeeds.Contains(seed))
        {
            _recentSeeds.Insert(0, seed);
            
            // 限制历史记录数量
            if (_recentSeeds.Count > MaxRecentSeeds)
            {
                _recentSeeds.RemoveAt(_recentSeeds.Count - 1);
            }
        }
        else
        {
            // 如果种子已存在，将其移到最前面
            _recentSeeds.Remove(seed);
            _recentSeeds.Insert(0, seed);
        }
    }

    /// <summary>
    /// 获取最近使用的种子列表
    /// </summary>
    public List<string> GetRecentSeeds()
    {
        return new List<string>(_recentSeeds);
    }

    /// <summary>
    /// 保存种子到存档数据
    /// </summary>
    public void SaveSeedsToFile(string filePath)
    {
        try
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);

            foreach (string seed in _recentSeeds)
            {
                file.StoreLine(seed);
            }

            GD.Print($"种子已保存到: {filePath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"保存种子失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从文件加载种子
    /// </summary>
    public void LoadSeedsFromFile(string filePath)
    {
        if (!FileAccess.FileExists(filePath))
        {
            GD.Print($"种子文件不存在: {filePath}");
            return;
        }

        try
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            _recentSeeds.Clear();

            while (file.GetPosition() < file.GetLength())
            {
                string line = file.GetLine().Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    _recentSeeds.Add(line);
                }
            }

            GD.Print($"从文件加载了 {_recentSeeds.Count} 个种子: {filePath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"加载种子失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证种子是否有效
    /// </summary>
    public bool IsValidSeed(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
            return false;
        
        // 种子不应超过一定长度
        if (seed.Length > 100)
            return false;
        
        // 种子应只包含字母、数字、下划线和连字符
        foreach (char c in seed)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                return false;
        }
        
        return true;
    }

    /// <summary>
    /// 使用当前种子生成器生成地图
    /// </summary>
    public void GenerateMapWithCurrentSeed(MapGenerator generator, int cellCount = 500)
    {
        string seed = GenerateNewSeed();
        generator.Generate(seed, cellCount);
    }

    /// <summary>
    /// 使用描述性种子生成地图
    /// </summary>
    public void GenerateMapWithDescriptiveSeed(MapGenerator generator, int cellCount = 500)
    {
        string seed = GenerateDescriptiveSeed();
        generator.Generate(seed, cellCount);
    }
}