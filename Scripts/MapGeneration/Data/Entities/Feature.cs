using System;
using System.Collections.Generic;

namespace TianYanShop.MapGeneration.Data.Entities
{
    /// <summary>
    /// 地理特征（海洋、湖泊、岛屿等）
    /// </summary>
    public class Feature
    {
        public int Id { get; set; }
        public FeatureType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CellCount { get; set; }
        public int FirstCell { get; set; }
        public List<int> Cells { get; } = new List<int>();
        public List<int> Shoreline { get; } = new List<int>();
        public float Height { get; set; }
        public float Temp { get; set; }
        public float Flux { get; set; }
        public float Evaporation { get; set; }
        public int? Outlet { get; set; }
        public int? OutletCell { get; set; }
        public bool Closed { get; set; }
        public int? River { get; set; }
        public int? EnteringFlux { get; set; }
        public List<int> Inlets { get; } = new List<int>();
        public string Group { get; set; } = string.Empty;
        public int Population { get; set; }
    }

    /// <summary>
    /// 河流数据
    /// </summary>
    public class RiverData
    {
        public int Id { get; set; }
        public int Source { get; set; }
        public int Mouth { get; set; }
        public int Parent { get; set; }
        public int Basin { get; set; }
        public float Length { get; set; }
        public float Discharge { get; set; }
        public float Width { get; set; }
        public float WidthFactor { get; set; }
        public float SourceWidth { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<int> Cells { get; } = new List<int>();
    }

    /// <summary>
    /// 湖泊数据
    /// </summary>
    public class LakeData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CellCount { get; set; }
        public List<int> Cells { get; } = new List<int>();
        public List<int> Shoreline { get; } = new List<int>();
        public float Height { get; set; }
        public float Temp { get; set; }
        public int? River { get; set; }
        public int? Outlet { get; set; }
    }

    /// <summary>
    /// 城市数据
    /// </summary>
    public class BurgData
    {
        public int Id { get; set; }
        public int Cell { get; set; }
        public Godot.Vector2 Position { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Population { get; set; }
        public int State { get; set; }
        public int Culture { get; set; }
        public bool Capital { get; set; }
        public bool Seaport { get; set; }
        public bool Walled { get; set; }
        public int Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool Removed { get; set; }
        public int? Religion { get; set; }
    }

    /// <summary>
    /// 国家数据
    /// </summary>
    public class StateData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Expansionism { get; set; }
        public int Capital { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Center { get; set; }
        public int Culture { get; set; }
        public string Color { get; set; } = string.Empty;
        public bool Lock { get; set; }
        public bool Removed { get; set; }
        public float[] Pole { get; set; } = new float[2];
        public List<int> Neighbors { get; } = new List<int>();
        public int Cells { get; set; }
        public int Area { get; set; }
        public int Burgs { get; set; }
        public int Rural { get; set; }
        public int Urban { get; set; }
        public List<Campaign> Campaigns { get; } = new List<Campaign>();
        public List<string> Diplomacy { get; } = new List<string>();
        public string FormName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public List<int> Provinces { get; } = new List<int>();
    }

    /// <summary>
    /// 战役数据
    /// </summary>
    public class Campaign
    {
        public string Name { get; set; } = string.Empty;
        public int Start { get; set; }
        public int? End { get; set; }
    }

    /// <summary>
    /// 文化数据
    /// </summary>
    public class CultureData
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public int Base { get; set; }
        public string Shield { get; set; } = string.Empty;
        public bool Lock { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Center { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float Expansionism { get; set; }
        public List<int?> Origins { get; } = new List<int?>();
        public bool Removed { get; set; }
        public int Cells { get; set; }
        public int Area { get; set; }
        public int Rural { get; set; }
        public int Urban { get; set; }
    }

    /// <summary>
    /// 宗教数据
    /// </summary>
    public class ReligionData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Center { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Expansion { get; set; } = string.Empty;
        public int Cells { get; set; }
        public int Area { get; set; }
    }

    /// <summary>
    /// 道路数据
    /// </summary>
    public class RouteData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public RouteType Type { get; set; }
        public List<int> Cells { get; } = new List<int>();
        public List<int> Path { get; set; } = new List<int>();
        public int Length { get; set; }
        public int StartBurg { get; set; }
        public int EndBurg { get; set; }
        public float Width { get; set; }
        public bool IsSeaRoute { get; set; }
        public int State { get; set; }
    }

    public enum RouteType
    {
        Road,
        Sea,
        River
    }
}
