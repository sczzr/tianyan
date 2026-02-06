using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class ReligionGenerator
    {
        private RandomManager _random;

        public ReligionGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            int numReligions = global::System.Math.Max(2, settings.StatesNumber / 3);

            GenerateReligions(graph, numReligions);
            ExpandReligions(graph);
        }

        private void GenerateReligions(VoronoiGraph graph, int numReligions)
        {
            string[] religionNames = {
                "Sun Faith", "Moon Cult", "Old Gods", "New Faith",
                "Nature Worship", "Sky Religion", "Ancestor Cult"
            };

            for (int i = 0; i < numReligions; i++)
            {
                int center = FindValidReligionCenter(graph);
                if (center >= 0)
                {
                    var religion = new ReligionData
                    {
                        Id = graph.ReligionsList.Count,
                        Name = i < religionNames.Length ? religionNames[i] : $"Faith_{i}",
                        Color = $"#{_random.NextInt(0xFFFFFF):X6}",
                        Center = center,
                        Type = "Organized",
                        Cells = 1,
                        Area = 1
                    };

                    graph.Religions[center] = (ushort)religion.Id;
                    graph.ReligionsList.Add(religion);
                }
            }
        }

        private int FindValidReligionCenter(VoronoiGraph graph)
        {
            int maxAttempts = 100;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int cell = _random.NextInt(graph.CellsCount);
                if (graph.Heights[cell] >= 32 &&
                    graph.Biomes[cell] != (ushort)BiomeType.Ocean)
                {
                    return cell;
                }
            }
            return _random.NextInt(graph.CellsCount);
        }

        private void ExpandReligions(VoronoiGraph graph)
        {
            int expansionRounds = 4;

            for (int round = 0; round < expansionRounds; round++)
            {
                var newReligionMap = new ushort[graph.CellsCount];
                Array.Copy(graph.Religions, newReligionMap, graph.CellsCount);

                foreach (var religion in graph.ReligionsList)
                {
                    ExpandReligion(graph, religion, newReligionMap);
                }

                Array.Copy(newReligionMap, graph.Religions, graph.CellsCount);
            }
        }

        private void ExpandReligion(VoronoiGraph graph, ReligionData religion, ushort[] newReligionMap)
        {
            var expansionCandidates = new List<int>();

            for (int i = 0; i < graph.CellsCount; i++)
            {
                if (graph.Religions[i] == religion.Id)
                {
                    if (graph.Neighbors[i] != null)
                    {
                        foreach (int neighbor in graph.Neighbors[i])
                        {
                            if (graph.Religions[neighbor] != religion.Id &&
                                graph.Heights[neighbor] >= 32)
                            {
                                expansionCandidates.Add(neighbor);
                            }
                        }
                    }
                }
            }

            expansionCandidates = ShuffleAndLimit(expansionCandidates, _random);

            foreach (int candidate in expansionCandidates)
            {
                float expansionChance = 0.35f;
                if (_random.NextFloat() < expansionChance)
                {
                    newReligionMap[candidate] = (ushort)religion.Id;
                }
            }
        }

        private List<int> ShuffleAndLimit(List<int> list, RandomManager random)
        {
            var result = new List<int>(list);
            random.Shuffle(result.ToArray());
            if (result.Count > 50)
                result = result.GetRange(0, 50);
            return result;
        }
    }
}
