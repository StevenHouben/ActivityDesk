﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ActivityDesk.Infrastructure;
using Microsoft.Surface.Presentation.Controls;

namespace ActivityDesk.Viewers
{
	/// <summary>
	/// Interaction logic for Note.xaml
	/// </summary>
	public partial class Note : ScatterViewItem,IResourceContainer
	{
        public event EventHandler Save = null;
        public event EventHandler Close = null;
        public bool Iconized { get; set; }
        public event EventHandler<LoadedResource> Copied = delegate { };

        public string ResourceType { get; set; }

        public string Name { get; set; }

        public Connection Connector { get; set; }
        public StrokeCollection Strokes { get; set; }

        public LoadedResource LoadedResource { get; set; }

		public Note()
		{
			InitializeComponent();
		}

        private SurfaceButton reset;
        private SurfaceInkCanvas painter;
        private SurfaceTextBox text;
        private Label label;
        public override void OnApplyTemplate()
        {
            if (painter != null)
                Strokes = painter.Strokes;

            if (text != null)
                Name = text.Text;

            if (GetTemplateChild("btnReset") != null)
            {
                reset = GetTemplateChild("btnReset") as SurfaceButton;
                reset.Click += new RoutedEventHandler(btnReset_Click);
            }

            if (GetTemplateChild("txtName") != null)
            {
                text = GetTemplateChild("txtName") as SurfaceTextBox;
                text.TextChanged += new TextChangedEventHandler(text_TextChanged);
                text.Text = Name;
            }

            if (GetTemplateChild("lblName") != null)
            {
                label = GetTemplateChild("lblName") as Label;
                label.Content = Name;
            }

            if (GetTemplateChild("Painter") != null)
            {
                painter = GetTemplateChild("Painter") as SurfaceInkCanvas;
                // Set up the DrawingAttributes for the pen.
                var inkDA = new DrawingAttributes {Color = Colors.Black, Height = 1, Width = 1, FitToCurve = false};
                painter.UsesTouchShape = false;

                painter.DefaultDrawingAttributes = inkDA;
                if(Strokes != null)
                    painter.Strokes = Strokes;
            }  
            base.OnApplyTemplate();
        }

        void text_TextChanged(object sender, TextChangedEventArgs e)
        {
            Name = text.Text;
        }
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            painter.Strokes.Clear();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            //todo
        }

        private void btnClose_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (Close != null)
                Close(this, new EventArgs());
        }


	}
}