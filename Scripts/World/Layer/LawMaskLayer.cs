using Godot;
using System;

namespace TianYanShop.World.Layer
{
    public partial class LawMaskLayer : Node2D
    {
        private TextureRect _lawGrid;
        private int _mapWidth;
        private int _mapHeight;
        private int _tileSize;

        public override void _Ready()
        {
            CreateLawGrid();
        }

        private void CreateLawGrid()
        {
            _lawGrid = new TextureRect();
            _lawGrid.Name = "LawGrid";
            _lawGrid.AnchorRight = 1.0f;
            _lawGrid.AnchorBottom = 1.0f;
            _lawGrid.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
            _lawGrid.Modulate = new Color(1, 1, 1, 0.5f);
            AddChild(_lawGrid);
        }

        public void UpdateLawData(Data.RealmTile[,] mapData, int tileSize)
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
                    Color color = GetLawColor(tile.Law);
                    image.SetPixel(x, y, color);
                }
            }

            var texture = ImageTexture.CreateFromImage(image);
            _lawGrid.Texture = texture;
            _lawGrid.CustomMinimumSize = new Vector2(_mapWidth * tileSize, _mapHeight * tileSize);
        }

        private Color GetLawColor(Data.LawTag law)
        {
            if (law == Data.LawTag.None)
                return new Color(0, 0, 0, 0);

            return law switch
            {
                Data.LawTag.Space => new Color(0.50f, 0.40f, 0.70f, 0.7f),
                Data.LawTag.Time => new Color(0.70f, 0.50f, 0.80f, 0.7f),
                Data.LawTag.LifeDeath => new Color(0.40f, 0.70f, 0.40f, 0.7f),
                Data.LawTag.Fate => new Color(0.80f, 0.60f, 0.30f, 0.7f),
                Data.LawTag.ForceField => new Color(0.60f, 0.30f, 0.70f, 0.7f),
                Data.LawTag.Mental => new Color(0.50f, 0.30f, 0.60f, 0.7f),
                Data.LawTag.Chaos => new Color(0.40f, 0.40f, 0.50f, 0.7f),
                Data.LawTag.Illusion => new Color(0.65f, 0.50f, 0.70f, 0.7f),
                Data.LawTag.Boundary => new Color(0.75f, 0.65f, 0.55f, 0.7f),
                _ => new Color(0, 0, 0, 0)
            };
        }
    }
}
