using System;
using System.Collections.Generic;
using Godot;

namespace FantasyMapGenerator.Scripts.Utils;

public partial class TranslationManager : Node
{
	private static TranslationManager _instance;
	private Dictionary<string, Dictionary<string, string>> _translations;
	private string _currentLanguage = "zh-CN";
	private bool _initialized;

	public static TranslationManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new TranslationManager();
				_instance.EnsureInitialized();
			}
			return _instance;
		}
	}

	public string CurrentLanguage
	{
		get => _currentLanguage;
		set
		{
			if (_translations != null && _translations.ContainsKey(value))
			{
				_currentLanguage = value;
				SaveLanguagePreference();
				EmitSignal(SignalName.LanguageChanged, _currentLanguage);
			}
		}
	}

	[Signal]
	public delegate void LanguageChangedEventHandler(string language);

	public TranslationManager()
	{
		_translations = new Dictionary<string, Dictionary<string, string>>();
	}

	private void EnsureInitialized()
	{
		if (_initialized) return;
		_initialized = true;
		LoadTranslations();
		LoadLanguagePreference();
	}

	public override void _Ready()
	{
		if (_instance == null)
		{
			_instance = this;
		}
		EnsureInitialized();
	}

	private void LoadTranslations()
	{
		_translations["zh-CN"] = new Dictionary<string, string>
		{
			["app_title"] = "奇幻地图生成器",
			["welcome_subtitle"] = "点击下方按钮生成地图",
			["new_game"] = "新游戏",
			["load_game"] = "加载存档",
			["mod_manager"] = "模组管理",
			["settings"] = "设置",
			["control_settings"] = "操作设置",
			["quit_game"] = "退出游戏",
			["version"] = "v1.0.0",
			["back"] = "返回",
			["sound_volume"] = "音效音量",
			["music_volume"] = "音乐音量",
			["language"] = "语言",
			["resolution"] = "分辨率",
			["fullscreen"] = "全屏",
			["controls_hint"] = "打开操作设置进行更细致的控制",
			["settings_tab_audio"] = "音频",
			["settings_tab_language"] = "语言",
			["settings_tab_display"] = "显示",
			["settings_tab_controls"] = "控制",
			["load_game_title"] = "加载存档",
			["installed_mods"] = "已安装的模组",
			["menu"] = "菜单",
			["map_menu"] = "地图",
			["map_settings"] = "地图设置",
			["map_display"] = "地图显示",
			["map_layers"] = "图层",
			["preset"] = "预设",
			["layer_preset"] = "图层预设",
			["layer_list"] = "图层列表",
			["editable_elements"] = "可编辑元素",
			["border_settings"] = "边界设置",
			["layer_preset_political"] = "政治地图",
			["layer_preset_cultural"] = "文化地图",
			["layer_preset_religions"] = "宗教地图",
			["layer_preset_provinces"] = "省份地图",
			["layer_preset_biomes"] = "生态地图",
			["layer_preset_heightmap"] = "高度图",
			["layer_preset_physical"] = "自然地貌",
			["layer_preset_poi"] = "兴趣点",
			["layer_preset_military"] = "军事地图",
			["layer_preset_emblems"] = "徽章",
			["layer_preset_landmass"] = "纯陆块",
			["layer_preset_custom"] = "自定义",
			["map_tab_layers"] = "图层",
			["map_tab_edit"] = "编辑",
			["map_tab_border"] = "边界",
			["map_tab_theme"] = "主题",
			["layer_section_base"] = "基础图层",
			["layer_section_political"] = "行政/政治",
			["layer_section_environment"] = "自然/环境",
			["layer_section_overlay"] = "标注/装饰",
			["layer_texture"] = "纹理",
			["layer_heightmap"] = "高度图",
			["layer_biomes"] = "生态",
			["layer_cells"] = "网格单元",
			["layer_grid"] = "网格",
			["layer_coordinates"] = "坐标",
			["layer_compass"] = "罗盘",
			["layer_rivers"] = "河流",
			["layer_relief"] = "地形符号",
			["layer_religions"] = "宗教",
			["layer_cultures"] = "文化",
			["layer_states"] = "国家",
			["layer_provinces"] = "省份",
			["layer_zones"] = "区域",
			["layer_borders"] = "边界",
			["layer_routes"] = "路线",
			["layer_temperature"] = "温度",
			["layer_population"] = "人口",
			["layer_ice"] = "冰雪",
			["layer_precipitation"] = "降水",
			["layer_emblems"] = "徽章",
			["layer_burg_icons"] = "城镇图标",
			["layer_labels"] = "标签",
			["layer_military"] = "军事",
			["layer_markers"] = "标记",
			["layer_rulers"] = "标尺",
			["layer_scale_bar"] = "比例尺",
			["layer_vignette"] = "暗角",
			["layer_unknown"] = "未知图层",
			["edit_biomes"] = "生态",
			["edit_burgs"] = "城镇",
			["edit_cultures"] = "文化",
			["edit_diplomacy"] = "外交",
			["edit_emblems"] = "徽章",
			["edit_heightmap"] = "高度图",
			["edit_markers"] = "标记",
			["edit_military"] = "军事",
			["edit_namesbase"] = "命名库",
			["edit_notes"] = "备注",
			["edit_provinces"] = "省份",
			["edit_religions"] = "宗教",
			["edit_rivers"] = "河流",
			["edit_routes"] = "路线",
			["edit_states"] = "国家",
			["edit_units"] = "单位",
			["edit_zones"] = "区域",
			["edit_unknown"] = "未知",
			["edit_group_political"] = "行政/政治",
			["edit_group_nature"] = "自然/环境",
			["edit_group_overlay"] = "标注/装饰",
			["map_layer_terrain"] = "地形图层",
			["map_layer_countries"] = "国家图层",
			["map_layer_ecology"] = "生态环境图层",
			["map_overlay"] = "叠加",
			["map_show_rivers"] = "显示河流",
			["map_show_names"] = "显示名称",
			["map_theme"] = "主题",
			["map_theme_style"] = "样式",
			["map_theme_heightmap"] = "高度着色",
			["map_theme_contour"] = "等高线",
			["map_theme_heatmap"] = "热力图",
			["system_menu"] = "系统",
			["resume"] = "返回游戏",
			["regenerate_map"] = "重新生成地图",
			["map_view_scale"] = "地图大小: {0}%",
			["generation_settings"] = "生成设置",
			["map_hierarchy_settings"] = "层级地图设置",
			["map_type"] = "地图类型",
			["map_type_global_city"] = "全球（市级）",
			["map_type_national_county"] = "全国（县级）",
			["map_type_custom"] = "自定义",
			["map_style"] = "地图风格",
			["map_style_ink_fantasy"] = "水墨奇幻",
			["map_style_parchment"] = "羊皮古卷",
			["map_style_naval_chart"] = "航海海图",
			["map_style_relief"] = "地形浮雕",
			["map_style_heatmap"] = "热力分层",
			["map_style_monochrome"] = "黑白线稿",
			["map_style_desc_ink_fantasy"] = "偏奇幻插画风，地貌和行政信息平衡，适合综合展示。",
			["map_style_desc_parchment"] = "偏古地图纸张质感，轮廓清晰，适合历史感世界观。",
			["map_style_desc_naval_chart"] = "强调海域层次和海岸线，适合群岛与航海题材。",
			["map_style_desc_relief"] = "强化山脉与地形起伏，适合地貌导向的地图阅读。",
			["map_style_desc_heatmap"] = "高对比温度感配色，适合快速观察高低地分布。",
			["map_style_desc_monochrome"] = "黑白线稿风格，信息简洁，适合打印或后续标注。",
			["cell_count"] = "单元数量",
			["world_cell_count"] = "世界单元数",
			["country_cell_count"] = "全国单元数",
			["province_cell_count"] = "省份单元数",
			["city_cell_count"] = "市级单元数",
			["country_settings"] = "国家设置",
			["country_count"] = "国家数量",
			["min_country_cells"] = "最小国家单元",
			["use_multithreading"] = "多线程生成",
			["enable_map_drilldown"] = "点击进入下级地图",
			["show_countries"] = "显示国家分区",
			["show_country_borders"] = "显示国家边界",
			["country_border_width"] = "边界粗细: {0}",
			["country_fill_alpha"] = "填充透明度: {0}",
			["country_border_color"] = "边界颜色",
			["route_settings"] = "路线设置",
			["route_burgs_min"] = "每国城镇最小: {0}",
			["route_burgs_max"] = "每国城镇最大: {0}",
			["route_extra_connection_chance"] = "额外连接概率: {0}",
			["route_extra_connection_scale"] = "连接尺度: {0}",
			["route_primary_width"] = "主干线宽: {0}",
			["route_secondary_width"] = "次干线宽: {0}",
			["route_slope_weight"] = "坡度惩罚: {0}",
			["route_elevation_weight"] = "高海拔惩罚: {0}",
			["route_water_penalty"] = "涉水惩罚: {0}",
			["route_bridge_flux"] = "桥点阈值: {0}",
			["route_bridge_multiplier"] = "桥点惩罚倍数: {0}",
			["river_density"] = "河流密集度: {0}x",
			["reset_map_settings"] = "恢复默认",
			["map_settings_saved"] = "设置已保存",
			["map_svg_exported"] = "矢量地图已导出 (Ctrl+Shift+E)",
			["map_svg_export_failed"] = "矢量地图导出失败",
			["apply_generate"] = "应用并生成",
			["close"] = "关闭",
			["back_to_main_menu"] = "返回主菜单",
			["back_to_parent_map"] = "返回上级地图",
			["game_paused"] = "游戏已暂停",
			["generate_new_map"] = "生成新地图",
			["welcome_generating_map"] = "正在生成地图，请稍候...",
			["sample_mod"] = "示例模组",
			["save_slot_1"] = "存档 1",
			["save_slot_2"] = "存档 2",
			["auto_save"] = "自动存档",
			["save_settings"] = "保存设置",
			["controls_title"] = "操作设置",
			["controls_zoom_section"] = "缩放设置",
			["controls_tab_zoom"] = "缩放",
			["controls_move_section"] = "移动设置",
			["controls_tab_move"] = "移动",
			["controls_tab_advanced"] = "高级",
			["controls_zoom_smoothing"] = "平滑缩放",
			["controls_zoom_speed"] = "缩放速度",
			["controls_zoom_impulse"] = "缩放惯性力度",
			["controls_zoom_damping"] = "缩放惯性阻尼",
			["controls_zoom_max_velocity"] = "缩放最大速度",
			["controls_zoom_step"] = "缩放步进",
			["controls_zoom_step_fine"] = "细调步进 (Ctrl)",
			["controls_zoom_step_coarse"] = "粗调步进 (Shift)",
			["controls_enable_keyboard_pan"] = "启用键盘移动",
			["controls_scale_move_speed"] = "移动速度随缩放调整",
			["controls_move_speed"] = "移动速度",
			["controls_move_acceleration"] = "移动加速度",
			["controls_move_damping"] = "移动阻尼",
			["controls_reset"] = "恢复默认",
			["preview_camera_desc"] = "缩放 {0}% · 高度 {1}",
			["preview_camera_status_building"] = "构建中",
			["preview_camera_desc_building"] = "正在创建3D寰宇...",
			["preview_camera_status_click"] = "点击星球",
			["preview_camera_desc_click"] = "启动3D寰宇预览",
			["preview_camera_idle_desc"] = "太阳系边缘",
			["preview_building_cosmos"] = "正在构建3D寰宇...",
			["preview_light_response"] = "光照响应: {0}%",
			["preview_distance_au"] = "{0} 光年",
			["preview_distance_million_km"] = "{0} 百万公里",
			["preview_distance_km"] = "{0} 公里"
		};

		_translations["en"] = new Dictionary<string, string>
		{
			["app_title"] = "Fantasy Map Generator",
			["welcome_subtitle"] = "Click the button below to generate a map",
			["new_game"] = "New Game",
			["load_game"] = "Load Game",
			["mod_manager"] = "Mod Manager",
			["settings"] = "Settings",
			["control_settings"] = "Controls",
			["quit_game"] = "Quit Game",
			["version"] = "v1.0.0",
			["back"] = "Back",
			["sound_volume"] = "Sound Volume",
			["music_volume"] = "Music Volume",
			["language"] = "Language",
			["resolution"] = "Resolution",
			["fullscreen"] = "Fullscreen",
			["controls_hint"] = "Open controls settings for detailed input options",
			["settings_tab_audio"] = "Audio",
			["settings_tab_language"] = "Language",
			["settings_tab_display"] = "Display",
			["settings_tab_controls"] = "Controls",
			["load_game_title"] = "Load Game",
			["installed_mods"] = "Installed Mods",
			["menu"] = "Menu",
			["map_menu"] = "Map",
			["map_settings"] = "Map Settings",
			["map_display"] = "Map Display",
			["map_layers"] = "Layers",
			["preset"] = "Preset",
			["layer_preset"] = "Layer Preset",
			["layer_list"] = "Layer List",
			["editable_elements"] = "Editable Elements",
			["border_settings"] = "Border Settings",
			["layer_preset_political"] = "Political Map",
			["layer_preset_cultural"] = "Cultural Map",
			["layer_preset_religions"] = "Religions Map",
			["layer_preset_provinces"] = "Provinces Map",
			["layer_preset_biomes"] = "Biomes Map",
			["layer_preset_heightmap"] = "Heightmap",
			["layer_preset_physical"] = "Physical Map",
			["layer_preset_poi"] = "Places of Interest",
			["layer_preset_military"] = "Military Map",
			["layer_preset_emblems"] = "Emblems",
			["layer_preset_landmass"] = "Pure Landmass",
			["layer_preset_custom"] = "Custom",
			["map_tab_layers"] = "Layers",
			["map_tab_edit"] = "Edit",
			["map_tab_border"] = "Borders",
			["map_tab_theme"] = "Theme",
			["layer_section_base"] = "Base Layers",
			["layer_section_political"] = "Political",
			["layer_section_environment"] = "Environment",
			["layer_section_overlay"] = "Labels & Decor",
			["layer_texture"] = "Texture",
			["layer_heightmap"] = "Heightmap",
			["layer_biomes"] = "Biomes",
			["layer_cells"] = "Cells",
			["layer_grid"] = "Grid",
			["layer_coordinates"] = "Coordinates",
			["layer_compass"] = "Compass",
			["layer_rivers"] = "Rivers",
			["layer_relief"] = "Relief",
			["layer_religions"] = "Religions",
			["layer_cultures"] = "Cultures",
			["layer_states"] = "States",
			["layer_provinces"] = "Provinces",
			["layer_zones"] = "Zones",
			["layer_borders"] = "Borders",
			["layer_routes"] = "Routes",
			["layer_temperature"] = "Temperature",
			["layer_population"] = "Population",
			["layer_ice"] = "Ice",
			["layer_precipitation"] = "Precipitation",
			["layer_emblems"] = "Emblems",
			["layer_burg_icons"] = "Burg Icons",
			["layer_labels"] = "Labels",
			["layer_military"] = "Military",
			["layer_markers"] = "Markers",
			["layer_rulers"] = "Rulers",
			["layer_scale_bar"] = "Scale Bar",
			["layer_vignette"] = "Vignette",
			["layer_unknown"] = "Unknown Layer",
			["edit_biomes"] = "Biomes",
			["edit_burgs"] = "Burgs",
			["edit_cultures"] = "Cultures",
			["edit_diplomacy"] = "Diplomacy",
			["edit_emblems"] = "Emblems",
			["edit_heightmap"] = "Heightmap",
			["edit_markers"] = "Markers",
			["edit_military"] = "Military",
			["edit_namesbase"] = "Namesbase",
			["edit_notes"] = "Notes",
			["edit_provinces"] = "Provinces",
			["edit_religions"] = "Religions",
			["edit_rivers"] = "Rivers",
			["edit_routes"] = "Routes",
			["edit_states"] = "States",
			["edit_units"] = "Units",
			["edit_zones"] = "Zones",
			["edit_unknown"] = "Unknown",
			["edit_group_political"] = "Political",
			["edit_group_nature"] = "Nature",
			["edit_group_overlay"] = "Overlay",
			["map_layer_terrain"] = "Terrain Layer",
			["map_layer_countries"] = "Countries Layer",
			["map_layer_ecology"] = "Ecology Layer",
			["map_overlay"] = "Overlays",
			["map_show_rivers"] = "Show Rivers",
			["map_show_names"] = "Show Names",
			["map_theme"] = "Theme",
			["map_theme_style"] = "Style",
			["map_theme_heightmap"] = "Heightmap",
			["map_theme_contour"] = "Contour",
			["map_theme_heatmap"] = "Heatmap",
			["system_menu"] = "System",
			["resume"] = "Resume",
			["regenerate_map"] = "Regenerate Map",
			["map_view_scale"] = "Map Size: {0}%",
			["generation_settings"] = "Generation Settings",
			["map_hierarchy_settings"] = "Hierarchy Settings",
			["map_type"] = "Map Type",
			["map_type_global_city"] = "Global (City)",
			["map_type_national_county"] = "National (County)",
			["map_type_custom"] = "Custom",
			["map_style"] = "Map Style",
			["map_style_ink_fantasy"] = "Ink Fantasy",
			["map_style_parchment"] = "Parchment Atlas",
			["map_style_naval_chart"] = "Naval Chart",
			["map_style_relief"] = "Relief",
			["map_style_heatmap"] = "Heat Layer",
			["map_style_monochrome"] = "Monochrome",
			["map_style_desc_ink_fantasy"] = "Balanced fantasy illustration style for both terrain and political readability.",
			["map_style_desc_parchment"] = "Old parchment tone with crisp contours, ideal for historical atmosphere.",
			["map_style_desc_naval_chart"] = "Ocean-forward palette highlighting coastlines and maritime regions.",
			["map_style_desc_relief"] = "Emphasizes mountain mass and landform structure for terrain-focused maps.",
			["map_style_desc_heatmap"] = "High-contrast elevation impression for quick terrain intensity scanning.",
			["map_style_desc_monochrome"] = "Clean grayscale linework suitable for print and manual annotation.",
			["cell_count"] = "Cell Count",
			["world_cell_count"] = "World Cell Count",
			["country_cell_count"] = "Country Cell Count",
			["province_cell_count"] = "Province Cell Count",
			["city_cell_count"] = "City Cell Count",
			["country_settings"] = "Country Settings",
			["country_count"] = "Country Count",
			["min_country_cells"] = "Min Country Cells",
			["use_multithreading"] = "Use Multithreading",
			["enable_map_drilldown"] = "Click to Enter Sub-map",
			["show_countries"] = "Show Countries",
			["show_country_borders"] = "Show Country Borders",
			["country_border_width"] = "Border Width: {0}",
			["country_fill_alpha"] = "Fill Opacity: {0}",
			["country_border_color"] = "Border Color",
			["route_settings"] = "Route Settings",
			["route_burgs_min"] = "Burgs Min: {0}",
			["route_burgs_max"] = "Burgs Max: {0}",
			["route_extra_connection_chance"] = "Extra Connection Chance: {0}",
			["route_extra_connection_scale"] = "Connection Scale: {0}",
			["route_primary_width"] = "Primary Width: {0}",
			["route_secondary_width"] = "Secondary Width: {0}",
			["route_slope_weight"] = "Slope Weight: {0}",
			["route_elevation_weight"] = "Elevation Weight: {0}",
			["route_water_penalty"] = "Water Penalty: {0}",
			["route_bridge_flux"] = "Bridge Flux Threshold: {0}",
			["route_bridge_multiplier"] = "Bridge Penalty Multiplier: {0}",
			["river_density"] = "River Density: {0}x",
			["reset_map_settings"] = "Reset to Defaults",
			["map_settings_saved"] = "Settings saved",
			["map_svg_exported"] = "Vector map exported (Ctrl+Shift+E)",
			["map_svg_export_failed"] = "Vector map export failed",
			["apply_generate"] = "Apply & Generate",
			["close"] = "Close",
			["back_to_main_menu"] = "Back to Main Menu",
			["back_to_parent_map"] = "Back to Parent Map",
			["game_paused"] = "Game Paused",
			["generate_new_map"] = "Generate New Map",
			["welcome_generating_map"] = "Generating map, please wait...",
			["sample_mod"] = "Sample Mod",
			["save_slot_1"] = "Save 1",
			["save_slot_2"] = "Save 2",
			["auto_save"] = "Auto Save",
			["save_settings"] = "Save Settings",
			["controls_title"] = "Controls",
			["controls_zoom_section"] = "Zoom Settings",
			["controls_tab_zoom"] = "Zoom",
			["controls_move_section"] = "Movement Settings",
			["controls_tab_move"] = "Move",
			["controls_tab_advanced"] = "Advanced",
			["controls_zoom_smoothing"] = "Smooth Zoom",
			["controls_zoom_speed"] = "Zoom Speed",
			["controls_zoom_impulse"] = "Zoom Inertia Strength",
			["controls_zoom_damping"] = "Zoom Inertia Damping",
			["controls_zoom_max_velocity"] = "Max Zoom Velocity",
			["controls_zoom_step"] = "Zoom Step",
			["controls_zoom_step_fine"] = "Fine Step (Ctrl)",
			["controls_zoom_step_coarse"] = "Coarse Step (Shift)",
			["controls_enable_keyboard_pan"] = "Enable Keyboard Pan",
			["controls_scale_move_speed"] = "Scale Move Speed With Zoom",
			["controls_move_speed"] = "Move Speed",
			["controls_move_acceleration"] = "Move Acceleration",
			["controls_move_damping"] = "Move Damping",
			["controls_reset"] = "Reset Defaults",
			["preview_camera_desc"] = "Zoom {0}% · Altitude {1}",
			["preview_camera_status_building"] = "Building",
			["preview_camera_desc_building"] = "Creating 3D cosmos...",
			["preview_camera_status_click"] = "Click Planet",
			["preview_camera_desc_click"] = "Start 3D cosmos preview",
			["preview_camera_idle_desc"] = "Solar System Edge",
			["preview_building_cosmos"] = "Building 3D cosmos...",
			["preview_light_response"] = "Light Response: {0}%",
			["preview_distance_au"] = "{0} AU",
			["preview_distance_million_km"] = "{0}M km",
			["preview_distance_km"] = "{0} km"
		};
	}

	private void LoadLanguagePreference()
	{
		// 先加载翻译
		LoadTranslations();
		
		// 从配置文件读取保存的语言设置
		var config = new ConfigFile();
		var error = config.Load("user://settings.cfg");
		if (error == Error.Ok && config.HasSection("general"))
		{
			var keys = config.GetSectionKeys("general");
			if (keys != null && Array.Exists(keys, k => k == "language"))
			{
				var savedLang = (string)config.GetValue("general", "language");
				if (_translations.ContainsKey(savedLang))
				{
					_currentLanguage = savedLang;
				}
			}
		}
	}

	private void SaveLanguagePreference()
	{
		var config = new ConfigFile();
		config.SetValue("general", "language", _currentLanguage);
		config.Save("user://settings.cfg");
	}

	public string Tr(string key)
	{
		EnsureInitialized();
		if (_translations.TryGetValue(_currentLanguage, out var langDict))
		{
			if (langDict.TryGetValue(key, out var value))
			{
				return value;
			}
		}
		return key;
	}

	public string TrWithFormat(string key, params string[] args)
	{
		var text = Tr(key);
		for (int i = 0; i < args.Length; i++)
		{
			text = text.Replace($"{{{i}}}", args[i]);
		}
		return text;
	}

	public string[] GetAvailableLanguages()
	{
		var languages = new List<string>();
		foreach (var lang in _translations.Keys)
		{
			languages.Add(lang);
		}
		return languages.ToArray();
	}
}
