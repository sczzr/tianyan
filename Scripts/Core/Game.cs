using Godot;
using FantasyMapGenerator.Scripts.Rendering;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Core;

/// <summary>
/// 游戏主界面
/// </summary>
public partial class Game : Control
{
	[ExportGroup("UI Components")]
	[Export] private MapView _mapView;
	[Export] private Button _menuButton;

	private TranslationManager _translationManager;

	public override void _Ready()
	{
		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		if (_mapView == null)
		{
			_mapView = GetNode<MapView>("MapView");
		}
		SetupUI();
	}

	private void OnLanguageChanged(string language)
	{
		UpdateUIText();
	}

	private void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_menuButton != null)
		{
			_menuButton.Text = tm.Tr("menu");
		}
	}

	private void SetupMapView()
	{
		if (_mapView == null)
		{
			_mapView = new MapView();
			_mapView.Name = "MapView";
			AddChild(_mapView);
			MoveChild(_mapView, 0);
		}
	}

	private void SetupUI()
	{
		if (_menuButton != null)
		{
			_menuButton.Pressed += OnMenuPressed;
		}
	}

	private void OnMenuPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
		}
	}
}
