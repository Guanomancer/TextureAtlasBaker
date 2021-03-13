using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Guanomancer.TextureAtlasBaker.Core
{
    public class AtlasBaker
    {
        public static string[] GetPixelFormats()
        {
            var pixelFormats = Enum.GetValues(typeof(PixelFormat));
            var list = new List<string>();
            foreach (var format in pixelFormats)
            {
                var str = format.ToString();
                if (str.StartsWith("Format"))
                    list.Add(str);
            }
            return list.ToArray();
        }

        public const int LAYOUT_MIN_VALUE = 1;
        public const int LAYOUT_MAX_VALUE = 100;
        public const int FORMAT_MIN_RESOLUTION = 1;
        public const int FORMAT_MAX_RESOLUTION = 32768;

        public int PreviewResolution { get; set; } = 512;

        private int _layoutWidth = 2;
        private int _layoutHeight = 2;
        private int _resolution = 2048;
        private PixelFormat _pixelFormat = PixelFormat.Format32bppArgb;

        private string[] _channelIdentifiers = new string[0];
        private string _outputFileFormat = "\\TextureSet_*.png";
        private string[,][] _inputFiles = new string[2, 2][];

        private string[] _maskChannels;

        public int LayoutWidth
        {
            get => _layoutWidth;
            set
            {
                if (value < LAYOUT_MIN_VALUE || value > LAYOUT_MAX_VALUE) throw new ArgumentOutOfRangeException("LayoutWidth", $"Layout width must be a positive number, and no more than {LAYOUT_MAX_VALUE}.");
                _layoutWidth = value;
            }
        }

        public int LayoutHeight
        {
            get => _layoutHeight;
            set
            {
                if (value < LAYOUT_MIN_VALUE || value > LAYOUT_MAX_VALUE) throw new ArgumentOutOfRangeException("LayoutHeight", $"Layout height must be a positive number, and no more than {LAYOUT_MAX_VALUE}.");
                _layoutHeight = value;
            }
        }

        public int Resolution
        {
            get => _resolution;
            set
            {
                if (value < FORMAT_MIN_RESOLUTION || value > FORMAT_MAX_RESOLUTION) throw new ArgumentOutOfRangeException("Resolution", $"Resolution must be a positive number, and no more than {FORMAT_MAX_RESOLUTION}. Preferably a power of 2.");
                _resolution = value;
            }
        }

        public PixelFormat PixelFormat
        {
            get => _pixelFormat;
            set
            {
                if (!Enum.IsDefined(typeof(PixelFormat), value)) throw new ArgumentOutOfRangeException("PixelFormat", "Please specify a valid PixelFormat.");
                _pixelFormat = value;
            }
        }

        public string PixelFormatString
        {
            get => _pixelFormat.ToString();
            set
            {
                if (!Enum.TryParse<PixelFormat>(value, out PixelFormat pixelFormat))
                    throw new ArgumentOutOfRangeException("PixelFormat", "Please specify a valid PixelFormat.");
                _pixelFormat = pixelFormat;
            }
        }

        public bool ValidatePixelFormatString(string value, out Exception exception)
        {
            exception = null;
            if (Enum.TryParse<PixelFormat>(value, out PixelFormat pixelFormat))
                exception = new ArgumentOutOfRangeException("PixelFormat", "Please specify a valid PixelFormat.");
            return exception == null;
        }

        public string[] ChannelIdentifiers
        {
            get => _channelIdentifiers;
            set
            {
                if (!ValidateChannelIdentifiers(value, out Exception exception)) throw exception;
                _channelIdentifiers = value;
            }
        }

        public bool ValidateChannelIdentifiers(string[] value, out Exception exception)
        {
            exception = null;
            if (value == null) exception = new ArgumentNullException("ChannelIdentifiers");
            else if (value.Length == 0) exception = new ArgumentException("At least one channel identifier must be specified.", "ChannelIdentifiers");
            else
            {
                foreach (var ident in value)
                    if (ident == null)
                        exception = new ArrayTypeMismatchException("A channel identifier can not be null.");
            }
            if (exception == null)
            {
                for (int i = 0; i < value.Length - 1; i++)
                {
                    for (int si = i + 1; si < value.Length; si++)
                        if (value[i] == value[si])
                            exception = new ArgumentOutOfRangeException("ChannelIdentifiers", $"Can not add two channels with the same identifier ({value[i]}).");
                }
            }
            return exception == null;
        }

        public string OutputFilenameFormat
        {
            get => _outputFileFormat;
            set
            {
                if (!ValidateOutputFilenameFormat(value, out Exception exception)) throw exception;
                _outputFileFormat = value;
            }
        }

        public bool ValidateOutputFilenameFormat(string value, out Exception exception)
        {
            exception = null;
            var testValue = value.Replace("*", "COLOR");
            if (Path.GetExtension(testValue).ToLower() != ".png") exception = new BadImageFormatException("Output file format must end with .png");
            else if (!Directory.Exists(Path.GetDirectoryName(testValue))) exception = new DirectoryNotFoundException($"Directory does not exist '{Path.GetDirectoryName(testValue)}'.");
            else if (value.IndexOf('*') == -1) exception = new ArgumentException("Output file format must contain a *, to define where to put channel names.");
            return exception == null;
        }

        public string[,][] InputFiles
        {
            get => _inputFiles;
            set
            {
                if (value == null) throw new ArgumentNullException("InputFiles");
                if (value.GetLength(0) != _layoutWidth || value.GetLength(1) != _layoutHeight) throw new ArgumentOutOfRangeException("InputFiles");
                for (int y = 0; y < value.GetLength(1); y++)
                {
                    for (int x = 0; x < value.GetLength(0); x++)
                    {
                        if (value[x, y] == null) throw new ArgumentNullException("InputFiles");
                        for (int i = 0; i < value[x, y].Length; i++)
                            if (value[x, y][i] == null) throw new ArgumentNullException("InputFiles");
                    }
                }
                _inputFiles = value;
            }
        }

        public string[] MaskChannels
        {
            get => _maskChannels;
            set
            {
                if (value == null) throw new ArgumentNullException("MaskChannels");
                if (value.Length != 4) throw new ArgumentOutOfRangeException("MaskChannels");
                foreach (var item in value)
                    if (item == null)
                        throw new ArgumentNullException("MaskChannels");
                _maskChannels = value;
            }
        }

        public bool GetInputFile(int column, int row, string channelIdentifier, out string file)
        {
            foreach (var filename in _inputFiles[column, row])
            {
                foreach (var chanIdent in channelIdentifier.Split('/'))
                {
                    if (filename.Contains(chanIdent))
                    {
                        file = filename;
                        return true;
                    }
                }
            }
            file = null;
            return false;
        }

        public string GetOutputFileFromChannelName(string channelName)
        {
            if (channelName.IndexOf('/') != -1)
                channelName = channelName.Split('/')[0];
            var file = _outputFileFormat.Replace("*", channelName);
            if (file.StartsWith("\\"))
                file = Path.GetFullPath(file.Substring(1));
            return file;
        }

        public void GenerateChannel(bool isPreview, string channelIdentifier, out byte[] buffer)
        {
            var resolution = isPreview ? PreviewResolution : _resolution;
            using (Bitmap bmp = GenerateChannelBitmap(isPreview, channelIdentifier))
            {
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int colSpan = resolution / _layoutWidth;
                    int rowSpan = resolution / _layoutHeight;
                    for (int col = 0; col < _layoutWidth; col++)
                    {
                        for (int row = 0; row < _layoutHeight; row++)
                        {
                            if (GetInputFile(col, row, channelIdentifier, out string file))
                            {
                                using (var img = Image.FromFile(file))
                                {
                                    var x = col * colSpan;
                                    var y = row * rowSpan;
                                    gfx.DrawImage(img, x, y, colSpan, rowSpan);
                                }
                            }
                        }
                    }
                }
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    buffer = ms.ToArray();
                }
            }
        }

        private Bitmap GenerateChannelBitmap(bool isPreview, string channelIdentifier)
        {
            var resolution = isPreview ? PreviewResolution : _resolution;
            Bitmap bmp = new Bitmap(resolution, resolution, _pixelFormat);
            using (var gfx = Graphics.FromImage(bmp))
            {
                int colSpan = resolution / _layoutWidth;
                int rowSpan = resolution / _layoutHeight;
                for (int col = 0; col < _layoutWidth; col++)
                {
                    for (int row = 0; row < _layoutHeight; row++)
                    {
                        var x = col * colSpan;
                        var y = row * rowSpan;
                        if (GetInputFile(col, row, channelIdentifier, out string file))
                        {
                            using (var img = Image.FromFile(file))
                            {
                                gfx.DrawImage(img, x, y, colSpan, rowSpan);
                            }
                        }
                    }
                }
            }
            return bmp;
        }
    }
}