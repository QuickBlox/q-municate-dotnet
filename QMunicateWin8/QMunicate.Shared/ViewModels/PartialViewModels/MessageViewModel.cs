using System;
using Windows.UI.Xaml.Media;
using Quickblox.Sdk.Builder;
using Quickblox.Sdk.GeneralDataModel.Models;
using Message = Quickblox.Sdk.GeneralDataModel.Models.Message;

namespace QMunicate.ViewModels.PartialViewModels
{
    public enum MessageType
    {
        Unknown,
        Incoming,
        Outgoing
    }

    public class MessageViewModel
    {
        public string MessageText { get; set; }
        public MessageType MessageType { get; set; }
        public DateTime DateTime { get; set; }
        public NotificationTypes NotificationType { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public ImageSource AttachedImage { get; set; }
    }
}
