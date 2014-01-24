using System;
using System.Threading.Tasks;
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

using NooSphere.Model;
using NooSphere.Model.Device;

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

            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;

            _deskManager = new DeskManager();
            _deskManager.Start(_documentContainer);

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

        #region Events


        private void Visualizer_VisualizationAdded(object sender, TagVisualizerEventArgs e)
        {

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
            
        }
        bool fileExists = false;

        private void SurfaceWindow_PreviewTouchDown(object sender, TouchEventArgs e)
        {
        }

        private async void BtnNote_OnClick_(object sender, RoutedEventArgs e)
        {
             var result = await GetImages();
             Dispatcher.Invoke(() => _documentContainer.AddWindow(new Image { Source = result.Item1 }, new Image { Source = result.Item2 }, "Paper"));

        }

        private async Task<Tuple<BitmapImage, BitmapImage>> GetImages()
        {
            BitmapImage img = null, imgThumb=null;
            await Task.Factory.StartNew(() =>
            {
                img = PdfConverter.ConvertPdfToImage(@"c:\cam.pdf");
                imgThumb = PdfConverter.ConvertPdfThumbnail(@"c:\cam.pdf");
            });

            return Tuple.Create( img, imgThumb);
        }
    }
}