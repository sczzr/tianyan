using System;
using System.Collections.Generic;
using TianYanShop.MapGeneration.Data;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 选择管理器
    /// </summary>
    public class SelectionManager
    {
        private HashSet<int> _selectedCells = new HashSet<int>();
        private int _lastSelectedCell = -1;

        public void Select(int cell)
        {
            _selectedCells.Clear();
            _selectedCells.Add(cell);
            _lastSelectedCell = cell;
        }

        public void SelectMultiple(IEnumerable<int> cells)
        {
            foreach (var cell in cells)
            {
                _selectedCells.Add(cell);
            }
            if (_lastSelectedCell < 0 && cells is List<int> list && list.Count > 0)
            {
                _lastSelectedCell = list[0];
            }
        }

        public void AddToSelection(int cell)
        {
            _selectedCells.Add(cell);
            _lastSelectedCell = cell;
        }

        public void RemoveFromSelection(int cell)
        {
            _selectedCells.Remove(cell);
            if (_lastSelectedCell == cell)
            {
                _lastSelectedCell = -1;
            }
        }

        public void Clear()
        {
            _selectedCells.Clear();
            _lastSelectedCell = -1;
        }

        public bool IsSelected(int cell)
        {
            return _selectedCells.Contains(cell);
        }

        public int[] GetSelectedCells()
        {
            var result = new int[_selectedCells.Count];
            _selectedCells.CopyTo(result);
            return result;
        }

        public int SelectedCount => _selectedCells.Count;

        public int LastSelectedCell => _lastSelectedCell;

        public bool HasSelection => _selectedCells.Count > 0;

        public void ToggleSelection(int cell)
        {
            if (_selectedCells.Contains(cell))
            {
                _selectedCells.Remove(cell);
            }
            else
            {
                _selectedCells.Add(cell);
                _lastSelectedCell = cell;
            }
        }

        public void SelectRange(int start, int end)
        {
            _selectedCells.Clear();
            int min = System.Math.Min(start, end);
            int max = System.Math.Max(start, end);

            for (int i = min; i <= max; i++)
            {
                _selectedCells.Add(i);
            }

            _lastSelectedCell = end;
        }

        public void ExpandSelectionByRadius(VoronoiGraph graph, float radius)
        {
            var newSelection = new HashSet<int>(_selectedCells);

            foreach (var cell in _selectedCells)
            {
                var neighbors = graph.Neighbors[cell];
                foreach (var neighbor in neighbors)
                {
                    float dist = graph.Points[neighbor].DistanceTo(graph.Points[cell]);
                    if (dist <= radius)
                    {
                        newSelection.Add(neighbor);
                    }
                }
            }

            _selectedCells = newSelection;
        }

        public void ContractSelectionByRadius(VoronoiGraph graph, float radius)
        {
            var newSelection = new HashSet<int>();

            foreach (var cell in _selectedCells)
            {
                bool keep = true;
                foreach (var neighbor in graph.Neighbors[cell])
                {
                    if (!_selectedCells.Contains(neighbor))
                    {
                        float dist = graph.Points[neighbor].DistanceTo(graph.Points[cell]);
                        if (dist <= radius)
                        {
                            keep = false;
                            break;
                        }
                    }
                }

                if (keep)
                {
                    newSelection.Add(cell);
                }
            }

            _selectedCells = newSelection;
        }
    }
}
