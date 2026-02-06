using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using TianYanShop.MapGeneration.Core;

namespace TianYanShop.MapGeneration.Data
{
    /// <summary>
    /// 名称库数据（.tres 资源格式）
    /// </summary>
    [Serializable]
    public partial class NameBase : Resource
    {
        [Export] public string Name { get; set; } = string.Empty;
        [Export] public int Index { get; set; }
        [Export] public int MinLength { get; set; } = 4;
        [Export] public int MaxLength { get; set; } = 9;
        [Export] public string DuplicateLetters { get; set; } = "aeiou";
        [Export] public string NameData { get; set; } = string.Empty;

        public NameBase() { }

        public NameBase(string name, int index, string data)
        {
            Name = name;
            Index = index;
            NameData = data;
            AnalyzeData();
        }

        private void AnalyzeData()
        {
            if (string.IsNullOrEmpty(NameData)) return;

            var names = NameData.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length == 0) return;

            MinLength = names.Min(n => n.Trim().Length);
            MaxLength = names.Max(n => n.Trim().Length);
        }

        public string[] GetNames()
        {
            if (string.IsNullOrEmpty(NameData))
                return Array.Empty<string>();

            return NameData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(n => n.Trim())
                          .Where(n => !string.IsNullOrEmpty(n))
                          .ToArray();
        }
    }

    /// <summary>
    /// 名称生成器（马尔可夫链）
    /// </summary>
    public class NameGenerator
    {
        private Dictionary<int, NameBase> _nameBases = new Dictionary<int, NameBase>();
        private Dictionary<int, Dictionary<string, List<string>>> _markovChains = new Dictionary<int, Dictionary<string, List<string>>>();
        private RandomManager _random;

        public NameGenerator()
        {
            _random = new RandomManager();
        }

        public NameGenerator(string seed)
        {
            _random = new RandomManager(seed);
        }

        public void AddNameBase(NameBase nameBase)
        {
            _nameBases[nameBase.Index] = nameBase;
            BuildMarkovChain(nameBase.Index);
        }

        public void LoadFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            foreach (var file in Directory.GetFiles(directoryPath, "*.tres"))
            {
                var resource = ResourceLoader.Load<NameBase>(file);
                if (resource != null)
                {
                    AddNameBase(resource);
                }
            }
        }

        private void BuildMarkovChain(int baseIndex)
        {
            if (!_nameBases.TryGetValue(baseIndex, out var nameBase)) return;

            var chain = new Dictionary<string, List<string>>();
            var names = nameBase.GetNames();

            foreach (var name in names)
            {
                string lowercaseName = name.ToLowerInvariant();
                for (int i = -1; i < lowercaseName.Length; i++)
                {
                    string prev = i < 0 ? "" : lowercaseName[i].ToString();
                    string syllable = "";

                    for (int c = i + 1; c < lowercaseName.Length && syllable.Length < 5; c++)
                    {
                        char current = lowercaseName[c];
                        char next = c + 1 < lowercaseName.Length ? lowercaseName[c + 1] : '\0';

                        syllable += current;

                        if (syllable == " " || syllable == "-") break;
                        if (c + 1 >= lowercaseName.Length) break;
                        if (lowercaseName[c + 1] == ' ' || lowercaseName[c + 1] == '-') break;

                        bool isVowel = IsVowel(current);
                        if (isVowel && c + 2 < lowercaseName.Length && IsVowel(lowercaseName[c + 2])) break;
                        if (isVowel && IsVowel(next)) break;

                        if (current == 'y' && next == 'e') continue;
                        if (current == 'o' && next == 'o') continue;
                        if (current == 'e' && next == 'e') continue;
                        if (current == 'a' && next == 'e') continue;
                        if (current == 'c' && next == 'h') continue;

                        if (i < 0 || !string.IsNullOrEmpty(syllable))
                        {
                            if (!chain.ContainsKey(prev))
                                chain[prev] = new List<string>();
                            if (!chain[prev].Contains(syllable))
                                chain[prev].Add(syllable);
                        }
                    }
                }
            }

            _markovChains[baseIndex] = chain;
        }

        private static bool IsVowel(char c)
        {
            return "aeiou".Contains(char.ToLowerInvariant(c));
        }

        public string GenerateName(int baseIndex, int? minLength = null, int? maxLength = null, string duplicateLetters = null)
        {
            if (!_nameBases.TryGetValue(baseIndex, out var nameBase)) return $"Name_{baseIndex}";
            if (!_markovChains.TryGetValue(baseIndex, out var chain)) return nameBase.Name;

            int min = minLength ?? nameBase.MinLength;
            int max = maxLength ?? nameBase.MaxLength;
            string dupl = duplicateLetters ?? nameBase.DuplicateLetters;

            if (!chain.TryGetValue("", out var starters))
            {
                starters = chain.Values.FirstOrDefault();
                if (starters == null) return nameBase.Name;
            }

            string result = "";
            string current = _random.NextItem(starters);

            for (int i = 0; i < 20; i++)
            {
                if (string.IsNullOrEmpty(current))
                {
                    if (result.Length < min)
                    {
                        current = "";
                        starters = chain.TryGetValue("", out var s) ? s : new List<string>();
                    }
                    else break;
                }
                else
                {
                    if (result.Length + current.Length > max)
                    {
                        if (result.Length < min) result += current;
                        break;
                    }

                    string lastChar = current.Length > 0 ? current.Substring(current.Length - 1) : "";
                    if (chain.TryGetValue(lastChar, out var nextOptions) && nextOptions.Count > 0)
                        current = _random.NextItem(nextOptions);
                    else if (chain.TryGetValue("", out var restartOptions) && restartOptions.Count > 0)
                        current = _random.NextItem(restartOptions);
                }

                result += current;
                current = "";
            }

            if (result.Length < 2)
            {
                result = _random.NextItem(nameBase.GetNames());
            }

            result = PostProcessName(result, dupl);
            return result;
        }

        private string PostProcessName(string name, string duplicateLetters)
        {
            var chars = name.ToCharArray();
            string processed = "";

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if (i > 0 && c == chars[i - 1] && !duplicateLetters.Contains(c))
                    continue;

                if (c == '\'' || c == ' ' || c == '-')
                {
                    if (i == chars.Length - 1) continue;
                    if (c == ' ' && i > 0 && chars[i - 1] == '-') continue;
                }

                if (i == 0)
                {
                    processed += char.ToUpperInvariant(c);
                }
                else if (i > 0 && (chars[i - 1] == ' ' || chars[i - 1] == '-'))
                {
                    processed += char.ToUpperInvariant(c);
                }
                else if (c == 'a' && i + 2 < chars.Length && chars[i + 1] == 'e')
                {
                    continue;
                }
                else
                {
                    processed += c;
                }
            }

            if (processed.Split(' ').Any(p => p.Length < 2))
            {
                processed = string.Join("", processed.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select((p, idx) => idx > 0 ? p.ToLower() : p));
            }

            return processed;
        }

        public void Seed(string seed)
        {
            _random.Seed(seed);
        }

        public void Clear()
        {
            _nameBases.Clear();
            _markovChains.Clear();
        }

        public int NameBaseCount => _nameBases.Count;
    }
}
