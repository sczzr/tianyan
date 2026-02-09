using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Genesis;

public class NarrativeSnapshot
{
	public string RoleTitle { get; set; } = string.Empty;
	public string Summary { get; set; } = string.Empty;
	public string ConsoleText { get; set; } = string.Empty;
}

public class NarrativeGenerator
{
	private readonly Dictionary<string, NarrativeConfig> _configCache = new();

	public NarrativeSnapshot RefreshText(UniverseData universeData)
	{
		if (universeData == null)
		{
			return new NarrativeSnapshot();
		}

		string language = TranslationManager.Instance.CurrentLanguage;
		var config = GetConfig(language);
		bool isZh = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

		string themeKey = ResolveThemeKey(universeData.LawAlignment);
		string roleKey = ResolveRoleKey(universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard);
		string planetKey = ResolvePlanetKey(universeData.CurrentPlanet?.Element ?? PlanetElement.Terra);

		string themeDesc = config.ThemeDesc.GetValueOrDefault(themeKey, string.Empty);
		string planetDesc = config.PlanetDesc.GetValueOrDefault(planetKey, string.Empty);
		string roleDesc = config.RoleDesc.GetValueOrDefault(roleKey, string.Empty);

		string console = BuildConsoleText(isZh, universeData, themeKey, themeDesc, planetDesc, roleDesc);
		string summary = BuildSummary(universeData, themeDesc, planetDesc, isZh);

		return new NarrativeSnapshot
		{
			RoleTitle = ResolveRoleTitle(universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard, isZh),
			Summary = summary,
			ConsoleText = console
		};
	}

	private string BuildConsoleText(bool isZh, UniverseData universeData, string themeKey, string themeDesc, string planetDesc, string roleDesc)
	{
		string lawLine = isZh
			? $"> [天道] 法则偏向: {universeData.LawAlignment}% ({ThemeLabel(themeKey, true)})"
			: $"> [Laws] Alignment: {universeData.LawAlignment}% ({ThemeLabel(themeKey, false)})";

		string planetLine = isZh
			? $"> [地脉] 星辰法相: {ResolveElementLabel(universeData.CurrentPlanet?.Element ?? PlanetElement.Terra, true)}"
			: $"> [Planet] Elemental type: {ResolveElementLabel(universeData.CurrentPlanet?.Element ?? PlanetElement.Terra, false)}";

		string civLine = isZh
			? $"> [演化] 星火密度 {universeData.CivilizationDensity}% · 光阴流速 {universeData.TimeFlowRate:0.00}x"
			: $"> [Evolution] Civilization density {universeData.CivilizationDensity}% · Time flow {universeData.TimeFlowRate:0.00}x";

		string roleLine = isZh
			? $"> [观测] {ResolveRoleTitle(universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard, true)}"
			: $"> [Role] {ResolveRoleTitle(universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard, false)}";

		string descLine = isZh
			? $"> [注解] {themeDesc} {planetDesc} {roleDesc}"
			: $"> [Lore] {themeDesc} {planetDesc} {roleDesc}";

		return string.Join("\n", lawLine, planetLine, civLine, roleLine, descLine);
	}

	private static string BuildSummary(UniverseData universeData, string themeDesc, string planetDesc, bool isZh)
	{
		if (isZh)
		{
			return $"当前界域摘要：{themeDesc}{planetDesc}（星火密度 {universeData.CivilizationDensity}%）";
		}

		return $"Realm summary: {themeDesc} {planetDesc} (Civilization density {universeData.CivilizationDensity}%).";
	}

	private NarrativeConfig GetConfig(string language)
	{
		string normalized = NormalizeLanguage(language);
		if (_configCache.TryGetValue(normalized, out var cached))
		{
			return cached;
		}

		var loaded = LoadConfigFromJson($"res://Assets/Config/narrative_{normalized}.json");
		if (loaded == null)
		{
			loaded = BuildFallbackConfig(normalized == "zh");
		}

		_configCache[normalized] = loaded;
		return loaded;
	}

	private static string NormalizeLanguage(string language)
	{
		if (string.IsNullOrWhiteSpace(language))
		{
			return "zh";
		}

		return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
	}

	private static NarrativeConfig LoadConfigFromJson(string path)
	{
		if (!FileAccess.FileExists(path))
		{
			return null;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			return null;
		}

		string jsonText = file.GetAsText();
		var parsed = Json.ParseString(jsonText);
		if (parsed.VariantType != Variant.Type.Dictionary)
		{
			return null;
		}

		var root = (Godot.Collections.Dictionary)parsed;
		var config = new NarrativeConfig
		{
			ThemeDesc = ReadStringMap(root, "theme_desc"),
			PlanetDesc = ReadStringMap(root, "planet_desc"),
			RoleDesc = ReadStringMap(root, "role_desc")
		};

		return config;
	}

	private static Dictionary<string, string> ReadStringMap(Godot.Collections.Dictionary root, string key)
	{
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!root.ContainsKey(key))
		{
			return map;
		}

		if (root[key].VariantType != Variant.Type.Dictionary)
		{
			return map;
		}

		var dict = (Godot.Collections.Dictionary)root[key];
		foreach (var dictKey in dict.Keys)
		{
			string textKey = dictKey.AsString();
			map[textKey] = dict[dictKey].AsString();
		}

		return map;
	}

	private static NarrativeConfig BuildFallbackConfig(bool isZh)
	{
		if (isZh)
		{
			return new NarrativeConfig
			{
				ThemeDesc = new Dictionary<string, string>
				{
					["magic"] = "此界灵气沛然，古老天道仍在回响。",
					["balance"] = "蒸汽与符文并行，古今秩序彼此纠缠。",
					["tech"] = "数据星海翻涌，理性法则主导文明轨迹。"
				},
				PlanetDesc = new Dictionary<string, string>
				{
					["terra"] = "后土承载万象，山海并育生机。",
					["pyro"] = "祝融烈焰穿脉而行，危机与矿藏并生。",
					["cryo"] = "玄冥极寒封域，寂静中孕育顽强文明。",
					["aero"] = "罡风浮陆交错，航路与空城共舞。"
				},
				RoleDesc = new Dictionary<string, string>
				{
					["simple"] = "你将以红尘客之眼，见天地一隅。",
					["standard"] = "你将执掌一方霸业，统御山河脉络。",
					["complex"] = "你将推演万象兴衰，观测文明全史。",
					["custom"] = "你将改写法则，重塑界域边界。"
				}
			};
		}

		return new NarrativeConfig
		{
			ThemeDesc = new Dictionary<string, string>
			{
				["magic"] = "Arcane currents flood the realm and old laws still echo.",
				["balance"] = "Runes and engines coexist in a fragile equilibrium.",
				["tech"] = "Data constellations govern history with precise logic."
			},
			PlanetDesc = new Dictionary<string, string>
			{
				["terra"] = "A fertile world of oceans and continents kindles civilization.",
				["pyro"] = "A volcanic domain where danger and power are inseparable.",
				["cryo"] = "A frozen frontier where resilience defines survival.",
				["aero"] = "Aero-isles drift in storm belts and sky routes define empires."
			},
			RoleDesc = new Dictionary<string, string>
			{
				["simple"] = "You observe as a wanderer in a single layer of the world.",
				["standard"] = "You govern a realm with strategic depth and structure.",
				["complex"] = "You orchestrate multi-layer evolution across civilizations.",
				["custom"] = "You rewrite the rules and define your own hierarchy."
			}
		};
	}

	private static string ResolveThemeKey(int lawAlignment)
	{
		if (lawAlignment <= 35)
		{
			return "magic";
		}

		if (lawAlignment >= 65)
		{
			return "tech";
		}

		return "balance";
	}

	private static string ResolvePlanetKey(PlanetElement element)
	{
		return element switch
		{
			PlanetElement.Terra => "terra",
			PlanetElement.Pyro => "pyro",
			PlanetElement.Cryo => "cryo",
			PlanetElement.Aero => "aero",
			_ => "terra"
		};
	}

	private static string ResolveRoleKey(HierarchyArchetype archetype)
	{
		return archetype switch
		{
			HierarchyArchetype.Simple => "simple",
			HierarchyArchetype.Standard => "standard",
			HierarchyArchetype.Complex => "complex",
			HierarchyArchetype.Custom => "custom",
			_ => "standard"
		};
	}

	private static string ResolveRoleTitle(HierarchyArchetype archetype, bool isZh)
	{
		if (isZh)
		{
			return archetype switch
			{
				HierarchyArchetype.Simple => "【 游历 · 红尘客 】",
				HierarchyArchetype.Standard => "【 经略 · 一方霸主 】",
				HierarchyArchetype.Complex => "【 演化 · 掌道者 】",
				HierarchyArchetype.Custom => "【 虚空 · 观测者 】",
				_ => "【 经略 · 一方霸主 】"
			};
		}

		return archetype switch
		{
			HierarchyArchetype.Simple => "[ Journey · Wanderer ]",
			HierarchyArchetype.Standard => "[ Strategy · Sovereign ]",
			HierarchyArchetype.Complex => "[ Evolution · Demiurge ]",
			HierarchyArchetype.Custom => "[ Void · Observer ]",
			_ => "[ Strategy · Sovereign ]"
		};
	}

	private static string ThemeLabel(string themeKey, bool isZh)
	{
		if (isZh)
		{
			return themeKey switch
			{
				"magic" => "灵气",
				"tech" => "天工",
				_ => "混沌"
			};
		}

		return themeKey switch
		{
			"magic" => "Magic",
			"tech" => "Tech",
			_ => "Balanced"
		};
	}

	private static string ResolveElementLabel(PlanetElement element, bool isZh)
	{
		if (isZh)
		{
			return element switch
			{
				PlanetElement.Terra => "后土",
				PlanetElement.Pyro => "祝融",
				PlanetElement.Cryo => "玄冥",
				PlanetElement.Aero => "罡风",
				_ => "后土"
			};
		}

		return element switch
		{
			PlanetElement.Terra => "Terra",
			PlanetElement.Pyro => "Pyro",
			PlanetElement.Cryo => "Cryo",
			PlanetElement.Aero => "Aero",
			_ => "Terra"
		};
	}

	private class NarrativeConfig
	{
		public Dictionary<string, string> ThemeDesc { get; set; } = new(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, string> PlanetDesc { get; set; } = new(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, string> RoleDesc { get; set; } = new(StringComparer.OrdinalIgnoreCase);
	}
}
