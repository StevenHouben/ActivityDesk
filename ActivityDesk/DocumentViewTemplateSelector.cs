﻿using System.Windows.Controls;
using System.Windows;

namespace ActivityDesk
{
    public class DocumentViewTemplateSelector : DataTemplateSelector
    {
      public DataTemplate FullSize { get; set; }
      public DataTemplate Docked { get; set; }

      public override DataTemplate SelectTemplate(object item, DependencyObject container)
      {
          if (((DeviceThumbnail)item).Center.X < 100 || ((DeviceThumbnail)item).Center.X < 1900)
              return Docked;
          else
              return FullSize;
      }
    }
}
