using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Helper class to manage screenshot operations
    /// </summary>
    public class ScreenshotHelper
    {
        private Bitmap _capturedImage;

        /// <summary>
        /// Capture a region from the screen
        /// </summary>
        /// <returns>Captured image as Bitmap</returns>
        public Bitmap CaptureRegion()
        {
            var regionSelector = new RegionSelectorForm();
            if (regionSelector.ShowDialog() == DialogResult.OK)
            {
                _capturedImage = regionSelector.CapturedImage;
                return _capturedImage;
            }
            return null;
        }

        /// <summary>
        /// Save captured image internally
        /// </summary>
        /// <param name="image">Bitmap image to save</param>
        public void SaveCapturedImage(Bitmap image)
        {
            _capturedImage = image;
        }

        /// <summary>
        /// Save screenshot to file
        /// </summary>
        /// <param name="filePath">File path to save</param>
        /// <returns>Success status</returns>
        public bool SaveToFile(string filePath)
        {
            if (_capturedImage == null)
            {
                MessageBox.Show("No screenshot captured", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                // Create directory if needed
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _capturedImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show($"Screenshot saved successfully.\n{filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Copy screenshot to clipboard
        /// </summary>
        /// <returns>Success status</returns>
        public bool CopyToClipboard()
        {
            if (_capturedImage == null)
            {
                MessageBox.Show("No screenshot captured", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                Clipboard.SetImage(_capturedImage);
                MessageBox.Show("Screenshot copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Get the captured image
        /// </summary>
        /// <returns>Captured Bitmap image</returns>
        public Bitmap GetCapturedImage()
        {
            return _capturedImage;
        }
    }
}
