using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace QMunicate.ViewModels
{
    public class ImagePreviewViewModel : ViewModel
    {
        private ImageSource image;

        public ImageSource Image
        {
            get { return image; }
            set { Set(ref image, value); }
        }

        #region Navigation

        public override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var imageSource = e.Parameter as ImageSource;
            if (imageSource != null)
                Image = imageSource;
        }

        #endregion

    }
}
