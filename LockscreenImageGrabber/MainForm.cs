using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LockscreenImageGrabber
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.loadWorker.RunWorkerAsync();

            this.flowLayoutPanel.Enabled = false;
            this.flowLayoutPanel.ContextMenu = new ContextMenu(
                new[]
                {
                    new MenuItem(Properties.Resources.SaveAllImages, this.SaveAllImagesContextMenuitem_OnClick, Shortcut.CtrlShiftS),
                });
        }

        private void loadWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var workingArea = Screen.PrimaryScreen.Bounds;
            var screenWidth = workingArea.Width;
            var screenHeight = workingArea.Height;

            var pathToImages =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets");

            if (!Directory.Exists(pathToImages))
            {
                MessageBox.Show(Properties.Resources.TheLockscreenAssetsFolderCouldNotBeFound);
            }

            var images = Directory.GetFiles(pathToImages);
            var imageIndex = 0;
            var imageCount = images.Length;

            const int maxThumbnailWidth = 200;

            var tempPath = Path.Combine(Path.GetTempPath(), "LockscreenImageGrabber");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            else
            {
                Directory.Delete(tempPath, true);
                Directory.CreateDirectory(tempPath);
            }

            foreach (var sourcePath in images)
            {
                var path = Path.Combine(tempPath, Path.GetFileName(sourcePath) + ".jpg");
                File.Copy(sourcePath, path, true);
                try
                {

                    using (var image = Image.FromFile(path))
                    {
                        if (image.Width != screenWidth || image.Height != screenHeight)
                        {
                            imageIndex++;
                            continue;
                        }

                        var height =
                            (int) Math.Floor((float) maxThumbnailWidth / ((float) image.Width / (float) image.Height));

                        this.loadWorker.ReportProgress(
                            imageIndex / imageCount * 100,
                            new ImageModel
                            {
                                SourcePath = sourcePath,
                                TempPath = path,
                                FileName = Path.GetFileNameWithoutExtension(path),
                                Thumbnail = image.GetThumbnailImage(maxThumbnailWidth, height, null, IntPtr.Zero)
                            });
                        imageIndex++;
                    }
                }
                catch (OutOfMemoryException)
                {
                    // File is not a valid image so Image.FromFile throws an OutOfMemoryException
                }
            }
        }

        private void loadWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            var image = e.UserState as ImageModel;
            if (image == null)
            {
                return;
            }

            var thumbnail = new Thumbnail
            {
                Name = image.FileName,
                Size = new Size(image.Thumbnail.Width, image.Thumbnail.Height),
                Image = image.Thumbnail,
                ImagePath = image.TempPath,
                Enabled = false
            };
            thumbnail.SaveAllContextItemClicked += this.Thumbnail_SaveAllContextItemClicked;

            this.flowLayoutPanel.Controls.Add(thumbnail);
        }

        private void loadWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            var imagesFound = false;

            foreach (var control in this.flowLayoutPanel.Controls)
            {
                var thumbnail = control as Thumbnail;
                if (thumbnail == null)
                {
                    continue;
                }

                thumbnail.Enabled = true;
                imagesFound = true;
            }

            if (!imagesFound)
            {
                var label = new Label
                {
                    AutoSize = true,
                    Text = Properties.Resources.ItSeemsYouHaveDisabledLockscreenSpotlightNoImagesWereFound,
                    Font = new Font("Segoe UI", 12),
                };
                this.flowLayoutPanel.Controls.Add(label);
            }

            this.flowLayoutPanel.Enabled = true;
            this.flowLayoutPanel.Focus();
        }

        private void Thumbnail_SaveAllContextItemClicked(object sender, EventArgs e)
        {
            this.SaveAllImagesContextMenuitem_OnClick(sender, e);
        }

        private void SaveAllImagesContextMenuitem_OnClick(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog { ShowNewFolderButton = true };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (!Directory.Exists(dialog.SelectedPath))
                {
                    MessageBox.Show(Properties.Resources.SelectedPathDoesNotExist);
                    return;
                }

                this.flowLayoutPanel.Enabled = false;

                this.bundleSaveWorker.RunWorkerAsync(new SaveModel { SelectedPath = dialog.SelectedPath });
            }
        }

        private void saveAllWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var model = e.Argument as SaveModel;
            if (model == null)
            {
                return;
            }

            foreach (var control in flowLayoutPanel.Controls)
            {
                var thumbnail = control as Thumbnail;
                if (thumbnail == null)
                {
                    continue;
                }

                var destPath = Path.Combine(model.SelectedPath, Path.GetFileName(thumbnail.ImagePath));
                var origFileName = Path.GetFileNameWithoutExtension(thumbnail.ImagePath);
                var origFileExt = Path.GetExtension(thumbnail.ImagePath);
                var index = 1;
                while (File.Exists(destPath))
                {
                    destPath = $"{Path.Combine(model.SelectedPath, origFileName)} ({index}){origFileExt}";
                    index++;
                }

                File.Copy(thumbnail.ImagePath, destPath);
            }
        }

        private void saveAllWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            this.flowLayoutPanel.Enabled = true;
        }

        private class SaveModel
        {
            public string SelectedPath { get; set; }
        }

        private class ImageModel
        {
            public string SourcePath { get; set; }

            public string TempPath { get; set; }

            public string FileName { get; set; }

            public Image Thumbnail { get; set; }

        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "LockscreenImageGrabber");
            if (!Directory.Exists(tempPath))
            {
                return;
            }

            try
            {
                Directory.Delete(tempPath, true);
            }
            catch
            {
                // Ignore errors.
            }
        }
    }
}
