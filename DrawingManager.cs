using System;
using System.Collections.Generic;
using System.Drawing;

namespace PrintScreenApp
{
    /// <summary>
    /// Manages drawing operations and history
    /// </summary>
    public class DrawingManager
    {
        private Stack<Bitmap> _undoStack = new Stack<Bitmap>();
        private Stack<Bitmap> _redoStack = new Stack<Bitmap>();
        private Bitmap _baseImage;
        private Bitmap _currentImage;

        public DrawingManager(Bitmap baseImage)
        {
            _baseImage = (Bitmap)baseImage.Clone();
            _currentImage = (Bitmap)baseImage.Clone();
        }

        /// <summary>
        /// Get current image
        /// </summary>
        public Bitmap GetCurrentImage()
        {
            return (Bitmap)_currentImage.Clone();
        }

        /// <summary>
        /// Save current state to undo stack
        /// </summary>
        public void SaveState()
        {
            _undoStack.Push((Bitmap)_currentImage.Clone());
            _redoStack.Clear();
        }

        /// <summary>
        /// Undo last operation
        /// </summary>
        public bool Undo()
        {
            if (_undoStack.Count == 0) return false;

            _redoStack.Push(_currentImage);
            _currentImage = _undoStack.Pop();
            return true;
        }

        /// <summary>
        /// Redo last undone operation
        /// </summary>
        public bool Redo()
        {
            if (_redoStack.Count == 0) return false;

            _undoStack.Push(_currentImage);
            _currentImage = _redoStack.Pop();
            return true;
        }

        /// <summary>
        /// Check if undo is available
        /// </summary>
        public bool CanUndo() => _undoStack.Count > 0;

        /// <summary>
        /// Check if redo is available
        /// </summary>
        public bool CanRedo() => _redoStack.Count > 0;

        /// <summary>
        /// Reset to base image
        /// </summary>
        public void Reset()
        {
            _currentImage.Dispose();
            _currentImage = (Bitmap)_baseImage.Clone();
            _undoStack.Clear();
            _redoStack.Clear();
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _baseImage?.Dispose();
            _currentImage?.Dispose();

            foreach (var img in _undoStack)
            {
                img?.Dispose();
            }
            _undoStack.Clear();

            foreach (var img in _redoStack)
            {
                img?.Dispose();
            }
            _redoStack.Clear();
        }
    }
}
