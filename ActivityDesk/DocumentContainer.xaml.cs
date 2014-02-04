using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
    public class ResourceHandle
    {
        public Device Device { get; set; }
        public Resource Resource { get; set; }
    }

    public partial class DocumentContainer
    {

        public event EventHandler<string> DeviceValueAdded= delegate { };
        public event EventHandler<string> DeviceValueRemoved = delegate { };
        public event EventHandler<ResourceHandle> ResourceHandled = delegate { };
        public event EventHandler<ResourceHandle> ResourceHandleReleased = delegate { };

        #region Dock dependency property


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


        public Collection<Note> Notes = new Collection<Note>();
        public Collection<ScatterViewItem> ResourceViewers = new Collection<ScatterViewItem>();

        public Dictionary<string, DeviceContainer> DeviceContainers = new Dictionary<string, DeviceContainer>(); 


        public void ValidateDevice(string value, Device device)
        {
            DeviceContainers[value].Connected = true;
            DeviceContainers[value].Device = device;
        }

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
                    TagRemovedBehavior = TagRemovedBehavior.Fade,
                    LostTagTimeout = 1000
                });
        }
        private void Visualizer_VisualizationAdded(object sender, TagVisualizerEventArgs e)
        {

            var tagValue = e.TagVisualization.VisualizedTag.Value.ToString();

            if (!DeviceContainers.ContainsKey(tagValue))
            {
                var dev = new DeviceContainer
                {
                    DeviceVisualization = e.TagVisualization as VisualizationTablet
                };
                dev.ResourceReleased += dev_ResourceReleased;
                DeviceContainers.Add(tagValue,dev);
            }
            else
            {
                var container = DeviceContainers[tagValue];

                if (container.VisualStyle != DeviceVisual.Thumbnail)
                {
                    Console.WriteLine("Error: double detection. Debug problem?");
                    return;
                }

                container.VisualStyle = DeviceVisual.Visualisation;
                container.DeviceVisualization = e.TagVisualization as VisualizationTablet;


                foreach (var res in container.DeviceThumbnail.LoadedResources)
                {
                    if (container.DeviceVisualization != null) 
                        container.DeviceVisualization.AddResource(res);
                }
                RemoveDevice(tagValue);

                container.Invalidate();
            }
            ((BaseVisualization)e.TagVisualization).Locked += Device_Locked;
            var vis = e.TagVisualization as VisualizationTablet;
            //if (vis != null)
            //    vis.ResourceReleased += dev_ResourceReleased;

            if (DeviceValueAdded != null)
                DeviceValueAdded(this, tagValue);

        }

        void dev_ResourceReleased(Device sender, ResourceReleasedEventArgs e)
        {
            AddResourceAtLocation(e.LoadedResource, new Point(e.Position.X, e.Position.Y));

           if(ResourceHandleReleased !=null)
               ResourceHandleReleased(this, new ResourceHandle{Device = e.Device,Resource = e.LoadedResource.Resource});
        }
        private void Device_Locked(object sender, LockedEventArgs e)
        {
            var container = DeviceContainers[e.VisualizedTag];

            if (container.Pinned)
            {
                container.Pinned = false;
                if (container.VisualStyle != DeviceVisual.Thumbnail) return;
                foreach (var res in container.DeviceThumbnail.LoadedResources)
                    AddResource(res);
                RemoveDevice(e.VisualizedTag);
                DeviceContainers.Remove(e.VisualizedTag);
            }
            else
                container.Pinned = true;
        }
        private void Visualizer_VisualizationRemoved(object sender, TagVisualizerEventArgs e)
        {
            var tagValue = e.TagVisualization.VisualizedTag.Value.ToString();
            if (!DeviceContainers.ContainsKey(tagValue))
                return;

            var container = DeviceContainers[tagValue];
            if (container.Pinned)
            {
                container.VisualStyle = DeviceVisual.Thumbnail;
                container.DeviceThumbnail = AddDevice(tagValue,
                    e.TagVisualization.Center);
                container.Invalidate();

                foreach (var res in container.DeviceVisualization.LoadedResources)
                    container.DeviceThumbnail.AddResource(res);
            }
            else
            {
                foreach (var res in container.DeviceVisualization.LoadedResources)
                    AddResource(res);

                DeviceContainers.Remove(tagValue);

                if (DeviceValueRemoved != null)
                    DeviceValueRemoved(this, tagValue);

            }
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
            Add(ink);
        }

        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock(syncLock) { // synchronize
                return random.Next(min, max);
            }
        }
        public void AddResource(LoadedResource resource)
        {
            var res = new ResourceViewer(resource);
            ResourceViewers.Add(res);
            res.Center = new Point(RandomNumber(200, 1000), RandomNumber(200, 800));
            Add(res);

        }
        public void AddResourceAtLocation(LoadedResource resource,Point p)
        {
            var res = new ResourceViewer(resource);

            var stb = new Storyboard();

            var moveCenter = new PointAnimation
            {
                FillBehavior = FillBehavior.Stop,
                From = new Point(p.X + 50, p.Y+50),
                To = new Point(p.X + 50, p.Y + 200),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
            };

            stb.Children.Add(moveCenter);
            Storyboard.SetTarget(moveCenter, res);
            Storyboard.SetTargetProperty(moveCenter, new PropertyPath(ScatterViewItem.CenterProperty));
            Add(res);

            stb.Completed += (sender, e) =>
            {
                ResourceViewers.Add(res);
                res.Center = (Point)moveCenter.To;
            };

            stb.Begin(this);

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
        public DeviceThumbnail AddDevice(string tagValue,Point position)
        {
            var dev = new DeviceThumbnail(tagValue) { Center = position };
            dev.Closed += dev_Closed;
            Add(dev);

            dev.CanScale = false;
            dev.CanRotate = true;

            return dev;
        }
        void dev_Closed(object sender, string dev)
        {
            if (!DeviceContainers.ContainsKey(dev)) return;
            var container = DeviceContainers[dev];

            container.Pinned = false;
            if (container.VisualStyle != DeviceVisual.Thumbnail) return;
            foreach (var res in container.DeviceThumbnail.LoadedResources)
                AddResource(res);
            RemoveDevice(dev);
            DeviceContainers.Remove(dev);
        }
        public void UpdateDevice(Device device)
        {
            //Devices[device.TagValue.ToString()].Name = device.Name;
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
            if (sender is DeviceThumbnail)
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

        public object resourcesendlock = new object();
        private bool check = true;
        private void CheckIntersections(ScatterViewItem item)
        {
            if (!((IResourceContainer)item).Iconized) 
                return;

            foreach (var dev in DeviceContainers.Values)
            {
                if (!dev.Connected) 
                    return;

                var rectItem = new Rect(
                    new Point(item.Center.X - _iconSize / 2, item.Center.Y - _iconSize / 2),
                    new Size(_iconSize, _iconSize));

                var rectDev = new Rect();

                switch (dev.VisualStyle)
                {
                    case DeviceVisual.Thumbnail:
                        rectDev = new Rect(
                                new Point(
                                    dev.DeviceThumbnail.Center.X - dev.DeviceThumbnail.ActualWidth / 2 + 80, 
                                    dev.DeviceThumbnail.Center.Y - dev.DeviceThumbnail.ActualHeight / 2 + 30),
                                new Size(
                                    dev.DeviceThumbnail.ActualWidth - 50, 
                                    dev.DeviceThumbnail.ActualHeight - 50));
                        break;
                    case DeviceVisual.Visualisation:
                        rectDev = new Rect(
                                new Point(
                                    dev.DeviceVisualization.Center.X - dev.DeviceVisualization.ActualWidth / 2 + 80,
                                    dev.DeviceVisualization.Center.Y - dev.DeviceVisualization.ActualHeight / 2 + 30),
                                new Size(
                                    dev.DeviceVisualization.ActualWidth - 50,
                                    dev.DeviceVisualization.ActualHeight - 50));
                        break;
                }

                bool result = rectItem.IntersectsWith(rectDev);
                if(result)
                {
                    if (InProgress.ContainsKey(item))
                    {
                        InProgress.Remove(item);
                        return;
                    }

                    InProgress.Add(item,true);
                    if (ResourceHandled != null)
                        ResourceHandled(this,
                            new ResourceHandle()
                            {
                                Device = dev.Device,
                                Resource = ((IResourceContainer) item).Resource.Resource
                            });
                    switch (dev.VisualStyle)
                    {
                        case DeviceVisual.Thumbnail:
                             dev.DeviceThumbnail.AddResource(((IResourceContainer)item).Resource);
                            break;
                        case DeviceVisual.Visualisation:
                            dev.DeviceVisualization.AddResource(((IResourceContainer)item).Resource);
                            break;
                    }

                    Remove(item);

                }
            }
        }
        private Dictionary<ScatterViewItem, bool> InProgress = new Dictionary<ScatterViewItem, bool>(); 
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
            if (!DeviceContainers.ContainsKey(p)) return;
            view.Items.Remove(DeviceContainers[p].DeviceThumbnail);
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
