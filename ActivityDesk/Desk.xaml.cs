/****************************************************************************
 (c) 2012 Steven Houben(shou@itu.dk) and Søren Nielsen(snielsen@itu.dk)

 Pervasive Interaction Technology Laboratory (pIT lab)
 IT University of Copenhagen

 This library is free software; you can redistribute it and/or 
 modify it under the terms of the GNU GENERAL PUBLIC LICENSE V3 or later, 
 as published by the Free Software Foundation. Check 
 http://www.gnu.org/licenses/gpl.html for details.
****************************************************************************/

using System;
using System.Windows;
using ActivityDesk.Helper.Pdf;
using ActivityDesk.Infrastructure;
using Microsoft.Surface.Presentation.Controls;
using System.Windows.Media;
using Microsoft.Surface.Presentation.Controls.TouchVisualizations;
using System.Threading;
using ActivityDesk.Visualizer.Definitions;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using ActivityDesk.Visualizer.Visualization;
using System.Windows.Input;

using ABC.Model;
using ABC.Model.Device;

namespace ActivityDesk
{
    public partial class Desk
    {
        #region Members 
        private DeskState _deskState;
        private Device _device;
        private readonly List<string> _lockedTags = new List<string>();
        private readonly List<string> _connectedDeviceTags = new List<string>();
        private readonly DocumentContainer _documentContainer = new DocumentContainer();
        private DeskManager _deskManager;

        #endregion

        #region Constructor

        public Desk()
        {
            InitializeComponent();

            TouchVisualizer.SetShowsVisualizations(_documentContainer, false);

            InitializeTags();

            documentViewContainer.Children.Add(_documentContainer);

            SetDeskState(DeskState.Ready);

            _device = new Device
            {
                DevicePortability = DevicePortability.Stationary,
                DeviceRole = DeviceRole.Mediator,
                DeviceType = DeviceType.Tabletop,
                Name = "Surface"
            };

            WindowStyle = WindowStyle.ThreeDBorderWindow;
            WindowState = WindowState.Minimized;

            _deskManager = new DeskManager();
            _deskManager.Start();

        }
        public BitmapSource ToBitmapSource(System.Drawing.Bitmap source)
        {
            BitmapSource bitSrc = null;

            var hBitmap = source.GetHbitmap();

            try
            {
                bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Win32Exception)
            {
                bitSrc = null;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }

            return bitSrc;
        }
        internal static class NativeMethods
        {
            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeleteObject(IntPtr hObject);
        }

        private void SetDeskState(DeskState deskState)
        {
            _deskState = deskState;

            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                Background = (ImageBrush)Resources["back"];
            }));
        }
        #endregion

        #region Initializers
        private void InitializeTags()
        {

            Visualizer.Definitions.Add(
                new SmartPhoneDefinition()
                {
                    Source = new Uri("Visualizer/Visualizations/SmartPhone.xaml", UriKind.Relative),
                    TagRemovedBehavior = TagRemovedBehavior.Disappear,
                    LostTagTimeout = 1000
                });
            Visualizer.Definitions.Add( 
                new TabletDefinition()
                {
                    Source = new Uri("Visualizer/Visualizations/VisualizationTablet.xaml", UriKind.Relative),
                    LostTagTimeout = 1000
                }
                );
        }
        #endregion

        #region UI
        private void VisualizeResouce(LegacyResource res, string path)
        {
            try
            {
                var image = new Image();
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(path, UriKind.Relative);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                image.Source = bitmapImage;
                image.Stretch = Stretch.Uniform;

                _documentContainer.AddResource(image,res.Name);
            }
            catch { }
        }

        #endregion

        #region Events


        private void Visualizer_VisualizationAdded(object sender, TagVisualizerEventArgs e)
        {
            if (lblStart.Visibility == Visibility.Visible)
                lblStart.Visibility = Visibility.Hidden;

            if (!_lockedTags.Contains(e.TagVisualization.VisualizedTag.Value.ToString()))
            {
                if (Visualizer.ActiveVisualizations.Count > 0)
                {
                    SetDeskState(DeskState.Active);
                }
            }
            else
            {
                _documentContainer.RemoveDevice(e.TagVisualization.VisualizedTag.Value.ToString());
            }
            ((BaseVisualization)e.TagVisualization).Locked += new LockedEventHandler(Desk_Locked);
        }
        private void Desk_Locked(object sender, LockedEventArgs e)
        {
            if (_lockedTags.Contains(e.VisualizedTag))
            {
                _lockedTags.Remove(e.VisualizedTag);
                _documentContainer.RemoveDevice(e.VisualizedTag);
            }
            else
            {
                _lockedTags.Add(e.VisualizedTag);
            }
        }

        private void Visualizer_VisualizationRemoved(object sender, TagVisualizerEventArgs e)
        {
            if (!_lockedTags.Contains(e.TagVisualization.VisualizedTag.Value.ToString()))
            {
                Thread.Sleep(3000);
                if (Visualizer.ActiveVisualizations.Count == 0)
                {
                    SetDeskState(DeskState.Ready);
                    lblStart.Visibility = Visibility.Visible;
                }


                Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
                {
                    //documentContainer.Clear();
                }));
            }
            else
                _documentContainer.AddDevice(new Device(){TagValue=e.TagVisualization.VisualizedTag.Value},e.TagVisualization.Center);

        }
        private void Visualizer_VisualizationMoved(object sender, TagVisualizerEventArgs e)
        {

        }
        #endregion
        private void button1_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            //if (!fileExists)
            //{
            //    _deskManager.AddAttachment();
            //    fileExists = true;
            //}

            //else
            //{
            //    var image = new Image();
            //    using (var stream = _deskManager.GetAttachment())
            //    {
            //        var bitmap = new BitmapImage();
            //        bitmap.BeginInit();
            //        bitmap.StreamSource = stream;
            //        bitmap.CacheOption = BitmapCacheOption.OnLoad;
            //        bitmap.EndInit();
            //        bitmap.Freeze();
            //        image.Source = bitmap;
            //    }
            //    _documentContainer.AddResource(image,"");
            //}
        }
        bool fileExists = false;

        private void SurfaceWindow_PreviewTouchDown(object sender, TouchEventArgs e)
        {
        }

        private void BtnNote_OnClick_(object sender, RoutedEventArgs e)
        {
            var img = PdfConverter.ConvertPdfToImage(@"c:\cam.pdf");
            var imgThumb = PdfConverter.ConvertPdfThumbnail(@"c:\cam.pdf");
            _documentContainer.AddPdf(img,imgThumb);
            
        }
    }
}