using System;
using Godot;
using TianYanShop.MapGeneration.Data;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 高度图编辑器
    /// </summary>
    public class HeightmapEditor
    {
        private VoronoiGraph _graph;
        private UndoRedoSystem _undoRedo;

        public HeightmapEditor(VoronoiGraph graph, UndoRedoSystem undoRedo)
        {
            _graph = graph;
            _undoRedo = undoRedo;
        }

        public void Raise(int cell, float radius, float strength)
        {
            var action = new EditAction
            {
                Type = EditActionType.Heightmap,
                Cells = GetCellsInRadius(cell, radius),
            };

            ApplyHeightChange(action, strength * 20f, landOnly: true);
            _undoRedo.Push(action);
        }

        public void Lower(int cell, float radius, float strength)
        {
            var action = new EditAction
            {
                Type = EditActionType.Heightmap,
                Cells = GetCellsInRadius(cell, radius),
            };

            ApplyHeightChange(action, -strength * 30f, landOnly: false);
            _undoRedo.Push(action);
        }

        public void Smooth(int cell, float radius, float strength)
        {
            var action = new EditAction
            {
                Type = EditActionType.Heightmap,
                Cells = GetCellsInRadius(cell, radius),
            };

            var cells = action.Cells;
            if (cells.Count < 2) return;

            float sum = 0;
            foreach (int c in cells)
            {
                sum += _graph.Heights[c];
            }
            float average = sum / cells.Count;

            action.OriginalValues = new byte[cells.Count];
            action.NewValues = new byte[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                action.OriginalValues[i] = _graph.Heights[cells[i]];
                float diff = average - _graph.Heights[cells[i]];
                float dist = _graph.Points[cells[i]].DistanceTo(_graph.Points[cell]);
                float falloff = 1f - global::System.Math.Min(dist / radius, 1f);
                _graph.Heights[cells[i]] = (byte)global::System.Math.Clamp(
                    _graph.Heights[cells[i]] + diff * strength * falloff, 0, 100);
                action.NewValues[i] = _graph.Heights[cells[i]];
            }

            _undoRedo.Push(action);
        }

        public void SetHeight(int cell, byte height)
        {
            var action = new EditAction
            {
                Type = EditActionType.Heightmap,
                Cells = new System.Collections.Generic.List<int> { cell },
                OriginalValues = new byte[] { _graph.Heights[cell] },
                NewValues = new byte[] { height }
            };

            _graph.Heights[cell] = height;
            _undoRedo.Push(action);
        }

        private System.Collections.Generic.List<int> GetCellsInRadius(int center, float radius)
        {
            var cells = new System.Collections.Generic.List<int>();
            float radiusSq = radius * radius;

            for (int i = 0; i < _graph.CellsCount; i++)
            {
                float distSq = _graph.Points[i].DistanceSquaredTo(_graph.Points[center]);
                if (distSq <= radiusSq)
                {
                    cells.Add(i);
                }
            }

            return cells;
        }

        private void ApplyHeightChange(EditAction action, float delta, bool landOnly)
        {
            var cells = action.Cells;
            action.OriginalValues = new byte[cells.Count];
            action.NewValues = new byte[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                int cell = cells[i];
                action.OriginalValues[i] = _graph.Heights[cell];

                if (landOnly && !_graph.IsLand(cell))
                {
                    action.NewValues[i] = _graph.Heights[cell];
                    continue;
                }

                float dist = _graph.Points[cell].DistanceTo(_graph.Points[action.Cells[0]]);
                float falloff = 1f - global::System.Math.Min(dist / 20f, 1f);
                byte newHeight = (byte)global::System.Math.Clamp(_graph.Heights[cell] + delta * falloff, 0, 100);

                _graph.Heights[cell] = newHeight;
                action.NewValues[i] = newHeight;
            }
        }
    }
}
