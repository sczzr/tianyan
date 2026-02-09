using System.Collections.Generic;
using Godot;

namespace FantasyMapGenerator.Scripts.Genesis;

public class ThemeManager
{
	private readonly ColorRect _magicLayer;
	private readonly ColorRect _techLayer;
	private readonly List<CanvasItem> _textItems;

	public ThemeManager(ColorRect magicLayer, ColorRect techLayer, IEnumerable<CanvasItem> textItems)
	{
		_magicLayer = magicLayer;
		_techLayer = techLayer;
		_textItems = textItems != null ? new List<CanvasItem>(textItems) : new List<CanvasItem>();
	}

	public void UpdateVisuals(int lawAlignment)
	{
		float alignment = Mathf.Clamp(lawAlignment / 100f, 0f, 1f);
		float magicAlpha = 1f - alignment;
		float techAlpha = alignment;

		if (_magicLayer != null)
		{
			_magicLayer.Modulate = new Color(1f, 1f, 1f, magicAlpha);
		}

		if (_techLayer != null)
		{
			_techLayer.Modulate = new Color(1f, 1f, 1f, techAlpha);
		}

		Color magicTint = new Color(1f, 0.96f, 0.88f, 1f);
		Color techTint = new Color(0.86f, 0.93f, 1f, 1f);
		Color mixedTint = magicTint.Lerp(techTint, alignment);

		foreach (var textItem in _textItems)
		{
			if (textItem != null)
			{
				textItem.Modulate = mixedTint;
			}
		}
	}
}
