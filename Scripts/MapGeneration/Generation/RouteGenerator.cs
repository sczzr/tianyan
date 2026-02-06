using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class RouteGenerator
    {
        private RandomManager _random;

        public RouteGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            if (settings.AddRoads)
                GenerateRoads(graph);

            if (settings.AddSeaRoutes)
                GenerateSeaRoutes(graph);
        }

        private void GenerateRoads(VoronoiGraph graph)
        {
            var burgsWithRoad = new HashSet<int>();

            for (int i = 0; i < graph.BurgsList.Count; i++)
            {
                var burg = graph.BurgsList[i];
                if (burgsWithRoad.Contains(burg.Cell))
                    continue;

                var nearbyBurgs = FindNearbyBurgs(graph, burg, 10);

                foreach (var nearbyBurg in nearbyBurgs)
                {
                    if (burgsWithRoad.Contains(nearbyBurg.Cell))
                        continue;

                    if (_random.NextFloat() < 0.4f)
                    {
                        CreateRoad(graph, burg, nearbyBurg);
                        burgsWithRoad.Add(burg.Cell);
                        burgsWithRoad.Add(nearbyBurg.Cell);
                    }
                }
            }
        }

        private List<BurgData> FindNearbyBurgs(VoronoiGraph graph, BurgData burg, int maxDistance)
        {
            var nearby = new List<BurgData>();

            foreach (var otherBurg in graph.BurgsList)
            {
                if (otherBurg.Cell == burg.Cell)
                    continue;

                float dist = graph.Points[burg.Cell].DistanceTo(graph.Points[otherBurg.Cell]);
                if (dist <= maxDistance * 10)
                {
                    nearby.Add(otherBurg);
                }
            }

            nearby.Sort((a, b) => graph.Points[burg.Cell].DistanceTo(graph.Points[a.Cell])
                .CompareTo(graph.Points[burg.Cell].DistanceTo(graph.Points[b.Cell])));

            return nearby.GetRange(0, global::System.Math.Min(3, nearby.Count));
        }

        private void CreateRoad(VoronoiGraph graph, BurgData start, BurgData end)
        {
            var path = FindPath(graph, start.Cell, end.Cell);

            if (path.Count >= 2)
            {
                var road = new RouteData
                {
                    Id = graph.RoutesList.Count,
                    Type = RouteType.Road,
                    StartBurg = start.Id,
                    EndBurg = end.Id,
                    Path = path,
                    Length = path.Count,
                    Width = 1f,
                    IsSeaRoute = false,
                    State = start.State
                };

                graph.RoutesList.Add(road);
            }
        }

        private List<int> FindPath(VoronoiGraph graph, int startCell, int endCell)
        {
            var path = new List<int>();
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            var cameFrom = new Dictionary<int, int>();

            queue.Enqueue(startCell);
            visited.Add(startCell);
            cameFrom[startCell] = -1;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                if (current == endCell)
                    break;

                if (graph.Neighbors[current] != null)
                {
                    foreach (int neighbor in graph.Neighbors[current])
                    {
                        if (!visited.Contains(neighbor) && CanTravelThrough(graph, neighbor))
                        {
                            visited.Add(neighbor);
                            cameFrom[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            if (!cameFrom.ContainsKey(endCell))
                return path;

            int cell = endCell;
            while (cell != -1)
            {
                path.Add(cell);
                cell = cameFrom[cell];
            }

            path.Reverse();
            return path;
        }

        private bool CanTravelThrough(VoronoiGraph graph, int cell)
        {
            return graph.Heights[cell] >= 32;
        }

        private void GenerateSeaRoutes(VoronoiGraph graph)
        {
            var coastalBurgs = new List<BurgData>();

            foreach (var burg in graph.BurgsList)
            {
                if (burg.Seaport)
                    coastalBurgs.Add(burg);
            }

            for (int i = 0; i < coastalBurgs.Count; i++)
            {
                for (int j = i + 1; j < coastalBurgs.Count; j++)
                {
                    if (_random.NextFloat() < 0.2f)
                    {
                        CreateSeaRoute(graph, coastalBurgs[i], coastalBurgs[j]);
                    }
                }
            }
        }

        private void CreateSeaRoute(VoronoiGraph graph, BurgData start, BurgData end)
        {
            var path = FindSeaPath(graph, start.Cell, end.Cell);

            if (path.Count >= 2)
            {
                var route = new RouteData
                {
                    Id = graph.RoutesList.Count,
                    Type = RouteType.Sea,
                    StartBurg = start.Id,
                    EndBurg = end.Id,
                    Path = path,
                    Length = path.Count,
                    Width = 2f,
                    IsSeaRoute = true,
                    State = start.State
                };

                graph.RoutesList.Add(route);
            }
        }

        private List<int> FindSeaPath(VoronoiGraph graph, int startCell, int endCell)
        {
            var path = new List<int>();
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            var cameFrom = new Dictionary<int, int>();

            queue.Enqueue(startCell);
            visited.Add(startCell);
            cameFrom[startCell] = -1;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                if (current == endCell)
                    break;

                if (graph.Neighbors[current] != null)
                {
                    foreach (int neighbor in graph.Neighbors[current])
                    {
                        if (!visited.Contains(neighbor) && CanSailThrough(graph, neighbor))
                        {
                            visited.Add(neighbor);
                            cameFrom[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            if (!cameFrom.ContainsKey(endCell))
                return path;

            int cell = endCell;
            while (cell != -1)
            {
                path.Add(cell);
                cell = cameFrom[cell];
            }

            path.Reverse();
            return path;
        }

        private bool CanSailThrough(VoronoiGraph graph, int cell)
        {
            return graph.Heights[cell] < 35;
        }
    }
}
