using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LockscreenImageGrabber
{
    public class Thumbnail : PictureBox
    {
        private bool mouseEnter;
        private string imagePath;
        private string imageFileSize;

        public Thumbnail()
        {
            this.ContextMenu = new ContextMenu(
                new MenuItem[]
                {
                    new MenuItem(Properties.Resources.OpenInDefaultImageViewer, this.OpenContextMenuItem_OnClick, Shortcut.CtrlO)
                    {
                        DefaultItem = true
                    },
                    new MenuItem("-"),
                    new MenuItem(Properties.Resources.SaveAs, this.SaveAsContextMenuItem_OnClick, Shortcut.CtrlS),
                    new MenuItem(Properties.Resources.CopyFilePath, this.CopyFilePathContextMenuItem_OnClick, Shortcut.CtrlC),
                    new MenuItem(Properties.Resources.CopyImage, this.CopyImageContextMenuItem_OnClick, Shortcut.CtrlShiftC),
                    new MenuItem("-"),
                    new MenuItem(Properties.Resources.SaveAllImages, this.SaveAllImagesContextMenuitem_OnClick, Shortcut.CtrlShiftS)
                });
        }

        public event EventHandler SaveAllContextItemClicked;

        public string ImagePath
        {
            get { return this.imagePath; }
            set
            {
                this.imagePath = value;
                this.imageFileSize = FormatSize(new FileInfo(this.ImagePath).Length);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseHover(e);

            this.Focus();
            this.mouseEnter = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            this.Parent.Focus();
            this.mouseEnter = false;
            this.Invalidate();
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);

            Process.Start(this.ImagePath);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!this.mouseEnter && !this.Focused)
            {
                return;
            }

            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 0, 0, this.Width, this.Height);
            e.Graphics.DrawRectangle(new Pen(Color.SteelBlue, 6), 0, 0, this.Width, this.Height);
            e.Graphics.DrawString(this.imageFileSize, new Font("Segoe UI", 10), Brushes.White, 10, this.Height - 25);
        }

        private static string FormatSize(long size)
        {
            var sizeIndicator = new[]
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };
            var sizeIndicatorIndex = 0;
            var remains = (decimal)size;
            var normalizedSize = (decimal)size;
            while (remains / 1014m > 1)
            {
                remains /= 1024m;
                normalizedSize = Math.Floor(remains * 10m) / 10m;

                if (sizeIndicatorIndex >= sizeIndicator.Length - 1)
                {
                    break;
                }

                sizeIndicatorIndex++;
            }

            return normalizedSize + sizeIndicator[sizeIndicatorIndex];
        }

        private void SaveAsContextMenuItem_OnClick(object sender, EventArgs eventArgs)
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = "jpg",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                FileName = Path.GetFileName(this.ImagePath)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (dialog.CheckPathExists)
                {
                    File.Copy(this.ImagePath, dialog.FileName);
                }
                else
                {
                    this.SaveAsContextMenuItem_OnClick(sender, eventArgs);
                }
            }
        }

        private void OpenContextMenuItem_OnClick(object sender, EventArgs e)
        {
            Process.Start(this.ImagePath);
        }

        private void CopyFilePathContextMenuItem_OnClick(object sender, EventArgs e)
        {
            Clipboard.SetText(this.ImagePath);
        }

        private void CopyImageContextMenuItem_OnClick(object sender, EventArgs e)
        {
            Clipboard.SetImage(Image.FromFile(this.ImagePath));
        }

        private void SaveAllImagesContextMenuitem_OnClick(object sender, EventArgs e)
        {
            this.SaveAllContextItemClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
