using System;
using QMunicate.Core.Observable;

namespace QMunicate.ViewModels.PartialViewModels
{
    public class SelectableListBoxItem<T> : ObservableObject
    {
        private bool isSelected;
        private T item;

        public SelectableListBoxItem(T item)
        {
            if(item == null) throw new ArgumentNullException("item");

            Item = item;
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { Set(ref isSelected, value); }
        }

        public T Item
        {
            get { return item; }
            set { Set(ref item, value); }
        }
    }
}
