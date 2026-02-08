using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Rendering;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI;

/// <summary>
/// 地图显示面板控制器：图层、编辑、边界、主题等
/// </summary>
public partial class MapDisplayPanelController : Control
{
	private MapView _mapView;
	private PanelContainer _mapDisplayPanel;
	private Label _mapDisplayTitleLabel;
	private Label _presetSectionLabel;
	private Label _layerPresetLabel;
	private Label _layerListLabel;
	private Label _editSectionLabel;
	private Button _borderHeaderButton;
	private Button _themeHeaderButton;
	private Label _themeLabel;
	private Label _layerBaseSectionLabel;
	private Label _layerPoliticalSectionLabel;
	private Label _layerEnvironmentSectionLabel;
	private Label _layerOverlaySectionLabel;
	private TabContainer _displayTabs;
	private VBoxContainer _borderContent;
	private VBoxContainer _themeContent;
	private HSlider _countryBorderWidthSlider;
	private HSlider _countryFillAlphaSlider;
	private Label _countryBorderWidthLabel;
	private Label _countryFillAlphaLabel;
	private Label _countryBorderColorLabel;
	private ColorPickerButton _countryBorderColorPicker;
	private OptionButton _layerPresetSelector;
	private OptionButton _mapThemeSelector;
	private VBoxContainer _layerListVBox;

	private readonly Dictionary<MapView.MapLayer, Button> _layerToggles = new();
	private readonly Dictionary<MapView.EditableElement, Button> _editButtons = new();
	private bool _updatingLayerToggles;

	public override void _Ready()
	{
		_mapDisplayPanel = GetNodeOrNull<PanelContainer>("MapDisplayPanel");
		_mapDisplayTitleLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/MapDisplayTitle");
		_presetSectionLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/PresetSectionLabel");
		_layerPresetLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/LayerPresetHBox/LayerPresetLabel");
		_layerPresetSelector = GetNodeOrNull<OptionButton>("MapDisplayPanel/MapDisplayVBox/LayerPresetHBox/LayerPresetSelector");
		_displayTabs = GetNodeOrNull<TabContainer>("MapDisplayPanel/MapDisplayVBox/DisplayTabs");
		_layerListLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListLabel");
		_layerListVBox = GetNodeOrNull<VBoxContainer>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox");
		_layerBaseSectionLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseSectionLabel");
		_layerPoliticalSectionLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalSectionLabel");
		_layerEnvironmentSectionLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/EnvironmentGroupVBox/LayerEnvironmentSectionLabel");
		_layerOverlaySectionLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlaySectionLabel");
		_editSectionLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditSectionLabel");
		_borderHeaderButton = GetNodeOrNull<Button>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderHeader");
		_themeHeaderButton = GetNodeOrNull<Button>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/ThemePage/ThemeSection/ThemeHeader");
		_themeLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/ThemePage/ThemeSection/ThemeContent/ThemeSelectorHBox/ThemeLabel");
		_borderContent = GetNodeOrNull<VBoxContainer>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderContent");
		_themeContent = GetNodeOrNull<VBoxContainer>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/ThemePage/ThemeSection/ThemeContent");
		_countryBorderWidthLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderContent/CountryBorderWidthHBox/CountryBorderWidthLabel");
		_countryBorderWidthSlider = GetNodeOrNull<HSlider>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderContent/CountryBorderWidthHBox/CountryBorderWidthSlider");
		_countryFillAlphaLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderContent/CountryFillAlphaHBox/CountryFillAlphaLabel");
		_countryFillAlphaSlider = GetNodeOrNull<HSlider>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderContent/CountryFillAlphaHBox/CountryFillAlphaSlider");
		_countryBorderColorLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderContent/CountryBorderColorHBox/CountryBorderColorLabel");
		_countryBorderColorPicker = GetNodeOrNull<ColorPickerButton>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/BorderPage/BorderSection/BorderContent/CountryBorderColorHBox/CountryBorderColorPicker");
		_mapThemeSelector = GetNodeOrNull<OptionButton>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/ThemePage/ThemeSection/ThemeContent/ThemeSelectorHBox/MapThemeSelector");
	}

	public void Initialize(MapView mapView)
	{
		_mapView = mapView;
		SetupMapDisplaySettings();
		UpdateUIText();
	}

	public void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_mapDisplayTitleLabel != null)
		{
			_mapDisplayTitleLabel.Text = tm.Tr("map_display");
		}

		if (_presetSectionLabel != null)
		{
			_presetSectionLabel.Text = tm.Tr("layer_preset");
		}

		if (_layerPresetLabel != null)
		{
			_layerPresetLabel.Text = tm.Tr("preset");
		}

		if (_layerListLabel != null)
		{
			_layerListLabel.Text = tm.Tr("layer_list");
		}

		if (_editSectionLabel != null)
		{
			_editSectionLabel.Text = tm.Tr("editable_elements");
		}

		if (_borderHeaderButton != null)
		{
			_borderHeaderButton.Text = tm.Tr("border_settings");
		}

		if (_themeHeaderButton != null)
		{
			_themeHeaderButton.Text = tm.Tr("map_theme");
		}

		if (_themeLabel != null)
		{
			_themeLabel.Text = tm.Tr("map_theme_style");
		}

		if (_layerBaseSectionLabel != null)
		{
			_layerBaseSectionLabel.Text = tm.Tr("layer_section_base");
		}

		if (_layerPoliticalSectionLabel != null)
		{
			_layerPoliticalSectionLabel.Text = tm.Tr("layer_section_political");
		}

		if (_layerEnvironmentSectionLabel != null)
		{
			_layerEnvironmentSectionLabel.Text = tm.Tr("layer_section_environment");
		}

		if (_layerOverlaySectionLabel != null)
		{
			_layerOverlaySectionLabel.Text = tm.Tr("layer_section_overlay");
		}

		if (_displayTabs != null)
		{
			_displayTabs.SetTabTitle(0, tm.Tr("map_tab_layers"));
			_displayTabs.SetTabTitle(1, tm.Tr("map_tab_edit"));
			_displayTabs.SetTabTitle(2, tm.Tr("map_tab_border"));
			_displayTabs.SetTabTitle(3, tm.Tr("map_tab_theme"));
		}

		var editGroupPoliticalLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupPoliticalLabel");
		var editGroupNatureLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupNatureLabel");
		var editGroupOverlayLabel = GetNodeOrNull<Label>("MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayLabel");
		if (editGroupPoliticalLabel != null)
		{
			editGroupPoliticalLabel.Text = tm.Tr("edit_group_political");
		}
		if (editGroupNatureLabel != null)
		{
			editGroupNatureLabel.Text = tm.Tr("edit_group_nature");
		}
		if (editGroupOverlayLabel != null)
		{
			editGroupOverlayLabel.Text = tm.Tr("edit_group_overlay");
		}

		foreach (var entry in _layerToggles)
		{
			entry.Value.Text = tm.Tr(GetLayerLabelKey(entry.Key));
		}

		foreach (var entry in _editButtons)
		{
			entry.Value.Text = tm.Tr(GetEditLabelKey(entry.Key));
		}

		SetupLayerPresetSelector();
		RefreshMapThemeOptions();
		UpdateCountryBorderWidthLabel();
		UpdateCountryFillAlphaLabel();
	}

	public void OnDisplayTabSelected(int tabIndex)
	{
		if (_displayTabs == null || _mapDisplayPanel == null)
		{
			return;
		}

		_displayTabs.CurrentTab = tabIndex;
		_mapDisplayPanel.Visible = true;
		CallDeferred(nameof(UpdateMapDisplayPanelSize));
	}

	private void UpdateMapDisplayPanelSize()
	{
		if (_mapDisplayPanel == null)
		{
			return;
		}

		var minSize = _mapDisplayPanel.GetCombinedMinimumSize();
		_mapDisplayPanel.OffsetTop = _mapDisplayPanel.OffsetBottom - minSize.Y;
	}

	private void SetupMapDisplaySettings()
	{
		SetupLayerPresetSelector();
		SetupLayerToggles();
		SetupEditableElements();
		SetupMapDisplaySections();

		if (_countryBorderWidthSlider != null)
		{
			_countryBorderWidthSlider.Value = _mapView?.CountryBorderWidth ?? 2f;
			_countryBorderWidthSlider.ValueChanged += OnCountryBorderWidthChanged;
		}

		if (_countryFillAlphaSlider != null)
		{
			_countryFillAlphaSlider.Value = _mapView?.CountryFillAlpha ?? 0.85f;
			_countryFillAlphaSlider.ValueChanged += OnCountryFillAlphaChanged;
		}

		if (_countryBorderColorPicker != null)
		{
			_countryBorderColorPicker.Color = _mapView?.CountryBorderColor ?? new Color(0.12f, 0.1f, 0.08f, 0.9f);
			_countryBorderColorPicker.ColorChanged += OnCountryBorderColorChanged;
		}

		if (_mapThemeSelector != null)
		{
			RefreshMapThemeOptions();
			_mapThemeSelector.ItemSelected += OnMapThemeSelected;
		}

		UpdateCountryBorderWidthLabel();
		UpdateCountryFillAlphaLabel();
		CallDeferred(nameof(SyncLayerUI));
	}

	private void SetupMapDisplaySections()
	{
		if (_borderHeaderButton != null && _borderContent != null)
		{
			_borderHeaderButton.Toggled += pressed => _borderContent.Visible = pressed;
			_borderContent.Visible = _borderHeaderButton.ButtonPressed;
		}

		if (_themeHeaderButton != null && _themeContent != null)
		{
			_themeHeaderButton.Toggled += pressed => _themeContent.Visible = pressed;
			_themeContent.Visible = _themeHeaderButton.ButtonPressed;
		}
	}

	private void SyncLayerUI()
	{
		UpdateLayerToggleStates();
		UpdateEditableButtons();
	}

	private void SetupLayerPresetSelector()
	{
		if (_layerPresetSelector == null)
		{
			return;
		}

		var presetCallable = new Callable(this, nameof(OnLayerPresetSelected));
		if (_layerPresetSelector.IsConnected(OptionButton.SignalName.ItemSelected, presetCallable))
		{
			_layerPresetSelector.Disconnect(OptionButton.SignalName.ItemSelected, presetCallable);
		}
		_layerPresetSelector.Clear();
		var tm = TranslationManager.Instance;
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_political"), (int)MapView.LayerPreset.Political);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_cultural"), (int)MapView.LayerPreset.Cultural);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_religions"), (int)MapView.LayerPreset.Religions);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_provinces"), (int)MapView.LayerPreset.Provinces);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_biomes"), (int)MapView.LayerPreset.Biomes);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_heightmap"), (int)MapView.LayerPreset.Heightmap);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_physical"), (int)MapView.LayerPreset.Physical);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_poi"), (int)MapView.LayerPreset.Poi);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_military"), (int)MapView.LayerPreset.Military);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_emblems"), (int)MapView.LayerPreset.Emblems);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_landmass"), (int)MapView.LayerPreset.Landmass);
		_layerPresetSelector.AddItem(tm.Tr("layer_preset_custom"), (int)MapView.LayerPreset.Custom);

		if (_mapView != null)
		{
			var index = _layerPresetSelector.GetItemIndex((int)_mapView.LayerPresetMode);
			if (index >= 0)
			{
				_layerPresetSelector.Selected = index;
			}
		}

		_layerPresetSelector.ItemSelected += OnLayerPresetSelected;
	}

	private void SetupLayerToggles()
	{
		_layerToggles.Clear();
		RegisterLayerToggle(MapView.MapLayer.Texture, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseGrid/LayerTextureToggle");
		RegisterLayerToggle(MapView.MapLayer.Heightmap, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseGrid/LayerHeightmapToggle");
		RegisterLayerToggle(MapView.MapLayer.Biomes, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseGrid/LayerBiomesToggle");
		RegisterLayerToggle(MapView.MapLayer.Cells, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseGrid/LayerCellsToggle");
		RegisterLayerToggle(MapView.MapLayer.Grid, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseGrid/LayerGridToggle");
		RegisterLayerToggle(MapView.MapLayer.Coordinates, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseGrid/LayerCoordinatesToggle");
		RegisterLayerToggle(MapView.MapLayer.Compass, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/BaseGroupVBox/LayerBaseGrid/LayerCompassToggle");
		RegisterLayerToggle(MapView.MapLayer.Rivers, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerRiversToggle");
		RegisterLayerToggle(MapView.MapLayer.Relief, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerReliefToggle");
		RegisterLayerToggle(MapView.MapLayer.Religions, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerReligionsToggle");
		RegisterLayerToggle(MapView.MapLayer.Cultures, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerCulturesToggle");
		RegisterLayerToggle(MapView.MapLayer.States, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerStatesToggle");
		RegisterLayerToggle(MapView.MapLayer.Provinces, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerProvincesToggle");
		RegisterLayerToggle(MapView.MapLayer.Zones, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerZonesToggle");
		RegisterLayerToggle(MapView.MapLayer.Borders, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow1/PoliticalGroupVBox/LayerPoliticalGrid/LayerBordersToggle");
		RegisterLayerToggle(MapView.MapLayer.Routes, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/EnvironmentGroupVBox/LayerEnvironmentGrid/LayerRoutesToggle");
		RegisterLayerToggle(MapView.MapLayer.Temperature, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/EnvironmentGroupVBox/LayerEnvironmentGrid/LayerTemperatureToggle");
		RegisterLayerToggle(MapView.MapLayer.Population, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/EnvironmentGroupVBox/LayerEnvironmentGrid/LayerPopulationToggle");
		RegisterLayerToggle(MapView.MapLayer.Ice, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/EnvironmentGroupVBox/LayerEnvironmentGrid/LayerIceToggle");
		RegisterLayerToggle(MapView.MapLayer.Precipitation, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/EnvironmentGroupVBox/LayerEnvironmentGrid/LayerPrecipitationToggle");
		RegisterLayerToggle(MapView.MapLayer.Emblems, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerEmblemsToggle");
		RegisterLayerToggle(MapView.MapLayer.BurgIcons, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerBurgIconsToggle");
		RegisterLayerToggle(MapView.MapLayer.Labels, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerLabelsToggle");
		RegisterLayerToggle(MapView.MapLayer.Military, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerMilitaryToggle");
		RegisterLayerToggle(MapView.MapLayer.Markers, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerMarkersToggle");
		RegisterLayerToggle(MapView.MapLayer.Rulers, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerRulersToggle");
		RegisterLayerToggle(MapView.MapLayer.ScaleBar, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerScaleBarToggle");
		RegisterLayerToggle(MapView.MapLayer.Vignette, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/LayersPage/LayerListVBox/LayerGroupRow2/OverlayGroupVBox/LayerOverlayGrid/LayerVignetteToggle");

		UpdateLayerToggleStates();
	}

	private void RegisterLayerToggle(MapView.MapLayer layer, string path)
	{
		var toggle = GetNodeOrNull<Button>(path);
		if (toggle == null)
		{
			return;
		}

		_layerToggles[layer] = toggle;
		toggle.Toggled += pressed => OnLayerToggleChanged(layer, pressed);
	}

	private void SetupEditableElements()
	{
		_editButtons.Clear();
		RegisterEditButton(MapView.EditableElement.States, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupPoliticalGrid/EditStatesButton");
		RegisterEditButton(MapView.EditableElement.Provinces, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupPoliticalGrid/EditProvincesButton");
		RegisterEditButton(MapView.EditableElement.Diplomacy, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupPoliticalGrid/EditDiplomacyButton");

		RegisterEditButton(MapView.EditableElement.Biomes, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupNatureGrid/EditBiomesButton");
		RegisterEditButton(MapView.EditableElement.Cultures, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupNatureGrid/EditCulturesButton");
		RegisterEditButton(MapView.EditableElement.Religions, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupNatureGrid/EditReligionsButton");
		RegisterEditButton(MapView.EditableElement.Rivers, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupNatureGrid/EditRiversButton");
		RegisterEditButton(MapView.EditableElement.Heightmap, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupNatureGrid/EditHeightmapButton");

		RegisterEditButton(MapView.EditableElement.Burgs, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditBurgsButton");
		RegisterEditButton(MapView.EditableElement.Emblems, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditEmblemsButton");
		RegisterEditButton(MapView.EditableElement.Markers, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditMarkersButton");
		RegisterEditButton(MapView.EditableElement.Military, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditMilitaryButton");
		RegisterEditButton(MapView.EditableElement.Routes, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditRoutesButton");
		RegisterEditButton(MapView.EditableElement.Units, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditUnitsButton");
		RegisterEditButton(MapView.EditableElement.Zones, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditZonesButton");
		RegisterEditButton(MapView.EditableElement.Namesbase, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditNamesbaseButton");
		RegisterEditButton(MapView.EditableElement.Notes, "MapDisplayPanel/MapDisplayVBox/DisplayTabs/EditPage/EditGroupsVBox/EditGroupOverlayGrid/EditNotesButton");

		UpdateEditableButtons();
	}

	private void RegisterEditButton(MapView.EditableElement element, string path)
	{
		var button = GetNodeOrNull<Button>(path);
		if (button == null)
		{
			return;
		}

		_editButtons[element] = button;
		button.Pressed += () => OnEditButtonPressed(element, button);
	}

	private void OnLayerPresetSelected(long index)
	{
		if (_mapView == null || _layerPresetSelector == null)
		{
			return;
		}

		var preset = (MapView.LayerPreset)_layerPresetSelector.GetItemId((int)index);
		_mapView.LayerPresetMode = preset;
		UpdateLayerToggleStates();
	}

	private void OnLayerToggleChanged(MapView.MapLayer layer, bool pressed)
	{
		if (_mapView == null || _updatingLayerToggles)
		{
			return;
		}

		_mapView.SetLayerEnabled(layer, pressed);
		EnsureCustomPreset();
	}

	private void EnsureCustomPreset()
	{
		if (_mapView == null || _layerPresetSelector == null)
		{
			return;
		}

		if (_mapView.LayerPresetMode != MapView.LayerPreset.Custom)
		{
			_mapView.LayerPresetMode = MapView.LayerPreset.Custom;
		}

		var index = _layerPresetSelector.GetItemIndex((int)MapView.LayerPreset.Custom);
		if (index >= 0)
		{
			_layerPresetSelector.Selected = index;
		}
	}

	private void UpdateLayerToggleStates()
	{
		if (_mapView == null)
		{
			return;
		}

		_updatingLayerToggles = true;
		foreach (var entry in _layerToggles)
		{
			entry.Value.ButtonPressed = _mapView.IsLayerEnabled(entry.Key);
		}
		_updatingLayerToggles = false;
	}

	private void OnEditButtonPressed(MapView.EditableElement element, Button button)
	{
		if (_mapView == null || button == null)
		{
			return;
		}

		bool pressed = button.ButtonPressed;
		foreach (var entry in _editButtons)
		{
			if (entry.Value == button)
			{
				continue;
			}
			entry.Value.ButtonPressed = false;
		}

		_mapView.CurrentEditableElement = pressed ? element : MapView.EditableElement.None;
	}

	private void UpdateEditableButtons()
	{
		if (_mapView == null)
		{
			return;
		}

		foreach (var entry in _editButtons)
		{
			entry.Value.ButtonPressed = _mapView.CurrentEditableElement == entry.Key;
		}
	}

	private string GetLayerLabelKey(MapView.MapLayer layer)
	{
		return layer switch
		{
			MapView.MapLayer.Texture => "layer_texture",
			MapView.MapLayer.Heightmap => "layer_heightmap",
			MapView.MapLayer.Biomes => "layer_biomes",
			MapView.MapLayer.Cells => "layer_cells",
			MapView.MapLayer.Grid => "layer_grid",
			MapView.MapLayer.Coordinates => "layer_coordinates",
			MapView.MapLayer.Compass => "layer_compass",
			MapView.MapLayer.Rivers => "layer_rivers",
			MapView.MapLayer.Relief => "layer_relief",
			MapView.MapLayer.Religions => "layer_religions",
			MapView.MapLayer.Cultures => "layer_cultures",
			MapView.MapLayer.States => "layer_states",
			MapView.MapLayer.Provinces => "layer_provinces",
			MapView.MapLayer.Zones => "layer_zones",
			MapView.MapLayer.Borders => "layer_borders",
			MapView.MapLayer.Routes => "layer_routes",
			MapView.MapLayer.Temperature => "layer_temperature",
			MapView.MapLayer.Population => "layer_population",
			MapView.MapLayer.Ice => "layer_ice",
			MapView.MapLayer.Precipitation => "layer_precipitation",
			MapView.MapLayer.Emblems => "layer_emblems",
			MapView.MapLayer.BurgIcons => "layer_burg_icons",
			MapView.MapLayer.Labels => "layer_labels",
			MapView.MapLayer.Military => "layer_military",
			MapView.MapLayer.Markers => "layer_markers",
			MapView.MapLayer.Rulers => "layer_rulers",
			MapView.MapLayer.ScaleBar => "layer_scale_bar",
			MapView.MapLayer.Vignette => "layer_vignette",
			_ => "layer_unknown"
		};
	}

	private string GetEditLabelKey(MapView.EditableElement element)
	{
		return element switch
		{
			MapView.EditableElement.Biomes => "edit_biomes",
			MapView.EditableElement.Burgs => "edit_burgs",
			MapView.EditableElement.Cultures => "edit_cultures",
			MapView.EditableElement.Diplomacy => "edit_diplomacy",
			MapView.EditableElement.Emblems => "edit_emblems",
			MapView.EditableElement.Heightmap => "edit_heightmap",
			MapView.EditableElement.Markers => "edit_markers",
			MapView.EditableElement.Military => "edit_military",
			MapView.EditableElement.Namesbase => "edit_namesbase",
			MapView.EditableElement.Notes => "edit_notes",
			MapView.EditableElement.Provinces => "edit_provinces",
			MapView.EditableElement.Religions => "edit_religions",
			MapView.EditableElement.Rivers => "edit_rivers",
			MapView.EditableElement.Routes => "edit_routes",
			MapView.EditableElement.States => "edit_states",
			MapView.EditableElement.Units => "edit_units",
			MapView.EditableElement.Zones => "edit_zones",
			_ => "edit_unknown"
		};
	}

	private void RefreshMapThemeOptions()
	{
		if (_mapThemeSelector == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		var selected = _mapThemeSelector.Selected;
		_mapThemeSelector.Clear();
		_mapThemeSelector.AddItem(tm.Tr("map_theme_heightmap"), (int)MapView.TerrainStyle.Heightmap);
		_mapThemeSelector.AddItem(tm.Tr("map_theme_contour"), (int)MapView.TerrainStyle.Contour);
		_mapThemeSelector.AddItem(tm.Tr("map_theme_heatmap"), (int)MapView.TerrainStyle.Heatmap);

		if (_mapView != null)
		{
			_mapThemeSelector.Selected = (int)_mapView.TerrainStyleMode;
		}
		else if (selected >= 0 && selected < _mapThemeSelector.ItemCount)
		{
			_mapThemeSelector.Selected = selected;
		}
	}

	private void OnMapThemeSelected(long index)
	{
		if (_mapView == null || _mapThemeSelector == null)
		{
			return;
		}

		_mapView.TerrainStyleMode = (MapView.TerrainStyle)_mapThemeSelector.GetItemId((int)index);
	}

	private void OnCountryBorderWidthChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.CountryBorderWidth = (float)value;
		}
		UpdateCountryBorderWidthLabel();
	}

	private void OnCountryFillAlphaChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.CountryFillAlpha = (float)value;
		}
		UpdateCountryFillAlphaLabel();
	}

	private void OnCountryBorderColorChanged(Color color)
	{
		if (_mapView != null)
		{
			_mapView.CountryBorderColor = color;
			_mapView.QueueRedraw();
		}
	}

	private void UpdateCountryBorderWidthLabel()
	{
		if (_countryBorderWidthLabel == null || _countryBorderWidthSlider == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		string widthText = _countryBorderWidthSlider.Value.ToString("0.0");
		_countryBorderWidthLabel.Text = tm.TrWithFormat("country_border_width", widthText);
	}

	private void UpdateCountryFillAlphaLabel()
	{
		if (_countryFillAlphaLabel == null || _countryFillAlphaSlider == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		string alphaText = _countryFillAlphaSlider.Value.ToString("0.00");
		_countryFillAlphaLabel.Text = tm.TrWithFormat("country_fill_alpha", alphaText);
	}
}
