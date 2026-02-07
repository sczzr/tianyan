using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI;

/// <summary>
/// 读取存档界面
/// </summary>
public partial class LoadGame : Control
{
	private Label _titleLabel;
	private Button _backButton;
	private VBoxContainer _saveList;

	private TranslationManager _translationManager;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("MainPanel/MainVBox/TitleLabel");
		_backButton = GetNode<Button>("MainPanel/MainVBox/BackButton");
		_saveList = GetNode<VBoxContainer>("MainPanel/MainVBox/ScrollContainer/SaveList");

		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		if (_backButton != null)
		{
			_backButton.Pressed += OnBackPressed;
		}

		UpdateUIText();
		LoadSaveFiles();
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
			_titleLabel.Text = tm.Tr("load_game_title");
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



	private void LoadSaveFiles()
	{
		var tm = TranslationManager.Instance;
		var saves = new Dictionary<string, string>
		{
			{ tm.Tr("save_slot_1"), "Save001Button" },
			{ tm.Tr("save_slot_2"), "Save002Button" },
			{ tm.Tr("auto_save"), "AutosaveButton" }
		};
		foreach (var save in saves)
		{
			var saveButton = GetNode<Button>($"MainPanel/MainVBox/ScrollContainer/SaveList/{save.Value}");
			if (saveButton != null)
			{
				saveButton.Text = save.Key;
				saveButton.Pressed += () => OnSavePressed(save.Key);
			}
		}
	}

	private void OnSavePressed(string saveName)
	{
		GD.Print($"Loading save: {saveName}");
	}
}
