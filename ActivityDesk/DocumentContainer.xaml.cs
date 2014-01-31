using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ActivityDesk.Infrastructure;
using ActivityDesk.Viewers;
using ActivityDesk.Visualizer.Definitions;
using ActivityDesk.Visualizer.Visualizations;
using Microsoft.Surface.Presentation.Controls;
using System.Collections.ObjectModel;
using Microsoft.Surface.Presentation.Controls.TouchVisualizations;
using NooSphere.Model;
using NooSphere.Model.Device;
using Note = ActivityDesk.Viewers.Note;

namespace ActivityDesk
{
    public class Intersection
    {
        public Device Device { get; set; }
        public Resource Resource { get; set; }
    }

    public partial class DocumentContainer
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
        private int _borderSize;
        private int _iconSize = 100;


        private readonly List<string> _lockedTags = new List<string>();
        public Collection<Note> Notes = new Collection<Note>();
        public Collection<ScatterViewItem> ResourceViewers = new Collection<ScatterViewItem>();
        public Dictionary<string, DeviceTumbnail> Devices = new Dictionary<string, DeviceTumbnail>();

        public event EventHandler<Intersection> IntersectionDetected = delegate { }; 

        public DocumentContainer()
        {

            InitializeComponent();

            InitializeTags();
            var metadata = new FrameworkPropertyMetadata(DockStates.Floating);
            DockState = DependencyProperty.RegisterAttached("DockState",
                                                                typeof(DockStates),
                                                                typeof(DocumentContainer), metadata);

            TouchVisualizer.SetShowsVisualizations(this, false);
            SizeChanged += DocumentContainer_SizeChanged;
        }
        private void InitializeTags()
        {

            Visualizer.Definitions.Add(
                new SmartPhoneDefinition
                {
                    Source = new Uri("Visualizer/Visualizations/SmartPhone.xaml", UriKind.Relative),
                    TagRemovedBehavior = TagRemovedBehavior.Disappear,
                    LostTagTimeout = 1000
                });
            Visualizer.Definitions.Add(
                new TabletDefinition
                {
                    Source = new Uri("Visualizer/Visualizations/VisualizationTablet.xaml", UriKind.Relative),
                    LostTagTimeout = 1000
                }
                );
        }
        private void Visualizer_VisualizationAdded(object sender, TagVisualizerEventArgs e)
        {
            if (!_lockedTags.Contains(e.TagVisualization.VisualizedTag.Value.ToString()))
            {
            }
            else
            {
                RemoveDevice(e.TagVisualization.VisualizedTag.Value.ToString());
            }
            ((BaseVisualization)e.TagVisualization).Locked += new LockedEventHandler(Desk_Locked);
        }
        private void Desk_Locked(object sender, LockedEventArgs e)
        {
            if (_lockedTags.Contains(e.VisualizedTag))
            {
                _lockedTags.Remove(e.VisualizedTag);
                RemoveDevice(e.VisualizedTag);
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



                Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
                {
                    //documentContainer.Clear();
                }));
            }
            else AddDevice(new Device(){TagValue=e.TagVisualization.VisualizedTag.Value},e.TagVisualization.Center);

        }
        private void Visualizer_VisualizationMoved(object sender, TagVisualizerEventArgs e)
        {

        }
        void DocumentContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _dockSize = 65;
            _borderSize = 20;
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
            view.Items.Clear();
        }
        public void AddNote()
        {
            var ink = new Note {Center = new Point(450, 450)};
            ink.Close += new EventHandler(ink_Close);
            Notes.Add(ink);
            this.Add(ink);
        }

        public void AddResource(LoadedResource resource)
        {
            var res = new ResourceViewer(resource);
            ResourceViewers.Add(res);
            var rnd = new Random();
            res.Center = new Point(rnd.Next(200, 1000), rnd.Next(200, 800));
            Add(res);


        }
        public void AddPdf(Image img,Image thumb)
        {
            var res = new PdfViewer(img,thumb) { Width = img.Width, Height = img.Height };
            ResourceViewers.Add(res);
            Add(res);
        }
        public void AddWindow(LoadedResource resource)
        {
            var res = new TouchWindow(resource);
            ResourceViewers.Add(res);
            res.Width = 1000;
            res.Height = 600;
            Add(res);
        }
        public void AddDevice(Device device,Point position)
        {
            var dev = new DeviceTumbnail(device) {Center = position};
            dev.ResourceReleased += dev_ResourceReleased;
            dev.Closed += dev_Closed;
            Devices.Add(device.TagValue.ToString(),dev);
            Add(dev);

            dev.CanScale = false;
        }

        void dev_ResourceReleased(object sender, LoadedResource e)
        {
            AddResource(e);
        }
        void dev_Closed(object sender, Device dev)
        {
            
            //RemoveDevice(dev.TagValue.ToString());
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
            element.Loaded += element_Loaded;
            element.Template = (ControlTemplate)element.FindResource("Floating");
            element.Orientation = 0;
            element.SizeChanged += element_SizeChanged;
            view.Items.Add(element);
        }

        void element_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DeviceTumbnail)
                return;
            var element = sender as ScatterViewItem;
            if (element == null) return;

            element.Template = (ControlTemplate)element.FindResource("Docked");
            ((IResourceContainer)element).Iconized = true;
        }
        void element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var item = sender as ScatterViewItem;
            if (item == null) return;

            if (e.NewSize.Width < 150 || e.NewSize.Height < 150)
            {
                item.Template = (ControlTemplate)item.FindResource("Docked");
                ((IResourceContainer) item).Iconized = true;
            }
            else
            {
                item.Template = (ControlTemplate)item.FindResource("Floating");
                ((IResourceContainer)item).Iconized = false;
            }
        }
        private void element_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            HandleDockingFromTouch((ScatterViewItem)sender,e.GetTouchPoint(view).Position);
            CheckIntersections((ScatterViewItem) sender);
        }
        private void CheckIntersections(ScatterViewItem item)
        {
            if (!((IResourceContainer)item).Iconized) 
                return;

            foreach (var dev in Devices.Values)
            {

                var rectItem = new Rect(
                    new Point(item.Center.X - _iconSize / 2, item.Center.Y - _iconSize / 2),
                    new Size(_iconSize, _iconSize));

                var rectDev = new Rect(
                    new Point(dev.Center.X - dev.ActualWidth / 2 + 80, dev.Center.Y - dev.ActualHeight / 2 + 30),
                    new Size(dev.ActualWidth-50, dev.ActualHeight-50));

                bool result = rectItem.IntersectsWith(rectDev);
                if(result)
                {
                    if (IntersectionDetected != null)
                        IntersectionDetected(this, new Intersection() { Device = dev.Device, Resource = ((IResourceContainer)item).Resource.Resource });

                    dev.Resource = ((IResourceContainer) item).Resource ;

                    Remove(item);
                }
            }
        }
        private void HandleDockingFromTouch(ScatterViewItem item,Point p)
        {
            var previousdockState = GetDockState(item);
            if (!(item.Width <= _minimumDockSize.Width) || !(item.Height <= _minimumDockSize.Height)) return;
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
                if (previousdockState == DockStates.Floating) 
                    return;
                if (((IResourceContainer)item).Iconized) item.Template = (ControlTemplate)item.FindResource("Docked");
                else item.Template = (ControlTemplate)item.FindResource("Floating");
                SetDockState(item, DockStates.Floating);
            }
            UpdateDock(item);
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
                {
                    var itemY = item.Center.Y;
                    if (itemY < _borderSize)
                        itemY = _borderSize;
                    item.Center = new Point(_rightDockX, itemY);
                    break;
                }
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
            view.Items.Remove(element);
        }
        public void RemoveDevice(string p)
        {
            if (!Devices.ContainsKey(p)) return;
            view.Items.Remove(Devices[p]);
            Devices.Remove(p);
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
