using Godot;
using System;

namespace TianYanShop.World.Layer
{
    public partial class SpiritMaskLayer : Node2D
    {
        private TextureRect _spiritGrid;
        private int _mapWidth;
        private int _mapHeight;
        private int _tileSize;

        public override void _Ready()
        {
            CreateSpiritGrid();
        }

        private void CreateSpiritGrid()
        {
            _spiritGrid = new TextureRect();
            _spiritGrid.Name = "SpiritGrid";
            _spiritGrid.AnchorRight = 1.0f;
            _spiritGrid.AnchorBottom = 1.0f;
            _spiritGrid.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
            _spiritGrid.Modulate = new Color(1, 1, 1, 0.6f);
            AddChild(_spiritGrid);
        }

        public void UpdateSpiritData(Data.RealmTile[,] mapData, int tileSize)
        {
            if (mapData == null) return;

            _mapWidth = mapData.GetLength(0);
            _mapHeight = mapData.GetLength(1);
            _tileSize = tileSize;

            var image = Image.CreateEmpty(_mapWidth, _mapHeight, false, Image.Format.Rgba8);

            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    var tile = mapData[x, y];
                    Color color = GetSpiritColor(tile.Spirit);
                    image.SetPixel(x, y, color);
                }
            }

            var texture = ImageTexture.CreateFromImage(image);
            _spiritGrid.Texture = texture;
            _spiritGrid.CustomMinimumSize = new Vector2(_mapWidth * tileSize, _mapHeight * tileSize);
        }

        private Color GetSpiritColor(Data.SpiritLevel spirit)
        {
            return spirit switch
            {
                Data.SpiritLevel.Desolate => new Color(0.50f, 0.45f, 0.40f, 0.8f),
                Data.SpiritLevel.Barren => new Color(0.55f, 0.50f, 0.45f, 0.7f),
                Data.SpiritLevel.Sparse => new Color(0.50f, 0.55f, 0.50f, 0.6f),
                Data.SpiritLevel.Normal => new Color(0.50f, 0.60f, 0.50f, 0.5f),
                Data.SpiritLevel.Rich => new Color(0.45f, 0.65f, 0.55f, 0.5f),
                Data.SpiritLevel.Abundant => new Color(0.40f, 0.70f, 0.60f, 0.4f),
                Data.SpiritLevel.Extreme => new Color(0.35f, 0.75f, 0.70f, 0.3f),
                _ => new Color(0, 0, 0, 0)
            };
        }
    }
}
