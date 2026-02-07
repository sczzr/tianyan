using System;
using System.Collections.Generic;
using Godot;

namespace FantasyMapGenerator.Scripts.Utils;

/// <summary>
/// 场景导航器，用于管理场景历史和支持返回功能
/// </summary>
public partial class SceneNavigator : Node
{
	private static SceneNavigator _instance;
	private readonly Stack<string> _sceneHistory = new Stack<string>();

	public static SceneNavigator Instance
	{
		get
		{
			if (_instance == null)
			{
				InitializeInstance();
			}
			return _instance;
		}
	}

	public override void _EnterTree()
	{
		if (_instance == null)
		{
			_instance = this;
		}
	}

	private static void InitializeInstance()
	{
		var sceneTree = Engine.GetMainLoop() as SceneTree;
		if (sceneTree == null)
		{
			GD.PushError("SceneNavigator: Unable to access SceneTree. Instance will operate without navigation support until the tree becomes available.");
			_instance = new SceneNavigator();
			return;
		}

		var existing = sceneTree.Root.GetNodeOrNull<SceneNavigator>(nameof(SceneNavigator));
		if (existing != null)
		{
			_instance = existing;
			return;
		}

		_instance = new SceneNavigator
		{
			Name = nameof(SceneNavigator)
		};
		sceneTree.Root.AddChild(_instance);
	}

	public void NavigateTo(string scenePath)
	{
		if (string.IsNullOrEmpty(scenePath))
		{
			GD.PushWarning("SceneNavigator.NavigateTo called with empty scene path.");
			return;
		}

		var tree = GetTree();
		if (tree == null)
		{
			GD.PushError("SceneNavigator.NavigateTo called before the node entered the SceneTree.");
			return;
		}

		var currentScene = tree.CurrentScene;
		string previousScenePath = null;
		if (currentScene != null && !string.IsNullOrEmpty(currentScene.SceneFilePath) && currentScene.SceneFilePath != scenePath)
		{
			previousScenePath = currentScene.SceneFilePath;
		}

		var error = tree.ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load scene: {scenePath}");
			return;
		}

		if (previousScenePath != null)
		{
			_sceneHistory.Push(previousScenePath);
		}
	}

	public bool CanGoBack()
	{
		return _sceneHistory.Count > 0;
	}

	public void GoBack()
	{
		var tree = GetTree();
		if (tree == null)
		{
			GD.PushError("SceneNavigator.GoBack called before the node entered the SceneTree.");
			return;
		}

		if (_sceneHistory.Count == 0)
		{
			return;
		}

		var previousScene = _sceneHistory.Pop();
		var error = tree.ChangeSceneToFile(previousScene);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load scene: {previousScene}");
			_sceneHistory.Push(previousScene);
		}
	}

	public void ClearHistory()
	{
		_sceneHistory.Clear();
	}
}
