using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UWPCam.Helper;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace UWPCam
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Private MainPage object for status updates
        //private MainPage rootPage = MainPage.Current;

        // Object to manage access to camera devices
        private MediaCapturePreviewer _previewer = null;

        LowLagMediaRecording _mediaRecording;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scenario1_PreviewSettings"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            _previewer = new MediaCapturePreviewer(PreviewControl, Dispatcher);
            PopulateDefaultParams();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await _previewer.CleanupCameraAsync();
        }

        private async Task InitCameraPreview()
        {
            var cameraDevice = (DeviceInformation)((ComboBoxItem)CamerasDropDown.SelectedItem).Tag;
            var audionDevice = (DeviceInformation)((ComboBoxItem)MicDropDown.SelectedItem).Tag;

            await _previewer.InitializeCameraAsync(cameraDevice, audionDevice);
            PopulateSettingsComboBox();

        }

        /// <summary>
        ///  Event handler for Preview settings combo box. Updates stream resolution based on the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ComboBoxSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (_previewer.IsPreviewing)
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;

                if(selectedItem == null)
                {
                    return;
                }
                var encodingProperties = (selectedItem.Tag as StreamResolution).EncodingProperties;
                await _previewer.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);
            }
        }

        /// <summary>
        /// Populates the combo box with all possible combinations of settings returned by the camera driver
        /// </summary>
        private void PopulateSettingsComboBox()
        {
            // Query all properties of the device
            IEnumerable<StreamResolution> allProperties = _previewer.MediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Select(x => new StreamResolution(x));

            allProperties = allProperties.Where(prop => prop.FrameRate >= 30 && prop.Height >= 480).OrderByDescending(x => x.FrameRate).ThenByDescending(x => x.Height * x.Width);

            // Order them by resolution then frame rate

            CameraSettings.Items.Clear();

            // Populate the combo box with the entries
            foreach (var property in allProperties)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = property.GetFriendlyName();
                comboBoxItem.Tag = property;
                CameraSettings.Items.Add(comboBoxItem);
            }
        }

        private async void PopulateDefaultParams()
        {
            await PopulateCameras();
            await PopulateMicrophones();
        }

        /// <summary>
        /// Populates available cameras list
        /// </summary>
        private async Task PopulateCameras()
        {
            // Get the camera devices
            var cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            //Search logitech brio as default
            var backFacingDevice = cameraDevices
                .FirstOrDefault(cam => cam.Name == "Logitech BRIO");

            var preferredDevice = backFacingDevice ?? cameraDevices.FirstOrDefault();

            // Populate the combo box with the entries
            foreach (var cameraDevice in cameraDevices)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = cameraDevice.Name;
                comboBoxItem.Tag = cameraDevice;
                CamerasDropDown.Items.Add(comboBoxItem);
            }

            CamerasDropDown.SelectedItem = CamerasDropDown.Items.FirstOrDefault(item => ((string)((ComboBoxItem)item).Content) == preferredDevice.Name);
        }

        /// <summary>
        /// Populates available Microphones list
        /// </summary>
        private async Task PopulateMicrophones()
        {
            // Get the camera devices
            var micDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);

            // Populate the combo box with the entries
            foreach (var micDevice in micDevices)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = micDevice.Name;
                comboBoxItem.Tag = micDevice;
                MicDropDown.Items.Add(comboBoxItem);
            }

            MicDropDown.SelectedItem = MicDropDown.Items.FirstOrDefault();
        }

        private async Task StartRecording()
        {
            try
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                
                //var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
                StorageFile file = await storageFolder.CreateFileAsync("record.mp4", CreationCollisionOption.GenerateUniqueName);

                this.FolderPath.Text = file.Path;

                _mediaRecording = await _previewer.MediaCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
                await _mediaRecording.StartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception when starting video recording: {0}", ex.ToString());
            }
        }

        private async Task StopRecording()
        {
            await _mediaRecording.StopAsync();
        }

        private async void InitCamSession_Click(object sender, RoutedEventArgs e)
        {
            await InitCameraPreview();
        }

        private async void StartRec_Click(object sender, RoutedEventArgs e)
        {
            await StartRecording();
        }

        private async void StopRec_Click(object sender, RoutedEventArgs e)
        {
            await StopRecording();
        }
    }
}
