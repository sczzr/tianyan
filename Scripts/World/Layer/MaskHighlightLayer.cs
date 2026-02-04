using Godot;
using System;

namespace TianYanShop.World.Layer
{
    public partial class MaskHighlightLayer : Node2D
    {
        private ColorRect _highlightRect;
        private int _tileSize;

        public override void _Ready()
        {
            CreateHighlightRect();
        }

        private void CreateHighlightRect()
        {
            _highlightRect = new ColorRect();
            _highlightRect.Name = "HighlightRect";
            _highlightRect.Color = new Color(1, 1, 0, 0.5f);
            _highlightRect.Visible = false;
            AddChild(_highlightRect);
        }

        public void ShowHighlight(Vector2I tilePos, Color highlightColor, float radius = 1.0f)
        {
            _highlightRect.Visible = true;
            _highlightRect.Color = highlightColor;

            int width = (int)((radius * 2 + 1) * _tileSize);
            int height = (int)((radius * 2 + 1) * _tileSize);

            _highlightRect.Size = new Vector2(width, height);
            _highlightRect.Position = new Vector2(
                tilePos.X * _tileSize - radius * _tileSize,
                tilePos.Y * _tileSize - radius * _tileSize
            );
        }

        public void HideHighlight()
        {
            _highlightRect.Visible = false;
        }

        public void SetTileSize(int tileSize)
        {
            _tileSize = tileSize;
        }
    }
}
