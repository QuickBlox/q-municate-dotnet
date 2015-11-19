using System;
using System.Windows.Input;

namespace QMunicate.Core.MessageService
{
    /// <summary>
    /// DialogCommand class.
    /// </summary>
    public class DialogCommand
    {
        #region Ctor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="caption">The caption for the command.</param>
        /// <param name="command">The command.</param>
        /// <param name="isDefault">A value that indicates whether a command is a Default button.</param>
        /// <param name="isCancel">A value that indicates whether a command is a Cancel button.</param>
        /// <param name="tag">The tag for the command.</param>
        public DialogCommand(String caption, ICommand command, Boolean isDefault = false, Boolean isCancel = false, Object tag = null)
        {
            Caption = caption;
            Command = command;
            IsDefault = isDefault;
            IsCancel = isCancel;
            Tag = tag;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the caption for the command.
        /// </summary>
        public String Caption { get; private set; }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public ICommand Command { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a command is a Default button.
        /// </summary>
        public Boolean IsDefault { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a command is a Cancel button.
        /// </summary>
        public Boolean IsCancel { get; private set; }

        /// <summary>
        /// Gets or sets the tag for the command.
        /// </summary>
        public Object Tag { get; private set; }

        #endregion
    }
}
