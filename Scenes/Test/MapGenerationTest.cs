using Godot;
using FantasyMapGenerator.Scripts.Rendering;
using FantasyMapGenerator.Scripts.UI;

namespace FantasyMapGenerator.Scenes.Test;

public partial class MapGenerationTest : Control
{
	private MapView _mapView;
	private MapGenerationController _controller;

	public override void _Ready()
	{
		// 获取节点引用
		_mapView = GetNode<MapView>("%MapView");
		_controller = GetNode<MapGenerationController>("%MapGenerationController");

		// 将MapView连接到控制器
		if (_controller != null)
		{
			_controller.MapView = _mapView;
		}

		GD.Print("地图生成器界面已准备就绪");
	}
}
