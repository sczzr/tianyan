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
        private HSlider _waterLevelSlider;
        private HSlider _forestSlider;
        private HSlider _desertSlider;
        private HSlider _lakeSlider;
        private Button _generateButton;
        private Button _regenerateButton;
        private RichTextLabel _terrainInfoLabel;

        private int _currentProvinceIndex = 0;

        public override void _Ready()
        {
            _renderer = GetNode<WorldMapRenderer>("WorldMapRenderer");

            _seedInput = GetNode<LineEdit>("UI/ControlPanel/VBoxContainer/SeedInput");
            _provinceOption = GetNode<OptionButton>("UI/ControlPanel/VBoxContainer/ProvinceOption");
            _widthSpin = GetNode<SpinBox>("UI/ControlPanel/VBoxContainer/SizeContainer/WidthSpin");
            _heightSpin = GetNode<SpinBox>("UI/ControlPanel/VBoxContainer/SizeContainer/HeightSpin");
            _waterLevelSlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/WaterLevelSlider");
            _forestSlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/ForestSlider");
            _desertSlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/DesertSlider");
            _lakeSlider = GetNode<HSlider>("UI/ControlPanel/VBoxContainer/LakeSlider");
            _generateButton = GetNode<Button>("UI/ControlPanel/VBoxContainer/GenerateButton");
            _regenerateButton = GetNode<Button>("UI/ControlPanel/VBoxContainer/RegenerateButton");
            _terrainInfoLabel = GetNode<RichTextLabel>("UI/TerrainInfoPanel/TerrainInfoLabel");

            SetupProvinceOptions();

            _generateButton.Pressed += OnGeneratePressed;
            _regenerateButton.Pressed += OnRegeneratePressed;
            _provinceOption.ItemSelected += OnProvinceSelected;

            // 滑块值改变时实时更新地图
            _waterLevelSlider.ValueChanged += OnCustomSliderChanged;
            _forestSlider.ValueChanged += OnCustomSliderChanged;
            _desertSlider.ValueChanged += OnCustomSliderChanged;
            _lakeSlider.ValueChanged += OnCustomSliderChanged;

            // 初始化滑块显示状态
            ToggleTerrainParameters(true);

            // 初始化地形介绍
            UpdateTerrainInfo();
        }

        private void UpdateTerrainInfo()
        {
            if (_currentProvinceIndex == 0)
            {
                float waterLevel = (float)_waterLevelSlider.Value;
                float forestDensity = (float)_forestSlider.Value;
                float desertDensity = (float)_desertSlider.Value;
                float lakeDensity = (float)_lakeSlider.Value;

                string waterDesc = waterLevel < 0.25f ? "低水位 - 海洋面积小" : (waterLevel > 0.45f ? "高水位 - 海洋面积大" : "中等水位");
                string forestDesc = forestDensity < 0.5f ? "稀疏森林" : (forestDensity > 1.5f ? "茂密森林" : "正常森林");
                string desertDesc = desertDensity < 0.5f ? "极少沙漠" : (desertDensity > 1.5f ? "大面积沙漠" : "少量沙漠");
                string lakeDesc = lakeDensity < 0.5f ? "极少湖泊" : (lakeDensity > 1.5f ? "众多湖泊" : "正常湖泊");

                _terrainInfoLabel.Text = "[b]自定义地形[/b]\n\n" +
                    "[color=#AAAAAA]水位:[/color] " + waterDesc + "\n" +
                    "[color=#AAAAAA]森林:[/color] " + forestDesc + "\n" +
                    "[color=#AAAAAA]沙漠:[/color] " + desertDesc + "\n" +
                    "[color=#AAAAAA]湖泊:[/color] " + lakeDesc;
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

                    string spiritDesc = config.SpiritDensity switch
                    {
                        < 0.3f => "[color=#FF6B6B]灵气稀薄[/color]",
                        < 0.5f => "[color=#AAAAAA]灵气一般[/color]",
                        < 0.7f => "[color=#6BFF6B]灵气充沛[/color]",
                        _ => "[color=#FFD700]灵气浓郁[/color]"
                    };

                    string specialRegionName = config.SpecialRegionType switch
                    {
                        SpecialRegionType.AncientBattlefield => "古战场",
                        SpecialRegionType.SacredMountain => "圣山",
                        SpecialRegionType.ForbiddenLand => "禁地",
                        SpecialRegionType.SpiritValley => "灵谷",
                        SpecialRegionType.DragonLair => "龙穴",
                        SpecialRegionType.FairyResidence => "仙境",
                        SpecialRegionType.DemonicRealm => "魔域",
                        SpecialRegionType.AncientTomb => "古墓",
                        SpecialRegionType.SpiritForest => "灵林",
                        SpecialRegionType.FloatingIsland => "浮空岛",
                        _ => "无"
                    };

                    string specialFeatures = "";
                    if (config.HasCaveParadise) specialFeatures += "[color=#6BFF6B]洞天福地[/color] ";
                    if (config.HasAncientRuins) specialFeatures += "[color=#FFD700]上古遗迹[/color] ";
                    if (config.HasSpiritVeins) specialFeatures += "[color=#6B6BFF]灵脉[/color] ";
                    if (config.HasMonsterActivity) specialFeatures += "[color=#FF6B6B]妖兽出没[/color] ";
                    if (string.IsNullOrEmpty(specialFeatures)) specialFeatures = "[color=#AAAAAA]无[/color]";

                    _terrainInfoLabel.Text = "[b]" + provinceName + "[/b] - " + terrainTypeName + "\n\n" +
                        "[color=#AAAAAA]" + config.Description + "[/color]\n\n" +
                        "[color=#AAAAAA]气候:[/color] " + rainDesc + ", " + tempDesc + "\n" +
                        "[color=#AAAAAA]森林:[/color] " + (int)(config.ForestRatio * 100) + "%  |  " +
                        "[color=#AAAAAA]沙漠:[/color] " + (int)(config.DesertRatio * 100) + "%\n" +
                        "[color=#AAAAAA]山地:[/color] " + (int)(config.MountainRatio * 100) + "%  |  " +
                        "[color=#AAAAAA]平原:[/color] " + (int)(config.PlainRatio * 100) + "%\n\n" +
                        "[b]灵气概况[/b]\n" +
                        "[color=#AAAAAA]浓郁度:[/color] " + spiritDesc + "\n" +
                        "[color=#AAAAAA]" + config.SpiritDescription + "[/color]\n\n" +
                        "[b]特殊区域[/b]\n" +
                        "[color=#AAAAAA]类型:[/color] " + specialRegionName + "\n" +
                        "[color=#AAAAAA]" + config.SpecialRegionDescription + "[/color]\n" +
                        "[color=#AAAAAA]特征:[/color] " + specialFeatures;
                }
            }
        }

        private void OnCustomSliderChanged(double value)
        {
            if (_currentProvinceIndex == 0)
            {
                _renderer.UseCustomParameters = true;
                _renderer.CustomWaterLevel = (float)_waterLevelSlider.Value;
                _renderer.CustomForestDensity = (float)_forestSlider.Value;
                _renderer.CustomDesertDensity = (float)_desertSlider.Value;
                _renderer.CustomLakeDensity = (float)_lakeSlider.Value;
                _renderer.RegenerateMap();
                UpdateTerrainInfo();
            }
        }

        private void SetupProvinceOptions()
        {
            ProvinceConfigManager.Initialize();
            var provinceNames = ProvinceConfigManager.GetAllProvinceNames();

            _provinceOption.Clear();
            
            // 添加"自定义"选项（ID为0）
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

            // 根据选择的项显示或隐藏地形参数
            // 当选择自定义(ID=0)时显示滑块，选择具体省份时隐藏
            bool isCustom = (index == 0);
            ToggleTerrainParameters(isCustom);

            // 更新渲染器的配置并重新生成地图
            if (isCustom)
            {
                _renderer.UseCustomParameters = true;
                _renderer.CustomWaterLevel = (float)_waterLevelSlider.Value;
                _renderer.CustomForestDensity = (float)_forestSlider.Value;
                _renderer.CustomDesertDensity = (float)_desertSlider.Value;
                _renderer.CustomLakeDensity = (float)_lakeSlider.Value;
                _renderer.ProvinceName = "";
                GD.Print($"切换到自定义模式 - 水位: {_renderer.CustomWaterLevel}, 森林: {_renderer.CustomForestDensity}, 沙漠: {_renderer.CustomDesertDensity}, 湖泊: {_renderer.CustomLakeDensity}");
            }
            else
            {
                _renderer.UseCustomParameters = false;
                _renderer.ProvinceName = _provinceOption.GetItemText((int)index);
            }
            _renderer.RegenerateMap();

            UpdateTerrainInfo();

            GD.Print("选择省份: " + _provinceOption.GetItemText((int)index) + ", 自定义模式: " + isCustom);
        }
        
        private void ToggleTerrainParameters(bool show)
        {
            // 显示或隐藏地形参数滑块及其标签
            var terrainLabel = GetNodeOrNull<Label>("UI/ControlPanel/VBoxContainer/TerrainLabel");
            var waterLevelLabel = GetNodeOrNull<Label>("UI/ControlPanel/VBoxContainer/WaterLevelLabel");
            var waterLevelSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/WaterLevelSlider");
            var forestLabel = GetNodeOrNull<Label>("UI/ControlPanel/VBoxContainer/ForestLabel");
            var forestSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/ForestSlider");
            var desertLabel = GetNodeOrNull<Label>("UI/ControlPanel/VBoxContainer/DesertLabel");
            var desertSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/DesertSlider");
            var lakeLabel = GetNodeOrNull<Label>("UI/ControlPanel/VBoxContainer/LakeLabel");
            var lakeSlider = GetNodeOrNull<HSlider>("UI/ControlPanel/VBoxContainer/LakeSlider");
            
            if (terrainLabel != null) terrainLabel.Visible = show;
            if (waterLevelLabel != null) waterLevelLabel.Visible = show;
            if (waterLevelSlider != null) waterLevelSlider.Visible = show;
            if (forestLabel != null) forestLabel.Visible = show;
            if (forestSlider != null) forestSlider.Visible = show;
            if (desertLabel != null) desertLabel.Visible = show;
            if (desertSlider != null) desertSlider.Visible = show;
            if (lakeLabel != null) lakeLabel.Visible = show;
            if (lakeSlider != null) lakeSlider.Visible = show;
        }

        private int ConvertTextToSeed(string text)
        {
            if (string.IsNullOrEmpty(text))
                return -1;

            int result;
            if (int.TryParse(text, out result))
            {
                return result;
            }

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

            // 判断是自定义还是具体省份
            bool isCustom = (_currentProvinceIndex == 0);
            string provinceName = "";
            
            if (!isCustom)
            {
                // 获取选中的省份名称（索引0是自定义，所以实际省份从索引1开始）
                provinceName = _provinceOption.GetItemText(_currentProvinceIndex);
            }
            
            GD.Print($"生成地图 - 自定义模式: {isCustom}, 省份: {provinceName}");

            _renderer.MapWidth = width;
            _renderer.MapHeight = height;
            _renderer.Seed = seed;
            _renderer.RandomSeed = seed < 0;

            _renderer.GetParent().RemoveChild(_renderer);
            QueueFree();

            var newRenderer = new WorldMapRenderer
            {
                MapWidth = width,
                MapHeight = height,
                Seed = seed,
                RandomSeed = seed < 0,
                ProvinceName = provinceName  // 如果为空字符串，WorldMapRenderer将使用滑块值
            };
            GetParent().AddChild(newRenderer);
            newRenderer.Name = "WorldMapRenderer";
        }

        private void OnRegeneratePressed()
        {
            int seed = ConvertTextToSeed(_seedInput.Text);
            _renderer.Seed = seed;
            _renderer.RandomSeed = seed < 0;

            // 确保自定义参数已更新
            if (_currentProvinceIndex == 0)
            {
                _renderer.UseCustomParameters = true;
                _renderer.CustomWaterLevel = (float)_waterLevelSlider.Value;
                _renderer.CustomForestDensity = (float)_forestSlider.Value;
                _renderer.CustomDesertDensity = (float)_desertSlider.Value;
                _renderer.CustomLakeDensity = (float)_lakeSlider.Value;
            }

            _renderer.RegenerateMap();
        }
    }
}
