using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

namespace QMunicate.Behaviors
{
    public class ListViewIncrementalLoadingBehavior : DependencyObject, IBehavior
    {
        #region Fields

        private ListView listView;

        #endregion

        #region IBehavior members

        public static readonly DependencyProperty LoadCommandProperty = DependencyProperty.Register("LoadCommand", typeof(ICommand), typeof(ListViewIncrementalLoadingBehavior), new PropertyMetadata(default(ICommand)));

        public ICommand LoadCommand
        {
            get { return (ICommand)GetValue(LoadCommandProperty); }
            set { SetValue(LoadCommandProperty, value); }
        }

        public DependencyObject AssociatedObject { get; private set; }

        public void Attach(DependencyObject associatedObject)
        {
            listView = associatedObject as ListView;
            if (listView == null) return;

            listView.Loaded += ListViewOnLoaded;
        }

        

        public void Detach()
        {
            if (listView == null) return;

            listView.Loaded -= ListViewOnLoaded;
        }

        #endregion

        #region Private methods

        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer) return depObj as ScrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void ListViewOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            ScrollViewer viewer = GetScrollViewer(listView);
            viewer.ViewChanged += ViewerOnViewChanged;
        }

        private void ViewerOnViewChanged(object sender, ScrollViewerViewChangedEventArgs scrollViewerViewChangedEventArgs)
        {
            ScrollViewer view = (ScrollViewer)sender;
            double progress = view.VerticalOffset / view.ScrollableHeight;
            if (progress > 0.7 && LoadCommand != null && LoadCommand.CanExecute(null))
            {
                LoadCommand.Execute(null);
            }
        }

        #endregion
    }
}
