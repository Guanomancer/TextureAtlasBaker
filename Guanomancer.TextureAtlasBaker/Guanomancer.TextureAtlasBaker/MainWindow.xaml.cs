using Guanomancer.TextureAtlasBaker.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Guanomancer.TextureAtlasBaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isInitialized = false;
        private AtlasBaker _baker = new AtlasBaker();

        public MainWindow()
        {
            InitializeComponent();
            _isInitialized = true;

            _txtHorizontal.Text = _baker.LayoutWidth.ToString();
            _txtVertical.Text = _baker.LayoutHeight.ToString();
            
            foreach (var pixelFormat in AtlasBaker.GetPixelFormats())
                _cboPixelFormat.Items.Add(pixelFormat);
            _cboPixelFormat.SelectedItem = _baker.PixelFormatString;

            RecreateFileGrid();
        }

        private void RecreateFileGrid()
        {
            if (!_isInitialized) return;

            _gridSourceFiles.Children.Clear();
            _gridSourceFiles.ColumnDefinitions.Clear();
            _gridSourceFiles.RowDefinitions.Clear();

            for(int c = 0; c < _baker.LayoutWidth; c++)
                _gridSourceFiles.ColumnDefinitions.Add(new ColumnDefinition());

            for(int r =0; r < _baker.LayoutHeight; r++)
                _gridSourceFiles.RowDefinitions.Add(new RowDefinition());

            for (int r = 0; r < _baker.LayoutHeight; r++)
            {
                for (int c = 0; c < _baker.LayoutWidth; c++)
                {
                    var list = new ListBox();
                    list.FontSize = 12;
                    list.SetValue(Grid.ColumnProperty, c);
                    list.SetValue(Grid.RowProperty, r);
                    list.AllowDrop = true;
                    list.DragEnter += SourceFileList_DragEnter;
                    list.Drop += SourceFileList_Drop;
                    list.MouseDoubleClick += SourceFileList_MouseDoubleClick;
                    _gridSourceFiles.Children.Add(list);
                }
            }
        }

        private void SourceFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;

            if (listBox.SelectedIndex != -1)
                listBox.Items.RemoveAt(listBox.SelectedIndex);
        }

        private void SourceFileList_Drop(object sender, DragEventArgs e)
        {
            var listBox = sender as ListBox;

            var fileData = e.Data.GetData(DataFormats.FileDrop) as string[];
            var list = new List<string>();
            list.AddRange(fileData);
            foreach (var item in listBox.Items)
                list.Add(item as string);
            listBox.Items.Clear();
            foreach (var item in list)
                listBox.Items.Add(item);
        }

        private void SourceFileList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Link;
            else
                e.Effects = DragDropEffects.None;
        }

        private void _btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void _btnClose_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            App.Current.MainWindow.DragMove();
        }

        private void _txtHorizontal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;

            int.TryParse(_txtHorizontal.Text, out int value);
            if (value >= AtlasBaker.LAYOUT_MIN_VALUE && value <= AtlasBaker.LAYOUT_MAX_VALUE)
                _baker.LayoutWidth = value;
            _txtHorizontal.Text = _baker.LayoutWidth.ToString();
            RecreateFileGrid();
        }

        private void _txtVertical_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;

            int.TryParse(_txtVertical.Text, out int value);
            if (value >= AtlasBaker.LAYOUT_MIN_VALUE && value <= AtlasBaker.LAYOUT_MAX_VALUE)
                _baker.LayoutHeight = value;
            _txtVertical.Text = _baker.LayoutHeight.ToString();
            RecreateFileGrid();
        }

        private void _txtLayerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;

            _btnAddLayer.IsEnabled = _txtLayerName.Text.Length > 0 && !_lstLayers.Items.Contains(_txtLayerName.Text);
        }

        private void _lstLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            _btnRemoveLayer.IsEnabled = _lstLayers.SelectedItem != null;
        }

        private void _btnAddLayer_Click(object sender, RoutedEventArgs e)
        {
            _lstLayers.Items.Add(_txtLayerName.Text);
            _txtLayerName.Text = "";
            UpdateOutputFiles();
        }

        private void _btnRemoveLayer_Click(object sender, RoutedEventArgs e)
        {
            _lstLayers.Items.RemoveAt(_lstLayers.SelectedIndex);
            UpdateOutputFiles();
        }

        private void UpdateOutputFiles()
        {
            _lstOutputFiles.Items.Clear();
            foreach (var layer in _lstLayers.Items)
                _lstOutputFiles.Items.Add(_baker.GetOutputFileFromLayerName(layer.ToString()));
        }

        private void _txtLayerName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _btnAddLayer.IsEnabled)
                _btnAddLayer_Click(this, null);
        }

        private void _txtOutputFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (_baker.ValidateOutputFilenameFormat(_txtOutputFile.Text, out Exception exception))
            {
                _baker.OutputFilenameFormat = _txtOutputFile.Text;
                _txtOutputFile.Background = Brushes.White;
                _txtOutputFile.ToolTip = "";
            }
            else
            {
                _txtOutputFile.Background = Brushes.Red;
                _txtOutputFile.ToolTip = exception.Message;
            }
        }

        private void _cboPixelFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            _baker.PixelFormatString = _cboPixelFormat.SelectedItem.ToString();
        }
    }
}
