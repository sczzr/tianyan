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
			["system_menu"] = "系统",
			["resume"] = "返回游戏",
			["regenerate_map"] = "重新生成地图",
			["map_view_scale"] = "地图大小: {0}%",
			["back_to_main_menu"] = "返回主菜单",
			["game_paused"] = "游戏已暂停",
			["generate_new_map"] = "生成新地图",
			["sample_mod"] = "示例模组",
			["save_slot_1"] = "存档 1",
			["save_slot_2"] = "存档 2",
			["auto_save"] = "自动存档",
			["save_settings"] = "保存设置"
		};

		_translations["en"] = new Dictionary<string, string>
		{
			["app_title"] = "Fantasy Map Generator",
			["new_game"] = "New Game",
			["load_game"] = "Load Game",
			["mod_manager"] = "Mod Manager",
			["settings"] = "Settings",
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
			["system_menu"] = "System",
			["resume"] = "Resume",
			["regenerate_map"] = "Regenerate Map",
			["map_view_scale"] = "Map Size: {0}%",
			["back_to_main_menu"] = "Back to Main Menu",
			["game_paused"] = "Game Paused",
			["generate_new_map"] = "Generate New Map",
			["sample_mod"] = "Sample Mod",
			["save_slot_1"] = "Save 1",
			["save_slot_2"] = "Save 2",
			["auto_save"] = "Auto Save",
			["save_settings"] = "Save Settings"
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
