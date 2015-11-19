using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using QMunicate.ViewModels;

namespace QMunicate
{

#if WINDOWS_PHONE_APP

    internal static class ContinuationManager
    {
        internal static void Continue(IContinuationActivatedEventArgs continuationActivatedEventArgs)
        {
            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null) return;
            var page = rootFrame.Content as Page;
            if (page == null) return;

            if (continuationActivatedEventArgs.Kind == ActivationKind.PickFileContinuation)
            {
                var openPickerContinuable = page.DataContext as IFileOpenPickerContinuable;
                var fileOpenArgs = continuationActivatedEventArgs as FileOpenPickerContinuationEventArgs;
                if(openPickerContinuable != null && fileOpenArgs != null)
                    openPickerContinuable.ContinueFileOpenPicker(fileOpenArgs.Files);
            }
        }

    }

#endif

    interface IFileOpenPickerContinuable
    {
        /// <summary>
        /// This method is invoked when the file open picker returns picked
        /// files
        /// </summary>
        /// <param name="files">Picked files</param>
        void ContinueFileOpenPicker(IReadOnlyList<StorageFile> files);
    }

}
