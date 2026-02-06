using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class StateGenerator
    {
        private RandomManager _random;

        public StateGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            int numStates = settings.StatesNumber;

            GenerateStates(graph, numStates);
            ExpandStates(graph);
            AssignCapitals(graph);
        }

        private void GenerateStates(VoronoiGraph graph, int numStates)
        {
            var stateCenters = new HashSet<int>();

            for (int i = 0; i < numStates; i++)
            {
                int center = FindValidStateCenter(graph, stateCenters);
                if (center >= 0)
                {
                    stateCenters.Add(center);

                    var state = new StateData
                    {
                        Id = graph.StatesList.Count,
                        Name = $"State_{i}",
                        Color = $"#{_random.NextInt(0xFFFFFF):X6}",
                        Center = center,
                        Capital = center,
                        Expansionism = _random.NextRange(0.1f, 0.5f),
                        Type = "Generic",
                        Culture = graph.Cultures[center],
                        Cells = 1,
                        Area = 1,
                        Burgs = 0
                    };

                    graph.States[center] = (ushort)state.Id;
                    graph.StatesList.Add(state);
                }
            }
        }

        private int FindValidStateCenter(VoronoiGraph graph, HashSet<int> existingCenters)
        {
            int maxAttempts = 100;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int cell = _random.NextInt(graph.CellsCount);
                if (!existingCenters.Contains(cell) &&
                    graph.Heights[cell] >= 32 &&
                    graph.Biomes[cell] != (ushort)BiomeType.Ocean)
                {
                    return cell;
                }
            }
            return _random.NextInt(graph.CellsCount);
        }

        private void ExpandStates(VoronoiGraph graph)
        {
            int expansionRounds = global::System.Math.Max(5, graph.StatesList.Count / 2);

            for (int round = 0; round < expansionRounds; round++)
            {
                var newStateMap = new ushort[graph.CellsCount];
                Array.Copy(graph.States, newStateMap, graph.CellsCount);

                foreach (var state in graph.StatesList)
                {
                    ExpandState(graph, state, newStateMap);
                }

                Array.Copy(newStateMap, graph.States, graph.CellsCount);
            }
        }

        private void ExpandState(VoronoiGraph graph, StateData state, ushort[] newStateMap)
        {
            var expansionCandidates = new List<int>();

            for (int i = 0; i < graph.CellsCount; i++)
            {
                if (graph.States[i] == state.Id)
                {
                    if (graph.Neighbors[i] != null)
                    {
                        foreach (int neighbor in graph.Neighbors[i])
                        {
                            if (graph.States[neighbor] != state.Id &&
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
                float expansionChance = 0.4f;
                if (graph.Cultures[candidate] == state.Culture)
                    expansionChance += 0.2f;

                if (_random.NextFloat() < expansionChance)
                {
                    newStateMap[candidate] = (ushort)state.Id;
                }
            }
        }

        private List<int> ShuffleAndLimit(List<int> list, RandomManager random)
        {
            var result = new List<int>(list);
            random.Shuffle(result.ToArray());
            if (result.Count > 100)
                result = result.GetRange(0, 100);
            return result;
        }

        private void AssignCapitals(VoronoiGraph graph)
        {
            foreach (var state in graph.StatesList)
            {
                state.Capital = state.Center;
            }
        }
    }
}
