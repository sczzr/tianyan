using Godot;
using System;
using System.Collections.Generic;
using TianYanShop.World.Config;

namespace TianYanShop.World.Map
{
    public partial class MapGeneratorSceneController : Control
    {
        private WorldMapRenderer _renderer;
        private LineEdit _seedInput;
        private OptionButton _provinceOption;
        private SpinBox _widthSpin;
        private SpinBox _heightSpin;
        private HSlider _temperatureSlider;
        private HSlider _precipitationSlider;
        private HSlider _continentalitySlider;
        private HSlider _elevationSlider;
        private Label _temperatureValue;
        private Label _precipitationValue;
        private Label _continentalityValue;
        private Label _elevationValue;
        private Button _generateButton;
        private Button _previewButton;
        private Button _randomSeedButton;
        private RichTextLabel _terrainInfoLabel;
        private Button _titleButton;
        private Button _collapseButton;
        private VBoxContainer _contentContainer;
        private PanelContainer _controlPanel;
        private int _currentProvinceIndex = 0;
        private int _lastSeed = -1;
        private bool _isCollapsed = false;
        private static readonly Random _staticRandom = new Random();

        public override void _Ready()
        {
            _renderer = GetNode<WorldMapRenderer>("WorldMapRenderer");

            _titleButton = GetNode<Button>("UI/ControlPanel/VBoxContainer/TitleContainer/Title");
            _collapseButton = GetNode<Button>("UI/ControlPanel/VBoxContainer/TitleContainer/CollapseButton");
            _contentContainer = GetNode<VBoxContainer>("UI/ControlPanel/VBoxContainer/ContentContainer");
            _controlPanel = GetNode<PanelContainer>("UI/ControlPanel");

            _seedInput = GetNode<LineEdit>("UI/ControlPanel/VBoxContainer/ContentContainer/SeedContainer/SeedInput");
            _provinceOption = GetNode<OptionButton>("UI/ControlPanel/VBoxContainer/ContentContainer/ProvinceOption");
            _widthSpin = GetNode<SpinBox>("UI/ControlPanel/VBoxContainer/ContentContainer/SizeContainer/WidthSpin");
            _heightSpin = GetNode<SpinBox>("UI/ControlPanel/VBoxContainer/ContentContainer/SizeContainer/HeightSpin");
            _temperatureSlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/TemperatureSlider");
            _precipitationSlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/PrecipitationSlider");
            _continentalitySlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/ContinentalitySlider");
            _elevationSlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/ElevationSlider");

            _temperatureValue = GetNode<Label>("UI/ControlPanel/VBoxContainer/ContentContainer/TemperatureRow/TemperatureValue");
            _precipitationValue = GetNode<Label>("UI/ControlPanel/VBoxContainer/ContentContainer/PrecipitationRow/PrecipitationValue");
            _continentalityValue = GetNode<Label>("UI/ControlPanel/VBoxContainer/ContentContainer/ContinentalityRow/ContinentalityValue");
            _elevationValue = GetNode<Label>("UI/ControlPanel/VBoxContainer/ContentContainer/ElevationRow/ElevationValue");

            _generateButton = GetNode<Button>("UI/ControlPanel/VBoxContainer/ContentContainer/ButtonContainer/GenerateButton");
            _previewButton = GetNode<Button>("UI/ControlPanel/VBoxContainer/ContentContainer/ButtonContainer/PreviewButton");
            _randomSeedButton = GetNode<Button>("UI/ControlPanel/VBoxContainer/ContentContainer/SeedContainer/RandomSeedButton");
            _terrainInfoLabel = GetNode<RichTextLabel>("UI/TerrainInfoPanel/TerrainInfoLabel");

            _generateButton.Pressed += OnGeneratePressed;
            _previewButton.Pressed += OnPreviewPressed;
            _randomSeedButton.Pressed += OnRandomSeedPressed;
            _provinceOption.ItemSelected += OnProvinceSelected;
            _titleButton.Pressed += OnCollapsePressed;
            _collapseButton.Pressed += OnCollapsePressed;

            _temperatureSlider.ValueChanged += OnSliderValueChanged;
            _precipitationSlider.ValueChanged += OnSliderValueChanged;
            _continentalitySlider.ValueChanged += OnSliderValueChanged;
            _elevationSlider.ValueChanged += OnSliderValueChanged;

            UpdateValueLabels();
            ToggleTerrainParameters(true);
            SetupProvinceOptions();
            UpdateTerrainInfo();
            InitializeRenderer();
            RegenerateMapWithCurrentSeed();
        }

        private void InitializeRenderer()
        {
            if (string.IsNullOrEmpty(_seedInput.Text))
            {
                int randomSeed = _staticRandom.Next();
                _seedInput.Text = randomSeed.ToString();
            }

            int seed = ConvertTextToSeed(_seedInput.Text);
            _lastSeed = seed;
            _renderer.Seed = seed;
            _renderer.RandomSeed = false;

            UpdateCustomParameters();
        }

        private void UpdateCustomParameters()
        {
            _renderer.UseCustomParameters = true;
            _renderer.CustomTemperature = (float)_temperatureSlider.Value;
            _renderer.CustomPrecipitation = (float)_precipitationSlider.Value;
            _renderer.CustomContinentality = (float)_continentalitySlider.Value;
            _renderer.CustomElevationVariation = (float)_elevationSlider.Value;
        }

        private void UpdateValueLabels()
        {
            float temperature = (float)_temperatureSlider.Value;
            float precipitation = (float)_precipitationSlider.Value;
            float continentality = (float)_continentalitySlider.Value;
            float elevation = (float)_elevationSlider.Value;

            float tempCelsius = temperature * 43 - 15;
            _temperatureValue.Text = $"{tempCelsius:F0}°C";

            float precipMm = precipitation * 2000;
            _precipitationValue.Text = $"{precipMm:F0}mm/年";

            string contDesc = continentality < 0.33f ? "海洋性" : (continentality < 0.66f ? "过渡性" : "大陆性");
            _continentalityValue.Text = contDesc;

            float elevMeters = elevation * 1500;
            _elevationValue.Text = $"{elevMeters:F0}m";
        }

        private void UpdateTerrainInfo()
        {
            if (_currentProvinceIndex == 0)
            {
                float temperature = (float)_temperatureSlider.Value;
                float precipitation = (float)_precipitationSlider.Value;
                float continentality = (float)_continentalitySlider.Value;
                float elevation = (float)_elevationSlider.Value;

                float tempCelsius = (temperature - 0.5f) * 60 - 10;
                float precipMm = precipitation * 2000;
                float elevMeters = elevation * 1500;

                string tempDesc = temperature < 0.3f ? "寒冷" : (temperature < 0.6f ? "温和" : "炎热");
                string precipDesc = precipitation < 0.3f ? "干旱" : (precipitation < 0.6f ? "适中" : "湿润");
                string contDesc = continentality < 0.3f ? "海洋性气候" : (continentality < 0.6f ? "过渡性气候" : "大陆性气候");
                string elevDesc = elevation < 0.5f ? "平原地形" : (elevation < 1.0f ? "丘陵地形" : "山地高原");

                _terrainInfoLabel.Text = "[b]自定义地形[/b]\n\n" +
                    $"[color=#AAAAAA]温度:[/color] {tempCelsius:F0}°C ({tempDesc})\n" +
                    $"[color=#AAAAAA]降水:[/color] {precipMm:F0}mm/年 ({precipDesc})\n" +
                    $"[color=#AAAAAA]气候:[/color] {contDesc}\n" +
                    $"[color=#AAAAAA]海拔:[/color] ~{elevMeters:F0}m ({elevDesc})";
            }
            else
            {
                string provinceName = _provinceOption.GetItemText(_currentProvinceIndex);
                ProvinceConfigManager.Initialize();
                var config = ProvinceConfigManager.GetProvince(provinceName);

                if (config != null)
                {
                    string terrainTypeName = config.TerrainType switch
                    {
                        ProvinceTerrainType.Plateau => "高原地形",
                        ProvinceTerrainType.Mountain => "山地地形",
                        ProvinceTerrainType.Plain => "平原地形",
                        ProvinceTerrainType.Coastal => "沿海地形",
                        ProvinceTerrainType.Basin => "盆地地形",
                        _ => "混合地形"
                    };

                    string rainDesc = config.BaseRainfall < 0.35f ? "干旱" : (config.BaseRainfall > 0.65f ? "湿润" : "适中");
                    string tempDesc = config.BaseTemperature < 0.35f ? "寒冷" : (config.BaseTemperature > 0.60f ? "温暖" : "温和");

                    _terrainInfoLabel.Text = $"[b]{provinceName}[/b] - {terrainTypeName}\n\n" +
                        $"[color=#AAAAAA]{config.Description}[/color]\n\n" +
                        $"[color=#AAAAAA]气候:[/color] {rainDesc}, {tempDesc}\n" +
                        $"[color=#AAAAAA]森林:[/color] {(int)(config.ForestRatio * 100)}%  |  " +
                        $"[color=#AAAAAA]沙漠:[/color] {(int)(config.DesertRatio * 100)}%\n" +
                        $"[color=#AAAAAA]山地:[/color] {(int)(config.MountainRatio * 100)}%  |  " +
                        $"[color=#AAAAAA]平原:[/color] {(int)(config.PlainRatio * 100)}%\n\n" +
                        $"[b]灵气概况[/b]\n" +
                        $"[color=#AAAAAA]浓郁度:[/color] {config.SpiritDescription}";
                }
            }
        }

        private void OnSliderValueChanged(double value)
        {
            UpdateValueLabels();
            if (_currentProvinceIndex == 0)
            {
                UpdateCustomParameters();
                _renderer.RegenerateMap();
                UpdateTerrainInfo();
            }
        }

        private void OnCollapsePressed()
        {
            _isCollapsed = !_isCollapsed;
            _contentContainer.Visible = !_isCollapsed;
            _collapseButton.Text = _isCollapsed ? "▶" : "▼";
            _controlPanel.CustomMinimumSize = new Vector2(_isCollapsed ? 100 : 250, 0);
            _controlPanel.MouseFilter = _isCollapsed ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Stop;
        }

        private void SetupProvinceOptions()
        {
            ProvinceConfigManager.Initialize();
            var provinceNames = ProvinceConfigManager.GetAllProvinceNames();

            _provinceOption.Clear();
            _provinceOption.AddItem("自定义");
            foreach (var name in provinceNames)
            {
                _provinceOption.AddItem(name);
            }
            _provinceOption.Selected = 0;
        }

        private void OnProvinceSelected(long index)
        {
            _currentProvinceIndex = (int)index;
            bool isCustom = (index == 0);
            ToggleTerrainParameters(isCustom);

            if (isCustom)
            {
                _renderer.UseCustomParameters = true;
                UpdateCustomParameters();
                _renderer.ProvinceName = "";
            }
            else
            {
                _renderer.UseCustomParameters = false;
                _renderer.ProvinceName = _provinceOption.GetItemText((int)index);
            }
            _renderer.RegenerateMap();
            UpdateTerrainInfo();
        }

        private void ToggleTerrainParameters(bool show)
        {
            var tempRow = GetNodeOrNull<HBoxContainer>("UI/ControlPanel/VBoxContainer/ContentContainer/TemperatureRow");
            var tempSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/TemperatureSlider");
            var precipRow = GetNodeOrNull<HBoxContainer>("UI/ControlPanel/VBoxContainer/ContentContainer/PrecipitationRow");
            var precipSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/PrecipitationSlider");
            var contRow = GetNodeOrNull<HBoxContainer>("UI/ControlPanel/VBoxContainer/ContentContainer/ContinentalityRow");
            var contSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/ContinentalitySlider");
            var elevRow = GetNodeOrNull<HBoxContainer>("UI/ControlPanel/VBoxContainer/ContentContainer/ElevationRow");
            var elevSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/ContentContainer/ElevationSlider");

            if (tempRow != null) tempRow.Visible = show;
            if (tempSlider != null) tempSlider.Visible = show;
            if (precipRow != null) precipRow.Visible = show;
            if (precipSlider != null) precipSlider.Visible = show;
            if (contRow != null) contRow.Visible = show;
            if (contSlider != null) contSlider.Visible = show;
            if (elevRow != null) elevRow.Visible = show;
            if (elevSlider != null) elevSlider.Visible = show;
        }

        private int ConvertTextToSeed(string text)
        {
            if (string.IsNullOrEmpty(text))
                return -1;

            if (int.TryParse(text, out int result))
                return result;

            int hashValue = 0;
            foreach (char c in text)
            {
                int charCode = (int)c;
                hashValue = (hashValue * 31 + charCode) & 0x7FFFFFFF;
            }
            return hashValue;
        }

        private void OnGeneratePressed()
        {
            int width = (int)_widthSpin.Value;
            int height = (int)_heightSpin.Value;
            int seed = ConvertTextToSeed(_seedInput.Text);
            bool isCustom = (_currentProvinceIndex == 0);
            string provinceName = isCustom ? "" : _provinceOption.GetItemText(_currentProvinceIndex);

            _renderer.MapWidth = width;
            _renderer.MapHeight = height;
            _renderer.Seed = seed;
            _renderer.RandomSeed = seed < 0;
            _renderer.ProvinceName = provinceName;

            _renderer.GetParent().RemoveChild(_renderer);
            QueueFree();

            var newRenderer = new WorldMapRenderer
            {
                MapWidth = width,
                MapHeight = height,
                Seed = seed,
                RandomSeed = seed < 0,
                ProvinceName = provinceName
            };
            GetParent().AddChild(newRenderer);
            newRenderer.Name = "WorldMapRenderer";
        }

        private void OnRandomSeedPressed()
        {
            int randomSeed = _staticRandom.Next();
            _seedInput.Text = randomSeed.ToString();
        }

        private void OnPreviewPressed()
        {
            RegenerateMapWithCurrentSeed();
        }

        private void RegenerateMapWithCurrentSeed()
        {
            int seed = ConvertTextToSeed(_seedInput.Text);
            _renderer.MapWidth = (int)_widthSpin.Value;
            _renderer.MapHeight = (int)_heightSpin.Value;

            if (seed != _lastSeed)
            {
                _lastSeed = seed;
                _renderer.Seed = seed;
                _renderer.RandomSeed = false;
            }

            if (_currentProvinceIndex == 0)
                UpdateCustomParameters();

            _renderer.RegenerateMap();
        }
    }
}
