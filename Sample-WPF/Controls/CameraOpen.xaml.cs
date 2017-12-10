using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFMediaKit.DirectShow.Controls;
using System.IO;

namespace ClientLibrary.Controls
{
    /// <summary>
    /// CameraOpen.xaml 的交互逻辑
    /// </summary>
    public partial class CameraOpen : Window
    {
        public CameraOpen()
        {
            InitializeComponent();
        }

        private void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vce.VideoCaptureSource = (string)cb.SelectedItem;
        }

        private void btnCapture_Click(object sender, RoutedEventArgs e)
        {
            btnanew_Button.IsEnabled = false;
            OK_Button.IsEnabled = false;
            //captureElement. 怎么抓取高清的原始图像           
            RenderTargetBitmap bmp = new RenderTargetBitmap(
                (int)vce.ActualWidth,
                (int)vce.ActualHeight,
                96, 96, PixelFormats.Default);
            bmp.Render(vce);

            BitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                byte[] captureData = ms.ToArray();
                File.WriteAllBytes("D:/3.jpg", captureData);
            }

            btnanew_Button.IsEnabled = true;
            OK_Button.IsEnabled = true;
            //btnCapture_Button.IsEnabled = false;
            vce.Pause();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cb.ItemsSource = MultimediaUtil.VideoInputNames;
            if (MultimediaUtil.VideoInputNames.Length > 0)
            {
                cb.SelectedIndex = 0;//默认选择第0个摄像头
            }
            else
            {
                MessageBox.Show("没有检测到任何摄像头");
            }
        }
        /// <summary>
        /// 重拍
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnanew_Click(object sender, RoutedEventArgs e)
        {
            vce.Play();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
