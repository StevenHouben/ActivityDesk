using System.Windows;
using ActivityDesk.Infrastructure;

namespace ActivityDesk
{
    public partial class Desk
    {
        public Desk()
        {
            InitializeComponent();

            Title = "deskv1";

            var documentContainer = new DocumentContainer();
            documentViewContainer.Children.Add(documentContainer);

            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;

            var deskManager = new DeskManager();
            deskManager.Start(documentContainer);

        }
    }
}