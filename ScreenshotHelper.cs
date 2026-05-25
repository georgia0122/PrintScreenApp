using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// ???????????????????????
    /// </summary>
    public class ScreenshotHelper
    {
        private Bitmap _capturedImage;

        /// <summary>
        /// ????????
        /// </summary>
        /// <returns>?????Bitmap??</returns>
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
        /// ?????????????????????
        /// </summary>
        /// <param name="image">?????Bitmap</param>
        public void SaveCapturedImage(Bitmap image)
        {
            _capturedImage = image;
        }

        /// <summary>
        /// ????????
        /// </summary>
        /// <param name="filePath">???????</param>
        /// <returns>??????</returns>
        public bool SaveToFile(string filePath)
        {
            if (_capturedImage == null)
            {
                MessageBox.Show("???????", "??", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                // ??????
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _capturedImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show($"???????\n{filePath}", "??", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"?????{ex.Message}", "??", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// ?????????
        /// </summary>
        /// <returns>??????</returns>
        public bool CopyToClipboard()
        {
            if (_capturedImage == null)
            {
                MessageBox.Show("???????", "??", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                Clipboard.SetImage(_capturedImage);
                MessageBox.Show("?????????", "??", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"?????{ex.Message}", "??", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// ??????
        /// </summary>
        /// <returns>?????Bitmap??</returns>
        public Bitmap GetCapturedImage()
        {
            return _capturedImage;
        }
    }
}
