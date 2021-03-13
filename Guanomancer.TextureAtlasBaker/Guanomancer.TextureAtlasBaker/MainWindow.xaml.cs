using Guanomancer.TextureAtlasBaker.Core;
using System;
using System.Collections.Generic;
using System.IO;
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
        private List<string>[,] _sources;

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

            _sources = new List<string>[_baker.LayoutWidth, _baker.LayoutHeight];

            for (int r = 0; r < _baker.LayoutHeight; r++)
                for (int c = 0; c < _baker.LayoutWidth; c++)
                    _sources[c, r] = new List<string>();

            _gridSourceFiles.Children.Clear();
            _gridSourceFiles.ColumnDefinitions.Clear();
            _gridSourceFiles.RowDefinitions.Clear();

            for (int c = 0; c < _baker.LayoutWidth; c++)
                _gridSourceFiles.ColumnDefinitions.Add(new ColumnDefinition());

            for (int r = 0; r < _baker.LayoutHeight; r++)
                _gridSourceFiles.RowDefinitions.Add(new RowDefinition());

            for (int r = 0; r < _baker.LayoutHeight; r++)
            {
                for (int c = 0; c < _baker.LayoutWidth; c++)
                {
                    var listBox = new ListBox();
                    listBox.Tag = new Point(c, r);
                    listBox.FontSize = 12;
                    listBox.SetValue(Grid.ColumnProperty, c);
                    listBox.SetValue(Grid.RowProperty, r);
                    listBox.AllowDrop = true;
                    listBox.DragEnter += SourceFileList_DragEnter;
                    listBox.Drop += (sender, e) =>
                    {
                        var box = sender as ListBox;
                        var pt = (Point)box.Tag;
                        var col = (int)pt.X;
                        var row = (int)pt.Y;
                        var fileData = e.Data.GetData(DataFormats.FileDrop) as string[];
                        var list = new List<string>();
                        list.AddRange(fileData);
                        foreach (var item in box.Items)
                            list.Add(item as string);
                        box.Items.Clear();
                        _sources[col, row].Clear();
                        foreach (var item in list)
                        {
                            box.Items.Add(item);
                            _sources[col, row].Add(item);
                        }
                    };
                    listBox.MouseDoubleClick += (sender, e) =>
                    {
                        if (listBox.SelectedIndex != -1)
                        {
                            var box = sender as ListBox;
                            var pt = (Point)box.Tag;
                            var col = (int)pt.X;
                            var row = (int)pt.Y;
                            _sources[col, row].Remove(box.SelectedItem.ToString());
                            listBox.Items.RemoveAt(box.SelectedIndex);
                        }
                    };
                    _gridSourceFiles.Children.Add(listBox);
                }
            }
        }

        private void SourceFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;

            if (listBox.SelectedIndex != -1)
                listBox.Items.RemoveAt(listBox.SelectedIndex);
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

        private void _txtChannelName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;

            _btnAddChannel.IsEnabled = _txtChannelName.Text.Length > 0 && !_lstChannels.Items.Contains(_txtChannelName.Text);
        }

        private void _lstChannels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            _btnRemoveChannel.IsEnabled = _lstChannels.SelectedItem != null;

            if (_lstChannels.SelectedItem != null)
            {
                UpdateBakerInputFiles();

                string selectedChannel;
                if (_lstChannels.SelectedItem is ListBoxItem)
                    selectedChannel = (_lstChannels.SelectedItem as ListBoxItem).Content.ToString();
                else
                    selectedChannel = _lstChannels.SelectedItem.ToString();

                Task.Run(() =>
                {
                    _baker.GenerateChannel(true, selectedChannel, out byte[] previewBuffer);
                    _imgPreview.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        using (var ms = new MemoryStream(previewBuffer))
                        {
                            var previewImage = new BitmapImage();
                            previewImage.BeginInit();
                            previewImage.StreamSource = ms;
                            previewImage.CacheOption = BitmapCacheOption.OnLoad;
                            previewImage.EndInit();
                            _imgPreview.Source = previewImage;
                        }
                    }));
                });
            }
        }

        private void UpdateBakerInputFiles()
        {
            var inputs = new string[_baker.LayoutWidth, _baker.LayoutHeight][];
            for (int r = 0; r < _baker.LayoutHeight; r++)
            {
                for (int c = 0; c < _baker.LayoutWidth; c++)
                {
                    inputs[c, r] = _sources[c, r].ToArray();
                }
            }
            _baker.InputFiles = inputs;
        }

        private void _btnAddChannel_Click(object sender, RoutedEventArgs e)
        {
            _lstChannels.Items.Add(_txtChannelName.Text);
            _txtChannelName.Text = "";
            UpdateOutputFiles();
        }

        private void _btnRemoveChannel_Click(object sender, RoutedEventArgs e)
        {
            _lstChannels.Items.RemoveAt(_lstChannels.SelectedIndex);
            UpdateOutputFiles();
        }

        private void UpdateOutputFiles()
        {
            _lstOutputFiles.Items.Clear();
            foreach (var layer in _lstChannels.Items)
                _lstOutputFiles.Items.Add(_baker.GetOutputFileFromChannelName(
                    layer is ListBoxItem ? (layer as ListBoxItem).Content.ToString() : layer.ToString()
                    ));
        }

        private void _txtChannelName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _btnAddChannel.IsEnabled)
                _btnAddChannel_Click(this, null);
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

        private void _btnExport_Click(object sender, RoutedEventArgs e)
        {
            UpdateBakerInputFiles();

            var channelIdentifiers = new List<string>();
            foreach (var channelIdentifier in _lstChannels.Items)
            {
                channelIdentifiers.Add(channelIdentifier is ListBoxItem ?
                    (channelIdentifier as ListBoxItem).Content.ToString() :
                    channelIdentifier.ToString());
            }
            Task.Run(() =>
            {
                foreach (var channelIdentifierString in channelIdentifiers)
                {
                    _baker.GenerateChannel(false, channelIdentifierString, out byte[] buffer);
                    File.WriteAllBytes(_baker.GetOutputFileFromChannelName(channelIdentifierString), buffer);
                }
            });
        }
    }
}
