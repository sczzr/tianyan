using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class NameGenerator
    {
        private RandomManager _random;

        public NameGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            GenerateBurgNames(graph);
            GenerateStateNames(graph);
            GenerateCultureNames(graph);
        }

        private void GenerateBurgNames(VoronoiGraph graph)
        {
            foreach (var burg in graph.BurgsList)
            {
                if (string.IsNullOrEmpty(burg.Name))
                {
                    burg.Name = GenerateRandomBurgName();
                }
            }
        }

        private void GenerateStateNames(VoronoiGraph graph)
        {
            foreach (var state in graph.StatesList)
            {
                if (string.IsNullOrEmpty(state.Name) || state.Name.StartsWith("State_"))
                {
                    state.Name = GenerateRandomStateName();
                }
            }
        }

        private void GenerateCultureNames(VoronoiGraph graph)
        {
            foreach (var culture in graph.CulturesList)
            {
                if (string.IsNullOrEmpty(culture.Name) || culture.Name.StartsWith("Culture_"))
                {
                    culture.Name = GenerateRandomCultureName();
                }
            }
        }

        private string GenerateRandomBurgName()
        {
            string[] prefixes = {
                "Port", "New", "Saint", "Fort", "Old", "Upper", "Lower",
                "King", "Queen", "Prince", "Royal", "Green", "White"
            };

            string[] roots = {
                "ford", "wich", "ton", "burg", "port", "ham", "ley",
                "field", "grove", "wood", "bridge", "haven", "cliff"
            };

            string[] suffixes = {
                "ville", "burgh", "town", "port", "chester", "caster", "bury"
            };

            string name;

            switch (_random.NextInt(4))
            {
                case 0:
                    name = $"{_random.NextItem(prefixes)}{_random.NextItem(roots)}";
                    break;
                case 1:
                    name = $"{_random.NextItem(prefixes)}{_random.NextItem(suffixes)}";
                    break;
                case 2:
                    name = $"{_random.NextItem(roots)}{_random.NextItem(suffixes)}";
                    break;
                default:
                    name = $"{_random.NextItem(prefixes)}{_random.NextItem(roots)}{_random.NextItem(suffixes)}";
                    break;
            }

            return char.ToUpper(name[0]) + name[1..];
        }

        private string GenerateRandomStateName()
        {
            string[] prefixes = {
                "Kingdom of", "Duchy of", "Principality of", "Empire of",
                "Republic of", "Sultanate of", "Kingdom", "Duchy", "Principality"
            };

            string[] roots = {
                "Valley", "Highlands", "Plains", "Mountains", "Coast",
                "Rivers", "Lakes", "Forests", "Islands", "Frontier"
            };

            string root = _random.NextItem(roots);

            if (_random.NextBool(0.5f))
            {
                return $"{_random.NextItem(prefixes)} {root}";
            }

            return root;
        }

        private string GenerateRandomCultureName()
        {
            string[] prefixes = {
                "Northern", "Southern", "Eastern", "Western",
                "Highland", "Lowland", "Coastal", "River"
            };

            string[] roots = {
                " peoples", " tribes", " nations", " kin", " folk", " clans"
            };

            string prefix = _random.NextItem(prefixes);
            string root = _random.NextItem(roots);

            return $"{prefix}{root}";
        }
    }
}
