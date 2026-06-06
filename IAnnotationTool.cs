using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public enum AnnotationToolKind
    {
        Pen,
        Arrow,
        Mosaic,
        Rectangle,
        Circle,
        Highlighter,
        Eraser
    }

    /// <summary>
    /// Interface for all annotation tools
    /// </summary>
    public interface IAnnotationTool
    {
        /// <summary>
        /// Tool name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Tool color
        /// </summary>
        Color ToolColor { get; set; }

        /// <summary>
        /// Tool size/width
        /// </summary>
        int ToolSize { get; set; }

        /// <summary>
        /// Handle mouse down event
        /// </summary>
        void OnMouseDown(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap);

        /// <summary>
        /// Handle mouse move event
        /// </summary>
        void OnMouseMove(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap);

        /// <summary>
        /// Handle mouse up event
        /// </summary>
        void OnMouseUp(MouseEventArgs e, Graphics graphics, Bitmap targetBitmap);

        /// <summary>
        /// Draw preview on graphics
        /// </summary>
        void DrawPreview(Graphics graphics);

        /// <summary>
        /// Finalize drawing and commit to bitmap
        /// </summary>
        void Commit(Graphics graphics, Bitmap targetBitmap);

        /// <summary>
        /// Reset tool state
        /// </summary>
        void Reset();
    }

    public interface ISourceImageTool
    {
        Bitmap SourceImage { get; set; }
    }
}
