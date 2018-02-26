//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-Face-Windows
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.ProjectOxford.Face.Controls
{
    /// <summary>
    /// Interaction logic for FaceDetectionPage.xaml
    /// </summary>
    public partial class FaceDetectionPage : Page, INotifyPropertyChanged
    {

        #region Fields

        /// <summary>
        /// Description dependency property
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(FaceDetectionPage));

        /// <summary>
        /// Face detection results in list container
        /// </summary>
        private ObservableCollection<Face> _detectedFaces = new ObservableCollection<Face>();

        /// <summary>
        /// Face detection results in text string
        /// </summary>
        private string _detectedResultsInText;

        /// <summary>
        /// Face detection results container
        /// </summary>
        private ObservableCollection<Face> _resultCollection = new ObservableCollection<Face>();

        /// <summary>
        /// Image used for rendering and detecting
        /// </summary>
        private ImageSource _selectedFile;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceDetectionPage" /> class
        /// </summary>
        public FaceDetectionPage()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Events

        /// <summary>
        /// Implement INotifyPropertyChanged event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets or sets description
        /// </summary>
        public string Description
        {
            get
            {
                return (string)GetValue(DescriptionProperty);
            }

            set
            {
                SetValue(DescriptionProperty, value);
            }
        }

        /// <summary>
        /// Gets face detection results
        /// </summary>
        public ObservableCollection<Face> DetectedFaces
        {
            get
            {
                return _detectedFaces;
            }
        }

        /// <summary>
        /// Gets or sets face detection results in text string
        /// </summary>
        public string DetectedResultsInText
        {
            get
            {
                return _detectedResultsInText;
            }

            set
            {
                _detectedResultsInText = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("DetectedResultsInText"));
                }
            }
        }

        /// <summary>
        /// Gets constant maximum image size for rendering detection result
        /// </summary>
        public int MaxImageSize
        {
            get
            {
                return 300;
            }
        }

        /// <summary>
        /// Gets face detection results
        /// </summary>
        public ObservableCollection<Face> ResultCollection
        {
            get
            {
                return _resultCollection;
            }
        }

        /// <summary>
        /// Gets or sets image for rendering and detecting
        /// </summary>
        public ImageSource SelectedFile
        {
            get
            {
                return _selectedFile;
            }

            set
            {
                _selectedFile = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedFile"));
                }
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Pick image for face detection and set detection result to result container
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event argument</param>
        private async void ImagePicker_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = getMergedPicture("D:\\3.jpg", "E:\\hackathon\\ls\\cognitive-Face-Windows\\data1\\lsm.jpg");
        }
        public BitmapSource getMergedPicture(string userPicturePath, string hisPicturePath)
        {
            var hismanImage = UIHelper.LoadImageAppliedOrientation(hisPicturePath);
            WriteableBitmap target = new WriteableBitmap(
            hismanImage.PixelWidth,
            hismanImage.PixelHeight,
            hismanImage.DpiX, hismanImage.DpiY,
            hismanImage.Format, null);
            WriteableBitmap[] targets = { target };
            getMergedPictureCore(userPicturePath, hisPicturePath, targets, hismanImage);
            return targets[0];

        }

        public async void getMergedPictureCore(string userPicturePath, string hisPicturePath, WriteableBitmap[] targets, BitmapImage hismanImage)
        {
            ///renderingImag 是一个bitmap类型
            var renderingImage = UIHelper.LoadImageAppliedOrientation(userPicturePath);

            // Call detection REST API
            using (var fStream = File.OpenRead(userPicturePath))
            {
                var fStream2 = File.OpenRead(hisPicturePath);
                string subscriptionKey = "3f7c942ba5344a61b0645fc7f92377db";
                string endpoint = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0";

                var faceServiceClient = new FaceServiceClient(subscriptionKey, endpoint);

                ProjectOxford.Face.Contract.Face[] faces = await faceServiceClient.DetectAsync(fStream, false, true, new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Glasses, FaceAttributeType.HeadPose, FaceAttributeType.FacialHair, FaceAttributeType.Emotion, FaceAttributeType.Hair, FaceAttributeType.Makeup, FaceAttributeType.Occlusion, FaceAttributeType.Accessories, FaceAttributeType.Noise, FaceAttributeType.Exposure, FaceAttributeType.Blur });
                ProjectOxford.Face.Contract.Face[] faces2 = await faceServiceClient.DetectAsync(fStream2, false, true, new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Glasses, FaceAttributeType.HeadPose, FaceAttributeType.FacialHair, FaceAttributeType.Emotion, FaceAttributeType.Hair, FaceAttributeType.Makeup, FaceAttributeType.Occlusion, FaceAttributeType.Accessories, FaceAttributeType.Noise, FaceAttributeType.Exposure, FaceAttributeType.Blur });

                //DetectedResultsInText = string.Format("{0} face(s) has been detected", faces.Length);
                if (faces.Length <= 0 || faces.Length <= 0)
                {
                    //return SelectedFile;
                    return;
                }
                var face = faces[0];
                var face2 = faces2[0];

                ///face0
                int upLeftX = (int)face.FaceLandmarks.EyebrowLeftOuter.X;
                int upLeftY = (int)face.FaceLandmarks.EyebrowLeftOuter.Y;
                int upLeft2Y = (int)face.FaceLandmarks.EyebrowLeftInner.Y;
                int upRight2Y = (int)face.FaceLandmarks.EyebrowRightInner.Y;
                int upRightX = (int)face.FaceLandmarks.EyebrowRightOuter.X;
                int upRightY = (int)face.FaceLandmarks.EyebrowRightOuter.Y;
                int downLeftX = (int)face.FaceLandmarks.MouthLeft.X;
                int downLeftY = (int)face.FaceLandmarks.MouthLeft.Y;
                int downRightX = (int)face.FaceLandmarks.MouthRight.X;
                int downRightY = (int)face.FaceLandmarks.MouthRight.Y;
                int downMiddle = (int)face.FaceLandmarks.UnderLipBottom.Y;


                ///get offset
                int faceNoseX = (int)(face.FaceLandmarks.NoseRootLeft.X + face.FaceLandmarks.NoseRootRight.X +
                    face.FaceLandmarks.NoseLeftAlarOutTip.X + face.FaceLandmarks.NoseRightAlarOutTip.X +
                    face.FaceLandmarks.NoseTip.X) / 5;
                int faceNoseY = (int)(face.FaceLandmarks.NoseRootLeft.Y + face.FaceLandmarks.NoseRootRight.Y +
                    face.FaceLandmarks.NoseLeftAlarOutTip.Y + face.FaceLandmarks.NoseRightAlarOutTip.Y +
                    face.FaceLandmarks.NoseTip.Y) / 5;
                int face2NoseX = (int)(face2.FaceLandmarks.NoseRootLeft.X + face2.FaceLandmarks.NoseRootRight.X +
                    face2.FaceLandmarks.NoseLeftAlarOutTip.X + face2.FaceLandmarks.NoseRightAlarOutTip.X +
                    face2.FaceLandmarks.NoseTip.X) / 5;
                int face2NoseY = (int)(face2.FaceLandmarks.NoseRootLeft.Y + face2.FaceLandmarks.NoseRootRight.Y +
                    face2.FaceLandmarks.NoseLeftAlarOutTip.Y + face2.FaceLandmarks.NoseRightAlarOutTip.Y +
                    face2.FaceLandmarks.NoseTip.Y) / 5;
                int offsetX = face2NoseX - faceNoseX;
                int offsetY = face2NoseY - faceNoseY;



                int pixelHeight = renderingImage.PixelHeight;
                int bitsPerPixel = renderingImage.Format.BitsPerPixel;
                int pixelWidth = renderingImage.PixelWidth;
                int stride = pixelWidth * bitsPerPixel / 8;

                int hismanStride = hismanImage.PixelWidth * hismanImage.Format.BitsPerPixel / 8;

                int[] allPixels = new int[hismanStride * hismanImage.PixelHeight];
                hismanImage.CopyPixels(allPixels, hismanStride, 0);
                targets[0].WritePixels(
                new Int32Rect(0, 0, hismanImage.PixelWidth, hismanImage.PixelHeight),
                                allPixels, hismanStride, 0);

                ////高度宽度矫正 TODO:
                int up = upRightY < upLeftY ? upRightY : upLeftY;
                up = up < upRight2Y ? up : upRight2Y;
                up = up < upLeft2Y ? up : upLeft2Y;
                int down = downRightY > downLeftY ? downRightY + 1 : downLeftY + 1;
                down = down > downMiddle ? down : downMiddle;
                int height = down - up;
                int maxWidth = upRightX - upLeftX;
                int minWidth = downRightX - downLeftX;
                int halfWidthDiff = (maxWidth - minWidth) / 2;
                down += height * 15 / 100; ;
                up -= height * 10 / 100;
                height = down - up;

                int[] leftEdge = new int[down - up];
                int[] rightEdge = new int[down - up];
                for (int i = 0; i < down - up - height / 10; i++)
                {
                    leftEdge[i] = upLeftX + halfWidthDiff * i / height - (int)System.Math.Sqrt(maxWidth - System.Math.Abs((down - up) / 2 - i));
                    rightEdge[i] = upRightX - halfWidthDiff * i / height + (int)System.Math.Sqrt(maxWidth - System.Math.Abs((down - up) / 2 - i));
                }
                for (int i = down - up - height / 10; i < down - up; i++)
                {
                    leftEdge[i] = upLeftX + halfWidthDiff * i / height;
                    rightEdge[i] = upRightX - halfWidthDiff * i / height;

                }
                List<byte[]> list = new List<byte[]>();
                for (int i = up; i < down; i += 1)
                {
                    int choosedWidth = rightEdge[i - up] - leftEdge[i - up];
                    int lineStride = choosedWidth * bitsPerPixel / 8;
                    byte[] pixels = new byte[lineStride];
                    var temp = new Int32Rect(leftEdge[i - up], i, choosedWidth, 1);
                    renderingImage.CopyPixels(temp, pixels, lineStride, 0);
                    list.Add(pixels);
                }
                for (int i = up; i < down; i += 1)
                {
                    int choosedWidth = rightEdge[i - up] - leftEdge[i - up];
                    int lineStride = choosedWidth * bitsPerPixel / 8;
                    targets[0].WritePixels(new Int32Rect(leftEdge[i - up] + offsetX, i + offsetY, choosedWidth, 1), list[i - up], lineStride, 0);
                }

            }
        }
        private string GetHair(Contract.Hair hair)
        {
            if (hair.HairColor.Length == 0)
            {
                if (hair.Invisible)
                    return "Invisible";
                else
                    return "Bald";
            }
            else
            {
                Contract.HairColorType returnColor = Contract.HairColorType.Unknown;
                double maxConfidence = 0.0f;

                for (int i = 0; i < hair.HairColor.Length; ++i)
                {
                    if (hair.HairColor[i].Confidence > maxConfidence)
                    {
                        maxConfidence = hair.HairColor[i].Confidence;
                        returnColor = hair.HairColor[i].Color;
                    }
                }

                return returnColor.ToString();
            }
        }

        private string GetAccessories(Contract.Accessory[] accessories)
        {
            if (accessories.Length == 0)
            {
                return "NoAccessories";
            }

            string []accessoryArray = new string[accessories.Length];
            
            for (int i = 0; i < accessories.Length; ++i)
            {
                accessoryArray[i] = accessories[i].Type.ToString();
            }

            return "Accessories: "+ String.Join(",", accessoryArray);
        }

        private string GetEmotion(Microsoft.ProjectOxford.Common.Contract.EmotionScores emotion)
        {
            string emotionType = string.Empty;
            double emotionValue = 0.0;
            if (emotion.Anger > emotionValue)
            {
                emotionValue = emotion.Anger;
                emotionType = "Anger";
            }
            if (emotion.Contempt > emotionValue)
            {
                emotionValue = emotion.Contempt;
                emotionType = "Contempt";
            }
            if (emotion.Disgust > emotionValue)
            {
                emotionValue = emotion.Disgust;
                emotionType = "Disgust";
            }
            if (emotion.Fear > emotionValue)
            {
                emotionValue = emotion.Fear;
                emotionType = "Fear";
            }
            if (emotion.Happiness > emotionValue)
            {
                emotionValue = emotion.Happiness;
                emotionType = "Happiness";
            }
            if (emotion.Neutral > emotionValue)
            {
                emotionValue = emotion.Neutral;
                emotionType = "Neutral";
            }
            if (emotion.Sadness > emotionValue)
            {
                emotionValue = emotion.Sadness;
                emotionType = "Sadness";
            }
            if (emotion.Surprise > emotionValue)
            {
                emotionValue = emotion.Surprise;
                emotionType = "Surprise";
            }
            return $"{emotionType}";
        }

        #endregion Methods
    }
}