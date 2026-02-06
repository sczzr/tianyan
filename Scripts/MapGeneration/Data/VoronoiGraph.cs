using System;
using System.Collections.Generic;
using Godot;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Data
{
    /// <summary>
    /// Voronoi图数据结构
    /// 存储地图的所有单元格、顶点及其属性
    /// </summary>
    [Serializable]
    public class VoronoiGraph
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellsCount { get; set; }
        public int VerticesCount { get; set; }

        // 单元格索引数组
        public int[] Indices { get; set; } = Array.Empty<int>();

        // 单元格邻居 [cellIndex] -> int[]
        public int[][] Neighbors { get; set; } = Array.Empty<int[]>();

        // 顶点邻居 [vertexIndex] -> int[]
        public int[][] VertexNeighbors { get; set; } = Array.Empty<int[]>();

        // 单元格多边形点
        public Vector2[] Points { get; set; } = Array.Empty<Vector2>();

        // 顶点坐标
        public Vector2[] Vertices { get; set; } = Array.Empty<Vector2>();

        // 边界标记
        public bool[] IsBorder { get; set; } = Array.Empty<bool>();

        // 属性数组
        public byte[] Heights { get; set; } = Array.Empty<byte>();
        public byte[] Terrains { get; set; } = Array.Empty<byte>();
        public ushort[] Rivers { get; set; } = Array.Empty<ushort>();
        public ushort[] Features { get; set; } = Array.Empty<ushort>();
        public ushort[] Biomes { get; set; } = Array.Empty<ushort>();
        public ushort[] Cultures { get; set; } = Array.Empty<ushort>();
        public ushort[] States { get; set; } = Array.Empty<ushort>();
        public ushort[] Burgs { get; set; } = Array.Empty<ushort>();
        public ushort[] Religions { get; set; } = Array.Empty<ushort>();
        public ushort[] Provinces { get; set; } = Array.Empty<ushort>();

        // 附加属性数组
        public int[] Population { get; set; } = Array.Empty<int>();
        public int[] Suitability { get; set; } = Array.Empty<int>();
        public int[] Flux { get; set; } = Array.Empty<int>();
        public int[] Confluence { get; set; } = Array.Empty<int>();
        public int[] Harbor { get; set; } = Array.Empty<int>();
        public int[] Haven { get; set; } = Array.Empty<int>();
        public int[] Ground { get; set; } = Array.Empty<int>();
        public int[] Area { get; set; } = Array.Empty<int>();

        // 地理元素列表
        public List<Feature> FeaturesList { get; } = new List<Feature>();
        public List<RiverData> RiversList { get; } = new List<RiverData>();
        public List<LakeData> LakesList { get; } = new List<LakeData>();
        public List<BurgData> BurgsList { get; } = new List<BurgData>();
        public List<StateData> StatesList { get; } = new List<StateData>();
        public List<CultureData> CulturesList { get; } = new List<CultureData>();
        public List<ReligionData> ReligionsList { get; } = new List<ReligionData>();
        public List<RouteData> RoutesList { get; } = new List<RouteData>();

        // 多边形数据
        public List<Vector2[]> CellPolygons { get; } = new List<Vector2[]>();

        public VoronoiGraph() { }

        public VoronoiGraph(int width, int height, int cellsCount)
        {
            Width = width;
            Height = height;
            CellsCount = cellsCount;
            InitializeArrays();
        }

        public void InitializeArrays()
        {
            int size = CellsCount;

            Indices = new int[size];
            Neighbors = new int[size][];
            VertexNeighbors = new int[size][];
            Points = new Vector2[size];
            IsBorder = new bool[size];
            Heights = new byte[size];
            Terrains = new byte[size];
            Rivers = new ushort[size];
            Features = new ushort[size];
            Biomes = new ushort[size];
            Cultures = new ushort[size];
            States = new ushort[size];
            Burgs = new ushort[size];
            Religions = new ushort[size];
            Provinces = new ushort[size];
            Population = new int[size];
            Suitability = new int[size];
            Flux = new int[size];
            Confluence = new int[size];
            Harbor = new int[size];
            Haven = new int[size];
            Ground = new int[size];
            Area = new int[size];
        }

        private void ResizeArray<T>(ref T[] array, int newSize)
        {
            T[] newArray = new T[newSize];
            Array.Copy(array, newArray, global::System.Math.Min(array.Length, newSize));
            array = newArray;
        }

        public int GetCellIndex(float x, float y)
        {
            float minDist = float.MaxValue;
            int nearestIndex = -1;

            for (int i = 0; i < CellsCount; i++)
            {
                float dx = Points[i].X - x;
                float dy = Points[i].Y - y;
                float dist = dx * dx + dy * dy;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        public int GetCellIndex(Vector2 position)
        {
            return GetCellIndex(position.X, position.Y);
        }

        public int GetNearestCell(int cellIndex)
        {
            if (Neighbors[cellIndex] == null || Neighbors[cellIndex].Length == 0)
                return cellIndex;

            int nearest = cellIndex;
            float minHeightDiff = float.MaxValue;
            int targetHeight = Heights[cellIndex];

            foreach (var neighbor in Neighbors[cellIndex])
            {
                float diff = global::System.Math.Abs(Heights[neighbor] - targetHeight);
                if (diff < minHeightDiff)
                {
                    minHeightDiff = diff;
                    nearest = neighbor;
                }
            }

            return nearest;
        }

        public bool IsLand(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < CellsCount && Heights[cellIndex] >= 20;
        }

        public bool IsWater(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < CellsCount && Heights[cellIndex] < 20;
        }

        public bool HasRiver(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < CellsCount && Rivers[cellIndex] > 0;
        }

        public bool HasBurg(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < CellsCount && Burgs[cellIndex] > 0;
        }

        public void CopyTo(VoronoiGraph target)
        {
            target.Width = Width;
            target.Height = Height;
            target.CellsCount = CellsCount;
            target.VerticesCount = VerticesCount;

            target.Indices = (int[])Indices.Clone();
            target.Points = (Vector2[])Points.Clone();
            target.IsBorder = (bool[])IsBorder.Clone();
            target.Heights = (byte[])Heights.Clone();
            target.Terrains = (byte[])Terrains.Clone();
            target.Rivers = (ushort[])Rivers.Clone();
            target.Features = (ushort[])Features.Clone();
            target.Biomes = (ushort[])Biomes.Clone();
            target.Cultures = (ushort[])Cultures.Clone();
            target.States = (ushort[])States.Clone();
            target.Burgs = (ushort[])Burgs.Clone();
            target.Religions = (ushort[])Religions.Clone();
            target.Provinces = (ushort[])Provinces.Clone();

            target.Population = (int[])Population.Clone();
            target.Suitability = (int[])Suitability.Clone();
            target.Flux = (int[])Flux.Clone();
            target.Confluence = (int[])Confluence.Clone();
            target.Harbor = (int[])Harbor.Clone();
            target.Haven = (int[])Haven.Clone();
            target.Ground = (int[])Ground.Clone();
            target.Area = (int[])Area.Clone();

            target.Neighbors = new int[Neighbors.Length][];
            for (int i = 0; i < Neighbors.Length; i++)
            {
                target.Neighbors[i] = (int[])Neighbors[i].Clone();
            }

            target.VertexNeighbors = new int[VertexNeighbors.Length][];
            for (int i = 0; i < VertexNeighbors.Length; i++)
            {
                target.VertexNeighbors[i] = (int[])VertexNeighbors[i].Clone();
            }
        }

        public VoronoiGraph Clone()
        {
            var clone = new VoronoiGraph();
            CopyTo(clone);
            return clone;
        }
    }
}
