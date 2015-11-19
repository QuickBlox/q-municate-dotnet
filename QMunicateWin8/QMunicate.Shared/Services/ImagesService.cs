using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using QMunicate.Helper;
using Quickblox.Sdk;

namespace QMunicate.Services
{
    public interface IImageService
    {
        Task<byte[]> GetPrivateImageBytes(int imageUploadId);
        Task<ImageSource> GetPrivateImage(int imageUploadId, int? decodePixelWidth = null, int? decodePixelHeight = null);
        Task<ImageSource> GetPublicImage(string imageUrl);
        void ClearImagesCache();
    }

    public class ImagesService : IImageService
    {
        #region Fields

        private const string imagesFolder = "images";
        private const string fileNameFormat = "{0}.jpg";
        private readonly IQuickbloxClient quickbloxClient;
        private readonly IFileStorage fileStorage;
        private static readonly List<int> thisSessionImages = new List<int>(); // image links that were loaded during this session of application
        private readonly object thisLock = new object();

        #endregion

        #region Ctor

        public ImagesService(IQuickbloxClient quickbloxClient, IFileStorage fileStorage)
        {
            this.fileStorage = fileStorage;
            this.quickbloxClient = quickbloxClient;
        }

        #endregion

        #region IImageService Members

        public async Task<byte[]> GetPrivateImageBytes(int imageUploadId)
        {
            bool isInThisSession;
            lock (this)
            {
                isInThisSession = thisSessionImages.Contains(imageUploadId);
            }

            return isInThisSession ? await GetImageBytesFromStorage(imageUploadId) : await GetImageBytesFromServer(imageUploadId);
        }

        public async Task<ImageSource> GetPrivateImage(int imageUploadId, int? decodePixelWidth = null, int? decodePixelHeight = null)
        {
            var imageBytes = await GetPrivateImageBytes(imageUploadId);
            if (imageBytes == null) return null;

            return await ImageHelper.CreateBitmapImage(imageBytes, decodePixelWidth, decodePixelHeight);
        }

        public async Task<ImageSource> GetPublicImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;

            try
            {
                return new BitmapImage(new System.Uri(imageUrl));
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public void ClearImagesCache()
        {
            lock (thisLock)
            {
                thisSessionImages.Clear();
            }
        }

        #endregion

        #region Private methods

        private async Task<byte[]> GetImageBytesFromServer(int imageUploadId)
        {
            var downloadResponse = await quickbloxClient.ContentClient.DownloadFileById(imageUploadId);
            if (downloadResponse.StatusCode == HttpStatusCode.OK)
            {
                await fileStorage.WriteToFile(imagesFolder, string.Format(fileNameFormat, imageUploadId), downloadResponse.Result);
                lock (thisLock)
                {
                    thisSessionImages.Add(imageUploadId);
                }
                return downloadResponse.Result;
            }

            return null;
        }

        private async Task<byte[]> GetImageBytesFromStorage(int imageUploadId)
        {
            return await fileStorage.ReadFile(imagesFolder, string.Format(fileNameFormat, imageUploadId));
        }

        #endregion

    }
}
