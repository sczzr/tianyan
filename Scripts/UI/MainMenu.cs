using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI;

public partial class MainMenu : Control
{
	[Export] private TextureRect _backgroundImage;
	[Export] private PanelContainer _menuPanel;
	[Export] private VBoxContainer _menuVBox;
	[Export] private Label _titleLabel;
	[Export] private Label _versionLabel;

	private Button _newGameButton;
	private Button _loadGameButton;
	private Button _modManagerButton;
	private Button _settingsButton;
	private Button _quitButton;

	private TranslationManager _translationManager;

	public override void _Ready()
	{
		GD.Print("MainMenu _Ready called");
		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		_newGameButton = GetNode<Button>("menuPanel/menuVBox/newGameButton");
		_loadGameButton = GetNode<Button>("menuPanel/menuVBox/loadGameButton");
		_modManagerButton = GetNode<Button>("menuPanel/menuVBox/modManagerButton");
		_settingsButton = GetNode<Button>("menuPanel/menuVBox/settingsButton");
		_quitButton = GetNode<Button>("menuPanel/menuVBox/quitButton");
		
		SetupMenuItems();
		UpdateUIText();
	}

	private void OnLanguageChanged(string language)
	{
		UpdateUIText();
	}

	private void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_newGameButton != null)
		{
			_newGameButton.Text = tm.Tr("new_game");
		}
		if (_loadGameButton != null)
		{
			_loadGameButton.Text = tm.Tr("load_game");
		}
		if (_modManagerButton != null)
		{
			_modManagerButton.Text = tm.Tr("mod_manager");
		}
		if (_settingsButton != null)
		{
			_settingsButton.Text = tm.Tr("settings");
		}
		if (_quitButton != null)
		{
			_quitButton.Text = tm.Tr("quit_game");
		}
	}

	private void SetupMenuItems()
	{
		if (_newGameButton != null)
		{
			_newGameButton.Pressed += OnNewGamePressed;
		}

		if (_loadGameButton != null)
		{
			_loadGameButton.Pressed += OnLoadGamePressed;
		}

		if (_modManagerButton != null)
		{
			_modManagerButton.Pressed += OnModManagerPressed;
		}

		if (_settingsButton != null)
		{
			_settingsButton.Pressed += OnSettingsPressed;
		}

		if (_quitButton != null)
		{
			_quitButton.Pressed += OnQuitGamePressed;
		}
	}

	private void OnNewGamePressed()
	{
		SwitchToScene("res://Scenes/Game/Game.tscn");
	}

	private void OnLoadGamePressed()
	{
		SwitchToScene("res://Scenes/UI/LoadGame.tscn");
	}

	private void OnModManagerPressed()
	{
		SwitchToScene("res://Scenes/Game/ModManager.tscn");
	}

	private void OnSettingsPressed()
	{
		SwitchToScene("res://Scenes/UI/Settings.tscn");
	}

	private void OnQuitGamePressed()
	{
		GetTree().Quit();
	}

	private void SwitchToScene(string scenePath)
	{
		SceneNavigator.Instance.NavigateTo(scenePath);
	}
}
