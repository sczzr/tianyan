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
			["resolution"] = "分辨率",
			["fullscreen"] = "全屏",
			["load_game_title"] = "加载存档",
			["installed_mods"] = "已安装的模组",
			["menu"] = "菜单",
			["map_menu"] = "地图",
			["map_settings"] = "地图设置",
			["map_display"] = "地图显示",
			["map_layers"] = "图层",
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
			["map_width"] = "宽度",
			["map_height"] = "高度",
			["cell_count"] = "单元数量",
			["country_settings"] = "国家设置",
			["country_count"] = "国家数量",
			["show_countries"] = "显示国家分区",
			["show_country_borders"] = "显示国家边界",
			["country_border_width"] = "边界粗细: {0}",
			["country_fill_alpha"] = "填充透明度: {0}",
			["country_border_color"] = "边界颜色",
			["river_density"] = "河流密集度: {0}x",
			["apply_generate"] = "应用并生成",
			["close"] = "关闭",
			["back_to_main_menu"] = "返回主菜单",
			["game_paused"] = "游戏已暂停",
			["generate_new_map"] = "生成新地图",
			["sample_mod"] = "示例模组",
			["save_slot_1"] = "存档 1",
			["save_slot_2"] = "存档 2",
			["auto_save"] = "自动存档",
			["save_settings"] = "保存设置",
			["controls_title"] = "操作设置",
			["controls_zoom_section"] = "缩放设置",
			["controls_move_section"] = "移动设置",
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
			["controls_reset"] = "恢复默认"
		};

		_translations["en"] = new Dictionary<string, string>
		{
			["app_title"] = "Fantasy Map Generator",
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
			["resolution"] = "Resolution",
			["fullscreen"] = "Fullscreen",
			["load_game_title"] = "Load Game",
			["installed_mods"] = "Installed Mods",
			["menu"] = "Menu",
			["map_menu"] = "Map",
			["map_settings"] = "Map Settings",
			["map_display"] = "Map Display",
			["map_layers"] = "Layers",
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
			["map_width"] = "Width",
			["map_height"] = "Height",
			["cell_count"] = "Cell Count",
			["country_settings"] = "Country Settings",
			["country_count"] = "Country Count",
			["show_countries"] = "Show Countries",
			["show_country_borders"] = "Show Country Borders",
			["country_border_width"] = "Border Width: {0}",
			["country_fill_alpha"] = "Fill Opacity: {0}",
			["country_border_color"] = "Border Color",
			["river_density"] = "River Density: {0}x",
			["apply_generate"] = "Apply & Generate",
			["close"] = "Close",
			["back_to_main_menu"] = "Back to Main Menu",
			["game_paused"] = "Game Paused",
			["generate_new_map"] = "Generate New Map",
			["sample_mod"] = "Sample Mod",
			["save_slot_1"] = "Save 1",
			["save_slot_2"] = "Save 2",
			["auto_save"] = "Auto Save",
			["save_settings"] = "Save Settings",
			["controls_title"] = "Controls",
			["controls_zoom_section"] = "Zoom Settings",
			["controls_move_section"] = "Movement Settings",
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
			["controls_reset"] = "Reset Defaults"
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
