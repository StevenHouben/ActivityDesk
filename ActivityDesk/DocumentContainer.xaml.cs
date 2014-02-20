using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ActivityDesk.Helper;
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
using Math = ActivityDesk.Helper.Math;
using Note = ActivityDesk.Viewers.Note;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using System.Windows.Media;

namespace ActivityDesk
{
    public class IconConnection
    {
        public ScatterViewItem Origin { get; set; }
        public ScatterViewItem Destination { get; set; }
    }

    public partial class DocumentContainer
    {
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
        private const int IconSize = 100;

        private const bool DeviceValidationIsEnabled = true;

        public readonly Collection<Note> Notes = new Collection<Note>();
        public readonly Collection<ScatterViewItem> VisualizedResources = new Collection<ScatterViewItem>();
        public readonly Dictionary<string, DeviceContainer> DeviceContainers = new Dictionary<string, DeviceContainer>();

        public Dictionary<string,LoadedResource> ResourceCache = new Dictionary<string, LoadedResource>();

        public Dictionary<Line,IconConnection> Connections = new Dictionary<Line, IconConnection>();

        public DocumentContainer()
        {
            InitializeComponent();

            //Enable gesture recognition support by the BLAKE NUI toolkit
            Events.RegisterGestureEventSupport(this);

            //Add tags to recognize devices
            AddTagsToRecognizeDevices();

            //Intialize the dockstate
            DockStateManager.DockState = DependencyProperty.RegisterAttached("DockState",
                typeof(DockStates),
                typeof(DocumentContainer), new FrameworkPropertyMetadata(DockStates.Floating));

            //Enable Tag Visualisation
            TouchVisualizer.SetShowsVisualizations(this, false);

            //Handle to the size change
            SizeChanged += DocumentContainer_SizeChanged;

            //Ignore all non tocuch devices
            CustomTopmostBehavior.Activate();
        }
        internal void UpdateContainer(Activity activity)
        {
           
        }

        /// <summary>
        /// Validate that the device visualisation is actually connected to a
        /// real device
        /// </summary>
        internal void ValidateDevice(string tagValue, Device device)
        {
            //Device not found
            if (!DeviceContainers.ContainsKey(tagValue)) 
                return;

            //Update the device connection state
            DeviceContainers[tagValue].Connected = true;
            DeviceContainers[tagValue].Device = device;
        }

        /// <summary>
        /// Reconnects a disconnected device to its thumbnail
        /// </summary>
        internal void ReconnectDevice(string tagValue, Device device)
        {
            //Device not found
            if (!DeviceContainers.ContainsKey(tagValue))
                return;

            //Update the device
            var container = DeviceContainers[tagValue];
            DeviceContainers[tagValue].Device = device;

            //Grab the local paired resources
            var resourcesToSend = container.LoadedResources;

            //Send all resources to the connected device  by creating a
            foreach (var res in resourcesToSend)
            {
                SendResourceToDevice(device,res.Resource);
            }
        }

        /// <summary>
        /// Builds the desk layout
        /// </summary>
        internal void Build(Activity act)
        {
            var configuration = act.Configuration as DeskConfiguration;

            //Clear all item from the desk
            Clear();

            //Clear all the devices by sending a NULL ResourceHandled
            foreach (var dev in DeviceContainers.Values)
            {
                ResourceHandleReleased(this, new ResourceHandle
                {
                    Device = dev.Device,
                    Resource = null
                });
            }

            //If no configuraton -> simply add all resources
            if (configuration == null)
                foreach (var res in act.Resources)
                {
                   AddResource(ResourceCache[res.Id],true);
                }
            //Build the desk from configuratoiom
            else
                BuildFromConfiguration(act,configuration);
        }

        /// <summary>
        /// Builds the desk layout based on a given configuration
        /// </summary>
        void BuildFromConfiguration(Activity act, DeskConfiguration configuration)
        {
            var handledResources = new List<string>();

            bool noDevice = true;

            //Loop the deviceconfigurations
            foreach (var dev in configuration.DeviceConfigurations)
            {
                // Device exists
                if (DeviceContainers.ContainsKey(dev.TagValue))
                {
                    var container = DeviceContainers[dev.TagValue];
                    noDevice = false;

                    //Device is connected
                    if (container.Connected)
                    {
                         if(dev.Thumbnail)
                        {
                            //Actual device is thumbnail
                            if (container.VisualStyle == DeviceVisual.Thumbnail)
                            {
                                container.DeviceThumbnail.Center = dev.Center;

                                HandleDockingFromTouch(container.DeviceThumbnail, dev.Center);

                                container.Pinned = dev.Pinned;

                                container.DeviceThumbnail.Orientation = dev.Orientation;
                                CheckRotation(container.DeviceThumbnail);

                            }
                        }
                        foreach (var def in dev.Configurations.Select(res => res as DefaultResourceConfiguration))
                        {
                            container.AddResource(ResourceCache[def.Resource.Id]);
                            handledResources.Add(def.Resource.Id);

                            //Send resource to device by
                            SendResourceToDevice(container.Device, def.Resource);
                        }
                    }
                   
                }

                if(noDevice)
                    foreach (var def in dev.Configurations.Select(res => res as DefaultResourceConfiguration))
                    {
                        AddResource(ResourceCache[def.Resource.Id], true);
                        handledResources.Add(def.Resource.Id);
                    }
             
            }

            //handle all other resources that are not connected to a device
            foreach (var deskConfig in configuration.Configurations.Select(config => config as DeskResourceConfiguration))
            {
                //Check if the config exists
                if (deskConfig == null)
                    return;


                var viewer = AddResourceAtLocation(ResourceCache[deskConfig.Resource.Id], deskConfig.Center);
                HandleDockingFromTouch(viewer, deskConfig.Center);
                handledResources.Add(deskConfig.Resource.Id);

                //Update the icon state
                 ((IResourceContainer)viewer).Iconized = deskConfig.Iconized;

                //Update the size
                viewer.Width = deskConfig.Size.Width;
                viewer.Height = deskConfig.Size.Height;

                //Update the orientation
                viewer.Orientation = deskConfig.Orientation;
            }
            foreach (var res in act.Resources)
            {
                if (!handledResources.Contains(res.Id))
                {
                    AddResource(ResourceCache[res.Id], true);
                }
            }
        }

        /// <summary>
        /// Grab the configuration from the desk
        /// </summary>
        internal DeskConfiguration GetDeskConfiguration()
        {
            //Create the configuration
            var deskConfiguration = new DeskConfiguration();

            //Loop and grab the properties of all the resources and add them to the list
            foreach (var resConfig in VisualizedResources.Select(item => new DeskResourceConfiguration
            {
                Center = item.Center,
                DockState = DockStateManager.GetDockState(item),
                Resource = ((IResourceContainer)item).LoadedResource.Resource,
                Iconized = ((IResourceContainer)item).Iconized,
                Orientation = item.Orientation,
                Size = new Size(item.Width, item.Height)
            }))
            {
                deskConfiguration.Configurations.Add(resConfig);
            }

            //Loop all connected devices
            foreach (var dev in DeviceContainers.Values)
            {
                //Create a device configuration using the properties
                var deviceConfig = new DeviceConfiguration
                {
                    Device = dev.Device,
                    Pinned = dev.Pinned,
                    TagValue = dev.TagValue
                };

                //Device is thumbnail
                if (dev.VisualStyle == DeviceVisual.Thumbnail)
                {
                    //Update the postion
                    deviceConfig.Center = dev.DeviceThumbnail.Center;

                    //Update the orientation
                    deviceConfig.Orientation = dev.DeviceThumbnail.Orientation;

                    //update the dockstate  (translated into x,y)
                    switch (DockStateManager.GetDockState(dev.DeviceThumbnail))
                    {
                        case DockStates.Left:
                            deviceConfig.Center = new Point(-10, deviceConfig.Center.Y);
                            break;
                        case DockStates.Right:
                            deviceConfig.Center = new Point(2000, deviceConfig.Center.Y);
                            break;
                        case DockStates.Top:
                            deviceConfig.Center = new Point(deviceConfig.Center.X, -10);
                            break;

                    }

                    deviceConfig.Thumbnail = true;
                }
                else
                {
                    deviceConfig.Thumbnail = false;
                }

                //Add all loaded resources to the device configuration
                foreach (var res in dev.LoadedResources)
                    deviceConfig.Configurations.Add(new DefaultResourceConfiguration { Resource = res.Resource });

                //Add the device configuration to main desk configuration
                deskConfiguration.DeviceConfigurations.Add(deviceConfig);
            }

            return deskConfiguration;
        }

        /// <summary>
        /// Notifies outsiders that a resource should be send to a device
        /// </summary>
        private void SendResourceToDevice(Device device, Resource resource)
        {
            ResourceHandled(this, new ResourceHandle
            {
                Device = device,
                Resource = resource
            });
        }

        /// <summary>
        /// Notifies outsiders that a resource should be removed from a device
        /// </summary>
        private void SendResourceRemoveToDevice(Device device, Resource resource)
        {
             ResourceHandleReleased(this, new ResourceHandle
             {
                 Device = device,
                 Resource = resource
             });
        }

        /// <summary>
        /// Notifies outsiders that a resource should be removed from a device
        /// </summary>
        private void SendResourceResetToDevice(Device device)
        {
            ResourceHandleReleased(this, new ResourceHandle
            {
                Device = device,
                Resource = null
            });
        }

        /// <summary>
        /// Initializes the TagVisualizations
        /// </summary>
        private void AddTagsToRecognizeDevices()
        {
            Visualizer.Definitions.Add(
                new TabletDefinition
                {
                    Source = new Uri("Visualizer/Visualizations/VisualizationTablet.xaml", UriKind.Relative),
                    TagRemovedBehavior = TagRemovedBehavior.Fade,
                    LostTagTimeout = 1000
                });
        }

        /// <summary>
        /// Handles the detection of a physical device
        /// </summary>
        private void Visualizer_VisualizationAdded(object sender, TagVisualizerEventArgs e)
        {
            //Grab the tag value of the detected device
            var tagValue = e.TagVisualization.VisualizedTag.Value.ToString(CultureInfo.InvariantCulture);

            //Tag value is not yet added
            if (!DeviceContainers.ContainsKey(tagValue))
            {
                //Create new device container
                var dev = new DeviceContainer
                {
                    DeviceVisualization = e.TagVisualization as VisualizationTablet,
                    TagValue = tagValue
                };

                //Hook event to handle resource releas
                dev.ResourceReleased += dev_ResourceReleased;

                //Add to list
                DeviceContainers.Add(tagValue,dev);

                //Debug
                if (!DeviceValidationIsEnabled)
                {
                    Debug.WriteLine("NooSphere Device validation disabled for thumbnails");
                    ValidateDevice(dev.TagValue, dev.Device);
                }
            }
            //Tag value is already added
            else
            {
                //Grab the container
                var container = DeviceContainers[tagValue];

                //We have two physical devices with the same tag
                if (container.VisualStyle != DeviceVisual.Thumbnail)
                {
                    Console.WriteLine("Error: double detection. Debug problem?");
                    return;
                }

                //connect the visualisation UI to the container
                container.DeviceVisualization = e.TagVisualization as VisualizationTablet;

                //Change the container visual to real device
                container.VisualStyle = DeviceVisual.Visualisation;


                //Loop the resources of the thumbnail and add them to the device
                foreach (var res in container.DeviceThumbnail.LoadedResources)
                {
                    if (container.DeviceVisualization != null) 
                        container.DeviceVisualization.AddResource(res);
                }

                //Remove the thumbnail
                RemoveDeviceVisualisation(tagValue);

                //Invalidate container to handle the state changes
                container.Invalidate();

                //Debug
                if (!DeviceValidationIsEnabled)
                {
                    Debug.WriteLine("NooSphere Device validation disabled for thumbnails");
                    ValidateDevice(container.TagValue, container.Device);
                }
            }

            //Locked event handler
            ((BaseVisualization)e.TagVisualization).Locked += Device_Locked;

            //Notify outsiders, a new device is added
            if (DeviceValueAdded != null)
                DeviceValueAdded(this, tagValue);
        }

        /// <summary>
        /// Handles the detection of the removal of a physical device
        /// </summary>
        private void Visualizer_VisualizationRemoved(object sender, TagVisualizerEventArgs e)
        {
            //Grab the tag value of the remove device
            var tagValue = e.TagVisualization.VisualizedTag.Value.ToString(CultureInfo.InvariantCulture);

            //Removed device is not in our list
            if (!DeviceContainers.ContainsKey(tagValue))
                return;

            //Removed devices is in our list
            var container = DeviceContainers[tagValue];

            //Container is pinned
            if (container.Pinned)
            {

                //Change the container state to thumbnail
                container.VisualStyle = DeviceVisual.Thumbnail;

                //Add new thumbnail
                container.DeviceThumbnail = AddDevice(tagValue,e.TagVisualization.Center);

                //Invalidate container
                container.Invalidate();

                //Add the resource from the detached device to the thumbnail
                foreach (var res in container.DeviceVisualization.LoadedResources)
                    container.AddResource(res);

                //Debug
                if (!DeviceValidationIsEnabled)
                {
                    Debug.WriteLine("NooSphere Device validation disabled for thumbnails");
                    ValidateDevice(container.TagValue, container.Device);
                }
            }
            //container is not pinned -> so remove detached device
            else
            {
                //Add all resources of the detached device to the desk
                foreach (var res in container.LoadedResources)
                    AddResource(res, true);

                //Reset resources on remote device
                SendResourceResetToDevice(container.Device);

                //Remove container
                DeviceContainers.Remove(tagValue);

                //Notify outsiders that device is removed
                if (DeviceValueRemoved != null)
                    DeviceValueRemoved(this, tagValue);

            }
        }

        /// <summary>
        /// A device has released a resource
        /// </summary>
        void dev_ResourceReleased(Device sender, ResourceReleasedEventArgs e)
        {
            //Add it again to the desk at the right position
            RestoreResource(e.LoadedResource, new Point(e.Position.X, e.Position.Y));

            //Remove the resource fromt the physical device
            SendResourceRemoveToDevice(e.Device,e.LoadedResource.Resource);
        }

        /// <summary>
        /// Device Lock state changed
        /// </summary>
        private void Device_Locked(object sender, LockedEventArgs e)
        {

            //Device not found
            if (!DeviceContainers.ContainsKey(e.VisualizedTag)) 
                return;

            //Grab container
            var container = DeviceContainers[e.VisualizedTag];

            //Container is pinned
            if (container.Pinned)
            {
                //Unpin the container
                container.Pinned = false;

                //Update the visual style in the container
                if (container.VisualStyle != DeviceVisual.Thumbnail) return;

                //Device in container is connected
                if (container.Connected)
                {
                    //Reset remove resource collection
                    SendResourceResetToDevice(container.Device);

                    //Add all released resources to the desk
                    foreach (var res in container.DeviceThumbnail.LoadedResources)
                        AddResource(res,true);
                }

                RemoveDeviceVisualisation(e.VisualizedTag);
                DeviceContainers.Remove(e.VisualizedTag);
            }
            //Container is not pinned -> pin it!
            else
                container.Pinned = true;
        }

        /// <summary>
        /// Handles the size change event of the entire document container
        /// </summary>
        void DocumentContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Calculate all the dock tresholds based on the screen size
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

        /// <summary>
        /// Clear the document container of all visualizations and device containers
        /// of all content
        /// </summary>
        public void Clear()
        {
            //Loop all device and clear the containers
            foreach (var container in DeviceContainers.Values)
                container.Clear();

            //Loop resources and remove them
            foreach (var res in VisualizedResources)
                view.Items.Remove(res);

            //Reset the list
            VisualizedResources.Clear();
        }

        /// <summary>
        /// Adds a plain scatterviewitem
        /// </summary>
        private void Add(ScatterViewItem element)
        {
            //Set the default dockstate to floating
            DockStateManager.SetDockState(element, DockStates.Floating);

            //Add handlers for manipulations, load and touches
            element.AddHandler(ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(element_ManipulationDeltaComplete), true);
            element.AddHandler(ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(element_ManipulationDelta), true);
            element.Loaded += element_Loaded;
            element.PreviewTouchDown += element_PreviewTouchDown;

            //Default orientation is 0 degrees
            element.Orientation = 0;;

            //Add the item to the view
            view.Items.Add(element);
        }

        /// <summary>
        /// Adds a loaded resource to the view
        /// </summary>
        public ScatterViewItem AddResource(LoadedResource resource, bool iconized = false)
        {
            return AddResourceAtLocation(resource, new Point(Math.RandomNumber(200, 1000), Math.RandomNumber(200, 800)), iconized);
        }

        /// <summary>
        /// Adds a loaded resource to the view
        /// </summary>
        public ScatterViewItem AddResourceAtLocation(LoadedResource resource, Point p, bool iconized = false)
        {
            //Grab the correct resource viewer based on the type of resource
            var res = ResourceViewerFromFileType(resource.Resource.FileType, resource);

            //By default iconize it
            ((IResourceContainer)res).Iconized = iconized;

            //Add resource to list of visualized resource
            VisualizedResources.Add(res);

            //Position the resource
            res.Center = p;

            //Add a handler to support copy
            ((IResourceContainer)res).Copied += res_Copied;

            //Add to the view
            Add(res);

            return res;
        }

        /// <summary>
        /// Restore a resource using an animation
        /// </summary>>
        public ScatterViewItem RestoreResource(LoadedResource resource, Point p)
        {
            //Grab the correct resource viewer based on the type of resource
            var res = ResourceViewerFromFileType(resource.Resource.FileType, resource);

            //it comes from a device, so lets iconize it
            ((IResourceContainer)res).Iconized = true;

            //Add a handler to support copy
            ((IResourceContainer)res).Copied += res_Copied;

            //New animation storyboard
            var stb = new Storyboard();

            //Move the icon away from the device
            var moveCenter = new PointAnimation
            {
                FillBehavior = FillBehavior.Stop,
                From = new Point(p.X + 50, p.Y + 50),
                To = new Point(p.X + 50, p.Y + 200),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
            };

            //Build the animation
            stb.Children.Add(moveCenter);
            Storyboard.SetTarget(moveCenter, res);
            Storyboard.SetTargetProperty(moveCenter, new PropertyPath(ScatterViewItem.CenterProperty));

            //Add the resource to the view
            Add(res);

            //Animation is done
            stb.Completed += (sender, e) =>
            {
                //position the icon once the animation is done
                VisualizedResources.Add(res);
                res.Center = (Point)moveCenter.To;
            };

            //Start the animation
            stb.Begin(this);

            return res;

        }

        /// <summary>
        /// Handles copying a resource
        /// </summary>
        void res_Copied(object sender, LoadedResource e)
        {
            //Grab the sender
            var item =  sender as ScatterViewItem;

            //var removeCopy = false;

            //ScatterViewItem itemToRemove = null;
            //foreach (var rv in VisualizedResources)
            //{

            //    if (((IResourceContainer)rv).Iconized && !(Equals(item, rv)))
            //    {
            //        var rectItem = new Rect(
            //      new Point(item.Center.X - IconSize / 2, item.Center.Y - IconSize / 2),
            //      new Size(IconSize, IconSize));

            //        var rvItem = new Rect(
            //          new Point(rv.Center.X - IconSize / 2, rv.Center.Y - IconSize / 2),
            //          new Size(IconSize, IconSize));

            //        if (rectItem.IntersectsWith(rvItem))
            //        {
            //            if (((IResourceContainer)rv).LoadedResource.Resource.Id ==
            //                ((IResourceContainer)item).LoadedResource.Resource.Id)
            //            {
            //                itemToRemove = item;
            //            }

            //        }
            //    }

            //}

            var iRes = item as IResourceContainer;
            if(iRes.Connector !=null)
            {
                RemoveLineBinding(item);
                RemoveLineBinding(iRes.Connector.Item);
                ((IResourceContainer) iRes.Connector.Item).Connector = null;
                Remove(item);
                
                VisualizedResources.Remove(item);
                return;
            }

            //Add a new resource visualization at the same postion + slight offset
            var rv_copy = AddResourceAtLocation(e, new Point(item.ActualCenter.X+20,item.ActualCenter.Y+20),true);

            var line = new Line { Stroke = PickBrush(), StrokeThickness = 4 };
            BindLineToScatterViewItems(line, item, rv_copy);

            Connections.Add(line, new IconConnection(){Origin = item,Destination = rv_copy});

            Canvas.Children.Add(line);
        }

      
        private Brush PickBrush()
        {
            Brush result = Brushes.Transparent;

            Random rnd = new Random();

            Type brushesType = typeof(Brushes);

            PropertyInfo[] properties = brushesType.GetProperties();

            int random = rnd.Next(properties.Length);
            result = (Brush)properties[random].GetValue(null, null);

            return result;
        }

        /// <summary>
        /// Grab the right resource viewer based on the type of resource
        /// </summary>
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

        /// <summary>
        /// Adds a PDF resource viewer
        /// </summary>
        public void AddPdf(LoadedResource loadedResource,bool iconized=false)
        {
            var res = new PdfViewer(loadedResource) { Iconized = iconized };
            VisualizedResources.Add(res);

            //Add to view
            Add(res);
        }

        /// <summary>
        /// Adds a Windowed resource viewer
        /// </summary>
        public void AddWindow(LoadedResource resource)
        {
            var res = new TouchWindow(resource);
            VisualizedResources.Add(res);

            //Default size
            res.Width = 1000;
            res.Height = 600;

            //Add to view
            Add(res);
        }

        /// <summary>
        /// Adds a device thumbnail at a positions
        /// </summary>
        public DeviceThumbnail AddDevice(string tagValue,Point position)
        {
            var dev = new DeviceThumbnail(tagValue) { Center = position };
            dev.Closed += dev_Closed;

            //Add to view
            Add(dev);

            //Allow for roation
            dev.CanScale = false;
            dev.CanRotate = true;

            return dev;
        }

        /// <summary>
        /// Handles the device close event
        /// </summary>
        void dev_Closed(object sender, string dev)
        {
            //Device not found
            if (!DeviceContainers.ContainsKey(dev)) return;

            //Reset the pinned state, so the device is decoupled
            var container = DeviceContainers[dev];
            container.Pinned = false;

            //Reset the resource on the actual device
            SendResourceResetToDevice(container.Device);
            
            //Container is not a thumbnail
            if (container.VisualStyle != DeviceVisual.Thumbnail) 
                return;

            //Restore all the resource of the thumbnail
            foreach (var res in container.DeviceThumbnail.LoadedResources)
                AddResource(res,true);

            //Remove the thumbnail and container
            RemoveDeviceVisualisation(dev);
            DeviceContainers.Remove(dev);
        }

        /// <summary>
        /// Handles the load event of all view elements
        /// </summary>
        void element_Loaded(object sender, RoutedEventArgs e)
        {
            //Sender is device thumbnail which we don't need
            if (sender is DeviceThumbnail)
                return;

            //Sender is a resource
            var element = sender as ScatterViewItem;

            if(element == null)
                return;

            //If set as iconized, we should render it as an icon
            if (((IResourceContainer)element).Iconized)
            {
                element.Template = (ControlTemplate)element.FindResource("Docked");
                ((IResourceContainer)element).Iconized = true;
            }
            //If noy set as iconized, we should render it as a floating resource
            else
            {
                element.Template = (ControlTemplate)element.FindResource("Floating");
                ((IResourceContainer)element).Iconized = false;
            }

            //Handle the size changed to update the float style
            element.SizeChanged += element_SizeChanged;
        }

        /// <summary>
        ///Handles the size changed event of all items
        /// </summary>
        void element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Item is a scatterview item
            var item = sender as ScatterViewItem;
            if (item == null) return;

            //If smaller than 150 -> iconize it
            if (e.NewSize.Width < 150 || e.NewSize.Height < 150)
            {
                item.Template = (ControlTemplate)item.FindResource("Docked");
                ((IResourceContainer)item).Iconized = true;
            }
            //If bigger than 150 -> make it float
            else
            {
                item.Template = (ControlTemplate)item.FindResource("Floating");
                ((IResourceContainer)item).Iconized = false;
            }
        }

        /// <summary>
        /// Handles the touch down event of all elements
        /// </summary>
        void element_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            //Check if the element should be docked
            HandleDockingFromTouch((ScatterViewItem)sender, e.Device.GetPosition(view));
        }

        /// <summary>
        /// Handles manipualtion of all elements
        /// </summary>
        void element_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            //Sender is a device
            var thumbnail = sender as DeviceThumbnail;
            if (thumbnail != null)
            {
                //Check if the device is being rotated
                CheckRotation(thumbnail);

                //Check if the device intesects with anoter device
                CheckDeviceIntersections(thumbnail);
            }
            //Sender is a resource
            else
            {
                var item = sender as ScatterViewItem;

                //Check for intersections between the resource and devices
                if (item != null)
                    CheckIntersections(item);
            }

            //Event is handled now
            e.Handled = true;
        }

        /// <summary>
        /// Checks if two device containers are intersecting
        /// </summary>
        private void CheckDeviceIntersections(DeviceThumbnail thumbnail)
        {
            //Only one container, so stop
            if(DeviceContainers.Count <= 1)
                return;

            //Loop all devices
            foreach (var dev in DeviceContainers.Values)
            {
                //Not intersection with itself
                if (dev.TagValue != thumbnail.VisualizedTag)
                {
                    //Not connected, so no intersection needed
                    if (!dev.Connected)
                        return;

                    //Calculate the collision rectangle of the device container
                    var rectItem = new Rect(
                        new Point(
                            thumbnail.Center.X - thumbnail.ActualWidth/2 + 80,
                            thumbnail.Center.Y - thumbnail.ActualHeight/2 + 30),
                        new Size(
                            thumbnail.ActualWidth - 50,
                            thumbnail.ActualHeight - 50));

                    //Calculate the rectangle of the device container based on visual
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

                    //Check if the rectangles intersect
                    var result = rectItem.IntersectsWith(rectDev);

                    //Rectangle intersect for the first time
                    if (result && DeviceContainers[thumbnail.VisualizedTag].Intersecting == false)
                    {
                        //Grab container
                        var container = DeviceContainers[thumbnail.VisualizedTag];

                        //Mark the devices as intersecting
                        DeviceContainers[thumbnail.VisualizedTag].Intersecting = dev.Intersecting = true;

                        //Loop the resources of the original device and put them on the desk
                        //to clear the container
                        foreach (var res in container.DeviceThumbnail.LoadedResources)
                            AddResource(res, true);

                        //Clear the container
                        container.Clear();

                        //Devic is thumbnail
                        if (dev.VisualStyle == DeviceVisual.Thumbnail)
                        {

                            //Loop the resources of the intersecting device
                            foreach (var res in dev.DeviceThumbnail.LoadedResources)
                            {
                                //Send the resource to the actual device
                                SendResourceToDevice(container.Device,res.Resource);
                              
                                //Add the resource to the visualization
                                thumbnail.AddResource(res);
                            }

                            //Update thumbnail
                            thumbnail.LoadedResource = dev.DeviceThumbnail.LoadedResource;
                        }
                    }
                    //No intersection detected
                    else if(!result)
                    {
                        //Reset intersections tyle
                        DeviceContainers[thumbnail.VisualizedTag].Intersecting = dev.Intersecting = false;
                    }
                }
            }
        }

        /// <summary>
        /// Checks the rotation angle of a device
        /// </summary>
        private void CheckRotation(DeviceThumbnail deviceThumbnail)
        {
            var or = deviceThumbnail.Orientation;

            //Mark the interruptability based on the angle
            deviceThumbnail.Interruptable = or > 180 && or <260;
        }

        
        /// <summary>
        /// Checks the intersections between resource and devices
        /// </summary>
        private void CheckIntersections(ScatterViewItem item)
        {
            //Item is iconize, so we don't allow dragging to devices
            if (!((IResourceContainer)item).Iconized) 
                return;

            //Loop devices
            foreach (var dev in DeviceContainers.Values)
            {
                //Disallow dragging to disconnected devices
                if (dev.Connected)
                {

                    //Calculate the collision rectangle of the resource
                    var rectItem = new Rect(
                        new Point(item.Center.X - IconSize/2, item.Center.Y - IconSize/2),
                        new Size(IconSize, IconSize));

                    //Calculate the collision rectangle of the device based on the visual
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

                    //Intersection found
                    if (rectItem.IntersectsWith(rectDev))
                    {
                        //Remove handlers to make sure the event is not called again
                        item.RemoveHandler(ManipulationCompletedEvent,
                            new EventHandler<ManipulationCompletedEventArgs>(element_ManipulationDeltaComplete));
                        item.RemoveHandler(ManipulationDeltaEvent,
                            new EventHandler<ManipulationDeltaEventArgs>(element_ManipulationDelta));
                        item.Loaded -= element_Loaded;
                        item.PreviewTouchDown -= element_PreviewTouchDown;
                        item.SizeChanged -= element_SizeChanged;


                        var iRes = item as IResourceContainer;
                        if (iRes.Connector != null)
                        {
                            RemoveLineBinding(item);
                            RemoveLineBinding(iRes.Connector.Item);
                            ((IResourceContainer)iRes.Connector.Item).Connector = null;
                        }
                        //Remove the resource
                        Remove(item);
                        VisualizedResources.Remove(item);

    

                       


                        RemoveLineBinding(item);

                        //Send it to the device
                        SendResourceToDevice(dev.Device, ((IResourceContainer) item).LoadedResource.Resource);

                        //Update the container based on the visual
                        switch (dev.VisualStyle)
                        {
                            case DeviceVisual.Thumbnail:
                                dev.DeviceThumbnail.AddResource(((IResourceContainer) item).LoadedResource);
                                break;
                            case DeviceVisual.Visualisation:
                                dev.DeviceVisualization.AddResource(((IResourceContainer) item).LoadedResource);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the docking of the resources
        /// </summary>
        private void HandleDockingFromTouch(ScatterViewItem item,Point p)
        {
            //Grab docking state
            var previousdockState = DockStateManager.GetDockState(item);

            //Item is to big to be docked
            if (!(item.Width <= _minimumDockSize.Width) || !(item.Height <= _minimumDockSize.Height)) 
                return;

            //Dock the resource based on the x,y of the resource item
            if (p.X < _leftDockTreshhold)
            {
                item.Template = (ControlTemplate) item.FindResource("Docked");
                DockStateManager.SetDockState(item, DockStates.Left);
            }
            else if (p.X > _rightDockTreshhold)
            {
                item.Template = (ControlTemplate) item.FindResource("Docked");
                DockStateManager.SetDockState(item, DockStates.Right);
            }
            else if (p.Y < _upperDockThreshold)
            {
                item.Template = (ControlTemplate) item.FindResource("Docked");
                DockStateManager.SetDockState(item, DockStates.Top);
            }
            else
            {
                if (previousdockState == DockStates.Floating) 
                    return;
                if (((IResourceContainer)item).Iconized) item.Template = (ControlTemplate)item.FindResource("Docked");
                else item.Template = (ControlTemplate)item.FindResource("Floating");
                DockStateManager.SetDockState(item, DockStates.Floating);
            }

            //Update the dock
            UpdateDock(item);
        }

        /// <summary>
        /// Handle docking from item
        /// </summary>
        private void HandleDocking(ScatterViewItem item)
        {
            HandleDockingFromTouch(item,item.Center);
        }

        /// <summary>
        /// Update the dockstate on an item
        /// </summary>
        private void UpdateDock(ScatterViewItem item)
        {
            var state = DockStateManager.GetDockState(item);

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

        /// <summary>
        /// Handles the manipulation complete event of all items
        /// </summary>
        void element_ManipulationDeltaComplete(object sender, ManipulationCompletedEventArgs e)
        {
            HandleDocking((ScatterViewItem)sender);
        }

        /// <summary>
        /// Removes an item from the view
        /// </summary>
        void Remove(object element)
        {
            view.Items.Remove(element);
        }

        /// <summary>
        /// Removes a device visualisation
        /// </summary>
        void RemoveDeviceVisualisation(string p)
        {
            if (!DeviceContainers.ContainsKey(p)) 
                return;
            view.Items.Remove(DeviceContainers[p].DeviceThumbnail);
        }

        /// <summary>
        /// Removes a device and visualization from the container
        /// </summary>
        public void RemoveDevice(string id)
        {
            Dispatcher.Invoke(() => 
            { 
                //Grab the container
                DeviceContainer container = null;
                foreach (var con in DeviceContainers.Values.Where(con => con.Device.Id == id))
                    container = con;
                if (container == null)
                    return;

                //Reset the resource on remote device
                SendResourceResetToDevice(container.Device);

                //Remove thumbnail visual
                if (container.VisualStyle == DeviceVisual.Thumbnail)
                {
                    foreach (var res in container.DeviceThumbnail.LoadedResources)
                        AddResource(res, true);
                    RemoveDeviceVisualisation(container.TagValue);
                }
                //Remove the actual device resource vis
                else
                    foreach (var res in container.DeviceVisualization.LoadedResources)
                        AddResource(res, true);
                DeviceContainers.Remove(container.TagValue);
            });
        }

        private void RemoveLineBinding(ScatterViewItem item)
        {
            BindingOperations.ClearAllBindings(item);

            var resContainer = item as IResourceContainer;
            if (resContainer != null && resContainer.Connector == null) return;

            if (resContainer != null) Canvas.Children.Remove(resContainer.Connector.ConnectionLine);
        }
        private void BindLineToScatterViewItems(Line line, ScatterViewItem origin,
    ScatterViewItem destination)
        {

            // Bind line.(X1,Y1) to origin.ActualCenter  
            BindingOperations.SetBinding(line, Line.X1Property, new Binding
            {
                Source = origin,
                Path = new PropertyPath("ActualCenter.X")
            });
            BindingOperations.SetBinding(line, Line.Y1Property, new Binding
            {
                Source = origin,
                Path = new PropertyPath("ActualCenter.Y")
            });

            // Bind line.(X2,Y2) to destination.ActualCenter  
            BindingOperations.SetBinding(line, Line.X2Property, new Binding
            {
                Source = destination,
                Path = new PropertyPath("ActualCenter.X")
            });
            BindingOperations.SetBinding(line, Line.Y2Property, new Binding
            {
                Source = destination,
                Path = new PropertyPath("ActualCenter.Y")
            });

            ((IResourceContainer)origin).Connector = new Connection() { ConnectionLine = line, Item = destination };
                ((IResourceContainer) destination).Connector = new Connection() {ConnectionLine = line, Item = origin};
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
