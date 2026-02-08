using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI;

/// <summary>
/// 设置界面
/// </summary>
public partial class Settings : Control
{
	private Label _titleLabel;
	private Label _volumeLabel;
	private Label _musicLabel;
	private Label _languageLabel;
	private Label _resolutionLabel;
	private Label _fullscreenLabel;
	private Label _controlSettingsHint;
	private TabContainer _tabs;
	private OptionButton _languageSelector;
	private HSlider _volumeSlider;
	private HSlider _musicSlider;
	private OptionButton _resolutionSelector;
	private CheckButton _fullscreenCheck;
	private Button _backButton;
	private Button _saveButton;
	private Button _controlSettingsButton;

	private TranslationManager _translationManager;

	// 可选分辨率列表
	private readonly Vector2I[] _resolutions = new[]
	{
		new Vector2I(1280, 720),
		new Vector2I(1600, 900),
		new Vector2I(1920, 1080),
		new Vector2I(2560, 1440),
		new Vector2I(3840, 2160)
	};
	
	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("SettingsPanel/SettingsVBox/TitleLabel");
		_tabs = GetNode<TabContainer>("SettingsPanel/SettingsVBox/Tabs");
		_volumeLabel = GetNode<Label>("SettingsPanel/SettingsVBox/Tabs/AudioPage/AudioGrid/VolumeLabel");
		_musicLabel = GetNode<Label>("SettingsPanel/SettingsVBox/Tabs/AudioPage/AudioGrid/MusicLabel");
		_volumeSlider = GetNode<HSlider>("SettingsPanel/SettingsVBox/Tabs/AudioPage/AudioGrid/VolumeSlider");
		_musicSlider = GetNode<HSlider>("SettingsPanel/SettingsVBox/Tabs/AudioPage/AudioGrid/MusicSlider");
		_languageLabel = GetNode<Label>("SettingsPanel/SettingsVBox/Tabs/LanguagePage/LanguageGrid/LanguageLabel");
		_languageSelector = GetNode<OptionButton>("SettingsPanel/SettingsVBox/Tabs/LanguagePage/LanguageGrid/LanguageSelector");
		_resolutionLabel = GetNode<Label>("SettingsPanel/SettingsVBox/Tabs/DisplayPage/DisplayGrid/ResolutionLabel");
		_resolutionSelector = GetNode<OptionButton>("SettingsPanel/SettingsVBox/Tabs/DisplayPage/DisplayGrid/ResolutionSelector");
		_fullscreenLabel = GetNode<Label>("SettingsPanel/SettingsVBox/Tabs/DisplayPage/DisplayGrid/FullscreenLabel");
		_fullscreenCheck = GetNode<CheckButton>("SettingsPanel/SettingsVBox/Tabs/DisplayPage/DisplayGrid/FullscreenCheck");
		_controlSettingsHint = GetNode<Label>("SettingsPanel/SettingsVBox/Tabs/ControlsPage/ControlSettingsHint");
		_controlSettingsButton = GetNode<Button>("SettingsPanel/SettingsVBox/Tabs/ControlsPage/ControlSettingsButton");
		_saveButton = GetNode<Button>("SettingsPanel/SettingsVBox/ButtonsHBox/SaveButton");
		_backButton = GetNode<Button>("SettingsPanel/SettingsVBox/ButtonsHBox/BackButton");

		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		if (_fullscreenCheck != null)
		{
			_fullscreenCheck.Toggled += OnFullscreenToggled;
		}
		if (_backButton != null)
		{
			_backButton.Pressed += OnBackPressed;
		}
		if (_saveButton != null)
		{
			_saveButton.Pressed += OnSavePressed;
		}
		if (_controlSettingsButton != null)
		{
			_controlSettingsButton.Pressed += OnControlSettingsPressed;
		}
		if (_languageSelector != null)
		{
			_languageSelector.ItemSelected += OnLanguageSelected;
			SetupLanguageOptions();
		}
		if (_resolutionSelector != null)
		{
			_resolutionSelector.ItemSelected += OnResolutionSelected;
			SetupResolutionOptions();
		}

		UpdateUIText();
	}
	
	private void SetupLanguageOptions()
	{
		if (_languageSelector == null) return;

		_languageSelector.Clear();
		_languageSelector.AddItem("中文", 0);
		_languageSelector.AddItem("English", 1);

		var currentLang = TranslationManager.Instance.CurrentLanguage;
		if (currentLang == "zh-CN")
		{
			_languageSelector.Selected = 0;
		}
		else
		{
			_languageSelector.Selected = 1;
		}
	}
	
	private void OnLanguageSelected(long index)
	{
		if (index == 0)
		{
			TranslationManager.Instance.CurrentLanguage = "zh-CN";
		}
		else
		{
			TranslationManager.Instance.CurrentLanguage = "en";
		}
	}
	
	private void OnLanguageChanged(string language)
	{
		UpdateUIText();
	}
	
	private void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_titleLabel != null)
		{
			_titleLabel.Text = tm.Tr("settings");
		}
		if (_volumeLabel != null)
		{
			_volumeLabel.Text = tm.Tr("sound_volume");
		}
		if (_musicLabel != null)
		{
			_musicLabel.Text = tm.Tr("music_volume");
		}
		if (_fullscreenCheck != null)
		{
			_fullscreenCheck.Text = tm.Tr("fullscreen");
		}
		if (_backButton != null)
		{
			_backButton.Text = tm.Tr("back");
		}
		if (_languageLabel != null)
		{
			_languageLabel.Text = tm.Tr("language");
		}
		if (_resolutionLabel != null)
		{
			_resolutionLabel.Text = tm.Tr("resolution");
		}
		if (_fullscreenLabel != null)
		{
			_fullscreenLabel.Text = tm.Tr("fullscreen");
		}
		if (_saveButton != null)
		{
			_saveButton.Text = tm.Tr("save_settings");
		}
		if (_controlSettingsButton != null)
		{
			_controlSettingsButton.Text = tm.Tr("control_settings");
		}
		if (_controlSettingsHint != null)
		{
			_controlSettingsHint.Text = tm.Tr("controls_hint");
		}
		if (_tabs != null)
		{
			_tabs.SetTabTitle(0, tm.Tr("settings_tab_audio"));
			_tabs.SetTabTitle(1, tm.Tr("settings_tab_language"));
			_tabs.SetTabTitle(2, tm.Tr("settings_tab_display"));
			_tabs.SetTabTitle(3, tm.Tr("settings_tab_controls"));
		}
	}
	
	private void OnFullscreenToggled(bool pressed)
	{
		if (pressed)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
	}
	
	private void OnBackPressed()
	{
		SceneNavigator.Instance.GoBack();
	}

	private void OnSavePressed()
	{
		var config = new ConfigFile();
		
		// 保存音量设置
		if (_volumeSlider != null)
		{
			config.SetValue("audio", "volume", _volumeSlider.Value);
		}
		if (_musicSlider != null)
		{
			config.SetValue("audio", "music_volume", _musicSlider.Value);
		}
		
		// 保存分辨率设置
		if (_resolutionSelector != null && _resolutionSelector.Selected >= 0)
		{
			var res = _resolutions[_resolutionSelector.Selected];
			config.SetValue("display", "resolution_x", res.X);
			config.SetValue("display", "resolution_y", res.Y);
		}
		
		// 保存全屏设置
		if (_fullscreenCheck != null)
		{
			config.SetValue("display", "fullscreen", _fullscreenCheck.ButtonPressed);
		}
		
		// 保存语言设置
		config.SetValue("general", "language", TranslationManager.Instance.CurrentLanguage);
		
		// 保存到文件
		var error = config.Save("user://settings.cfg");
		if (error == Error.Ok)
		{
			GD.Print("Settings saved successfully");
		}
		else
		{
			GD.PrintErr($"Failed to save settings: {error}");
		}
	}

	private void OnControlSettingsPressed()
	{
		SceneNavigator.Instance.NavigateTo("res://Scenes/UI/ControlSettings.tscn");
	}

	private void SetupResolutionOptions()
	{
		if (_resolutionSelector == null) return;

		_resolutionSelector.Clear();
		for (int i = 0; i < _resolutions.Length; i++)
		{
			var res = _resolutions[i];
			_resolutionSelector.AddItem($"{res.X}x{res.Y}", i);
		}

		// 设置当前分辨率为默认选项
		var currentWidth = DisplayServer.WindowGetSize().X;
		int selectedIndex = 2; // 默认 1920x1080
		for (int i = 0; i < _resolutions.Length; i++)
		{
			if (_resolutions[i].X == currentWidth)
			{
				selectedIndex = i;
				break;
			}
		}
		_resolutionSelector.Selected = selectedIndex;
	}

	private void OnResolutionSelected(long index)
	{
		if (index < 0 || index >= _resolutions.Length) return;

		var newSize = _resolutions[index];

		// 如果当前是全屏，先退出全屏模式
		var currentMode = DisplayServer.WindowGetMode();
		if (currentMode == DisplayServer.WindowMode.Fullscreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}

		// 设置窗口大小
		DisplayServer.WindowSetSize(newSize);

		// 尝试兼容方式设置窗口位置（居中）
		var screenId = DisplayServer.WindowGetCurrentScreen();
		var screenSize = DisplayServer.ScreenGetSize(screenId);
		var screenPos = new Vector2I(
			(screenSize.X - newSize.X) / 2,
			(screenSize.Y - newSize.Y) / 2
		);
		DisplayServer.WindowSetPosition(screenPos, screenId);

		GD.Print($"Resolution changed to: {newSize.X}x{newSize.Y}");
	}
}
