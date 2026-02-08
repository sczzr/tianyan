using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI.Screens;

/// <summary>
/// Mod 管理界面
/// </summary>
public partial class ModManager : Control
{
	private Label _titleLabel;
	private Label _sampleModLabel;
	private CheckButton _sampleModCheck;
	private Button _backButton;
	private VBoxContainer _modList;

	private TranslationManager _translationManager;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("MainPanel/MainVBox/TitleLabel");
		_backButton = GetNode<Button>("MainPanel/MainVBox/BackButton");
		_modList = GetNode<VBoxContainer>("MainPanel/MainVBox/ScrollContainer/ModList");
		_sampleModLabel = GetNode<Label>("MainPanel/MainVBox/ScrollContainer/ModList/SampleModLabel");
		_sampleModCheck = GetNode<CheckButton>("MainPanel/MainVBox/ScrollContainer/ModList/SampleModCheck");

		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		if (_backButton != null)
		{
			_backButton.Pressed += OnBackPressed;
		}

		UpdateUIText();
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
			_titleLabel.Text = tm.Tr("mod_manager");
		}
		if (_sampleModLabel != null)
		{
			_sampleModLabel.Text = tm.Tr("installed_mods");
		}
		if (_sampleModCheck != null)
		{
			_sampleModCheck.Text = tm.Tr("sample_mod");
		}
		if (_backButton != null)
		{
			_backButton.Text = tm.Tr("back");
		}
	}

	private void OnBackPressed()
	{
		SceneNavigator.Instance.GoBack();
	}
}
