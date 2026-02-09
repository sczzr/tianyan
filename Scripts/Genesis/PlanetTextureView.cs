using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Genesis;

public partial class PlanetTextureView : TextureRect
{
	[Signal]
	public delegate void PreviewActivatedEventHandler();

	[Signal]
	public delegate void LivePhotoModeChangedEventHandler(bool enabled);

	[Signal]
	public delegate void LightFollowModeChangedEventHandler(bool enabled);

	[Signal]
	public delegate void LightResponseChangedEventHandler(float strength);

	[Signal]
	public delegate void SnapshotRefreshRequestedEventHandler();

	[Signal]
	public delegate void SkyTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void PlanetSurfaceTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void MoonTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void SunTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void DownloadedOnlyFilterChangedEventHandler(bool enabled);

	private ShaderMaterial _previewMaterial;
	private PanelContainer _buildPanel;
	private Label _buildLabel;
	private ProgressBar _buildProgress;
	private PanelContainer _pauseBadge;
	private Control _orbitSection;
	private Control _planetPhotoPanel;
	private TextureRect _planetPhotoTexture;
	private Control _photoViewerOverlay;
	private TextureRect _photoViewerTexture;
	private Button _photoViewerCloseButton;
	private CheckButton _livePhotoToggle;
	private CheckButton _lightFollowToggle;
	private Label _lightResponseLabel;
	private HSlider _lightResponseSlider;
	private Button _refreshPhotoButton;
	private OptionButton _skyTextureSelector;
	private OptionButton _planetSurfaceTextureSelector;
	private OptionButton _moonTextureSelector;
	private OptionButton _sunTextureSelector;
	private CheckButton _downloadedOnlyToggle;

	private int _lawAlignment = 50;
	private PlanetData _planetData = new PlanetData
	{
		Element = PlanetElement.Terra,
		OceanCoverage = 0.35f,
		Temperature = 0.5f,
		AtmosphereDensity = 0.55f
	};
	private bool _useStaticPreview = true;
	private readonly Dictionary<int, string> _skyTexturePathById = new();
	private readonly Dictionary<int, string> _planetTexturePathById = new();
	private readonly Dictionary<int, string> _moonTexturePathById = new();
	private readonly Dictionary<int, string> _sunTexturePathById = new();

	private readonly struct TextureOption
	{
		public TextureOption(string source, string label, string path)
		{
			Source = source;
			Label = label;
			Path = path;
		}

		public string Source { get; }

		public string Label { get; }
		public string Path { get; }
	}

	private static readonly TextureOption[] SkyTextureOptions =
	{
		new TextureOption("NASA", "银河 (EXR)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k_gal.exr"),
		new TextureOption("NASA", "星空 (EXR)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k.exr"),
		new TextureOption("NASA", "银河 (JPG)", "res://Assets/Textures/Stars/NASA/milkyway_2020_4k_gal_print.jpg"),
		new TextureOption("NASA", "银河星图 (JPG)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k_gal_print.jpg"),
		new TextureOption("NASA", "星图 (JPG)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k_print.jpg"),
		new TextureOption("SolarScope", "星空", "res://Assets/Textures/Stars/SolarSystemScope/2k_stars.jpg"),
		new TextureOption("SolarScope", "银河", "res://Assets/Textures/Stars/SolarSystemScope/2k_stars_milky_way.jpg")
	};

	private static readonly TextureOption[] PlanetSurfaceOptions =
	{
		new TextureOption("内置", "程序生成 (动态)", string.Empty),
		new TextureOption("SolarScope", "Earth 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_earth_daymap.jpg"),
		new TextureOption("SolarScope", "Mars 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_mars.jpg"),
		new TextureOption("SolarScope", "Jupiter 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_jupiter.jpg"),
		new TextureOption("SolarScope", "Saturn 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_saturn.jpg"),
		new TextureOption("OpenGameArt", "Red Surface", "res://Assets/Textures/Planets/OpenGameArt/RedPlanetAnimationSurface.jpg"),
		new TextureOption("OpenGameArt", "Planet Preview", "res://Assets/Textures/Planets/OpenGameArt/d6eQU.png"),
		new TextureOption("NASA JPL", "Earth (Legacy)", "res://Assets/Textures/Planets/NASA_JPL/earth.jpg")
	};

	private static readonly TextureOption[] MoonTextureOptions =
	{
		new TextureOption("内置", "默认卫星材质", string.Empty),
		new TextureOption("SolarScope", "Moon 2K", "res://Assets/Textures/Moons/SolarSystemScope/2k_moon.jpg")
	};

	private static readonly TextureOption[] SunTextureOptions =
	{
		new TextureOption("内置", "默认恒星材质", string.Empty),
		new TextureOption("SolarScope", "Sun 2K", "res://Assets/Textures/Stars/SolarSystemScope/2k_sun.jpg")
	};

	public bool IsLivePhotoUpdateEnabled => _livePhotoToggle?.ButtonPressed ?? true;
	public bool IsLightFollowEnabled => _lightFollowToggle?.ButtonPressed ?? true;
	public float LightResponseStrength => _lightResponseSlider != null
		? Mathf.Clamp((float)_lightResponseSlider.Value, 0f, 1f)
		: 0.75f;
	public bool ShowDownloadedOnly => _downloadedOnlyToggle?.ButtonPressed ?? false;
	public string SelectedSkyTexturePath { get; private set; } = string.Empty;
	public string SelectedPlanetSurfaceTexturePath { get; private set; } = string.Empty;
	public string SelectedMoonTexturePath { get; private set; } = string.Empty;
	public string SelectedSunTexturePath { get; private set; } = string.Empty;

	public override void _Ready()
	{
		ExpandMode = ExpandModeEnum.IgnoreSize;
		StretchMode = StretchModeEnum.Scale;
		MouseFilter = MouseFilterEnum.Stop;

		_buildPanel = GetNodeOrNull<PanelContainer>("BuildPanel");
		_buildLabel = GetNodeOrNull<Label>("BuildPanel/BuildVBox/BuildLabel");
		_buildProgress = GetNodeOrNull<ProgressBar>("BuildPanel/BuildVBox/BuildProgress");
		_pauseBadge = GetNodeOrNull<PanelContainer>("PauseBadge");
		_orbitSection = GetNodeOrNull<Control>("OrbitSection");
		_planetPhotoPanel = GetNodeOrNull<Control>("PlanetPhotoPanel");
		_planetPhotoTexture = GetNodeOrNull<TextureRect>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetPhotoTexture");
		_photoViewerOverlay = GetNodeOrNull<Control>("PhotoViewerOverlay");
		_photoViewerTexture = GetNodeOrNull<TextureRect>("PhotoViewerOverlay/PhotoViewerTexture");
		_photoViewerCloseButton = GetNodeOrNull<Button>("PhotoViewerOverlay/PhotoViewerCloseButton");
		_livePhotoToggle = GetNodeOrNull<CheckButton>("PlanetPhotoPanel/PlanetPhotoVBox/LivePhotoToggle");
		_lightFollowToggle = GetNodeOrNull<CheckButton>("PlanetPhotoPanel/PlanetPhotoVBox/LightFollowToggle");
		_lightResponseLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/LightResponseLabel");
		_lightResponseSlider = GetNodeOrNull<HSlider>("PlanetPhotoPanel/PlanetPhotoVBox/LightResponseSlider");
		_refreshPhotoButton = GetNodeOrNull<Button>("PlanetPhotoPanel/PlanetPhotoVBox/RefreshPhotoButton");
		_skyTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/SkyTextureSelector");
		_planetSurfaceTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetSurfaceTextureSelector");
		_moonTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/MoonTextureSelector");
		_sunTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/SunTextureSelector");
		_downloadedOnlyToggle = GetNodeOrNull<CheckButton>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyToggle");

		EnsureTextureSelectorNodes();

		SetupTextureSelectors();

		if (_planetPhotoTexture != null)
		{
			_planetPhotoTexture.MouseFilter = MouseFilterEnum.Stop;
			_planetPhotoTexture.MouseDefaultCursorShape = CursorShape.PointingHand;
			_planetPhotoTexture.GuiInput += OnPlanetPhotoTextureGuiInput;
		}

		if (_photoViewerOverlay != null)
		{
			_photoViewerOverlay.Visible = false;
			_photoViewerOverlay.MouseFilter = MouseFilterEnum.Stop;
			_photoViewerOverlay.GuiInput += OnPhotoViewerOverlayGuiInput;
		}

		if (_photoViewerCloseButton != null)
		{
			_photoViewerCloseButton.Pressed += OnPhotoViewerClosePressed;
		}

		if (_livePhotoToggle != null)
		{
			_livePhotoToggle.Toggled += OnLivePhotoToggleChanged;
		}

		if (_lightFollowToggle != null)
		{
			_lightFollowToggle.Toggled += OnLightFollowToggleChanged;
		}

		if (_lightResponseSlider != null)
		{
			_lightResponseSlider.ValueChanged += OnLightResponseSliderChanged;
			UpdateLightResponseLabel((float)_lightResponseSlider.Value);
		}

		if (_refreshPhotoButton != null)
		{
			_refreshPhotoButton.Pressed += OnRefreshPhotoPressed;
		}

		if (_downloadedOnlyToggle != null)
		{
			_downloadedOnlyToggle.Toggled += OnDownloadedOnlyToggleChanged;
		}

		SetBuildState(false, 0f, true);
		SetEditingPaused(false);
		EnterStaticMode();
		UpdatePreview(_planetData, _lawAlignment);
	}

	public override void _Process(double delta)
	{
		if (!_useStaticPreview || _previewMaterial == null)
		{
			return;
		}

		_previewMaterial.SetShaderParameter("time_sec", (float)Time.GetTicksMsec() / 1000f);
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (_photoViewerOverlay?.Visible == true)
		{
			if (inputEvent is InputEventKey keyEvent
				&& keyEvent.Pressed
				&& keyEvent.Keycode == Key.Escape)
			{
				ClosePhotoViewer();
				AcceptEvent();
			}

			return;
		}

		if (_buildPanel?.Visible == true)
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton
			&& mouseButton.Pressed
			&& mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (IsPointInInteractiveOverlay(mouseButton.Position))
			{
				return;
			}

			EmitSignal(SignalName.PreviewActivated);
			AcceptEvent();
		}
	}

	public void UpdatePreview(PlanetData planetData, int lawAlignment)
	{
		if (planetData != null)
		{
			_planetData = ClonePlanetData(planetData);
		}

		_lawAlignment = Mathf.Clamp(lawAlignment, 0, 100);
		EnsurePreviewMaterial();
		if (_previewMaterial == null)
		{
			return;
		}

		_previewMaterial.SetShaderParameter("element_type", ResolveElementType(_planetData.Element));
		_previewMaterial.SetShaderParameter("ocean_level", Mathf.Lerp(0.2f, 0.78f, Mathf.Clamp(_planetData.OceanCoverage, 0f, 1f)));
		_previewMaterial.SetShaderParameter("temperature", Mathf.Clamp(_planetData.Temperature, 0f, 1f));
		_previewMaterial.SetShaderParameter("atmosphere_density", Mathf.Clamp(_planetData.AtmosphereDensity, 0f, 1f));
		_previewMaterial.SetShaderParameter("cloud_speed", Mathf.Lerp(0.08f, 0.34f, _lawAlignment / 100f));
		_previewMaterial.SetShaderParameter("time_sec", (float)Time.GetTicksMsec() / 1000f);
	}

	public void SetLawAlignment(int lawAlignment)
	{
		_lawAlignment = Mathf.Clamp(lawAlignment, 0, 100);
		UpdatePreview(_planetData, _lawAlignment);
	}

	public void SetBuildState(bool isBuilding, float progress01, bool isZh)
	{
		if (_buildPanel != null)
		{
			_buildPanel.Visible = isBuilding;
		}

		if (_buildLabel != null)
		{
			_buildLabel.Text = isZh ? "正在构建3D寰宇..." : "Building 3D cosmos...";
		}

		if (_buildProgress != null)
		{
			_buildProgress.Value = Mathf.Clamp(progress01, 0f, 1f) * 100f;
		}
	}

	public void SetEditingPaused(bool isPaused)
	{
		if (_pauseBadge != null)
		{
			_pauseBadge.Visible = isPaused;
		}
	}

	public void SetPlanetSnapshot(Texture2D texture)
	{
		if (_planetPhotoTexture != null && texture != null)
		{
			_planetPhotoTexture.Texture = texture;
		}

		if (_photoViewerTexture != null && texture != null)
		{
			_photoViewerTexture.Texture = texture;
		}
	}

	public bool IsPointInInteractiveOverlay(Vector2 localPoint)
	{
		return IsControlHit(_orbitSection, localPoint)
			|| IsControlHit(_planetPhotoPanel, localPoint)
			|| IsControlHit(_photoViewerOverlay, localPoint);
	}

	public void EnterStaticMode()
	{
		_useStaticPreview = true;
		MouseFilter = MouseFilterEnum.Stop;
		EnsurePreviewMaterial();
	}

	public void EnterViewportMode()
	{
		_useStaticPreview = false;
		MouseFilter = MouseFilterEnum.Ignore;
		Material = null;
	}

	private void EnsureTextureSelectorNodes()
	{
		var photoVBox = GetNodeOrNull<VBoxContainer>("PlanetPhotoPanel/PlanetPhotoVBox");
		if (photoVBox == null)
		{
			return;
		}

		if (_downloadedOnlyToggle == null)
		{
			_downloadedOnlyToggle = new CheckButton
			{
				Name = "DownloadedOnlyToggle",
				Text = "仅显示已下载资源",
				ButtonPressed = false
			};
			InsertBeforePlanetPhoto(photoVBox, _downloadedOnlyToggle);
		}

		if (_sunTextureSelector == null)
		{
			var label = CreateCompactSelectorLabel("SunTextureLabel", "恒星贴图");
			_sunTextureSelector = new OptionButton
			{
				Name = "SunTextureSelector"
			};
			InsertBeforePlanetPhoto(photoVBox, label);
			InsertBeforePlanetPhoto(photoVBox, _sunTextureSelector);
		}

		if (_planetSurfaceTextureSelector == null)
		{
			var label = CreateCompactSelectorLabel("PlanetSurfaceTextureLabel", "行星贴图");
			_planetSurfaceTextureSelector = new OptionButton
			{
				Name = "PlanetSurfaceTextureSelector"
			};
			InsertBeforePlanetPhoto(photoVBox, label);
			InsertBeforePlanetPhoto(photoVBox, _planetSurfaceTextureSelector);
		}

		if (_moonTextureSelector == null)
		{
			var label = CreateCompactSelectorLabel("MoonTextureLabel", "卫星贴图");
			_moonTextureSelector = new OptionButton
			{
				Name = "MoonTextureSelector"
			};
			InsertBeforePlanetPhoto(photoVBox, label);
			InsertBeforePlanetPhoto(photoVBox, _moonTextureSelector);
		}
	}

	private void SetupTextureSelectors()
	{
		SetupSkyTextureSelector();
		SetupPlanetSurfaceTextureSelector();
		SetupMoonTextureSelector();
		SetupSunTextureSelector();
	}

	private void SetupSkyTextureSelector()
	{
		if (_skyTextureSelector == null)
		{
			return;
		}

		_skyTextureSelector.ItemSelected -= OnSkyTextureSelectorItemSelected;
		_skyTextureSelector.Clear();
		_skyTexturePathById.Clear();

		bool showDownloadedOnly = ShowDownloadedOnly;
		int itemId = 1;
		int selectedIndex = -1;

		for (int i = 0; i < SkyTextureOptions.Length; i++)
		{
			TextureOption option = SkyTextureOptions[i];
			if (showDownloadedOnly && !ResourceLoader.Exists(option.Path))
			{
				continue;
			}

			_skyTextureSelector.AddItem(option.Label, itemId);
			_skyTexturePathById[itemId] = option.Path;
			if (option.Path == SelectedSkyTexturePath)
			{
				selectedIndex = _skyTextureSelector.ItemCount - 1;
			}

			itemId++;
		}

		if (_skyTextureSelector.ItemCount > 0)
		{
			if (selectedIndex < 0)
			{
				selectedIndex = 0;
			}

			_skyTextureSelector.Select(selectedIndex);
			int selectedId = _skyTextureSelector.GetItemId(selectedIndex);
			if (_skyTexturePathById.TryGetValue(selectedId, out string texturePath))
			{
				SelectedSkyTexturePath = texturePath;
			}
		}

		_skyTextureSelector.ItemSelected += OnSkyTextureSelectorItemSelected;
	}

	private void SetupPlanetSurfaceTextureSelector()
	{
		if (_planetSurfaceTextureSelector == null)
		{
			return;
		}

		_planetSurfaceTextureSelector.ItemSelected -= OnPlanetSurfaceTextureSelectorItemSelected;
		_planetSurfaceTextureSelector.Clear();
		_planetTexturePathById.Clear();

		PopulateTextureSelector(
			_planetSurfaceTextureSelector,
			_planetTexturePathById,
			PlanetSurfaceOptions,
			SelectedPlanetSurfaceTexturePath,
			out string selectedPath,
			out _);

		SelectedPlanetSurfaceTexturePath = selectedPath;
		_planetSurfaceTextureSelector.ItemSelected += OnPlanetSurfaceTextureSelectorItemSelected;
	}

	private void SetupMoonTextureSelector()
	{
		if (_moonTextureSelector == null)
		{
			return;
		}

		_moonTextureSelector.ItemSelected -= OnMoonTextureSelectorItemSelected;
		_moonTextureSelector.Clear();
		_moonTexturePathById.Clear();

		PopulateTextureSelector(
			_moonTextureSelector,
			_moonTexturePathById,
			MoonTextureOptions,
			SelectedMoonTexturePath,
			out string selectedPath,
			out _);

		SelectedMoonTexturePath = selectedPath;
		_moonTextureSelector.ItemSelected += OnMoonTextureSelectorItemSelected;
	}

	private void SetupSunTextureSelector()
	{
		if (_sunTextureSelector == null)
		{
			return;
		}

		_sunTextureSelector.ItemSelected -= OnSunTextureSelectorItemSelected;
		_sunTextureSelector.Clear();
		_sunTexturePathById.Clear();

		PopulateTextureSelector(
			_sunTextureSelector,
			_sunTexturePathById,
			SunTextureOptions,
			SelectedSunTexturePath,
			out string selectedPath,
			out _);

		SelectedSunTexturePath = selectedPath;
		_sunTextureSelector.ItemSelected += OnSunTextureSelectorItemSelected;
	}

	private void PopulateTextureSelector(
		OptionButton selector,
		Dictionary<int, string> pathMap,
		TextureOption[] options,
		string targetPath,
		out string selectedPath,
		out int selectedIndex)
	{
		bool showDownloadedOnly = ShowDownloadedOnly;
		int itemId = 1;
		selectedIndex = -1;
		selectedPath = string.Empty;

		for (int i = 0; i < options.Length; i++)
		{
			TextureOption option = options[i];
			bool isDefaultOption = string.IsNullOrEmpty(option.Path);
			if (showDownloadedOnly && !isDefaultOption && !ResourceLoader.Exists(option.Path))
			{
				continue;
			}

			selector.AddItem(FormatOptionLabel(option), itemId);
			pathMap[itemId] = option.Path;
			if (option.Path == targetPath)
			{
				selectedIndex = selector.ItemCount - 1;
			}

			itemId++;
		}

		if (selector.ItemCount <= 0)
		{
			selector.AddItem("(无可用资源)", 1);
			pathMap[1] = string.Empty;
			selectedIndex = 0;
		}

		if (selectedIndex < 0)
		{
			selectedIndex = 0;
		}

		selector.Select(selectedIndex);
		int selectedId = selector.GetItemId(selectedIndex);
		if (pathMap.TryGetValue(selectedId, out string finalPath))
		{
			selectedPath = finalPath;
		}
	}

	private void OnSkyTextureSelectorItemSelected(long index)
	{
		if (_skyTextureSelector == null)
		{
			return;
		}

		int itemId = _skyTextureSelector.GetItemId((int)index);
		if (_skyTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedSkyTexturePath = texturePath;
			EmitSignal(SignalName.SkyTextureChanged, texturePath);
		}
	}

	private void OnPlanetSurfaceTextureSelectorItemSelected(long index)
	{
		if (_planetSurfaceTextureSelector == null)
		{
			return;
		}

		int itemId = _planetSurfaceTextureSelector.GetItemId((int)index);
		if (_planetTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedPlanetSurfaceTexturePath = texturePath;
			EmitSignal(SignalName.PlanetSurfaceTextureChanged, texturePath);
		}
	}

	private void OnMoonTextureSelectorItemSelected(long index)
	{
		if (_moonTextureSelector == null)
		{
			return;
		}

		int itemId = _moonTextureSelector.GetItemId((int)index);
		if (_moonTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedMoonTexturePath = texturePath;
			EmitSignal(SignalName.MoonTextureChanged, texturePath);
		}
	}

	private void OnSunTextureSelectorItemSelected(long index)
	{
		if (_sunTextureSelector == null)
		{
			return;
		}

		int itemId = _sunTextureSelector.GetItemId((int)index);
		if (_sunTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedSunTexturePath = texturePath;
			EmitSignal(SignalName.SunTextureChanged, texturePath);
		}
	}

	private void OnDownloadedOnlyToggleChanged(bool toggledOn)
	{
		SetupTextureSelectors();
		EmitSignal(SignalName.DownloadedOnlyFilterChanged, toggledOn);
	}

	private void OnLivePhotoToggleChanged(bool toggledOn)
	{
		EmitSignal(SignalName.LivePhotoModeChanged, toggledOn);
	}

	private void OnLightFollowToggleChanged(bool toggledOn)
	{
		EmitSignal(SignalName.LightFollowModeChanged, toggledOn);
	}

	private void OnLightResponseSliderChanged(double value)
	{
		float strength = Mathf.Clamp((float)value, 0f, 1f);
		UpdateLightResponseLabel(strength);
		EmitSignal(SignalName.LightResponseChanged, strength);
	}

	private void OnRefreshPhotoPressed()
	{
		EmitSignal(SignalName.SnapshotRefreshRequested);
	}

	private void OnPlanetPhotoTextureGuiInput(InputEvent inputEvent)
	{
		if (_planetPhotoTexture?.Texture == null)
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton
			&& mouseButton.Pressed
			&& mouseButton.ButtonIndex == MouseButton.Left)
		{
			OpenPhotoViewer(_planetPhotoTexture.Texture);
			AcceptEvent();
		}
	}

	private void OnPhotoViewerOverlayGuiInput(InputEvent inputEvent)
	{
		if (_photoViewerOverlay?.Visible != true)
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton
			&& mouseButton.Pressed
			&& mouseButton.ButtonIndex == MouseButton.Left)
		{
			ClosePhotoViewer();
			AcceptEvent();
		}
	}

	private void OnPhotoViewerClosePressed()
	{
		ClosePhotoViewer();
	}

	private void UpdateLightResponseLabel(float value)
	{
		if (_lightResponseLabel != null)
		{
			_lightResponseLabel.Text = $"光照响应: {Mathf.RoundToInt(Mathf.Clamp(value, 0f, 1f) * 100f)}%";
		}
	}

	private void OpenPhotoViewer(Texture2D texture)
	{
		if (_photoViewerOverlay == null || _photoViewerTexture == null || texture == null)
		{
			return;
		}

		_photoViewerTexture.Texture = texture;
		_photoViewerOverlay.Visible = true;
		_photoViewerOverlay.GrabFocus();
	}

	private void ClosePhotoViewer()
	{
		if (_photoViewerOverlay == null)
		{
			return;
		}

		_photoViewerOverlay.Visible = false;
	}

	private void EnsurePreviewMaterial()
	{
		if (!_useStaticPreview)
		{
			return;
		}

		if (_previewMaterial == null)
		{
			var shader = GD.Load<Shader>("res://Shaders/PlanetPreview.gdshader");
			if (shader == null)
			{
				return;
			}

			_previewMaterial = new ShaderMaterial
			{
				Shader = shader
			};
		}

		Material = _previewMaterial;
		Texture = null;
		SelfModulate = Colors.White;
	}

	private static bool IsControlHit(Control control, Vector2 point)
	{
		if (control == null || !control.Visible)
		{
			return false;
		}

		var rect = new Rect2(control.Position, control.Size);
		return rect.HasPoint(point);
	}

	private void InsertBeforePlanetPhoto(VBoxContainer photoVBox, Control control)
	{
		if (photoVBox == null || control == null)
		{
			return;
		}

		photoVBox.AddChild(control);

		if (_planetPhotoTexture == null)
		{
			return;
		}

		int photoIndex = FindChildIndex(photoVBox, _planetPhotoTexture);
		int controlIndex = FindChildIndex(photoVBox, control);
		if (photoIndex >= 0 && controlIndex >= 0 && controlIndex > photoIndex)
		{
			photoVBox.MoveChild(control, photoIndex);
		}
	}

	private static int FindChildIndex(Node parent, Node child)
	{
		if (parent == null || child == null)
		{
			return -1;
		}

		for (int i = 0; i < parent.GetChildCount(); i++)
		{
			if (parent.GetChild(i) == child)
			{
				return i;
			}
		}

		return -1;
	}

	private static Label CreateCompactSelectorLabel(string name, string text)
	{
		var label = new Label
		{
			Name = name,
			Text = text
		};

		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.82f, 0.95f));
		return label;
	}

	private static string FormatOptionLabel(TextureOption option)
	{
		if (string.IsNullOrWhiteSpace(option.Source))
		{
			return option.Label;
		}

		return $"[{option.Source}] {option.Label}";
	}

	private static float ResolveElementType(PlanetElement element)
	{
		return element switch
		{
			PlanetElement.Terra => 0f,
			PlanetElement.Pyro => 1f,
			PlanetElement.Cryo => 2f,
			PlanetElement.Aero => 3f,
			_ => 0f
		};
	}

	private static PlanetData ClonePlanetData(PlanetData planetData)
	{
		if (planetData == null)
		{
			return new PlanetData();
		}

		return new PlanetData
		{
			PlanetId = planetData.PlanetId,
			Name = planetData.Name,
			Element = planetData.Element,
			Size = planetData.Size,
			OceanCoverage = planetData.OceanCoverage,
			Temperature = planetData.Temperature,
			AtmosphereDensity = planetData.AtmosphereDensity
		};
	}
}
