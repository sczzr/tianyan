using System;
using System.Collections.Generic;
using Godot;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Math;
using TianYanShop.MapGeneration.Rendering;
using TianYanShop.MapGeneration.Editor.Tools;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 地图编辑器主控制器
    /// </summary>
    public partial class MapEditor : Godot.Node2D
    {
        [Signal] public delegate void MapChangedEventHandler();
        [Signal] public delegate void SelectionChangedEventHandler(int[] selectedCells);

        private VoronoiGraph _graph;
        private MapRenderer _renderer;
        private EditorState _state;
        private UndoRedoSystem _undoRedo;
        private SelectionManager _selection;

        private bool _isEditing = false;
        private Vector2 _lastMousePos;

        public VoronoiGraph Graph => _graph;
        public EditorState State => _state;

        public MapEditor()
        {
            _state = new EditorState();
            _undoRedo = new UndoRedoSystem();
            _selection = new SelectionManager();
        }

        public override void _Ready()
        {
            _renderer = new MapRenderer();
            AddChild(_renderer);
            _renderer.ShowBiomes = true;
        }

        public void LoadGraph(VoronoiGraph graph)
        {
            _graph = graph.Clone();
            _renderer.Render(_graph);
            _state = new EditorState();
            _undoRedo.Clear();
        }

        public void LoadFromPath(string path)
        {
        }

        public void SaveToPath(string path)
        {
        }

        public new void _Process(float delta)
        {
            if (_isEditing && Input.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 mousePos = GetLocalMousePosition();
                if (mousePos != _lastMousePos)
                {
                    _lastMousePos = mousePos;
                    ApplyTool(mousePos);
                }
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    _isEditing = mouseEvent.Pressed;
                    if (mouseEvent.Pressed)
                    {
                        _lastMousePos = GetLocalMousePosition();
                        ApplyTool(_lastMousePos);
                    }
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Right)
                {
                    if (mouseEvent.Pressed)
                    {
                        SelectCellsAt(mouseEvent.Position);
                    }
                }
            }
            else if (@event is InputEventKey keyEvent)
            {
                if (keyEvent.Pressed)
                {
                    if (keyEvent.Keycode == Key.Z && keyEvent.IsCommandOrControlPressed())
                    {
                        Undo();
                    }
                    else if (keyEvent.Keycode == Key.Y && keyEvent.IsCommandOrControlPressed())
                    {
                        Redo();
                    }
                }
            }
        }

        private void ApplyTool(Vector2 position)
        {
            if (_graph == null || _state.SelectedTool == null) return;

            int cell = _graph.GetCellIndex(position);
            if (cell < 0 || cell >= _graph.CellsCount) return;

            var affectedCells = GetCellsInRadius(cell, _state.BrushRadius);

            var action = new EditAction
            {
                Type = EditActionType.Heightmap,
                Cells = affectedCells,
                OriginalValues = new byte[affectedCells.Count],
                NewValues = new byte[affectedCells.Count]
            };

            for (int i = 0; i < affectedCells.Count; i++)
            {
                action.OriginalValues[i] = _graph.Heights[affectedCells[i]];
                action.NewValues[i] = (byte)global::System.Math.Clamp(
                    _graph.Heights[affectedCells[i]] + _state.BrushStrength * 10,
                    0, 100
                );
            }

            ApplyAction(action);
        }

        private List<int> GetCellsInRadius(int center, float radius)
        {
            var cells = new List<int>();
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

        private void ApplyAction(EditAction action)
        {
            for (int i = 0; i < action.Cells.Count; i++)
            {
                _graph.Heights[action.Cells[i]] = action.NewValues[i];
            }

            _undoRedo.Push(action);
            _renderer.QueueRedraw();
            EmitSignal(SignalName.MapChanged);
        }

        public void SelectTool<T>() where T : EditorTool, new()
        {
            _state.SelectedTool = new T();
        }

        public void SelectTool(EditorToolType type)
        {
            _state.SelectedToolType = type;
        }

        public void SetBrushRadius(float radius)
        {
            _state.BrushRadius = global::System.Math.Clamp(radius, 1, 100);
        }

        public void SetBrushStrength(float strength)
        {
            _state.BrushStrength = global::System.Math.Clamp(strength, -1f, 1f);
        }

        private void SelectCellsAt(Vector2 position)
        {
            int cell = _graph.GetCellIndex(position);
            if (cell >= 0 && cell < _graph.CellsCount)
            {
                _selection.Select(cell);
                EmitSignal(SignalName.SelectionChanged, _selection.GetSelectedCells());
            }
        }

        public void Undo()
        {
            var action = _undoRedo.Undo();
            if (action != null)
            {
                for (int i = 0; i < action.Cells.Count; i++)
                {
                    _graph.Heights[action.Cells[i]] = action.OriginalValues[i];
                }
                _renderer.QueueRedraw();
                EmitSignal(SignalName.MapChanged);
            }
        }

        public void Redo()
        {
            var action = _undoRedo.Redo();
            if (action != null)
            {
                for (int i = 0; i < action.Cells.Count; i++)
                {
                    _graph.Heights[action.Cells[i]] = action.NewValues[i];
                }
                _renderer.QueueRedraw();
                EmitSignal(SignalName.MapChanged);
            }
        }

        public void ExportScene(string path)
        {
        }
    }

    /// <summary>
    /// 编辑操作
    /// </summary>
    public class EditAction
    {
        public EditActionType Type { get; set; }
        public List<int> Cells { get; set; } = new List<int>();
        public byte[] OriginalValues { get; set; } = Array.Empty<byte>();
        public byte[] NewValues { get; set; } = Array.Empty<byte>();
    }

    public enum EditActionType
    {
        Heightmap,
        Feature,
        State,
        Burg
    }
}
