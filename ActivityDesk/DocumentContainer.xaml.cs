using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ActivityDesk.Windowing;
using Microsoft.Surface.Presentation.Controls;
using System.Collections.ObjectModel;
using NooSphere.Model.Device;
using UserControl = System.Windows.Controls.UserControl;

namespace ActivityDesk
{
    /// <summary>
    /// Interaction logic for DocumentView.xaml
    /// </summary>
    public partial class DocumentContainer : UserControl
    {
        #region Image dependency property


        public static DependencyProperty DockState;

        public static DockStates GetDockState(DependencyObject obj)
        {
          return (DockStates) obj.GetValue(DockState);
        }
        public static void SetDockState(DependencyObject obj, DockStates value)
        {
          obj.SetValue(DockState, value);
        }

        #endregion

        private int _rightDockX = 1820;
        private int _leftDockX = 100;
        private int _upperDockY = 100;
        private int _upperDockThreshold = 100;
        private int _leftDockTreshhold = 100;
        private int _rightDockTreshhold = 1820;
        private int _dockSize;
        private Size _minimumDockSize;

        public Collection<Note> Notes = new Collection<Note>();
        public Collection<ScatterViewItem> ResourceViewers = new Collection<ScatterViewItem>();
        public Dictionary<string, DeviceTumbnail> Devices = new Dictionary<string, DeviceTumbnail>();

        public DocumentContainer()
        {
            InitializeComponent();

            //register dockstate dependency property
            var metadata = new FrameworkPropertyMetadata(DockStates.Floating);
            DockState = DependencyProperty.RegisterAttached("DockState",
                                                                typeof(DockStates),
                                                                typeof(DocumentContainer), metadata);

            SizeChanged += DocumentContainer_SizeChanged;
        }

        void DocumentContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _dockSize = 65;
            _rightDockX = (int)(ActualWidth - _dockSize);
            _rightDockTreshhold = (int) (ActualWidth - _dockSize);

            _leftDockX = _dockSize;
            _leftDockTreshhold = _dockSize;

            _upperDockY = _dockSize;
            _upperDockThreshold = _dockSize;

            _minimumDockSize = new Size(ActualWidth/1.3, ActualHeight/1.3);
        }
        public void Clear()
        {
            this.view.Items.Clear();
        }
        public void AddNote()
        {
            var ink = new Note {Center = new Point(450, 450)};
            ink.Close += new EventHandler(ink_Close);
            Notes.Add(ink);
            this.Add(ink);
        }
        public void AddResource(Image img,string text)
        {
            var res = new ResourceViewer(img, text) { Width = img.Width, Height = img.Height };
            ResourceViewers.Add(res);
            Add(res);
        }

        public void AddPdf(Image img,Image thumb)
        {
            var res = new PdfViewer(img,thumb) { Width = img.Width, Height = img.Height };
            ResourceViewers.Add(res);
            Add(res);
        }
        public void AddWindow(FrameworkElement content, Image thumbnail,string title)
        {
            var res = new TouchWindow(content, thumbnail,title);
            ResourceViewers.Add(res);
            Add(res);
        }
        public void AddDevice(Device device,Point position)
        {
            var dev = new DeviceTumbnail {Name = device.Name, Center = position};
            Devices.Add(device.TagValue.ToString(),dev);
            this.Add(dev);
        }
        public void UpdateDevice(Device device)
        {
            Devices[device.TagValue.ToString()].Name = device.Name;
        }
        private void ink_Close(object sender, EventArgs e)
        {

        }
        private void Add(ScatterViewItem element)
        {
            SetDockState(element, DockStates.Floating);
            element.AddHandler(ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(element_ManipulationDelta), true);
            element.PreviewTouchMove += element_PreviewTouchMove;
            element.Template = (ControlTemplate)element.FindResource("Floating");
            element.Orientation = 0;
            element.CanRotate = false;
            view.Items.Add(element);
            element.Width = 1000;
            element.Height = 600;
        }
        private void element_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            HandleDockingFromTouch((ScatterViewItem)sender,e.GetTouchPoint(view).Position);
        }
        private void HandleDockingFromTouch(ScatterViewItem item,Point p)
        {

            if (item.Width <= _minimumDockSize.Width && item.Height <= _minimumDockSize.Height)
            {
                if (p.X < _leftDockTreshhold)
                {
                    item.Template = (ControlTemplate) item.FindResource("Docked");
                    SetDockState(item, DockStates.Left);
                }
                else if (p.X > _rightDockTreshhold)
                {
                    item.Template = (ControlTemplate) item.FindResource("Docked");
                    SetDockState(item, DockStates.Right);
                }
                else if (p.Y < _upperDockThreshold)
                {
                    item.Template = (ControlTemplate) item.FindResource("Docked");
                    SetDockState(item, DockStates.Top);
                }
                else
                {
                    item.Template = (ControlTemplate) item.FindResource("Floating");
                    SetDockState(item, DockStates.Floating);
                }
                UpdateDock(item);
            }
        }
        private void HandleDocking(ScatterViewItem item)
        {
            HandleDockingFromTouch(item,item.Center);
        }
        private void UpdateDock(ScatterViewItem item)
        {
            var state = GetDockState(item);

            switch (state)
            {
                case DockStates.Left:
                    item.Center = new Point(_leftDockX, item.Center.Y);
                    break;
                case DockStates.Right:
                    item.Center = new Point(_rightDockX, item.Center.Y);
                    break;
                case DockStates.Top:
                    item.Center = new Point(item.Center.X, _upperDockY);
                    break;
            }
            item.Orientation = 0;
        }
        void element_ManipulationDelta(object sender, ManipulationCompletedEventArgs e)
        {
            HandleDocking((ScatterViewItem)sender);
        }

        public void Remove(object element)
        {
            this.view.Items.Remove(element);
        }

        public void RemoveDevice(string p)
        {
            if(Devices.ContainsKey(p))
            {
                view.Items.Remove(Devices[p]);
                Devices.Remove(p);
            }
        }
    }
    public enum DockStates
    {
        Left,
        Right,
        Floating,
        Top
    }
}
