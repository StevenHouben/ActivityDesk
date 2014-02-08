using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ActivityDesk.Infrastructure;
using ActivityDesk.Viewers;
using ActivityDesk.Visualizer.Definitions;
using ActivityDesk.Visualizer.Visualizations;
using Blake.NUI.WPF.Gestures;
using Microsoft.Surface.Presentation.Controls;
using System.Collections.ObjectModel;
using Microsoft.Surface.Presentation.Controls.TouchVisualizations;
using Microsoft.Surface.Presentation.Input;
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

        private static readonly Random Random = new Random();
        private static readonly object SyncLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock (SyncLock)
            { // synchronize
                return Random.Next(min, max);
            }
        }

        public static DependencyProperty DockState;
        public static DockStates GetDockState(DependencyObject obj)
        {
            return (DockStates)obj.GetValue(DockState);
        }
        public static void SetDockState(DependencyObject obj, DockStates value)
        {
            obj.SetValue(DockState, value);
        }

        public event EventHandler<string> DeviceValueAdded = delegate { };
        public event EventHandler<string> DeviceValueRemoved = delegate { };
        public event EventHandler<ResourceHandle> ResourceHandled = delegate { };
        public event EventHandler<ResourceHandle> ResourceHandleReleased = delegate { };

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

            Events.RegisterGestureEventSupport(this);

            InitializeTags();
            var metadata = new FrameworkPropertyMetadata(DockStates.Floating);
            DockState = DependencyProperty.RegisterAttached("DockState",
                typeof (DockStates),
                typeof (DocumentContainer), metadata);

            TouchVisualizer.SetShowsVisualizations(this, false);
            SizeChanged += DocumentContainer_SizeChanged;
        }

        internal void Build(Dictionary<string,LoadedResource> resources, DeskConfiguration configuration)
        {
            Clear();

            foreach (var dev in DeviceContainers.Values)
            {
                ResourceHandleReleased(this, new ResourceHandle()
                {
                    Device = dev.Device,
                    Resource = null
                });
            }

            if (configuration == null)
                foreach (var res in resources.Values)
                {
                   AddResource(res,true);
                }
            else
                BuildFromConfiguration(resources, configuration);
        }

        void BuildFromConfiguration(Dictionary<string, LoadedResource> resources, DeskConfiguration configuration)
        {
            foreach (var dev in configuration.DeviceConfigurations)
            {
                // Device exists
                if (DeviceContainers.ContainsKey(dev.TagValue))
                {
                    var container = DeviceContainers[dev.TagValue];

                    //Device is connected
                    if (container.Connected)
                    {
                        
                        //device is stored as thumbnail
                        if (dev.Thumbnail)
                        {
                         
                            //actual device is thumbnail
                            if (container.VisualStyle == DeviceVisual.Thumbnail)
                            {
                                container.DeviceThumbnail.Center = dev.Center;
                                HandleDockingFromTouch(container.DeviceThumbnail, dev.Center);
                                container.Pinned = dev.Pinned;
                                container.DeviceThumbnail.Orientation = dev.Orientation;

                                foreach (
                                    var def in dev.Configurations.Select(res => res as DefaultResourceConfiguration))
                                {
                                    container.DeviceThumbnail.AddResource(resources[def.Resource.Id]);
                                    ResourceHandled(this, new ResourceHandle()
                                    {
                                        Device = container.Device,
                                        Resource = def.Resource
                                    });
                                }
                            }
                            else
                            {
                                foreach (var def in dev.Configurations.Select(res => res as DefaultResourceConfiguration))
                                {
                                    container.DeviceVisualization.AddResource(resources[def.Resource.Id]);
                                    ResourceHandled(this, new ResourceHandle()
                                    {
                                        Device = container.Device,
                                        Resource = def.Resource
                                    });                           
                                }
                            }
                        }
                        //device is not stored as thumbnail
                        else
                        {
                            foreach (var def in dev.Configurations.Select(res => res as DefaultResourceConfiguration))
                            {
                                container.DeviceVisualization.AddResource(resources[def.Resource.Id]);
                                ResourceHandled(this, new ResourceHandle()
                                {
                                    Device = container.Device,
                                    Resource = def.Resource
                                });
                            }
                        }
                    }
                    //Device is not connected
                    else
                    {
                        foreach (var def in dev.Configurations.Select(res => res as DefaultResourceConfiguration))
                            AddResource(resources[def.Resource.Id],true);
                    }
                }
                //Device is not found
                else
                {
                    foreach (var def in dev.Configurations.Select(res => res as DefaultResourceConfiguration))
                    {
                        AddResource(resources[def.Resource.Id],true);
                    }
                }
            }

            //handle all other resources
            foreach (var deskConfig in configuration.Configurations.Select(config => config as DeskResourceConfiguration))
            {
                if (deskConfig == null)
                    return;

                 var viewer = AddResourceAtLocation(resources[deskConfig.Resource.Id], deskConfig.Center);

                 ((IResourceContainer)viewer).Iconized = deskConfig.Iconized;
                    viewer.Width = deskConfig.Size.Width;
                    viewer.Height = deskConfig.Size.Height;
                viewer.Orientation = deskConfig.Orientation;
            }
        }

        internal DeskConfiguration GetDeskConfiguration()
        {

            var deskConfiguration = new DeskConfiguration();

            //Handle all resources on the desk

            foreach (var resConfig in ResourceViewers.Select(item => new DeskResourceConfiguration()
            {
                Center = item.Center,
                DockState = GetDockState(item),
                Resource = ((IResourceContainer) item).Resource.Resource,
                Iconized = ((IResourceContainer) item).Iconized,
                Orientation = item.Orientation,
                Size = new Size(item.Width,item.Height)
            }))
            {
                deskConfiguration.Configurations.Add(resConfig);
            }

            foreach (var dev in DeviceContainers.Values)
            {
                var deviceConfig = new DeviceConfiguration
                {
                    Device = dev.Device,
                    Pinned = dev.Pinned,
                    TagValue = dev.TagValue
                };

                if (dev.VisualStyle == DeviceVisual.Thumbnail)
                {
                    deviceConfig.Center = dev.DeviceThumbnail.Center;
                    deviceConfig.Orientation = dev.DeviceThumbnail.Orientation;
                    switch(GetDockState(dev.DeviceThumbnail) )
                    {
                        case DockStates.Left:
                            deviceConfig.Center = new Point(-10, deviceConfig.Center.Y);
                            break;
                        case DockStates.Right:
                            deviceConfig.Center = new Point(2000,deviceConfig.Center.Y);
                            break;
                        case DockStates.Top:
                            deviceConfig.Center = new Point( deviceConfig.Center.X,-10);
                            break;

                    }
                    deviceConfig.Thumbnail = true;

                    foreach (var res in dev.DeviceThumbnail.LoadedResources)
                    {
                        deviceConfig.Configurations.Add(new DefaultResourceConfiguration() { Resource = res.Resource });
                    }
                }
                else
                {
                    foreach (var res in dev.DeviceVisualization.LoadedResources)
                    {
                        deviceConfig.Configurations.Add(new DefaultResourceConfiguration() { Resource = res.Resource });
                        deviceConfig.Thumbnail = false;
                    }
                }
                deskConfiguration.DeviceConfigurations.Add(deviceConfig);
            }

            return deskConfiguration;
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
                    DeviceVisualization = e.TagVisualization as VisualizationTablet,
                    TagValue = tagValue
                };
                dev.ResourceReleased += dev_ResourceReleased;
                DeviceContainers.Add(tagValue,dev);

                Debug.WriteLine("NooSphere Device validation disabled for thumbnails");
                ValidateDevice(dev.TagValue, dev.Device);
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
                RemoveDeviceVisualisation(tagValue);

                container.Invalidate();

                ValidateDevice(container.TagValue, container.Device);
            }
            ((BaseVisualization)e.TagVisualization).Locked += Device_Locked;

            if (DeviceValueAdded != null)
                DeviceValueAdded(this, tagValue);

        }

        void dev_ResourceReleased(Device sender, ResourceReleasedEventArgs e)
        {
            RestoreResource(e.LoadedResource, new Point(e.Position.X, e.Position.Y));

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
                if (container.Connected)
                {
                    if (ResourceHandleReleased != null)
                        ResourceHandleReleased(this, new ResourceHandle { Device = container.Device, Resource = null });

                    foreach (var res in container.DeviceThumbnail.LoadedResources)
                        AddResource(res,true);
                }
                RemoveDeviceVisualisation(e.VisualizedTag);
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

                Debug.WriteLine("NooSphere Device validation disabled for thumbnails");
                ValidateDevice(container.TagValue,container.Device);
            }
            else
            {
                foreach (var res in container.DeviceVisualization.LoadedResources)
                    AddResource(res,true);


                if (ResourceHandleReleased != null)
                    ResourceHandleReleased(this, new ResourceHandle { Device = container.Device, Resource = null });

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
            foreach (var container in DeviceContainers.Values)
                container.Clear();

            foreach (var res in ResourceViewers)
            {
                view.Items.Remove(res);
            }
            ResourceViewers.Clear();
        }


        private void Add(ScatterViewItem element)
        {
            SetDockState(element, DockStates.Floating);

            element.AddHandler(ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(element_ManipulationDelta), true);
            element.AddHandler(ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(element_ManipulationDelta), true);

            element.Loaded += element_Loaded;
            element.PreviewTouchDown += element_PreviewTouchDown;

            element.Orientation = 0;;

            view.Items.Add(element);
        }
        public ScatterViewItem AddResource(LoadedResource resource, bool iconized = false)
        {
            return AddResourceAtLocation(resource, new Point(RandomNumber(200, 1000), RandomNumber(200, 800)), iconized);
        }
        public ScatterViewItem AddResourceAtLocation(LoadedResource resource, Point p, bool iconized = false)
        {
            var res = ResourceViewerFromFileType(resource.Resource.FileType, resource);
            ((IResourceContainer)res).Iconized = iconized;
            ResourceViewers.Add(res);
            res.Center = p;
            ((IResourceContainer)res).Copied += res_Copied;
            Add(res);
            return res;
        }
        public ScatterViewItem RestoreResource(LoadedResource resource, Point p)
        {
            var res = ResourceViewerFromFileType(resource.Resource.FileType, resource);
            ((IResourceContainer)res).Iconized = true;
            ((IResourceContainer)res).Copied += res_Copied;

            var stb = new Storyboard();

            var moveCenter = new PointAnimation
            {
                FillBehavior = FillBehavior.Stop,
                From = new Point(p.X + 50, p.Y + 50),
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

            return res;

        }

        void res_Copied(object sender, LoadedResource e)
        {
            var rv =  sender as ScatterViewItem;
            var point = rv.TranslatePoint(new Point(),this );

            AddResourceAtLocation(e, point,true);

        }

        public ScatterViewItem ResourceViewerFromFileType(string resourceType,LoadedResource res)
        {
            switch (resourceType)
            {
                case "IMG":
                    return new ResourceViewer(res);
                case "PDF":
                    return new TouchWindow(res);
            }
            return new ResourceViewer(res);
        }
        public void AddPdf(LoadedResource loadedResource,bool iconized=false)
        {
            var res = new PdfViewer(loadedResource) { Iconized = iconized };
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

            if(ResourceHandleReleased != null)
                ResourceHandleReleased(this, new ResourceHandle { Device = container.Device,Resource = null });

            if (container.VisualStyle != DeviceVisual.Thumbnail) return;
            foreach (var res in container.DeviceThumbnail.LoadedResources)
                AddResource(res,true);
            RemoveDeviceVisualisation(dev);
            DeviceContainers.Remove(dev);
        }
        public void UpdateDevice(Device device)
        {
            //Devices[device.TagValue.ToString()].Name = device.Name;
        }
        void element_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DeviceThumbnail)
                return;
            var element = sender as ScatterViewItem;

            if (((IResourceContainer)element).Iconized)
            {
                element.Template = (ControlTemplate)element.FindResource("Docked");
                ((IResourceContainer)element).Iconized = true;
            }
            else
            {
                element.Template = (ControlTemplate)element.FindResource("Floating");
                ((IResourceContainer)element).Iconized = false;
            }

            element.SizeChanged += element_SizeChanged;

        }
        void element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var item = sender as ScatterViewItem;
            if (item == null) return;


            if (e.NewSize.Width < 150 || e.NewSize.Height < 150)
            {
                item.Template = (ControlTemplate)item.FindResource("Docked");
                ((IResourceContainer)item).Iconized = true;
            }
            else
            {
                item.Template = (ControlTemplate)item.FindResource("Floating");
                ((IResourceContainer)item).Iconized = false;
            }
        }
        void element_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            HandleDockingFromTouch((ScatterViewItem)sender, e.Device.GetPosition(view));
        }
        void element_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var thumbnail = sender as DeviceThumbnail;
            if (thumbnail != null)
            {
                CheckRotation(thumbnail);
                CheckDeviceIntersections(thumbnail);
            }
            else
            {
                var item = sender as ScatterViewItem;
                if (item != null)
                    CheckIntersections(item);
            }
            e.Handled = true;
        }
        private void CheckDeviceIntersections(DeviceThumbnail thumbnail)
        {
            if(DeviceContainers.Count <= 1)
                return;

            foreach (var dev in DeviceContainers.Values)
            {
                if (dev.TagValue != thumbnail.VisualizedTag)
                {
                    if (!dev.Connected)
                        return;
                    var rectItem = new Rect(
                        new Point(
                            thumbnail.Center.X - thumbnail.ActualWidth/2 + 80,
                            thumbnail.Center.Y - thumbnail.ActualHeight/2 + 30),
                        new Size(
                            thumbnail.ActualWidth - 50,
                            thumbnail.ActualHeight - 50));

                    var rectDev = new Rect();

                    switch (dev.VisualStyle)
                    {
                        case DeviceVisual.Thumbnail:
                            rectDev = new Rect(
                                new Point(
                                    dev.DeviceThumbnail.Center.X - dev.DeviceThumbnail.ActualWidth/2 + 80,
                                    dev.DeviceThumbnail.Center.Y - dev.DeviceThumbnail.ActualHeight/2 + 30),
                                new Size(
                                    dev.DeviceThumbnail.ActualWidth - 50,
                                    dev.DeviceThumbnail.ActualHeight - 50));
                            break;
                        case DeviceVisual.Visualisation:
                            rectDev = new Rect(
                                new Point(
                                    dev.DeviceVisualization.Center.X - dev.DeviceVisualization.ActualWidth/2 +
                                    80,
                                    dev.DeviceVisualization.Center.Y - dev.DeviceVisualization.ActualHeight/2 +
                                    30),
                                new Size(
                                    dev.DeviceVisualization.ActualWidth - 50,
                                    dev.DeviceVisualization.ActualHeight - 50));
                            break;
                    }

                    var result = rectItem.IntersectsWith(rectDev);

                    if (result && DeviceContainers[thumbnail.VisualizedTag].Intersecting == false)
                    {
                        var container = DeviceContainers[thumbnail.VisualizedTag];
                        DeviceContainers[thumbnail.VisualizedTag].Intersecting = dev.Intersecting = true;
                        foreach (var res in container.DeviceThumbnail.LoadedResources)
                            AddResource(res, true);
                        container.Clear();

                        if (dev.VisualStyle == DeviceVisual.Thumbnail)
                        {
                            foreach (var res in dev.DeviceThumbnail.LoadedResources)
                            {
                                if (ResourceHandled != null)
                                    ResourceHandled(this,
                                        new ResourceHandle()
                                        {
                                            Device = container.Device,
                                            Resource = res.Resource
                                        });
                                thumbnail.AddResource(res);
                            }
                            thumbnail.Resource = dev.DeviceThumbnail.Resource;
                        }
                    }
                    else if(!result)
                    {
                        DeviceContainers[thumbnail.VisualizedTag].Intersecting = dev.Intersecting = false;
                    }
                }
            }
        }

        private void CheckRotation(DeviceThumbnail deviceThumbnail)
        {
            var or = deviceThumbnail.Orientation;
            deviceThumbnail.Interruptable = or > 180 && or <260;
        }

        

        private void CheckIntersections(ScatterViewItem item)
        {

            if (!((IResourceContainer)item).Iconized) 
                return;

            foreach (var dev in DeviceContainers.Values)
            {
                if (!dev.Connected)
                    return;
                var rectItem = new Rect(
                    new Point(item.Center.X - _iconSize/2, item.Center.Y - _iconSize/2),
                    new Size(_iconSize, _iconSize));

                var rectDev = new Rect();

                switch (dev.VisualStyle)
                {
                    case DeviceVisual.Thumbnail:
                        rectDev = new Rect(
                            new Point(
                                dev.DeviceThumbnail.Center.X - dev.DeviceThumbnail.ActualWidth/2 + 80,
                                dev.DeviceThumbnail.Center.Y - dev.DeviceThumbnail.ActualHeight/2 + 30),
                            new Size(
                                dev.DeviceThumbnail.ActualWidth - 50,
                                dev.DeviceThumbnail.ActualHeight - 50));
                        break;
                    case DeviceVisual.Visualisation:
                        rectDev = new Rect(
                            new Point(
                                dev.DeviceVisualization.Center.X - dev.DeviceVisualization.ActualWidth/2 +
                                80,
                                dev.DeviceVisualization.Center.Y - dev.DeviceVisualization.ActualHeight/2 +
                                30),
                            new Size(
                                dev.DeviceVisualization.ActualWidth - 50,
                                dev.DeviceVisualization.ActualHeight - 50));
                        break;
                }

                bool result = rectItem.IntersectsWith(rectDev);


                if (result)
                {
                    item.RemoveHandler(ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(element_ManipulationDelta));
                    item.RemoveHandler(ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(element_ManipulationDelta));
                    item.Loaded -= element_Loaded;
                    item.PreviewTouchDown -= element_PreviewTouchDown;
                    item.SizeChanged -= element_SizeChanged;

                    Remove(item);
                    ResourceViewers.Remove(item);
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
                            dev.DeviceThumbnail.AddResource(((IResourceContainer) item).Resource);
                            break;
                        case DeviceVisual.Visualisation:
                            dev.DeviceVisualization.AddResource(((IResourceContainer) item).Resource);
                            break;
                    }
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
        void RemoveDeviceVisualisation(string p)
        {
            if (!DeviceContainers.ContainsKey(p)) 
                return;
            view.Items.Remove(DeviceContainers[p].DeviceThumbnail);
        }

        public void RemoveDevice(string id)
        {
            Dispatcher.Invoke(() => 
            { 
                DeviceContainer container = null;
                foreach (var con in DeviceContainers.Values.Where(con => con.Device.Id == id))
                    container = con;
                if (container == null)
                    return;

                if (ResourceHandleReleased != null)
                    ResourceHandleReleased(this, new ResourceHandle { Device = container.Device, Resource = null });

                if (container.VisualStyle == DeviceVisual.Thumbnail)
                {
                    foreach (var res in container.DeviceThumbnail.LoadedResources)
                        AddResource(res, true);
                    RemoveDeviceVisualisation(container.TagValue);
                }
                else
                    foreach (var res in container.DeviceVisualization.LoadedResources)
                        AddResource(res, true);
                DeviceContainers.Remove(container.TagValue);
            });
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
