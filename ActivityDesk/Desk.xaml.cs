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

        private readonly DocumentContainer _documentContainer = new DocumentContainer();
        private DeskManager _deskManager;

        #region Constructor

        public Desk()
        {
            InitializeComponent();

            Title = "deskv1";

            documentViewContainer.Children.Add(_documentContainer);

            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;

            _deskManager = new DeskManager();
            _deskManager.Start(_documentContainer);

        }


        #endregion

        #region Initializers

        #endregion

        #region Events


        #endregion
    }
}