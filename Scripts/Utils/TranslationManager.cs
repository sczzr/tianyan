using System;
using System.Collections.Generic;
using Godot;

namespace FantasyMapGenerator.Scripts.Utils;

public partial class TranslationManager : Node
{
	private static TranslationManager _instance;
	private Dictionary<string, Dictionary<string, string>> _translations;
	private string _currentLanguage = "zh-CN";

	public static TranslationManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new TranslationManager();
			}
			return _instance;
		}
	}

	public string CurrentLanguage
	{
		get => _currentLanguage;
		set
		{
			if (_translations.ContainsKey(value))
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

	public override void _Ready()
	{
		if (_instance == null)
		{
			_instance = this;
		}
		LoadTranslations();
		LoadLanguagePreference();
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
			["fullscreen"] = "全屏",
			["load_game_title"] = "加载存档",
			["installed_mods"] = "已安装的模组",
			["menu"] = "菜单"
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
			["fullscreen"] = "Fullscreen",
			["load_game_title"] = "Load Game",
			["installed_mods"] = "Installed Mods",
			["menu"] = "Menu"
		};
	}

	private void LoadLanguagePreference()
	{
		_currentLanguage = "zh-CN";
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
