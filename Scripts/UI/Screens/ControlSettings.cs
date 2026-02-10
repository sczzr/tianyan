using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI.Screens;

/// <summary>
/// 操作设置界面
/// </summary>
public partial class ControlSettings : Control
{
	private const string ConfigPath = "user://settings.cfg";
	private const string ConfigSection = "controls";

	private const float DefaultZoomStep = 0.05f;
	private const float DefaultFineZoomStep = 0.01f;
	private const float DefaultCoarseZoomStep = 0.1f;
	private const bool DefaultUseSmoothing = true;
	private const float DefaultZoomSpeed = 10f;
	private const float DefaultZoomImpulse = 6f;
	private const float DefaultZoomDamping = 12f;
	private const float DefaultMaxZoomVelocity = 3f;
	private const bool DefaultEnableKeyboardPan = true;
	private const float DefaultMoveSpeed = 240f;
	private const float DefaultMoveAcceleration = 1200f;
	private const float DefaultMoveDamping = 1600f;
	private const bool DefaultScaleMoveSpeedByZoom = true;

	private Label _titleLabel;
	private Label _zoomSpeedLabel;
	private Label _zoomImpulseLabel;
	private Label _zoomDampingLabel;
	private Label _maxZoomVelocityLabel;
	private Label _zoomStepLabel;
	private Label _fineZoomStepLabel;
	private Label _coarseZoomStepLabel;
	private Label _moveSpeedLabel;
	private Label _moveAccelerationLabel;
	private Label _moveDampingLabel;
	private TabContainer _tabs;
	private Label _zoomSmoothingLabel;
	private Label _enableKeyboardPanLabel;
	private Label _scaleMoveSpeedLabel;

	private CheckBox _zoomSmoothingCheck;
	private CheckBox _enableKeyboardPanCheck;
	private CheckBox _scaleMoveSpeedCheck;

	private HSlider _zoomSpeedSlider;
	private HSlider _zoomImpulseSlider;
	private HSlider _zoomDampingSlider;
	private HSlider _maxZoomVelocitySlider;
	private HSlider _zoomStepSlider;
	private HSlider _fineZoomStepSlider;
	private HSlider _coarseZoomStepSlider;
	private HSlider _moveSpeedSlider;
	private HSlider _moveAccelerationSlider;
	private HSlider _moveDampingSlider;

	private Label _zoomSpeedValueLabel;
	private Label _zoomImpulseValueLabel;
	private Label _zoomDampingValueLabel;
	private Label _maxZoomVelocityValueLabel;
	private Label _zoomStepValueLabel;
	private Label _fineZoomStepValueLabel;
	private Label _coarseZoomStepValueLabel;
	private Label _moveSpeedValueLabel;
	private Label _moveAccelerationValueLabel;
	private Label _moveDampingValueLabel;

	private Button _saveButton;
	private Button _resetButton;
	private Button _backButton;

	private TranslationManager _translationManager;

	public override void _Ready()
	{
		_titleLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/TitleLabel");
		_tabs = GetNodeOrNull<TabContainer>("SettingsPanel/SettingsVBox/Tabs");
		_zoomSpeedLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomSpeedLabel");
		_zoomImpulseLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomImpulseLabel");
		_zoomDampingLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomDampingLabel");
		_maxZoomVelocityLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/MaxZoomVelocityLabel");
		_zoomStepLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomStepLabel");
		_fineZoomStepLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/FineZoomStepLabel");
		_coarseZoomStepLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/CoarseZoomStepLabel");
		_moveSpeedLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveSpeedLabel");
		_moveAccelerationLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveAccelerationLabel");
		_moveDampingLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveDampingLabel");

		_enableKeyboardPanCheck = GetNodeOrNull<CheckBox>("SettingsPanel/SettingsVBox/Tabs/AdvancedPage/AdvancedGrid/EnableKeyboardPanCheck");
		_scaleMoveSpeedCheck = GetNodeOrNull<CheckBox>("SettingsPanel/SettingsVBox/Tabs/AdvancedPage/AdvancedGrid/ScaleMoveSpeedCheck");
		_zoomSmoothingLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/AdvancedPage/AdvancedGrid/ZoomSmoothingLabel");
		_enableKeyboardPanLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/AdvancedPage/AdvancedGrid/EnableKeyboardPanLabel");
		_scaleMoveSpeedLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/AdvancedPage/AdvancedGrid/ScaleMoveSpeedLabel");
		_zoomSmoothingCheck = GetNodeOrNull<CheckBox>("SettingsPanel/SettingsVBox/Tabs/AdvancedPage/AdvancedGrid/ZoomSmoothingCheck");

		_zoomSpeedSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomSpeedSlider");
		_zoomImpulseSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomImpulseSlider");
		_zoomDampingSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomDampingSlider");
		_maxZoomVelocitySlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/MaxZoomVelocitySlider");
		_zoomStepSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomStepSlider");
		_fineZoomStepSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/FineZoomStepSlider");
		_coarseZoomStepSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/CoarseZoomStepSlider");
		_moveSpeedSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveSpeedSlider");
		_moveAccelerationSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveAccelerationSlider");
		_moveDampingSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveDampingSlider");

		_zoomSpeedValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomSpeedValueLabel");
		_zoomImpulseValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomImpulseValueLabel");
		_zoomDampingValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomDampingValueLabel");
		_maxZoomVelocityValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/MaxZoomVelocityValueLabel");
		_zoomStepValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/ZoomStepValueLabel");
		_fineZoomStepValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/FineZoomStepValueLabel");
		_coarseZoomStepValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/ZoomPage/ZoomGrid/CoarseZoomStepValueLabel");
		_moveSpeedValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveSpeedValueLabel");
		_moveAccelerationValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveAccelerationValueLabel");
		_moveDampingValueLabel = GetNodeOrNull<Label>("SettingsPanel/SettingsVBox/Tabs/MovePage/MoveGrid/MoveDampingValueLabel");

		_saveButton = GetNodeOrNull<Button>("SettingsPanel/SettingsVBox/ButtonsHBox/SaveButton");
		_resetButton = GetNodeOrNull<Button>("SettingsPanel/SettingsVBox/ButtonsHBox/ResetButton");
		_backButton = GetNodeOrNull<Button>("SettingsPanel/SettingsVBox/ButtonsHBox/BackButton");


		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		BindEvents();
		LoadSettings();
		UpdateUIText();
		UpdateValueLabels();
	}

	private void BindEvents()
	{
		if (_saveButton != null)
		{
			_saveButton.Pressed += OnSavePressed;
		}
		if (_resetButton != null)
		{
			_resetButton.Pressed += OnResetPressed;
		}
		if (_backButton != null)
		{
			_backButton.Pressed += OnBackPressed;
		}

		BindSlider(_zoomSpeedSlider);
		BindSlider(_zoomImpulseSlider);
		BindSlider(_zoomDampingSlider);
		BindSlider(_maxZoomVelocitySlider);
		BindSlider(_zoomStepSlider);
		BindSlider(_fineZoomStepSlider);
		BindSlider(_coarseZoomStepSlider);
		BindSlider(_moveSpeedSlider);
		BindSlider(_moveAccelerationSlider);
		BindSlider(_moveDampingSlider);
	}

	private void BindSlider(HSlider slider)
	{
		if (slider == null)
		{
			return;
		}

		slider.ValueChanged += _ => UpdateValueLabels();
	}

	private void LoadSettings()
	{
		ApplyDefaults();

		var config = new ConfigFile();
		if (config.Load(ConfigPath) != Error.Ok)
		{
			return;
		}

		SetCheckValue(_zoomSmoothingCheck, ReadBool(config, "use_smoothing", DefaultUseSmoothing));
		SetCheckValue(_enableKeyboardPanCheck, ReadBool(config, "enable_keyboard_pan", DefaultEnableKeyboardPan));
		SetCheckValue(_scaleMoveSpeedCheck, ReadBool(config, "scale_move_speed_by_zoom", DefaultScaleMoveSpeedByZoom));

		SetSliderValue(_zoomSpeedSlider, ReadFloat(config, "zoom_speed", DefaultZoomSpeed));
		SetSliderValue(_zoomImpulseSlider, ReadFloat(config, "zoom_impulse", DefaultZoomImpulse));
		SetSliderValue(_zoomDampingSlider, ReadFloat(config, "zoom_damping", DefaultZoomDamping));
		SetSliderValue(_maxZoomVelocitySlider, ReadFloat(config, "max_zoom_velocity", DefaultMaxZoomVelocity));
		SetSliderValue(_zoomStepSlider, ReadFloat(config, "zoom_step", DefaultZoomStep));
		SetSliderValue(_fineZoomStepSlider, ReadFloat(config, "fine_zoom_step", DefaultFineZoomStep));
		SetSliderValue(_coarseZoomStepSlider, ReadFloat(config, "coarse_zoom_step", DefaultCoarseZoomStep));
		SetSliderValue(_moveSpeedSlider, ReadFloat(config, "move_speed", DefaultMoveSpeed));
		SetSliderValue(_moveAccelerationSlider, ReadFloat(config, "move_acceleration", DefaultMoveAcceleration));
		SetSliderValue(_moveDampingSlider, ReadFloat(config, "move_damping", DefaultMoveDamping));
	}

	private void ApplyDefaults()
	{
		SetCheckValue(_zoomSmoothingCheck, DefaultUseSmoothing);
		SetCheckValue(_enableKeyboardPanCheck, DefaultEnableKeyboardPan);
		SetCheckValue(_scaleMoveSpeedCheck, DefaultScaleMoveSpeedByZoom);

		SetSliderValue(_zoomSpeedSlider, DefaultZoomSpeed);
		SetSliderValue(_zoomImpulseSlider, DefaultZoomImpulse);
		SetSliderValue(_zoomDampingSlider, DefaultZoomDamping);
		SetSliderValue(_maxZoomVelocitySlider, DefaultMaxZoomVelocity);
		SetSliderValue(_zoomStepSlider, DefaultZoomStep);
		SetSliderValue(_fineZoomStepSlider, DefaultFineZoomStep);
		SetSliderValue(_coarseZoomStepSlider, DefaultCoarseZoomStep);
		SetSliderValue(_moveSpeedSlider, DefaultMoveSpeed);
		SetSliderValue(_moveAccelerationSlider, DefaultMoveAcceleration);
		SetSliderValue(_moveDampingSlider, DefaultMoveDamping);
	}

	private float ReadFloat(ConfigFile config, string key, float defaultValue)
	{
		if (!config.HasSectionKey(ConfigSection, key))
		{
			return defaultValue;
		}

		var value = config.GetValue(ConfigSection, key, defaultValue);
		var text = value.AsString();
		if (float.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (bool.TryParse(text, out var parsedBool))
		{
			return parsedBool ? 1f : 0f;
		}

		return defaultValue;
	}

	private bool ReadBool(ConfigFile config, string key, bool defaultValue)
	{
		if (!config.HasSectionKey(ConfigSection, key))
		{
			return defaultValue;
		}

		var value = config.GetValue(ConfigSection, key, defaultValue);
		var text = value.AsString();
		if (bool.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (int.TryParse(text, out var parsedInt))
		{
			return parsedInt != 0;
		}

		if (float.TryParse(text, out var parsedFloat))
		{
			return Mathf.Abs(parsedFloat) > 0.0001f;
		}

		return defaultValue;
	}

	private void SetSliderValue(HSlider slider, float value)
	{
		if (slider == null)
		{
			return;
		}

		slider.Value = value;
	}

	private void SetCheckValue(CheckBox checkButton, bool value)
	{
		if (checkButton == null)
		{
			return;
		}

		checkButton.ButtonPressed = value;
	}

	private void UpdateValueLabels()
	{
		SetValueLabel(_zoomSpeedValueLabel, _zoomSpeedSlider, "0.0");
		SetValueLabel(_zoomImpulseValueLabel, _zoomImpulseSlider, "0.0");
		SetValueLabel(_zoomDampingValueLabel, _zoomDampingSlider, "0.0");
		SetValueLabel(_maxZoomVelocityValueLabel, _maxZoomVelocitySlider, "0.00");
		SetValueLabel(_zoomStepValueLabel, _zoomStepSlider, "0.000");
		SetValueLabel(_fineZoomStepValueLabel, _fineZoomStepSlider, "0.000");
		SetValueLabel(_coarseZoomStepValueLabel, _coarseZoomStepSlider, "0.000");
		SetValueLabel(_moveSpeedValueLabel, _moveSpeedSlider, "0");
		SetValueLabel(_moveAccelerationValueLabel, _moveAccelerationSlider, "0");
		SetValueLabel(_moveDampingValueLabel, _moveDampingSlider, "0");
	}

	private void SetValueLabel(Label label, HSlider slider, string format)
	{
		if (label == null || slider == null)
		{
			return;
		}

		label.Text = ((float)slider.Value).ToString(format);
	}

	private void OnLanguageChanged(string language)
	{
		UpdateUIText();
	}

	private void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_titleLabel != null)
		{
			_titleLabel.Text = tm.Tr("controls_title");
		}
		if (_tabs != null)
		{
		_tabs.SetTabTitle(0, tm.Tr("controls_tab_zoom"));
		_tabs.SetTabTitle(1, tm.Tr("controls_tab_move"));
		_tabs.SetTabTitle(2, tm.Tr("controls_tab_advanced"));
		}
		if (_zoomSmoothingCheck != null)
		{
			_zoomSmoothingCheck.Text = string.Empty;
		}
		if (_zoomSmoothingLabel != null)
		{
			_zoomSmoothingLabel.Text = tm.Tr("controls_zoom_smoothing");
		}
		if (_zoomSpeedLabel != null)
		{
			_zoomSpeedLabel.Text = tm.Tr("controls_zoom_speed");
		}
		if (_zoomImpulseLabel != null)
		{
			_zoomImpulseLabel.Text = tm.Tr("controls_zoom_impulse");
		}
		if (_zoomDampingLabel != null)
		{
			_zoomDampingLabel.Text = tm.Tr("controls_zoom_damping");
		}
		if (_maxZoomVelocityLabel != null)
		{
			_maxZoomVelocityLabel.Text = tm.Tr("controls_zoom_max_velocity");
		}
		if (_zoomStepLabel != null)
		{
			_zoomStepLabel.Text = tm.Tr("controls_zoom_step");
		}
		if (_fineZoomStepLabel != null)
		{
			_fineZoomStepLabel.Text = tm.Tr("controls_zoom_step_fine");
		}
		if (_coarseZoomStepLabel != null)
		{
			_coarseZoomStepLabel.Text = tm.Tr("controls_zoom_step_coarse");
		}
		if (_enableKeyboardPanCheck != null)
		{
			_enableKeyboardPanCheck.Text = string.Empty;
		}
		if (_enableKeyboardPanLabel != null)
		{
			_enableKeyboardPanLabel.Text = tm.Tr("controls_enable_keyboard_pan");
		}
		if (_scaleMoveSpeedCheck != null)
		{
			_scaleMoveSpeedCheck.Text = string.Empty;
		}
		if (_scaleMoveSpeedLabel != null)
		{
			_scaleMoveSpeedLabel.Text = tm.Tr("controls_scale_move_speed");
		}
		if (_moveSpeedLabel != null)
		{
			_moveSpeedLabel.Text = tm.Tr("controls_move_speed");
		}
		if (_moveAccelerationLabel != null)
		{
			_moveAccelerationLabel.Text = tm.Tr("controls_move_acceleration");
		}
		if (_moveDampingLabel != null)
		{
			_moveDampingLabel.Text = tm.Tr("controls_move_damping");
		}
		if (_saveButton != null)
		{
			_saveButton.Text = tm.Tr("save_settings");
		}
		if (_resetButton != null)
		{
			_resetButton.Text = tm.Tr("controls_reset");
		}
		if (_backButton != null)
		{
			_backButton.Text = tm.Tr("back");
		}
	}

	private void OnSavePressed()
	{
		var config = new ConfigFile();
		config.Load(ConfigPath);

		config.SetValue(ConfigSection, "use_smoothing", _zoomSmoothingCheck?.ButtonPressed ?? DefaultUseSmoothing);
		config.SetValue(ConfigSection, "enable_keyboard_pan", _enableKeyboardPanCheck?.ButtonPressed ?? DefaultEnableKeyboardPan);
		config.SetValue(ConfigSection, "scale_move_speed_by_zoom", _scaleMoveSpeedCheck?.ButtonPressed ?? DefaultScaleMoveSpeedByZoom);

		config.SetValue(ConfigSection, "zoom_speed", GetSliderValue(_zoomSpeedSlider, DefaultZoomSpeed));
		config.SetValue(ConfigSection, "zoom_impulse", GetSliderValue(_zoomImpulseSlider, DefaultZoomImpulse));
		config.SetValue(ConfigSection, "zoom_damping", GetSliderValue(_zoomDampingSlider, DefaultZoomDamping));
		config.SetValue(ConfigSection, "max_zoom_velocity", GetSliderValue(_maxZoomVelocitySlider, DefaultMaxZoomVelocity));
		config.SetValue(ConfigSection, "zoom_step", GetSliderValue(_zoomStepSlider, DefaultZoomStep));
		config.SetValue(ConfigSection, "fine_zoom_step", GetSliderValue(_fineZoomStepSlider, DefaultFineZoomStep));
		config.SetValue(ConfigSection, "coarse_zoom_step", GetSliderValue(_coarseZoomStepSlider, DefaultCoarseZoomStep));
		config.SetValue(ConfigSection, "move_speed", GetSliderValue(_moveSpeedSlider, DefaultMoveSpeed));
		config.SetValue(ConfigSection, "move_acceleration", GetSliderValue(_moveAccelerationSlider, DefaultMoveAcceleration));
		config.SetValue(ConfigSection, "move_damping", GetSliderValue(_moveDampingSlider, DefaultMoveDamping));

		var error = config.Save(ConfigPath);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to save control settings: {error}");
		}
	}

	private float GetSliderValue(HSlider slider, float defaultValue)
	{
		if (slider == null)
		{
			return defaultValue;
		}

		return (float)slider.Value;
	}

	private void OnResetPressed()
	{
		ApplyDefaults();
		UpdateValueLabels();
	}

	private void OnBackPressed()
	{
		SceneNavigator.Instance.GoBack();
	}

	public override void _ExitTree()
	{
		if (_translationManager != null)
		{
			_translationManager.LanguageChanged -= OnLanguageChanged;
		}
	}
}
