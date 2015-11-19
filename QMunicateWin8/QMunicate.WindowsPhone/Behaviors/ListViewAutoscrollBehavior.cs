using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace QMunicate.Behaviors
{
    public class ListViewAutoscrollBehavior : DependencyObject, IBehavior
    {
        #region Fields

        private ListView listBox;

        #endregion

        #region IBehavior members

        public DependencyObject AssociatedObject { get; private set; }

        public void Attach(DependencyObject associatedObject)
        {
            listBox = associatedObject as ListView;
            if (listBox != null && listBox.Items != null)
                listBox.Items.VectorChanged += ItemsOnVectorChanged;
        }

        public void Detach()
        {
            if (listBox != null && listBox.Items != null)
                listBox.Items.VectorChanged -= ItemsOnVectorChanged;
        }

        #endregion

        #region Private methods

        private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            if (listBox == null) return;

            var lastItem = listBox.Items.LastOrDefault();
            if (lastItem != null)
            {
                listBox.UpdateLayout();
                listBox.ScrollIntoView(lastItem);
            }
        }

        #endregion
    }
}
