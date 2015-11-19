using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QMunicate.Core.Command;

namespace QMunicate.Core.MessageService
{
    /// <summary>
    /// IMessageService interface.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task ShowAsync(String title, String message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="dialogCommands"></param>
        /// <returns></returns>
        Task ShowAsync(String title, String message, IEnumerable<DialogCommand> dialogCommands);
    }
}
