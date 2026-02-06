using System;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Math;

namespace TianYanShop.MapGeneration.Generation
{
    public class MapGenerator
    {
        public RandomManager Random { get; private set; }
        public VoronoiGraph Graph { get; private set; }

        private HeightmapGenerator _heightmapGenerator;
        private FeatureGenerator _featureGenerator;
        private BiomeGenerator _biomeGenerator;
        private LakeGenerator _lakeGenerator;
        private RiverGenerator _riverGenerator;
        private CultureGenerator _cultureGenerator;
        private StateGenerator _stateGenerator;
        private BurgGenerator _burgGenerator;
        private ReligionGenerator _religionGenerator;
        private RouteGenerator _routeGenerator;
        private NameGenerator _nameGenerator;

        public MapGenerator()
        {
            Random = new RandomManager();
        }

        public MapGenerator(string seed)
        {
            Random = new RandomManager(seed);
        }

        public VoronoiGraph Generate(MapSettings settings)
        {
            Graph = new VoronoiGraph();

            InitializeGenerators();

            GenerateVoronoiDiagram(settings);
            GenerateHeightmap(settings);
            GenerateFeatures(settings);
            GenerateBiomes(settings);
            GenerateLakes(settings);
            GenerateRivers(settings);
            GenerateCultures(settings);
            GenerateStates(settings);
            GenerateBurgs(settings);
            GenerateReligions(settings);
            GenerateRoutes(settings);
            GenerateNames(settings);

            return Graph;
        }

        private void InitializeGenerators()
        {
            _heightmapGenerator = new HeightmapGenerator(Random);
            _featureGenerator = new FeatureGenerator(Random);
            _biomeGenerator = new BiomeGenerator(Random);
            _lakeGenerator = new LakeGenerator(Random);
            _riverGenerator = new RiverGenerator(Random);
            _cultureGenerator = new CultureGenerator(Random);
            _stateGenerator = new StateGenerator(Random);
            _burgGenerator = new BurgGenerator(Random);
            _religionGenerator = new ReligionGenerator(Random);
            _routeGenerator = new RouteGenerator(Random);
            _nameGenerator = new NameGenerator(Random);
        }

        private void GenerateVoronoiDiagram(MapSettings settings)
        {
            GD.Print("GenerateVoronoiDiagram() started");
            int width = settings.Width;
            int height = settings.Height;
            int cellsCount = settings.CellsCount;

            Graph.Width = width;
            Graph.Height = height;
            Graph.CellsCount = cellsCount;
            GD.Print($"Graph initialized: {width}x{height}, {cellsCount} cells");
            
            Graph.InitializeArrays();
            GD.Print("Arrays initialized");

            for (int i = 0; i < cellsCount; i++)
            {
                Graph.Points[i] = new Vector2(
                    Random.NextFloat() * width,
                    Random.NextFloat() * height
                );
                Graph.Heights[i] = (byte)(Random.NextFloat() * 100);
                Graph.Terrains[i] = (byte)TerrainType.Land;
                Graph.Biomes[i] = (ushort)BiomeType.TemperateSeasonalForest;
            }
            GD.Print($"Points generated: {cellsCount}");
            
            // 简化：跳过 Voronoi 构建，直接设置基本属性
            for (int i = 0; i < cellsCount; i++)
            {
                Graph.Neighbors[i] = new int[0];
                Graph.IsBorder[i] = false;
            }
            
            GD.Print("Voronoi diagram simplified");
        }

        private void GenerateHeightmap(MapSettings settings)
        {
            _heightmapGenerator.Generate(Graph, settings);
        }

        private void GenerateFeatures(MapSettings settings)
        {
            _featureGenerator.Generate(Graph, settings);
        }

        private void GenerateBiomes(MapSettings settings)
        {
            _biomeGenerator.Generate(Graph, settings);
        }

        private void GenerateLakes(MapSettings settings)
        {
            _lakeGenerator.Generate(Graph, settings);
        }

        private void GenerateRivers(MapSettings settings)
        {
            _riverGenerator.Generate(Graph, settings);
        }

        private void GenerateCultures(MapSettings settings)
        {
            _cultureGenerator.Generate(Graph, settings);
        }

        private void GenerateStates(MapSettings settings)
        {
            _stateGenerator.Generate(Graph, settings);
        }

        private void GenerateBurgs(MapSettings settings)
        {
            _burgGenerator.Generate(Graph, settings);
        }

        private void GenerateReligions(MapSettings settings)
        {
            _religionGenerator.Generate(Graph, settings);
        }

        private void GenerateRoutes(MapSettings settings)
        {
            _routeGenerator.Generate(Graph, settings);
        }

        private void GenerateNames(MapSettings settings)
        {
            _nameGenerator.Generate(Graph, settings);
        }
    }

    public class MapSettings
    {
        public int Width { get; set; } = 1024;
        public int Height { get; set; } = 1024;
        public int CellsCount { get; set; } = 8000;
        public string Seed { get; set; } = "";
        public int StatesNumber { get; set; } = 10;
        public int BurgsNumber { get; set; } = 50;
        public float LandProbability { get; set; } = 0.5f;
        public bool AddRivers { get; set; } = true;
        public bool AddLakes { get; set; } = true;
        public bool AddRoads { get; set; } = true;
        public bool AddSeaRoutes { get; set; } = true;
    }
}
