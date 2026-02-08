using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI;

public partial class MainMenu : Control
{
	private ColorRect _background;
	private Window _rootWindow;
	private Vector2I _lastWindowSize;
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
		_background = GetNode<ColorRect>("Background");
		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		_newGameButton = GetNode<Button>("menuPanel/menuVBox/newGameButton");
		_loadGameButton = GetNode<Button>("menuPanel/menuVBox/loadGameButton");
		_modManagerButton = GetNode<Button>("menuPanel/menuVBox/modManagerButton");
		_settingsButton = GetNode<Button>("menuPanel/menuVBox/settingsButton");
		_quitButton = GetNode<Button>("menuPanel/menuVBox/quitButton");
		_titleLabel ??= GetNodeOrNull<Label>("menuPanel/menuVBox/titleLabel");
		_versionLabel ??= GetNodeOrNull<Label>("menuPanel/menuVBox/versionLabel");
		
		SetupMenuItems();
		UpdateUIText();

		_rootWindow = GetTree().Root;
		_lastWindowSize = DisplayServer.WindowGetSize();
		DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.ResizeDisabled, false);
		var windowMode = DisplayServer.WindowGetMode();
		var resizable = !DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.ResizeDisabled);
		var minSize = DisplayServer.WindowGetMinSize();
		var maxSize = DisplayServer.WindowGetMaxSize();
		GD.Print($"MainMenu initial sizes: window={_rootWindow?.Size} ds_window={_lastWindowSize} viewport={GetViewportRect().Size} self={Size} background={_background?.Size}");
		GD.Print($"MainMenu window state: mode={windowMode} resizable={resizable} min={minSize} max={maxSize}");
		if (_rootWindow != null)
		{
			_rootWindow.SizeChanged += OnWindowSizeChanged;
		}
		OnWindowSizeChanged();
	}

	public override void _ExitTree()
	{
		if (_rootWindow != null)
		{
			_rootWindow.SizeChanged -= OnWindowSizeChanged;
		}
	}

	private void OnWindowSizeChanged()
	{
		var targetSize = (Vector2)DisplayServer.WindowGetSize();
		if (targetSize == Vector2.Zero)
		{
			targetSize = _rootWindow?.Size ?? GetViewportRect().Size;
		}
		Size = targetSize;
		if (_background != null)
		{
			_background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			_background.Size = targetSize;
		}
		GD.Print($"MainMenu resized: window={_rootWindow?.Size} ds_window={DisplayServer.WindowGetSize()} viewport={GetViewportRect().Size} self={Size} background={_background?.Size}");
	}

	public override void _Process(double delta)
	{
		var current = DisplayServer.WindowGetSize();
		if (current != _lastWindowSize)
		{
			_lastWindowSize = current;
			OnWindowSizeChanged();
		}
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
		if (_titleLabel != null)
		{
			_titleLabel.Text = tm.Tr("app_title");
		}
		if (_versionLabel != null)
		{
			_versionLabel.Text = tm.Tr("version");
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
