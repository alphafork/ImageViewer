﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ImageFramework.Annotations;
using ImageFramework.DirectX;
using ImageFramework.ImageLoader;
using ImageFramework.Model.Shader;
using SharpDX.DXGI;

namespace ImageFramework.Model
{
    public class ImagesModel : INotifyPropertyChanged, IDisposable
    {
        private MitchellNetravaliScaleShader scaleShader;
        private struct Dimension
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int Depth { get; set; }
        }

        private Dimension[] dimensions = null;

        public class MipmapMismatch : Exception
        {
            public MipmapMismatch(string message) : base(message)
            { }
        }

        public class ImageData : IDisposable
        {
            public ITexture Image { get; private set; }
            public int NumMipmaps => Image.NumMipmaps;
            public int NumLayers => Image.NumLayers;
            public bool IsHdr => Image.Format == Format.R32G32B32A32_Float;
            public string Filename { get; }
            public GliFormat OriginalFormat { get; }

            internal ImageData(ITexture image, string filename, GliFormat originalFormat)
            {
                Image = image;
                Filename = filename;
                OriginalFormat = originalFormat;
            }

            internal void GenerateMipmaps(int levels)
            {
                var tmp = Image.GenerateMipmapLevels(levels);
                Image.Dispose();
                Image = tmp;
            }

            internal void DeleteMipmaps()
            {
                var tmp = Image.CloneWithoutMipmaps();
                Image.Dispose();
                Image = tmp;
            }

            public void Dispose()
            {
                Image.Dispose();
            }

            /*public void Scale(int width, int height, MitchellNetravaliScaleShader scale)
            {
                var tmp = scale.Run(Image, width, height);
                Image.Dispose();
                Image = tmp;
            }*/
        }

        private readonly List<ImageData> images = new List<ImageData>();
        public IReadOnlyList<ImageData> Images { get; }

        // this property change will be triggered if the image order changes (and not if the number of images changes)
        public static string ImageOrder = nameof(ImageOrder);
        public int NumImages => Images.Count;
        public int NumMipmaps => Images.Count == 0 ? 0 : Images[0].NumMipmaps;
        public int NumLayers => Images.Count == 0 ? 0 : Images[0].NumLayers;

        /// <summary>
        /// true if any image is hdr
        /// </summary>
        public bool IsHdr => Images.Any(imageData => imageData.IsHdr);

        /// width for the biggest mipmap (or 0 if no images are present)
        public int Width => Images.Count != 0 ? GetWidth(0) : 0;

        /// height for the biggest mipmap (or 0 if no images are present)
        public int Height => Images.Count != 0 ? GetHeight(0) : 0;

        /// depth for the biggest mipmap (or 0 if no images are present)
        public int Depth => Images.Count != 0 ? GetDepth(0) : 0;

        public Type ImageType => Images.Count == 0 ? null : Images[0].Image.GetType();

        // helper for many other models
        /// <summary>
        /// previous number of images
        /// </summary>
        public int PrevNumImages { get; protected set; } = 0;

        /// width of the mipmap
        public int GetWidth(int mipmap)
        {
            Debug.Assert(Images.Count != 0);
            Debug.Assert(mipmap < NumMipmaps && mipmap >= 0);
            return dimensions[mipmap].Width;
        }

        /// height of the mipmap
        public int GetHeight(int mipmap)
        {
            Debug.Assert(Images.Count != 0);
            Debug.Assert(mipmap < NumMipmaps && mipmap >= 0);
            return dimensions[mipmap].Height;
        }

        /// depth of the mipmap
        public int GetDepth(int mipmap)
        {
            Debug.Assert(Images.Count != 0);
            Debug.Assert(mipmap < NumMipmaps && mipmap >= 0);
            return dimensions[mipmap].Depth;
        }

        /// returns true if the dimension match with the internal images 
        public bool HasMatchingProperties(ITexture tex)
        {
            if (tex.NumLayers != NumLayers) return false;
            if (tex.NumMipmaps != NumMipmaps) return false;
            if (tex.Width != Width) return false;
            if (tex.Height != Height) return false;
            if (tex.Depth != Depth) return false;
            if (tex.GetType() != ImageType) return false;
            return true;
        }

        public ImagesModel(MitchellNetravaliScaleShader scaleShader)
        {
            this.scaleShader = scaleShader;
            Images = images;
        }

        /// <summary>
        /// tries to add the image to the current collection.
        /// </summary>
        /// <exception cref="ImagesModel.MipmapMismatch">will be thrown if everything except the number of mipmaps matches</exception>
        /// <exception cref="Exception">will be thrown if the images does not match with the current set of images</exception>
        public void AddImage(ITexture image, string name, GliFormat originalFormat)
        {
            if (Images.Count == 0) // first image
            {
                InitDimensions(image);
                images.Add(new ImageData(image, name, originalFormat));
                PrevNumImages = 0;
                OnPropertyChanged(nameof(NumImages));
                OnPropertyChanged(nameof(NumLayers));
                OnPropertyChanged(nameof(NumMipmaps));
                OnPropertyChanged(nameof(IsHdr));
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
                OnPropertyChanged(nameof(Depth));
                OnPropertyChanged(nameof(ImageType));
            }
            else // test if compatible with previous images
            {
                if(image.GetType() != ImageType)
                    throw new Exception($"{name}: Incompatible with internal texture types. Expected {ImageType.Name} but got {image.GetType().Name}");

                if (image.NumLayers != NumLayers)
                    throw new Exception(
                        $"{name}: Inconsistent amount of layers. Expected {NumLayers} got {image.NumLayers}");

                if (image.GetWidth(0) != GetWidth(0) || image.GetHeight(0) != GetHeight(0) ||
                    image.GetDepth(0) != GetDepth(0))
                {
                    if (Depth > 1)
                        throw new Exception(
                            $"{name}: Image resolution mismatch. Expected {GetWidth(0)}x{GetHeight(0)}x{GetDepth(0)} but got {image.GetWidth(0)}x{image.GetHeight(0)}x{image.GetDepth(0)}");

                    throw new Exception(
                        $"{name}: Image resolution mismatch. Expected {GetWidth(0)}x{GetHeight(0)} but got {image.GetWidth(0)}x{image.GetHeight(0)}");
                }


                if (image.NumMipmaps != NumMipmaps)
                    throw new ImagesModel.MipmapMismatch(
                        $"{name}: Inconsistent amount of mipmaps. Expected {NumMipmaps} got {image.NumMipmaps}");

                // remember old properties
                var isHdr = IsHdr;

                images.Add(new ImageData(image, name, originalFormat));
                PrevNumImages = NumImages - 1;
                OnPropertyChanged(nameof(NumImages));

                if (isHdr != IsHdr)
                    OnPropertyChanged(nameof(IsHdr));
            }
        }

        public void DeleteImage(int imageId)
        {
            Debug.Assert(imageId >= 0 && imageId < NumImages);

            // remember old properties
            var isHdr = IsHdr;

            // delete old data
            images[imageId].Dispose();
            images.RemoveAt(imageId);

            PrevNumImages = NumImages + 1;
            OnPropertyChanged(nameof(NumImages));

            if (isHdr != IsHdr)
                OnPropertyChanged(nameof(IsHdr));

            if (NumImages == 0)
            {
                // everything was resettet
                OnPropertyChanged(nameof(NumLayers));
                OnPropertyChanged(nameof(NumMipmaps));
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
                OnPropertyChanged(nameof(Depth));
                OnPropertyChanged(nameof(ImageType));
            }
        }

        /// <summary>
        /// moves the image from index 1 to index 2
        /// </summary>
        /// <param name="idx1">current image index</param>
        /// <param name="idx2">index after moving the image</param>
        public void MoveImage(int idx1, int idx2)
        {
            Debug.Assert(idx1 >= 0);
            Debug.Assert(idx2 >= 0);
            Debug.Assert(idx1 < NumImages);
            Debug.Assert(idx2 < NumImages);
            if (idx1 == idx2) return;

            var i = images[idx1];
            images.RemoveAt(idx1);
            images.Insert(idx2, i);

            OnPropertyChanged(nameof(ImageOrder));
        }

        /// <summary>
        /// generates mipmaps for all images
        /// </summary>
        public void GenerateMipmaps()
        {
            Debug.Assert(NumMipmaps == 1);

            // compute new mipmap levels
            var levels = ComputeMaxMipLevels();
            if (levels == NumMipmaps) return;

            foreach (var image in Images)
            {
                image.GenerateMipmaps(levels);
            }

            // recalc dimensions array
            var w = Width;
            var h = Height;
            dimensions = new Dimension[levels];
            for (int i = 0; i < levels; ++i)
            {
                dimensions[i].Width = w;
                dimensions[i].Height = h;
                w = Math.Max(w / 2, 1);
                h = Math.Max(h / 2, 1);
            }

            OnPropertyChanged(nameof(NumMipmaps));
        }

        public void DeleteMipmaps()
        {
            Debug.Assert(NumMipmaps > 1);
            foreach (var image in Images)
            {
                image.DeleteMipmaps();
            }
            // refresh dimensions array
            var w = Width;
            var h = Height;
            dimensions = new Dimension[1];
            dimensions[0].Width = w;
            dimensions[0].Height = h;

            OnPropertyChanged(nameof(NumMipmaps));
        }

        public static int ComputeMaxMipLevels(int width, int height, int depth)
        {
            return ComputeMaxMipLevels(width, Math.Max(height, depth));
        }

        public static int ComputeMaxMipLevels(int width, int height)
        {
            return ComputeMaxMipLevels(Math.Max(width, height));
        }

        public static int ComputeMaxMipLevels(int width)
        {
            var maxMip = 1;
            while ((width /= 2) > 0) ++maxMip;
            return maxMip;
        }

        /// <summary>
        /// scales all images to the given dimensions
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ScaleImages(int width, int height)
        {
            if (NumImages == 0) return;
            if (Width == width && Height == height) return;
            /*var prevMipmaps = NumMipmaps;

            foreach (var imageData in Images)
            {
                imageData.Scale(width, height, scaleShader);
            }

            InitDimensions(images[0].Image);

            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            if(prevMipmaps != NumMipmaps)
                OnPropertyChanged(nameof(NumMipmaps));*/

            throw new NotImplementedException();
        }

        /// <summary>
        /// computes the maximum amount of mipmap levels for the current width and height
        /// </summary>
        /// <returns></returns>
        private int ComputeMaxMipLevels()
        {
            return ComputeMaxMipLevels(Width, Height);
        }

        protected void InitDimensions(ITexture image)
        {
            dimensions = new Dimension[image.NumMipmaps];
            for (var i = 0; i < image.NumMipmaps; ++i)
            {
                dimensions[i] = new Dimension
                {
                    Width = image.GetWidth(i),
                    Height = image.GetHeight(i),
                    Depth = image.GetDepth(i)
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            foreach (var imageData in Images)
            {
                imageData.Dispose();
            }
        }

        public ITexture CreateEmptyTexture()
        {
            Debug.Assert(images.Count != 0);
            return images[0].Image.Create(NumLayers, NumMipmaps, Width, Height, Depth, Format.R32G32B32A32_Float, true);
        }
    }
}
