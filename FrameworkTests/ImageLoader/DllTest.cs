﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageFramework.DirectX;
using ImageFramework.ImageLoader;
using ImageFramework.Model.Shader;
using ImageFramework.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DXGI;

namespace FrameworkTests.ImageLoader
{
    [TestClass]
    public class DllTest
    {
        private void VerifySmallLdr(Image image, Color.Channel channels)
        {
            Assert.AreEqual(1, image.NumMipmaps);
            Assert.AreEqual(1, image.NumLayers);
            Assert.AreEqual(3, image.GetWidth(0));
            Assert.AreEqual(3, image.GetHeight(0));
            Assert.AreEqual(Format.R8G8B8A8_UNorm_SRgb, image.Format.DxgiFormat);

            TestData.CompareWithSmall(image, channels);
        }

        public void VerifySmallHdr(Image image, Color.Channel channels)
        {
            Assert.AreEqual(1, image.NumMipmaps);
            Assert.AreEqual(1, image.NumLayers);
            Assert.AreEqual(3, image.GetWidth(0));
            Assert.AreEqual(3, image.GetHeight(0));
            Assert.AreEqual(Format.R32G32B32A32_Float, image.Format.DxgiFormat);

            TestData.CompareWithSmall(image, channels);
        }

        [TestMethod]
        public void StbiLdr()
        {
            VerifySmallLdr(IO.LoadImage(TestData.Directory + "small.png"), Color.Channel.Rgb);

            VerifySmallLdr(IO.LoadImage(TestData.Directory + "small_a.png"), Color.Channel.Rgba);

            VerifySmallLdr(IO.LoadImage(TestData.Directory + "small.bmp"), Color.Channel.Rgb);

            VerifySmallLdr(IO.LoadImage(TestData.Directory + "small.jpg"), Color.Channel.Rgb);
        }

        [TestMethod]
        public void StbiHdr()
        {
            VerifySmallHdr(IO.LoadImage(TestData.Directory + "small.hdr"), Color.Channel.Rgb);
        }

        [TestMethod]
        public void PfmColor()
        {
            VerifySmallHdr(IO.LoadImage(TestData.Directory + "small.pfm"), Color.Channel.Rgb);
        }

        [TestMethod]
        public void PfmGrayscale()
        {
            VerifySmallHdr(IO.LoadImage(TestData.Directory + "small_g.pfm"), Color.Channel.R);
        }

        [TestMethod]
        public void DDSSimple()
        {
            VerifySmallHdr(IO.LoadImage(TestData.Directory + "small.dds"), Color.Channel.Rgba);
        }

        [TestMethod]
        public void KTXSimple()
        {
            VerifySmallHdr(IO.LoadImage(TestData.Directory + "small.ktx"), Color.Channel.Rgba);
        }

        [TestMethod]
        public void LoadDdsCubemap()
        {
            var tex = new TextureArray2D(IO.LoadImage(TestData.Directory + "cubemap.dds"));
            Assert.AreEqual(6, tex.NumLayers);
            Assert.AreEqual(3, tex.NumMipmaps);
        }

        [TestMethod]
        public void LoadKtxCubemap()
        {
            var tex = new TextureArray2D(IO.LoadImage(TestData.Directory + "cubemap.ktx"));
            Assert.AreEqual(6, tex.NumLayers);
            Assert.AreEqual(3, tex.NumMipmaps);
        }
    }
}
