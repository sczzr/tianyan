using Godot;
using System;

namespace TianYanShop.World.Layer
{
    public partial class ElementMaskLayer : Node2D
    {
        private TextureRect _elementGrid;
        private int _mapWidth;
        private int _mapHeight;
        private int _tileSize;

        public override void _Ready()
        {
            CreateElementGrid();
        }

        private void CreateElementGrid()
        {
            _elementGrid = new TextureRect();
            _elementGrid.Name = "ElementGrid";
            _elementGrid.AnchorRight = 1.0f;
            _elementGrid.AnchorBottom = 1.0f;
            _elementGrid.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
            _elementGrid.Modulate = new Color(1, 1, 1, 0.5f);
            AddChild(_elementGrid);
        }

        public void UpdateElementData(Data.RealmTile[,] mapData, int tileSize)
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
                    Color color = GetElementColor(tile.PrimaryElement);
                    image.SetPixel(x, y, color);
                }
            }

            var texture = ImageTexture.CreateFromImage(image);
            _elementGrid.Texture = texture;
            _elementGrid.CustomMinimumSize = new Vector2(_mapWidth * tileSize, _mapHeight * tileSize);
        }

        private Color GetElementColor(Data.ElementTag element)
        {
            if (element == Data.ElementTag.None)
                return new Color(0, 0, 0, 0);

            return element switch
            {
                Data.ElementTag.Metal => new Color(0.80f, 0.80f, 0.85f, 0.6f),
                Data.ElementTag.Wood => new Color(0.20f, 0.55f, 0.25f, 0.6f),
                Data.ElementTag.Water => new Color(0.25f, 0.35f, 0.65f, 0.6f),
                Data.ElementTag.Fire => new Color(0.85f, 0.35f, 0.15f, 0.6f),
                Data.ElementTag.Earth => new Color(0.60f, 0.50f, 0.30f, 0.6f),
                Data.ElementTag.Wind => new Color(0.70f, 0.75f, 0.70f, 0.6f),
                Data.ElementTag.Thunder => new Color(0.55f, 0.45f, 0.75f, 0.6f),
                Data.ElementTag.Light => new Color(0.95f, 0.90f, 0.65f, 0.6f),
                Data.ElementTag.Dark => new Color(0.15f, 0.15f, 0.30f, 0.6f),
                Data.ElementTag.Ice => new Color(0.75f, 0.90f, 1.00f, 0.6f),
                Data.ElementTag.Sound => new Color(0.65f, 0.60f, 0.55f, 0.6f),
                Data.ElementTag.Crystal => new Color(0.60f, 0.70f, 0.75f, 0.6f),
                Data.ElementTag.Swamp => new Color(0.30f, 0.40f, 0.30f, 0.6f),
                _ => new Color(0, 0, 0, 0)
            };
        }
    }
}
