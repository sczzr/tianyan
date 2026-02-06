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
		GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
	}



	private void LoadSaveFiles()
	{
		var saves = new Dictionary<string, string>
		{
			{ "存档 001", "Save001Button" },
			{ "存档 002", "Save002Button" },
			{ "自动存档", "AutosaveButton" }
		};
		foreach (var save in saves)
		{
			var saveButton = GetNode<Button>($"MainPanel/MainVBox/ScrollContainer/SaveList/{save.Value}");
			if (saveButton != null)
			{
				saveButton.Pressed += () => OnSavePressed(save.Key);
			}
		}
	}

	private void OnSavePressed(string saveName)
	{
		GD.Print($"Loading save: {saveName}");
	}
}
