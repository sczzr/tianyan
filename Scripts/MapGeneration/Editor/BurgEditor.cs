using System;
using System.Collections.Generic;
using Godot;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 城市编辑器（添加/删除城市）
    /// </summary>
    public class BurgEditor
    {
        private VoronoiGraph _graph;
        private UndoRedoSystem _undoRedo;

        public BurgEditor(VoronoiGraph graph, UndoRedoSystem undoRedo)
        {
            _graph = graph;
            _undoRedo = undoRedo;
        }

        public bool AddBurg(int cell, string name = null)
        {
            if (cell < 0 || cell >= _graph.CellsCount) return false;
            if (!_graph.IsLand(cell)) return false;
            if (_graph.Burgs[cell] > 0) return false;

            int newId = _graph.BurgsList.Count;
            var burg = new BurgData
            {
                Id = newId,
                Cell = cell,
                Position = _graph.Points[cell],
                Name = name ?? $"Burg_{newId}",
                Population = 500,
                Type = (int)BurgType.Village,
                State = _graph.States[cell],
                Culture = _graph.Cultures[cell],
                X = _graph.Points[cell].X,
                Y = _graph.Points[cell].Y
            };

            _graph.BurgsList.Add(burg);
            _graph.Burgs[cell] = (ushort)newId;

            var action = new EditAction
            {
                Type = EditActionType.Burg,
                Cells = new List<int> { cell },
                OriginalValues = new byte[] { 0 },
                NewValues = new byte[] { (byte)newId }
            };
            _undoRedo.Push(action);

            return true;
        }

        public bool RemoveBurg(int cell)
        {
            if (cell < 0 || cell >= _graph.CellsCount) return false;
            ushort burgId = _graph.Burgs[cell];
            if (burgId == 0) return false;

            byte originalValue = (byte)_graph.Burgs[cell];
            _graph.Burgs[cell] = 0;

            if (burgId < _graph.BurgsList.Count)
            {
                _graph.BurgsList[burgId].Removed = true;
                _graph.BurgsList[burgId].Cell = -1;
            }

            var action = new EditAction
            {
                Type = EditActionType.Burg,
                Cells = new List<int> { cell },
                OriginalValues = new byte[] { originalValue },
                NewValues = new byte[] { 0 }
            };
            _undoRedo.Push(action);

            return true;
        }

        public bool RemoveBurgById(int burgId)
        {
            if (burgId <= 0 || burgId >= _graph.BurgsList.Count) return false;

            var burg = _graph.BurgsList[burgId];
            if (burg.Cell < 0) return false;

            return RemoveBurg(burg.Cell);
        }

        public void SetBurgName(int burgId, string name)
        {
            if (burgId <= 0 || burgId >= _graph.BurgsList.Count) return;
            _graph.BurgsList[burgId].Name = name;
        }

        public void SetBurgPopulation(int burgId, int population)
        {
            if (burgId <= 0 || burgId >= _graph.BurgsList.Count) return;
            _graph.BurgsList[burgId].Population = global::System.Math.Max(0, population);
        }

        public void SetBurgType(int burgId, BurgType type)
        {
            if (burgId <= 0 || burgId >= _graph.BurgsList.Count) return;
            _graph.BurgsList[burgId].Type = (int)type;
        }

        public void SetBurgCapital(int burgId, bool isCapital)
        {
            if (burgId <= 0 || burgId >= _graph.BurgsList.Count) return;
            _graph.BurgsList[burgId].Capital = isCapital;
        }

        public BurgData GetBurg(int burgId)
        {
            if (burgId <= 0 || burgId >= _graph.BurgsList.Count) return null;
            return _graph.BurgsList[burgId];
        }

        public int GetBurgIdAtCell(int cell)
        {
            if (cell < 0 || cell >= _graph.CellsCount) return -1;
            return _graph.Burgs[cell];
        }

        public List<BurgData> GetAllBurgs()
        {
            var burgs = new List<BurgData>();
            for (int i = 1; i < _graph.BurgsList.Count; i++)
            {
                if (!_graph.BurgsList[i].Removed)
                    burgs.Add(_graph.BurgsList[i]);
            }
            return burgs;
        }

        public List<BurgData> GetBurgsInState(int stateId)
        {
            var burgs = new List<BurgData>();
            foreach (var burg in GetAllBurgs())
            {
                if (burg.State == stateId)
                    burgs.Add(burg);
            }
            return burgs;
        }

        public bool IsValidBurgLocation(int cell)
        {
            if (cell < 0 || cell >= _graph.CellsCount) return false;
            if (!_graph.IsLand(cell)) return false;
            if (_graph.Suitability[cell] < 20) return false;
            if (_graph.Biomes[cell] == (int)BiomeType.Snow) return false;
            if (_graph.Biomes[cell] == (int)BiomeType.ColdDesert) return false;
            if (_graph.Burgs[cell] > 0) return false;

            foreach (var neighbor in _graph.Neighbors[cell])
            {
                if (_graph.Burgs[neighbor] > 0)
                {
                    float dist = _graph.Points[cell].DistanceTo(_graph.Points[neighbor]);
                    if (dist < 10) return false;
                }
            }

            return true;
        }

        public int FindBestBurgLocation()
        {
            int bestCell = -1;
            float bestScore = -1;

            for (int i = 0; i < _graph.CellsCount; i++)
            {
                if (!IsValidBurgLocation(i)) continue;

                float score = _graph.Suitability[i];
                if (_graph.Harbor[i] > 0) score += 20;
                if (_graph.Rivers[i] > 0) score += 15;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = i;
                }
            }

            return bestCell;
        }
    }
}
