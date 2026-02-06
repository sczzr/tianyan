using System;
using System.Collections.Generic;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 撤销/重做系统
    /// </summary>
    public class UndoRedoSystem
    {
        private readonly int _maxHistory = 50;
        private readonly List<EditAction> _undoStack = new List<EditAction>();
        private readonly List<EditAction> _redoStack = new List<EditAction>();

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Push(EditAction action)
        {
            _undoStack.Add(action);
            _redoStack.Clear();

            while (_undoStack.Count > _maxHistory)
            {
                _undoStack.RemoveAt(0);
            }
        }

        public EditAction Undo()
        {
            if (_undoStack.Count == 0) return null;

            var action = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(action);

            return action;
        }

        public EditAction Redo()
        {
            if (_redoStack.Count == 0) return null;

            var action = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(action);

            return action;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public void Commit()
        {
            _redoStack.Clear();
        }

        public EditAction PeekUndo()
        {
            return _undoStack.Count > 0 ? _undoStack[_undoStack.Count - 1] : null;
        }

        public EditAction PeekRedo()
        {
            return _redoStack.Count > 0 ? _redoStack[_redoStack.Count - 1] : null;
        }
    }
}
