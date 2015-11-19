using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace QMunicate.Core.MessageService
{
    /// <summary>
    /// MessageService class.
    /// </summary>
    public class MessageService : IMessageService
    {
        #region Fields

        private static Boolean showing;

        #endregion

        #region IMessageService Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ShowAsync(String title, String message)
        {
            await ShowAsync(title, message, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="dialogCommands"></param>
        /// <returns></returns>
        public async Task ShowAsync(String title, String message, IEnumerable<DialogCommand> dialogCommands)
        {
            if (showing) return;
            showing = true;
            var messageDialog = new MessageDialog(message ?? String.Empty, title);
            if (dialogCommands != null)
            {
                var commands = dialogCommands.Select(c => new UICommand(c.Caption, command => c.Command.Execute(null), c.Tag));
                foreach (var command in commands)
                {
                    messageDialog.Commands.Add(command);
                }
            }
            await messageDialog.ShowAsync();
            showing = false;
        }

        #endregion
    }
}
