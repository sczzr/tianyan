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
	private OptionButton _languageSelector;
	private HSlider _volumeSlider;
	private HSlider _musicSlider;
	private CheckButton _fullscreenCheck;
	private Button _backButton;

	private TranslationManager _translationManager;

	public override void _Ready()
	{
		_backButton = GetNode<Button>("SettingsPanel/SettingsVBox/BackButton");
		_languageSelector = GetNode<OptionButton>("SettingsPanel/SettingsVBox/LanguageSelector");
		_volumeSlider = GetNode<HSlider>("SettingsPanel/SettingsVBox/VolumeSlider");
		_musicSlider = GetNode<HSlider>("SettingsPanel/SettingsVBox/MusicSlider");
		_fullscreenCheck = GetNode<CheckButton>("SettingsPanel/SettingsVBox/FullscreenCheck");

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
		if (_languageSelector != null)
		{
			_languageSelector.ItemSelected += OnLanguageSelected;
			SetupLanguageOptions();
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
		GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
	}
}
