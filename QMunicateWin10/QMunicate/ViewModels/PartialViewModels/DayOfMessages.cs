using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QMunicate.ViewModels.PartialViewModels
{
    /// <summary>
    /// Represents a group of messages for one day
    /// </summary>
    public class DayOfMessages : ObservableCollection<MessageViewModel>
    {
        public DayOfMessages()
        {
        }

        public DayOfMessages(IEnumerable<MessageViewModel> items)
            : base(items)
        {
        }

        public DateTime Date { get; set; }
    }
}
