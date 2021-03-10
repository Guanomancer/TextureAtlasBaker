using System;
using System.Collections.Generic;
using System.Drawing;
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

namespace Guanomancer.TextureAtlasBaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private BitmapImage _preview;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdatePreview()
        {
            System.Diagnostics.Debug.WriteLine("Update preview!");

            if (_preview == null)
                _preview = new BitmapImage();

            _preview.BeginInit();

            _preview.EndInit();
            imgPreview.Source = _preview;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePreview();
        }

        private void txtOutputFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!txtOutputFile.Text.Contains('*'))
            {
                System.Diagnostics.Debug.WriteLine("Please include a * in the filename, to note where the layer name should be inserted.");
            }
            if(Path.GetExtension(txtOutputFile.Text).ToLower() != ".png")
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
    }
}
