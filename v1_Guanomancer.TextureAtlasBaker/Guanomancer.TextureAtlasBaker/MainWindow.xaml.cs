using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Path = System.IO.Path;
using Bmp = System.Drawing.Bitmap;
using Color = System.Drawing.Color;
using Img = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Guanomancer.TextureAtlasBaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isInitialized;

        public MainWindow()
        {
            InitializeComponent();
            _isInitialized = true;
        }

        private void UpdatePreview()
        {
            if (!_isInitialized)
                return;
            if (lstLayerNames.Items.Count == 0)
                return;
            if (lstSourceFiles.Items.Count == 0)
                return;
            if (lstLayerNames.SelectedItem == null)
                return;

            var resolution = int.Parse((cboResolution.SelectedItem as ComboBoxItem).Content as string);
            var layerName = lstLayerNames.SelectedItem as string;
            var files = new List<string>();
            foreach (var item in lstSourceFiles.Items)
            {
                var file = item as string;
                if (file.IndexOf(layerName) != -1)
                    files.Add(file);
            }
            if (files.Count == 0)
                return;
            var cols = (int)sldCols.Value;
            var rows = (int)sldRows.Value;

            Task.Run(() => { GeneratePreview(resolution, files, cols, rows); });
        }

        private void GeneratePreview(int resolution, List<string> files, int cols, int rows)
        {
            GenerateChannel(resolution, files, cols, rows, (bmp) =>
            {
                var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                imgPreview.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var previewImage = new BitmapImage();
                    previewImage.BeginInit();
                    previewImage.StreamSource = ms;
                    previewImage.CacheOption = BitmapCacheOption.OnLoad;
                    previewImage.EndInit();
                    imgPreview.Source = previewImage;

                    ms.Dispose();
                }));
            });
        }

        private void GenerateFinal(int resolution, List<string> files, int cols, int rows, string outputFile)
        {
            foreach(var layerName in lstLayerNames.Items)
            {
                var channelFiles = new List<string>();
                foreach (var file in files)
                    if (file.IndexOf(layerName as string) != -1)
                        channelFiles.Add(file);
                GenerateChannel(resolution, channelFiles, cols, rows, (bmp) =>
                {
                    bmp.Save(outputFile.Replace("*", layerName as string), ImageFormat.Png);
                });
            }
        }

        private void GenerateChannel(int resolution, List<string> files, int cols, int rows, Action<Bmp> saveAction)
        {
            using (Bmp bmp = new Bmp(resolution, resolution, PixelFormat.Format32bppArgb))
            {
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int colSpan = resolution / cols;
                    int rowSpan = resolution / rows;
                    int fileIndex = 0;
                    for (int col = 0; col < cols; col++)
                    {
                        for (int row = 0; row < rows; row++)
                        {
                            if (fileIndex >= files.Count)
                                break;
                            using (var img = Img.FromFile(files[fileIndex]))
                            {
                                var x = col * colSpan;
                                var y = row * rowSpan;
                                gfx.DrawImage(img, x, y, colSpan, rowSpan);
                            }

                            fileIndex++;
                        }
                    }
                }
                saveAction(bmp);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePreview();
        }

        private void txtOutputFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!txtOutputFile.Text.Contains("*"))
            {
                System.Diagnostics.Debug.WriteLine("Please include a * in the filename, to note where the layer name should be inserted.");
            }
            if (Path.GetExtension(txtOutputFile.Text).ToLower() != ".png")
            {
                System.Diagnostics.Debug.WriteLine($"Error: Filename {txtOutputFile.Text} has an invalid image format extension {Path.GetExtension(txtOutputFile.Text)}.");
            }
        }

        private void btnRemoveLayer_Click(object sender, RoutedEventArgs e)
        {
            var items = new string[lstLayerNames.SelectedItems.Count];
            lstLayerNames.SelectedItems.CopyTo(items, 0);
            foreach (var item in items)
                lstLayerNames.Items.Remove(item);
            txtLayerIdentifier_TextChanged(null, null);
        }

        private void btnAddLayer_Click(object sender, RoutedEventArgs e)
        {
            lstLayerNames.Items.Add(txtLayerIdentifier.Text);
            txtLayerIdentifier_TextChanged(null, null);
        }

        private void txtLayerIdentifier_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnAddLayer == null) return;
            btnAddLayer.IsEnabled =
                txtLayerIdentifier.Text.Length > 0 &&
                !lstLayerNames.Items.Contains(txtLayerIdentifier.Text);
        }

        private void txtLayerIdentifier_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                btnAddLayer_Click(sender, null);
        }

        private void lstLayerNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnRemoveLayer.IsEnabled =
                lstLayerNames.SelectedItems.Count > 0;

            UpdatePreview();
        }

        private void lstSourceFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnRemoveSourceFile.IsEnabled =
                lstSourceFiles.SelectedItems.Count > 0;
        }

        private void btnRemoveSourceFile_Click(object sender, RoutedEventArgs e)
        {
            var items = new string[lstSourceFiles.SelectedItems.Count];
            lstSourceFiles.SelectedItems.CopyTo(items, 0);
            foreach (var item in items)
                lstSourceFiles.Items.Remove(item);
        }

        private void lstSourceFiles_Drop(object sender, DragEventArgs e)
        {
            var fileData = e.Data.GetData(DataFormats.FileDrop) as string[];
            var list = new List<string>();
            list.AddRange(fileData);
            foreach (var item in lstSourceFiles.Items)
                list.Add(item as string);
            lstSourceFiles.Items.Clear();
            foreach (var item in list)
                lstSourceFiles.Items.Add(item);

            UpdatePreview();
        }

        private void lstSourceFiles_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
                e.Effects = DragDropEffects.None;
        }

        private void cboResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void btnRenderAllChannels_Click(object sender, RoutedEventArgs e)
        {
            if (lstLayerNames.Items.Count == 0)
                return;
            if (lstSourceFiles.Items.Count == 0)
                return;
            if (lstLayerNames.SelectedItem == null)
                return;

            var resolution = int.Parse((cboResolution.SelectedItem as ComboBoxItem).Content as string);

            var layerName = lstLayerNames.SelectedItem as string;

            var files = new List<string>();
            foreach (var item in lstSourceFiles.Items)
            {
                var file = item as string;
                files.Add(file);
            }
            if (files.Count == 0)
                return;

            var cols = (int)sldCols.Value;
            var rows = (int)sldRows.Value;

            var outputFile = txtOutputFile.Text;

            Task.Run(() => { GenerateFinal(resolution, files, cols, rows, outputFile); });
        }
    }
}
