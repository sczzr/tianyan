using System;
using System.Collections.Generic;
using Godot;
using TianYanShop.MapGeneration.Data.Types;

namespace TianYanShop.MapGeneration.Core
{
    /// <summary>
    /// 地图生成常量定义
    /// </summary>
    public static class MapConstants
    {
        public const int DEFAULT_WIDTH = 1024;
        public const int DEFAULT_HEIGHT = 1024;
        public const int DEFAULT_CELLS = 8000;

        public const byte HEIGHT_OCEAN = 20;
        public const byte HEIGHT_LAND = 21;
        public const byte HEIGHT_LAKE = 15;

        public const int NEIGHBOR_MAX = 7;
        public const int VERTEX_NEIGHBOR_MAX = 3;

        public static readonly Vector2i[] Directions = new Vector2i[]
        {
            new Vector2i(1, 0),
            new Vector2i(1, 1),
            new Vector2i(0, 1),
            new Vector2i(-1, 1),
            new Vector2i(-1, 0),
            new Vector2i(-1, -1),
            new Vector2i(0, -1),
            new Vector2i(1, -1)
        };

        public static readonly float[] CosineDirections = new float[]
        {
            1.0f, 0.7071f, 0.0f, -0.7071f, -1.0f, -0.7071f, 0.0f, 0.7071f
        };

        public static readonly float[] SineDirections = new float[]
        {
            0.0f, 0.7071f, 1.0f, 0.7071f, 0.0f, -0.7071f, -1.0f, -0.7071f
        };

        public const float FLUX_FACTOR = 500f;
        public const float MAX_FLUX_WIDTH = 1f;
        public const float LENGTH_FACTOR = 200f;
        public const float LENGTH_STEP_WIDTH = 1f / LENGTH_FACTOR;

        public static readonly float[] LengthProgression = new float[]
        {
            1f / LENGTH_FACTOR,
            2f / LENGTH_FACTOR,
            3f / LENGTH_FACTOR,
            5f / LENGTH_FACTOR,
            8f / LENGTH_FACTOR,
            13f / LENGTH_FACTOR,
            21f / LENGTH_FACTOR,
            34f / LENGTH_FACTOR
        };

        public const int MIN_FLUX_TO_FORM_RIVER = 30;

        public const int RIVER_POINT_STEP = 10;

        public const float DEFAULT_BRUSH_RADIUS = 10f;
        public const float DEFAULT_BRUSH_STRENGTH = 0.5f;

        public const int MAX_UNDO_STACK = 50;

        public const float RIVER_MEANDERING = 0.5f;

        public const int RIVER_MIN_CELLS = 3;

        public static readonly Dictionary<string, int> RiverTypeMain = new Dictionary<string, int>
        {
            { "River", 1 },
            { "Creek", 9 },
            { "Brook", 3 },
            { "Stream", 1 }
        };

        public static readonly Dictionary<string, int> RiverTypeFork = new Dictionary<string, int>
        {
            { "Fork", 1 },
            { "Branch", 1 }
        };

        public const string BIOME_OCEAN = "Ocean";
        public const string BIOME_LAKE = "Lake";
        public const string BIOME_LAND = "Land";

        public static readonly string[] CultureTypes = new string[]
        {
            "Generic", "Naval", "River", "Nomadic", "Highland", "Hunting", "Lake"
        };

        public static readonly string[] StateFormsMonarchy = new string[]
        {
            "Duchy", "Grand Duchy", "Principality", "Kingdom", "Empire"
        };

        public static readonly string[] StateFormsRepublic = new string[]
        {
            "Republic", "Federation", "Trade Company", "Most Serene Republic",
            "Oligarchy", "Tetrarchy", "Triumvirate", "Diarchy", "Junta"
        };
    }
}
