using Guanomancer.TextureAtlasBaker.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guanomancer.TextureAtlasBaker.Test
{
    [TestFixture]
    public class AtlasBaker_Tests
    {
        [Test]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(50, false)]
        [TestCase(100, false)]
        [TestCase(101, true)]
        public void LayoutWidth_LayoutHeight_ThrowsArgumentOutOfRangeException_WhenOutOfRange_OtherwiseSetsLayoutWidth(int testValue, bool expectException)
        {
            var baker = new AtlasBaker();

            if (expectException)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { baker.LayoutWidth = testValue; });
                Assert.Throws<ArgumentOutOfRangeException>(() => { baker.LayoutHeight = testValue; });
            }
            else
            {
                baker.LayoutWidth = testValue;
                baker.LayoutHeight = testValue;
                Assert.AreEqual(testValue, baker.LayoutWidth);
                Assert.AreEqual(testValue, baker.LayoutHeight);
            }
        }

        [Test]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(2048, false)]
        [TestCase(32768, false)]
        [TestCase(32769, true)]
        public void Resolution_ThrowsArgumentOutOfRangeException_WhenOutOfRange_OtherwiseSetsResolution(int testValue, bool expectException)
        {
            var baker = new AtlasBaker();

            if (expectException)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { baker.Resolution = testValue; });
            }
            else
            {
                baker.Resolution = testValue;
                Assert.AreEqual(testValue, baker.Resolution);
            }
        }

        [Test]
        [TestCase(-1, true)]
        [TestCase(PixelFormat.Format32bppArgb, false)]
        public void PixelFormat_ThrowsArgumentOutOfRangeException_WhenOutOfRange_OtherwiseSetsPixelFormat(PixelFormat testValue, bool expectException)
        {
            var baker = new AtlasBaker();

            if (expectException)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { baker.PixelFormat = testValue; });
            }
            else
            {
                baker.PixelFormat = testValue;
                Assert.AreEqual(testValue, baker.PixelFormat);
            }
        }

        [Test]
        [TestCase("c:/file_*.png")]
        [TestCase("/file_*.png")]
        [TestCase("file_*.bmp", typeof(BadImageFormatException))]
        [TestCase("file_*.", typeof(BadImageFormatException))]
        [TestCase("file_*", typeof(BadImageFormatException))]
        [TestCase("file_*.png", typeof(DirectoryNotFoundException))]
        [TestCase("this dir does not exist/file_*.png", typeof(DirectoryNotFoundException))]
        [TestCase("c:/this dir does not exist/file_*.png", typeof(DirectoryNotFoundException))]
        [TestCase("/file.png", typeof(ArgumentException))]
        public void OutputFileFormat_Throws_WhenInvalidFormat_OtherwiseSetsOutputFileFormat(string testValue, Type expectedExceptionTypeIfAny = null)
        {
            var baker = new AtlasBaker();

            if (expectedExceptionTypeIfAny != null)
            {
                Assert.Throws(expectedExceptionTypeIfAny, () => { baker.OutputFilenameFormat = testValue; });
            }
            else
            {
                baker.OutputFilenameFormat = testValue;
                Assert.AreEqual(testValue, baker.OutputFilenameFormat);
            }
        }

        [Test]
        [TestCase(null, typeof(ArgumentNullException))]
        [TestCase(new string[0], typeof(ArgumentException))]
        [TestCase(new string[] { "x", null, "z" }, typeof(ArrayTypeMismatchException))]
        [TestCase(new string[] { "x", "x" }, typeof(ArgumentOutOfRangeException))]
        [TestCase(new string[] { "x", "y" }, null)]
        public void LayerIdentifiers_Throws_WhenInvalid_OtherwiseSetsLayerIdentifiers(string[] testValue, Type expectedExceptionTypeIfAny)
        {
            var baker = new AtlasBaker();

            if (expectedExceptionTypeIfAny != null)
            {
                Assert.Throws(expectedExceptionTypeIfAny, () => { baker.LayerIdentifiers = testValue; });
            }
            else
            {
                baker.LayerIdentifiers = testValue;
                Assert.AreEqual(testValue, baker.LayerIdentifiers);
            }
        }
    }
}
