using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class BurgGenerator
    {
        private RandomManager _random;

        public BurgGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            int numBurgs = settings.BurgsNumber;

            GenerateBurgs(graph, numBurgs);
        }

        private void GenerateBurgs(VoronoiGraph graph, int numBurgs)
        {
            int burgsCreated = 0;
            int maxAttempts = numBurgs * 10;
            int attempts = 0;

            while (burgsCreated < numBurgs && attempts < maxAttempts)
            {
                attempts++;

                int cell = _random.NextInt(graph.CellsCount);

                if (CanPlaceBurg(graph, cell))
                {
                    CreateBurg(graph, cell);
                    burgsCreated++;
                }
            }
        }

        private bool CanPlaceBurg(VoronoiGraph graph, int cell)
        {
            if (graph.Heights[cell] < 32)
                return false;

            if (graph.Biomes[cell] == (ushort)BiomeType.Ocean)
                return false;

            if (graph.Burgs[cell] > 0)
                return false;

            if (graph.Rivers[cell] > 0 && graph.Heights[cell] < 35)
                return false;

            if (HasNearbyBurg(graph, cell))
                return false;

            return true;
        }

        private bool HasNearbyBurg(VoronoiGraph graph, int cell)
        {
            float minDistance = 10f;

            if (graph.Neighbors[cell] != null)
            {
                foreach (int neighbor in graph.Neighbors[cell])
                {
                    if (graph.Burgs[neighbor] > 0)
                        return true;
                }
            }

            for (int i = 0; i < graph.BurgsList.Count; i++)
            {
                float dist = graph.Points[cell].DistanceTo(graph.Points[graph.BurgsList[i].Cell]);
                if (dist < minDistance)
                    return true;
            }

            return false;
        }

        private void CreateBurg(VoronoiGraph graph, int cell)
        {
            int stateId = graph.States[cell];
            int cultureId = graph.Cultures[cell];

            var burg = new BurgData
            {
                Id = graph.BurgsList.Count,
                Cell = cell,
                State = stateId,
                Name = GenerateBurgName(),
                Population = _random.NextInt(100, 5000),
                Capital = (graph.StatesList.Count > stateId && graph.StatesList[stateId].Center == cell),
                Culture = cultureId,
                Position = new Godot.Vector2(graph.Points[cell].X, graph.Points[cell].Y),
                X = graph.Points[cell].X,
                Y = graph.Points[cell].Y,
                Seaport = graph.Heights[cell] >= 32 && IsNearCoast(graph, cell),
                Walled = _random.NextBool(0.3f),
                Type = (int)BurgType.Town
            };

            graph.Burgs[cell] = (ushort)(burg.Id + 1);
            graph.BurgsList.Add(burg);
        }

        private string GenerateBurgName()
        {
            string[] prefixes = { "Port", "New", "Saint", "Fort", "Old", "Upper", "Lower" };
            string[] suffixes = { "ville", "burg", "ton", "port", "field", "ford", "wick" };

            string prefix = _random.NextItem(prefixes);
            string suffix = _random.NextItem(suffixes);

            if (_random.NextBool(0.3f))
                return $"{prefix}{suffix}";

            return $"{char.ToUpper(prefix[0])}{prefix[1..]}{suffix}";
        }

        private bool IsNearCoast(VoronoiGraph graph, int cell)
        {
            if (graph.Neighbors[cell] == null)
                return false;

            foreach (int neighbor in graph.Neighbors[cell])
            {
                if (graph.Heights[neighbor] < 32)
                    return true;
            }
            return false;
        }
    }
}
