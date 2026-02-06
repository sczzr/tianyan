using Godot;
using System;
using FantasyMapGenerator.Scripts.Core;
using FantasyMapGenerator.Scripts.Map;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;
using FantasyMapGenerator.Scripts.Rendering;

namespace FantasyMapGenerator.Scripts.UI;

/// <summary>
/// 地图生成控制器UI
/// </summary>
public partial class MapGenerationController : Control
{
    private MapView _mapView;
    
    public MapView MapView 
    { 
        get => _mapView; 
        set 
        { 
            _mapView = value;
            // 可以在这里添加额外的逻辑来响应MapView的变化
        } 
    }
    
    // UI元素
    private HSlider _cellCountSlider;
    private HSlider _waterLevelSlider;
    private OptionButton _themeSelector;
    private Button _generateButton;
    private Button _regenerateButton;
    private LineEdit _seedInput;
    private Label _infoLabel;
    
    private MapGenerationManager _generationManager;
    private TranslationManager _translationManager;

    public override void _Ready()
    {
        _translationManager = TranslationManager.Instance;
        _translationManager.LanguageChanged += OnLanguageChanged;
        
        InitializeUI();
        SetupEvents();
        UpdateUIText();
        
        // 初始化地图生成管理器
        _generationManager = new MapGenerationManager();
        _generationManager.OnMapGenerated += OnMapGenerated;
        _generationManager.OnProgressUpdated += OnProgressUpdated;
        
        // 生成初始地图
        GenerateInitialMap();
    }

    private void InitializeUI()
    {
        _cellCountSlider = GetNode<HSlider>("VBoxContainer/CellCountControl/HSlider");
        _waterLevelSlider = GetNode<HSlider>("VBoxContainer/WaterLevelControl/HSlider");
        _themeSelector = GetNode<OptionButton>("VBoxContainer/ThemeControl/OptionButton");
        _generateButton = GetNode<Button>("VBoxContainer/GenerateButton");
        _regenerateButton = GetNode<Button>("VBoxContainer/RegenerateButton");
        _seedInput = GetNode<LineEdit>("VBoxContainer/SeedControl/LineEdit");
        _infoLabel = GetNode<Label>("VBoxContainer/InfoLabel");
        
        // 设置滑块默认值
        _cellCountSlider.Value = 500;
        _waterLevelSlider.Value = 0.35f;
        
        // 添加主题选项
        _themeSelector.AddItem("大陆型", (int)MapTheme.Continental);
        _themeSelector.AddItem("岛屿型", (int)MapTheme.Island);
        _themeSelector.AddItem("群岛型", (int)MapTheme.Archipelago);
        _themeSelector.AddItem("盘古大陆型", (int)MapTheme.Pangea);
        _themeSelector.AddItem("大陆加岛屿型", (int)MapTheme.ContinentAndIslands);
        _themeSelector.AddItem("地中海型", (int)MapTheme.Mediterranean);
        _themeSelector.AddItem("半岛型", (int)MapTheme.Peninsula);
        _themeSelector.Select(0); // 默认选择大陆型
    }

    private void SetupEvents()
    {
        _generateButton.Pressed += OnGeneratePressed;
        _regenerateButton.Pressed += OnRegeneratePressed;
        _cellCountSlider.ValueChanged += OnCellCountChanged;
        _waterLevelSlider.ValueChanged += OnWaterLevelChanged;
        _themeSelector.ItemSelected += OnThemeChanged;
    }

    private void OnLanguageChanged(string language)
    {
        UpdateUIText();
    }

    private void UpdateUIText()
    {
        var tm = TranslationManager.Instance;
        
        if (_generateButton != null)
            _generateButton.Text = tm.Tr("generate_new_map");
        
        if (_regenerateButton != null)
            _regenerateButton.Text = tm.Tr("regenerate_map");
    }

    private void OnGeneratePressed()
    {
        string seed = _seedInput.Text;
        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = GenerateRandomSeedString();
        }
        
        int cellCount = (int)_cellCountSlider.Value;
        float waterLevel = (float)_waterLevelSlider.Value;
        MapTheme selectedTheme = (MapTheme)_themeSelector.Selected;

        // 根据选择的主题生成地图
        if (selectedTheme == MapTheme.Custom)
        {
            _generationManager.GenerateMap(seed, cellCount, waterLevel);
        }
        else
        {
            _generationManager.GenerateThemedMap(selectedTheme, cellCount, waterLevel);
        }
    }

    private void OnRegeneratePressed()
    {
        _generationManager.RegenerateCurrentMap();
    }

    private void OnCellCountChanged(double value)
    {
        if (MapView != null)
        {
            MapView.CellCount = (int)value;
        }
    }

    private void OnWaterLevelChanged(double value)
    {
        if (MapView != null)
        {
            MapView.SetWaterLevel((float)value);
        }
    }

    private void OnThemeChanged(long index)
    {
        // 主题变化时不需要立即执行，只在生成时应用
    }

    private void OnMapGenerated(MapData mapData)
    {
        UpdateInfoLabel();
    }

    private void OnProgressUpdated(float progress)
    {
        // 在实际项目中，这里可以更新进度条
        GD.Print($"地图生成进度: {progress:P}");
    }

    private void GenerateInitialMap()
    {
        _generationManager.GenerateNewMap(
            (int)_cellCountSlider.Value,
            (float)_waterLevelSlider.Value
        );
    }

    private void UpdateInfoLabel()
    {
        if (_generationManager.CurrentMapData != null)
        {
            var info = _generationManager.GetCurrentMapInfo();
            if (info != null)
            {
                _infoLabel.Text = $"地图信息 - 种子: {info.Seed}, 单元数: {info.CellCount}, " +
                                $"水域: {info.WaterLevel:P}, 地貌: {info.FeatureCount}, 河流: {info.RiverCount}";
            }
        }
    }

    private string GenerateRandomSeedString()
    {
        var random = new Random();
        return $"seed_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{random.Next(1000, 9999)}";
    }

    public override void _ExitTree()
    {
        if (_translationManager != null)
        {
            _translationManager.LanguageChanged -= OnLanguageChanged;
        }
        
        if (_generationManager != null)
        {
            _generationManager.OnMapGenerated -= OnMapGenerated;
            _generationManager.OnProgressUpdated -= OnProgressUpdated;
        }
    }
}