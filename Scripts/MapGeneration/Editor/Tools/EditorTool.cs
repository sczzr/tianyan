using System;
using Godot;
using TianYanShop.MapGeneration.Data;

namespace TianYanShop.MapGeneration.Editor.Tools
{
    /// <summary>
    /// 编辑器工具基类
    /// </summary>
    public abstract class EditorTool
    {
        public virtual string Name => "Tool";
        public virtual string Icon => "";
        public virtual float DefaultRadius => 10f;
        public virtual float DefaultStrength => 0.5f;

        public virtual void Initialize() { }
        public virtual void Shutdown() { }

        public abstract void Apply(VoronoiGraph graph, int cell, float radius, float strength);

        protected float GetFalloff(float distance, float radius)
        {
            if (distance >= radius) return 0f;
            float t = distance / radius;
            return (float)global::System.Math.Pow(1 - t, 2);
        }

        protected int[] GetCellsInRadius(VoronoiGraph graph, int center, float radius)
        {
            var cells = new System.Collections.Generic.List<int>();
            float radiusSq = radius * radius;

            for (int i = 0; i < graph.CellsCount; i++)
            {
                float distSq = graph.Points[i].DistanceSquaredTo(graph.Points[center]);
                if (distSq <= radiusSq)
                {
                    cells.Add(i);
                }
            }

            return cells.ToArray();
        }
    }

    /// <summary>
    /// 山丘笔刷
    /// </summary>
    public class HillBrush : EditorTool
    {
        public override string Name => "Hill Brush";
        public override float DefaultRadius => 15f;
        public override float DefaultStrength => 0.3f;

        public override void Apply(VoronoiGraph graph, int cell, float radius, float strength)
        {
            var cells = GetCellsInRadius(graph, cell, radius);

            foreach (int c in cells)
            {
                if (!graph.IsLand(c)) continue;

                float falloff = GetFalloff(graph.Points[c].DistanceTo(graph.Points[cell]), radius);
                float raise = strength * 20f * falloff;

                graph.Heights[c] = (byte)global::System.Math.Clamp(graph.Heights[c] + raise, 0, 100);
            }
        }
    }

    /// <summary>
    /// 坑洼笔刷
    /// </summary>
    public class PitBrush : EditorTool
    {
        public override string Name => "Pit Brush";
        public override float DefaultRadius => 10f;
        public override float DefaultStrength => 0.5f;

        public override void Apply(VoronoiGraph graph, int cell, float radius, float strength)
        {
            var cells = GetCellsInRadius(graph, cell, radius);

            foreach (int c in cells)
            {
                float falloff = GetFalloff(graph.Points[c].DistanceTo(graph.Points[cell]), radius);
                float lower = strength * 30f * falloff;

                graph.Heights[c] = (byte)global::System.Math.Clamp(graph.Heights[c] - lower, 0, 100);
            }
        }
    }

    /// <summary>
    /// 山脉笔刷
    /// </summary>
    public class RangeBrush : EditorTool
    {
        public override string Name => "Mountain Brush";
        public override float DefaultRadius => 8f;
        public override float DefaultStrength => 0.7f;

        public override void Apply(VoronoiGraph graph, int cell, float radius, float strength)
        {
            var cells = GetCellsInRadius(graph, cell, radius);

            foreach (int c in cells)
            {
                if (!graph.IsLand(c)) continue;
                if (graph.Heights[c] < MapGeneration.Core.MapConstants.HEIGHT_LAND) continue;

                float falloff = GetFalloff(graph.Points[c].DistanceTo(graph.Points[cell]), radius);
                float raise = strength * 40f * falloff;

                graph.Heights[c] = (byte)global::System.Math.Clamp(graph.Heights[c] + raise, MapGeneration.Core.MapConstants.HEIGHT_LAND, 100);
            }
        }
    }

    /// <summary>
    /// 平滑笔刷
    /// </summary>
    public class SmoothBrush : EditorTool
    {
        public override string Name => "Smooth Brush";
        public override float DefaultRadius => 20f;
        public override float DefaultStrength => 0.5f;

        public override void Apply(VoronoiGraph graph, int cell, float radius, float strength)
        {
            var cells = GetCellsInRadius(graph, cell, radius);

            if (cells.Length < 2) return;

            float sum = 0;
            foreach (int c in cells)
            {
                sum += graph.Heights[c];
            }
            float average = sum / cells.Length;

            foreach (int c in cells)
            {
                float falloff = GetFalloff(graph.Points[c].DistanceTo(graph.Points[cell]), radius);
                float diff = average - graph.Heights[c];
                graph.Heights[c] = (byte)global::System.Math.Clamp(graph.Heights[c] + diff * strength * falloff, 0, 100);
            }
        }
    }

    /// <summary>
    /// 河谷笔刷
    /// </summary>
    public class TroughBrush : EditorTool
    {
        public override string Name => "Valley Brush";
        public override float DefaultRadius => 12f;
        public override float DefaultStrength => 0.6f;

        public override void Apply(VoronoiGraph graph, int cell, float radius, float strength)
        {
            var cells = GetCellsInRadius(graph, cell, radius);

            foreach (int c in cells)
            {
                float falloff = GetFalloff(graph.Points[c].DistanceTo(graph.Points[cell]), radius);
                float lower = strength * 25f * falloff;

                if (graph.IsLand(c))
                {
                    graph.Heights[c] = (byte)global::System.Math.Clamp(graph.Heights[c] - lower, MapGeneration.Core.MapConstants.HEIGHT_OCEAN, 100);
                }
            }
        }
    }

    /// <summary>
    /// 海峡笔刷
    /// </summary>
    public class StraitBrush : EditorTool
    {
        public override string Name => "Strait Brush";
        public override float DefaultRadius => 15f;
        public override float DefaultStrength => 0.8f;

        public override void Apply(VoronoiGraph graph, int cell, float radius, float strength)
        {
            var cells = GetCellsInRadius(graph, cell, radius);

            foreach (int c in cells)
            {
                float falloff = GetFalloff(graph.Points[c].DistanceTo(graph.Points[cell]), radius);
                float lower = strength * 40f * falloff;

                graph.Heights[c] = (byte)global::System.Math.Clamp(graph.Heights[c] - lower, 0, MapGeneration.Core.MapConstants.HEIGHT_OCEAN);
            }
        }
    }

    /// <summary>
    /// 掩码笔刷
    /// </summary>
    public class MaskBrush : EditorTool
    {
        public override string Name => "Mask Brush";
        public override float DefaultRadius => 50f;
        public override float DefaultStrength => 0.5f;

        public override void Apply(VoronoiGraph graph, int cell, float radius, float strength)
        {
        }
    }
}
