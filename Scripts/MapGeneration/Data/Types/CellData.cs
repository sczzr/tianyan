using System.Collections.Generic;
using TianYanShop.MapGeneration.Data.Types;

namespace TianYanShop.MapGeneration.Data
{
    /// <summary>
    /// 单元格数据类型
    /// </summary>
    public class CellData
    {
        public int Index { get; set; }
        public Vector2i GridPosition { get; set; }
        public Godot.Vector2 Position { get; set; }
        public List<int> Neighbors { get; } = new List<int>();
        public List<int> VertexNeighbors { get; } = new List<int>();
        public List<Godot.Vector2> PolygonPoints { get; } = new List<Godot.Vector2>();

        public byte Height { get; set; }
        public byte Terrain { get; set; }
        public ushort RiverId { get; set; }
        public ushort FeatureId { get; set; }
        public ushort Biome { get; set; }
        public ushort Culture { get; set; }
        public ushort State { get; set; }
        public ushort Burg { get; set; }
        public ushort Religion { get; set; }
        public ushort Province { get; set; }

        public int Population { get; set; }
        public int Suitability { get; set; }
        public int Flux { get; set; }
        public int Confluence { get; set; }
        public int Harbor { get; set; }
        public int Haven { get; set; }
        public int Ground { get; set; }
        public int Area { get; set; }

        public bool IsBorder { get; set; }

        public CellData() { }

        public CellData(int index, float x, float y)
        {
            Index = index;
            Position = new Godot.Vector2(x, y);
        }

        public void CopyFrom(CellData other)
        {
            Height = other.Height;
            Terrain = other.Terrain;
            RiverId = other.RiverId;
            FeatureId = other.FeatureId;
            Biome = other.Biome;
            Culture = other.Culture;
            State = other.State;
            Burg = other.Burg;
            Religion = other.Religion;
            Province = other.Province;
            Population = other.Population;
            Suitability = other.Suitability;
            Flux = other.Flux;
            Confluence = other.Confluence;
            Harbor = other.Harbor;
            Haven = other.Haven;
            Ground = other.Ground;
            Area = other.Area;
        }
    }

    /// <summary>
    /// 顶点数据类型
    /// </summary>
    public class VertexData
    {
        public int Index { get; set; }
        public Godot.Vector2 Position { get; set; }
        public List<int> CellNeighbors { get; } = new List<int>(3);
        public List<int> VertexNeighbors { get; } = new List<int>();

        public VertexData() { }

        public VertexData(int index, float x, float y)
        {
            Index = index;
            Position = new Godot.Vector2(x, y);
        }
    }

    /// <summary>
    /// 多边形数据
    /// </summary>
    public class PolygonData
    {
        public int CellIndex { get; set; }
        public List<Godot.Vector2> Points { get; } = new List<Godot.Vector2>();

        public PolygonData() { }

        public PolygonData(int cellIndex)
        {
            CellIndex = cellIndex;
        }
    }
}
