using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ActivityDesk.Helper.Pdf;
using ActivityDesk.Infrastructure;
using System.Windows.Media.Imaging;
using System.Windows.Input;

using NooSphere.Model.Device;

namespace ActivityDesk
{
    public partial class Desk
    {
        #region Members 
        private DeskState _deskState;
        private Device _device;
        private readonly List<string> _connectedDeviceTags = new List<string>();
        private readonly DocumentContainer _documentContainer = new DocumentContainer();
        private DeskManager _deskManager;

        #endregion

        #region Constructor

        public Desk()
        {
            InitializeComponent();

            Title = "deskv1";

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

        private void SetDeskState(DeskState deskState)
        {
            _deskState = deskState;
        }
        #endregion

        #region Initializers
        
        #endregion

        #region Events


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
             //var result = await GetImages();
             //Dispatcher.Invoke(() => _documentContainer.AddWindow(new Image { Source = result.Item1 }, new Image { Source = result.Item2 }, "Paper","PDF"));

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