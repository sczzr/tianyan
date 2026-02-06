using System;
using System.IO;
using Godot;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Entities;
using TianYanShop.MapGeneration.Math;
using TianYanShop.MapGeneration.Rendering;

namespace TianYanShop.MapGeneration.Export
{
    /// <summary>
    /// 地图导出基类
    /// </summary>
    public abstract class MapExporter
    {
        protected VoronoiGraph Graph { get; set; }

        public abstract void Export(string path);
        public abstract string Extension { get; }

        protected void SetGraph(VoronoiGraph graph)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }
    }

    /// <summary>
    /// Godot Scene 导出器
    /// </summary>
    public class GodotSceneExporter : MapExporter
    {
        public override string Extension => ".tscn";

        public override void Export(string path)
        {
            if (Graph == null) throw new InvalidOperationException("Graph not set");

            var scene = CreateScene();
            var packedScene = new PackedScene();
            packedScene.Pack(scene);

            Error err = ResourceSaver.Save(packedScene, path);
            if (err != Error.Ok)
            {
                GD.PrintErr($"Failed to save scene: {err}");
            }
        }

        private Node2D CreateScene()
        {
            var root = new Node2D();
            root.Name = "WorldMap";

            var renderer = new MapRenderer();
            renderer.Name = "MapRenderer";
            renderer.Render(Graph);
            root.AddChild(renderer);

            return root;
        }
    }

    /// <summary>
    /// 二进制数据导出器
    /// </summary>
    public class BinaryExporter : MapExporter
    {
        public override string Extension => ".bin";

        public override void Export(string path)
        {
            if (Graph == null) throw new InvalidOperationException("Graph not set");

            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            WriteHeader(writer);
            WriteGraphData(writer);
            WriteEntities(writer);

            GD.Print($"Map exported to {path}");
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write(Graph.Width);
            writer.Write(Graph.Height);
            writer.Write(Graph.CellsCount);
            writer.Write(Graph.VerticesCount);
        }

        private void WriteGraphData(BinaryWriter writer)
        {
            WriteIntArray(writer, Graph.Indices);
            WriteByteArray(writer, Graph.Heights);
            WriteByteArray(writer, Graph.Terrains);

            WriteUShortArray(writer, Graph.Rivers);
            WriteUShortArray(writer, Graph.Features);
            WriteUShortArray(writer, Graph.Biomes);
            WriteUShortArray(writer, Graph.Cultures);
            WriteUShortArray(writer, Graph.States);
            WriteUShortArray(writer, Graph.Burgs);
            WriteUShortArray(writer, Graph.Religions);

            writer.Write(Graph.Points.Length);
            foreach (var p in Graph.Points)
            {
                writer.Write(p.X);
                writer.Write(p.Y);
            }

            writer.Write(Graph.IsBorder.Length);
            foreach (var b in Graph.IsBorder)
            {
                writer.Write(b);
            }
        }

        private void WriteIntArray(BinaryWriter writer, int[] array)
        {
            writer.Write(array.Length);
            foreach (var b in array)
            {
                writer.Write(b);
            }
        }

        private void WriteByteArray(BinaryWriter writer, byte[] array)
        {
            writer.Write(array.Length);
            foreach (var b in array)
            {
                writer.Write(b);
            }
        }

        private void WriteUShortArray(BinaryWriter writer, ushort[] array)
        {
            writer.Write(array.Length);
            foreach (var b in array)
            {
                writer.Write(b);
            }
        }

        private void WriteEntities(BinaryWriter writer)
        {
            WriteFeatureList(writer, Graph.FeaturesList);
            WriteRiverList(writer, Graph.RiversList);
            WriteBurgList(writer, Graph.BurgsList);
            WriteStateList(writer, Graph.StatesList);
            WriteCultureList(writer, Graph.CulturesList);
        }

        private void WriteFeatureList(BinaryWriter writer, System.Collections.Generic.List<Feature> features)
        {
            writer.Write(features.Count);
            foreach (var f in features)
            {
                writer.Write(f.Id);
                writer.Write((int)f.Type);
                writer.Write(f.Name ?? "");
                writer.Write(f.CellCount);
            }
        }

        private void WriteRiverList(BinaryWriter writer, System.Collections.Generic.List<RiverData> rivers)
        {
            writer.Write(rivers.Count);
            foreach (var r in rivers)
            {
                writer.Write(r.Id);
                writer.Write(r.Source);
                writer.Write(r.Mouth);
                writer.Write(r.Length);
                writer.Write(r.Width);
            }
        }

        private void WriteBurgList(BinaryWriter writer, System.Collections.Generic.List<BurgData> burgs)
        {
            writer.Write(burgs.Count);
            foreach (var b in burgs)
            {
                writer.Write(b.Id);
                writer.Write(b.Cell);
                writer.Write(b.Name ?? "");
                writer.Write(b.Population);
                writer.Write(b.Type);
            }
        }

        private void WriteStateList(BinaryWriter writer, System.Collections.Generic.List<StateData> states)
        {
            writer.Write(states.Count);
            foreach (var s in states)
            {
                writer.Write(s.Id);
                writer.Write(s.Name ?? "");
                writer.Write(s.Culture);
                writer.Write(s.Capital);
            }
        }

        private void WriteCultureList(BinaryWriter writer, System.Collections.Generic.List<CultureData> cultures)
        {
            writer.Write(cultures.Count);
            foreach (var c in cultures)
            {
                writer.Write(c.Id);
                writer.Write(c.Name ?? "");
                writer.Write(c.Type ?? "");
            }
        }
    }

    /// <summary>
    /// JSON 导出器
    /// </summary>
    public class JsonExporter : MapExporter
    {
        public override string Extension => ".json";

        public override void Export(string path)
        {
            if (Graph == null) throw new InvalidOperationException("Graph not set");

            var json = new Godot.Collections.Dictionary();

            json["width"] = Graph.Width;
            json["height"] = Graph.Height;
            json["cellsCount"] = Graph.CellsCount;

            var heights = new Godot.Collections.Array();
            foreach (var h in Graph.Heights) heights.Add(h);
            json["heights"] = heights;

            var biomes = new Godot.Collections.Array();
            foreach (var b in Graph.Biomes) biomes.Add(b);
            json["biomes"] = biomes;

            var states = new Godot.Collections.Array();
            foreach (var s in Graph.States) states.Add(s);
            json["states"] = states;

            var cultures = new Godot.Collections.Array();
            foreach (var c in Graph.Cultures) cultures.Add(c);
            json["cultures"] = cultures;

            var burgs = new Godot.Collections.Array();
            foreach (var b in Graph.Burgs) burgs.Add(b);
            json["burgs"] = burgs;

            File.WriteAllText(path, Json.Stringify(json));
            GD.Print($"Map exported to {path}");
        }
    }

    /// <summary>
    /// 地图导入器
    /// </summary>
    public class MapImporter
    {
        public static VoronoiGraph ImportBinary(string path)
        {
            if (!File.Exists(path))
            {
                GD.PrintErr($"File not found: {path}");
                return null;
            }

            using var stream = new FileStream(path, FileMode.Open);
            using var reader = new BinaryReader(stream);

            var graph = new VoronoiGraph();

            graph.Width = reader.ReadInt32();
            graph.Height = reader.ReadInt32();
            graph.CellsCount = reader.ReadInt32();
            graph.VerticesCount = reader.ReadInt32();

            graph.InitializeArrays();

            ReadIntArray(reader, graph.Indices);
            ReadByteArray(reader, graph.Heights);
            ReadByteArray(reader, graph.Terrains);
            ReadUShortArray(reader, graph.Rivers);
            ReadUShortArray(reader, graph.Features);
            ReadUShortArray(reader, graph.Biomes);
            ReadUShortArray(reader, graph.Cultures);
            ReadUShortArray(reader, graph.States);
            ReadUShortArray(reader, graph.Burgs);
            ReadUShortArray(reader, graph.Religions);

            for (int i = 0; i < graph.Points.Length; i++)
            {
                graph.Points[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }

            ReadBoolArray(reader, graph.IsBorder);

            return graph;
        }

        public static VoronoiGraph ImportJson(string path)
        {
            if (!File.Exists(path))
            {
                GD.PrintErr($"File not found: {path}");
                return null;
            }

            string content = File.ReadAllText(path);
            var json = new Json();
            var error = json.Parse(content);
            if (error != 0)
            {
                GD.PrintErr($"Failed to parse JSON: {error}");
                return null;
            }
            var jsonDict = json.Data.AsGodotDictionary();

            var graph = new VoronoiGraph();

            graph.Width = (int)jsonDict["width"];
            graph.Height = (int)jsonDict["height"];
            graph.CellsCount = (int)jsonDict["cellsCount"];

            graph.InitializeArrays();

            var heights = jsonDict["heights"].AsGodotArray();
            for (int i = 0; i < graph.CellsCount && i < heights.Count; i++)
            {
                graph.Heights[i] = (byte)(int)heights[i];
            }

            return graph;
        }

        private static void ReadIntArray(BinaryReader reader, int[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt32();
            }
        }

        private static void ReadByteArray(BinaryReader reader, byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadByte();
            }
        }

        private static void ReadUShortArray(BinaryReader reader, ushort[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt16();
            }
        }

        private static void ReadBoolArray(BinaryReader reader, bool[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadBoolean();
            }
        }
    }
}
