using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace UWPCam.Helper
{
    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };

    public class MediaCapturePreviewer
    {
        CoreDispatcher _dispatcher;
        CaptureElement _previewControl;

        public MediaCapturePreviewer(CaptureElement previewControl, CoreDispatcher dispatcher)
        {
            _previewControl = previewControl;
            _dispatcher = dispatcher;
        }

        public bool IsPreviewing { get; private set; }
        public bool IsRecording { get; set; }
        public MediaCapture MediaCapture { get; private set; }

        /// <summary>
        /// Sets encoding properties on a camera stream. Ensures CaptureElement and preview stream are stopped before setting properties.
        /// </summary>
        public async Task SetMediaStreamPropertiesAsync(MediaStreamType streamType, IMediaEncodingProperties encodingProperties)
        {
            // Stop preview and unlink the CaptureElement from the MediaCapture object
            await MediaCapture.StopPreviewAsync();
            _previewControl.Source = null;

            // Apply desired stream properties
            await MediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);

            // Recreate the CaptureElement pipeline and restart the preview
            _previewControl.Source = MediaCapture;
            await MediaCapture.StartPreviewAsync();
        }

        /// <summary>
        /// Initializes the MediaCapture, starts preview.
        /// </summary>
        public async Task InitializeCameraAsync(DeviceInformation camera, DeviceInformation microphone)
        {
            await CleanupCameraAsync();
            MediaCapture = new MediaCapture();
            MediaCapture.Failed += MediaCapture_Failed;

            try
            {
                await MediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = camera.Id,
                    AudioDeviceId = microphone.Id
                });
                _previewControl.Source = MediaCapture;
                await MediaCapture.StartPreviewAsync();
                IsPreviewing = true;
            }
            catch (UnauthorizedAccessException)
            {
                // This can happen if access to the camera has been revoked.
                //MainPage.Current.NotifyUser("The app was denied access to the camera", NotifyType.ErrorMessage);
                await CleanupCameraAsync();
            }
        }

        public async Task CleanupCameraAsync()
        {
            if (IsRecording)
            {
                await MediaCapture.StopRecordAsync();
                IsRecording = false;
            }

            if (IsPreviewing)
            {
                await MediaCapture.StopPreviewAsync();
                IsPreviewing = false;
            }

            _previewControl.Source = null;

            if (MediaCapture != null)
            {
                MediaCapture.Dispose();
                MediaCapture = null;
            }
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs e)
        {
            var task = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                //MainPage.Current.NotifyUser("Preview stopped: " + e.Message, NotifyType.ErrorMessage);
                IsRecording = false;
                IsPreviewing = false;
                await CleanupCameraAsync();
            });
        }
    }
}
