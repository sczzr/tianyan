using Godot;
using System;
using System.Collections.Generic;

using TianYanShop.Data;

namespace TianYanShop.World.Layer
{
    public enum MapLayerType
    {
        Terrain,
        Spirit,
        Element,
        Law,
        Highlight
    }

    public partial class MapLayerManager : Node
    {
        private SpiritMaskLayer _spiritLayer;
        private ElementMaskLayer _elementLayer;
        private LawMaskLayer _lawLayer;
        private MaskHighlightLayer _highlightLayer;

        private Dictionary<MapLayerType, bool> _layerVisibility = new()
        {
            { MapLayerType.Terrain, true },
            { MapLayerType.Spirit, false },
            { MapLayerType.Element, false },
            { MapLayerType.Law, false },
            { MapLayerType.Highlight, false }
        };

        private Dictionary<MapLayerType, float> _layerOpacity = new()
        {
            { MapLayerType.Terrain, 1.0f },
            { MapLayerType.Spirit, 0.6f },
            { MapLayerType.Element, 0.5f },
            { MapLayerType.Law, 0.5f },
            { MapLayerType.Highlight, 0.7f }
        };

        public override void _Ready()
        {
            InitializeLayers();
        }

        private void InitializeLayers()
        {
            _spiritLayer = new SpiritMaskLayer();
            _spiritLayer.Name = "SpiritMaskLayer";
            AddChild(_spiritLayer);

            _elementLayer = new ElementMaskLayer();
            _elementLayer.Name = "ElementMaskLayer";
            AddChild(_elementLayer);

            _lawLayer = new LawMaskLayer();
            _lawLayer.Name = "LawMaskLayer";
            AddChild(_lawLayer);

            _highlightLayer = new MaskHighlightLayer();
            _highlightLayer.Name = "HighlightMaskLayer";
            AddChild(_highlightLayer);

            UpdateLayerVisibility();
        }

        public void SetLayerVisibility(MapLayerType type, bool visible)
        {
            if (_layerVisibility.ContainsKey(type))
            {
                _layerVisibility[type] = visible;
                UpdateLayerVisibility();
            }
        }

        public void SetLayerOpacity(MapLayerType type, float opacity)
        {
            if (_layerOpacity.ContainsKey(type))
            {
                _layerOpacity[type] = Mathf.Clamp(opacity, 0.0f, 1.0f);
                UpdateLayerVisibility();
            }
        }

        public bool IsLayerVisible(MapLayerType type)
        {
            return _layerVisibility.TryGetValue(type, out var visible) && visible;
        }

        public float GetLayerOpacity(MapLayerType type)
        {
            return _layerOpacity.TryGetValue(type, out var opacity) ? opacity : 0.5f;
        }

        private void UpdateLayerVisibility()
        {
            if (_spiritLayer != null)
            {
                _spiritLayer.Visible = _layerVisibility[MapLayerType.Spirit];
                _spiritLayer.Modulate = new Color(1, 1, 1, _layerOpacity[MapLayerType.Spirit]);
            }

            if (_elementLayer != null)
            {
                _elementLayer.Visible = _layerVisibility[MapLayerType.Element];
                _elementLayer.Modulate = new Color(1, 1, 1, _layerOpacity[MapLayerType.Element]);
            }

            if (_lawLayer != null)
            {
                _lawLayer.Visible = _layerVisibility[MapLayerType.Law];
                _lawLayer.Modulate = new Color(1, 1, 1, _layerOpacity[MapLayerType.Law]);
            }

            if (_highlightLayer != null)
            {
                _highlightLayer.Visible = _layerVisibility[MapLayerType.Highlight];
                _highlightLayer.Modulate = new Color(1, 1, 1, _layerOpacity[MapLayerType.Highlight]);
            }
        }

        public void UpdateMapData(Data.RealmTile[,] mapData, int tileSize)
        {
            _spiritLayer?.UpdateSpiritData(mapData, tileSize);
            _elementLayer?.UpdateElementData(mapData, tileSize);
            _lawLayer?.UpdateLawData(mapData, tileSize);
        }

        public void ShowHighlight(Vector2I tilePos, Color highlightColor, float radius = 1.0f)
        {
            _highlightLayer?.ShowHighlight(tilePos, highlightColor, radius);
        }

        public void HideHighlight()
        {
            _highlightLayer?.HideHighlight();
        }

        public void ToggleSpiritLayer()
        {
            SetLayerVisibility(MapLayerType.Spirit, !IsLayerVisible(MapLayerType.Spirit));
        }

        public void ToggleElementLayer()
        {
            SetLayerVisibility(MapLayerType.Element, !IsLayerVisible(MapLayerType.Element));
        }

        public void ToggleLawLayer()
        {
            SetLayerVisibility(MapLayerType.Law, !IsLayerVisible(MapLayerType.Law));
        }
    }
}
