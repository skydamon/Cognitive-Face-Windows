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

using ClientLibrary.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClientContract = Microsoft.ProjectOxford.Face.Contract;

namespace Microsoft.ProjectOxford.Face.Controls
{
    /// <summary>
    /// Interaction logic for FaceFindSimilar.xaml
    /// </summary>
    public partial class FaceFindSimilarPage : Page, INotifyPropertyChanged
    {

        #region Fields

        /// <summary>
        /// Description dependency property
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(FaceFindSimilarPage));

        /// <summary>
        /// Faces collection which will be used to find similar from
        /// </summary>
        private ObservableCollection<Face> _facesCollection = new ObservableCollection<Face>();

        /// <summary>
        /// Find personal match mode similar results
        /// </summary>
        private ObservableCollection<FindSimilarResult> _findSimilarMatchPersonCollection = new ObservableCollection<FindSimilarResult>();

        /// <summary>
        /// Find facial match mode similar results  
        /// </summary>
        private ObservableCollection<FindSimilarResult> _findSimilarMatchCollection = new ObservableCollection<FindSimilarResult>();

        /// <summary>
        /// User picked image file
        /// </summary>
        private ImageSource _selectedFile;
        private Guid[] faceid_list = new Guid[6]; //= new String()[10];//Guid.NewGuid()[10];
        //private HashSet<string, string> media_name = new HashSet<string, string>;
        //private Hashtable media_name = new Hashtable(); 
        private Dictionary<string, string> media_name = new Dictionary<string, string>();
        /// <summary>
        /// Query faces
        /// </summary>
        private ObservableCollection<Face> _targetFaces = new ObservableCollection<Face>();

        /// <summary>
        /// max concurrent process number for client query.
        /// </summary>
        private int _maxConcurrentProcesses;

        /// <summary>
        /// Temporary stored face list name
        /// </summary>
        private string _faceListName = Guid.NewGuid().ToString();

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceFindSimilarPage" /> class
        /// </summary>
        public FaceFindSimilarPage()
        {
            InitializeComponent();
            _maxConcurrentProcesses = 4;
        }
        #endregion Constructors

        #region Events

        /// <summary>
        /// Implement INotifyPropertyChanged interface
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
        /// Gets faces collection which will be used to find similar from 
        /// </summary>
        public ObservableCollection<Face> FacesCollection
        {
            get
            {
                return _facesCollection;
            }
        }

        /// <summary>
        /// Gets find "matchFace" mode similar results
        /// </summary>
        public ObservableCollection<FindSimilarResult> FindSimilarMatchFaceCollection
        {
            get
            {
                return _findSimilarMatchCollection;
            }
        }
        
        /// <summary>
        /// Gets find "matchPerson" mode similar results
        /// </summary>
        public ObservableCollection<FindSimilarResult> FindSimilarMatchPersonCollection
        {
            get
            {
                return _findSimilarMatchPersonCollection;
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
        /// Gets or sets user picked image file
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

        /// <summary>
        /// Gets query faces
        /// </summary>
        public ObservableCollection<Face> TargetFaces
        {
            get
            {
                return _targetFaces;
            }
        }
        public Guid[] faceidlist
        {
            get
            {
                return faceid_list;
            }
        }
        private ImageSource _MergeImage1;
        //public ImageSource MergeImage1;//=> getMergedPicture("D://1.jpg", "D://3.jpg");
        
        public ImageSource MergeImage1
        {
            get
            {
                return _MergeImage1;
            }

            set
            {
                _MergeImage1 = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("MergeImage1"));
                }
            }
        }
        
        #endregion Properties

        #region Methods

        /// <summary>
        /// Pick image and call find similar with both two modes for each faces detected
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void FindSimilar_Click(object sender, RoutedEventArgs e)
        {
            // Show file picker
            //OpenCameraButton.IsEnabled = false;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".jpg";
            dlg.Filter = "Image files (*.jpg, *.png, *.bmp, *.gif) | *.jpg; *.png; *.bmp; *.gif";
            var filePicker = dlg.ShowDialog();

            if (filePicker.HasValue && filePicker.Value)
            {
                // User picked image
                // Clear previous detection and find similar results
                TargetFaces.Clear();
                FindSimilarMatchPersonCollection.Clear();
                FindSimilarMatchFaceCollection.Clear();
                var sw = Stopwatch.StartNew();

                var pickedImagePath = dlg.FileName;
                var renderingImage = UIHelper.LoadImageAppliedOrientation(pickedImagePath);
                var imageInfo = UIHelper.GetImageInfoForRendering(renderingImage);
                SelectedFile = renderingImage;

                // Detect all faces in the picked image
                using (var fStream = File.OpenRead(pickedImagePath))
                {
                    MainWindow.Log("Request: Detecting faces in {0}", SelectedFile);

                    MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
                    string subscriptionKey = mainWindow._scenariosControl.SubscriptionKey;
                    string endpoint = mainWindow._scenariosControl.SubscriptionEndpoint;
                    var faceServiceClient = new FaceServiceClient(subscriptionKey, endpoint);
                    var faces = await faceServiceClient.DetectAsync(fStream);

                    // Update detected faces on UI
                    foreach (var face in UIHelper.CalculateFaceRectangleForRendering(faces, MaxImageSize, imageInfo))
                    {
                        TargetFaces.Add(face);
                    }

                    MainWindow.Log("Response: Success. Detected {0} face(s) in {1}", faces.Length, SelectedFile);

                    // Find two modes similar faces for each face
                    foreach (var f in faces)
                    {
                        var faceId = f.FaceId;
                        MainWindow.Log("Request: Finding similar faces in Personal Match Mode for face {0}", faceId);

                        try
                        {
                            // Default mode, call find matchPerson similar REST API, the result contains all the face ids which is personal similar to the query face
                            const int requestCandidatesCount = 4;
                            var result = await faceServiceClient.FindSimilarAsync(faceId, faceid_list, requestCandidatesCount);
                            //faceServiceClient.F                           
                            // Update find matchPerson similar results collection for rendering
                            var personSimilarResult = new FindSimilarResult();
                            personSimilarResult.Faces = new ObservableCollection<Face>();
                            personSimilarResult.QueryFace = new Face()
                            {
                                ImageFile = SelectedFile,
                                Top = f.FaceRectangle.Top,
                                Left = f.FaceRectangle.Left,
                                Width = f.FaceRectangle.Width,
                                Height = f.FaceRectangle.Height,
                                FaceId = faceId.ToString(),
                            };
                            foreach (var fr in result)
                            {
                                var candidateFace = FacesCollection.First(ff => ff.FaceId == fr.FaceId.ToString());
                                Face newFace = new Face();
                                newFace.ImageFile = candidateFace.ImageFile;
                                newFace.Confidence = fr.Confidence;
                                newFace.FaceId = candidateFace.FaceId;
                                personSimilarResult.Faces.Add(newFace);
                            }

                            MainWindow.Log("Response: Found {0} similar faces for face {1}", personSimilarResult.Faces.Count, faceId);

                            FindSimilarMatchPersonCollection.Add(personSimilarResult);
                        }
                        catch (FaceAPIException ex)
                        {
                            MainWindow.Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                        }

                        try
                        {
                            // Call find facial match similar REST API, the result faces the top N with the highest similar confidence 
                            const int requestCandidatesCount = 4;
                            var result = await faceServiceClient.FindSimilarAsync(faceId, faceid_list, FindSimilarMatchMode.matchFace, requestCandidatesCount);

                            // Update "matchFace" similar results collection for rendering
                            var faceSimilarResults = new FindSimilarResult();
                            faceSimilarResults.Faces = new ObservableCollection<Face>();
                            faceSimilarResults.QueryFace = new Face()
                            {
                                ImageFile = SelectedFile,
                                Top = f.FaceRectangle.Top,
                                Left = f.FaceRectangle.Left,
                                Width = f.FaceRectangle.Width,
                                Height = f.FaceRectangle.Height,
                                FaceId = faceId.ToString(),
                            };
                            foreach (var fr in result)
                            {
                                var candidateFace = FacesCollection.First(ff => ff.FaceId == fr.FaceId.ToString());
                                Face newFace = new Face();
                                newFace.ImageFile = candidateFace.ImageFile;
                                //Bitmap imag = new Bitmap();
                                //(candidateFace.ImageFile);
                                //g2.
                                // MainWindow.Log("Response: Found {0} similar faces for face {1}", , faceId);
                                newFace.Confidence = fr.Confidence;
                                newFace.Top = candidateFace.Top;
                                newFace.Left = candidateFace.Left;
                                newFace.Width = candidateFace.Width;
                                newFace.Height = candidateFace.Height;
                                newFace.FaceId = fr.FaceId.ToString();//candidateFace.FaceId;
                                faceSimilarResults.Faces.Add(newFace);

                            }
                            var candidate1 = FacesCollection.First(ff => ff.FaceId == result[0].FaceId.ToString());
                            Bitmap graph = new Bitmap(UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Width, UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Height);
                            Graphics g2 = Graphics.FromImage(graph);

                            g2.DrawImage(UIHelper.ImageSourceToBitmap(candidate1.ImageFile), 0, 0);
                            // Rectangle zuibiao = new Rectangle(f.FaceRectangle.Left, f.FaceRectangle.Top, f.FaceRectangle.Width, f.FaceRectangle.Height);
                            Rectangle zuibiao = new Rectangle(candidate1.Left, candidate1.Top, candidate1.Width, candidate1.Height);
                            //g2.DrawImageUnscaled(UIHelper.ImageSourceToBitmap(candidateFace.ImageFile),0,0);
                            g2.DrawImage(UIHelper.ImageSourceToBitmap(SelectedFile), zuibiao, f.FaceRectangle.Left, f.FaceRectangle.Top, f.FaceRectangle.Width, f.FaceRectangle.Height, GraphicsUnit.Pixel);
                            System.Drawing.Image saveImage = System.Drawing.Image.FromHbitmap(graph.GetHbitmap());
                            saveImage.Save(@"E:\hackathon\ls\cognitive-Face-Windows\data1\image1.jpg", ImageFormat.Jpeg);

                            Bitmap graph1 = new Bitmap(UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Width, UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Height);
                            Graphics g3 = Graphics.FromImage(graph1);

                            g3.DrawImage(UIHelper.ImageSourceToBitmap(candidate1.ImageFile), 0, 0);
                            System.Drawing.Image saveImage1 = System.Drawing.Image.FromHbitmap(graph1.GetHbitmap());
                            saveImage1.Save(@"E:\hackathon\ls\cognitive-Face-Windows\image1.jpg", ImageFormat.Jpeg);
                            MainWindow.Log("Response: Found {0} similar faces for face {1}", faceSimilarResults.Faces.Count, faceId);
                            MergeImage1 = getMergedPicture(@"D:\3.jpg", @"E:\hackathon\ls\cognitive-Face-Windows\image1.jpg");
                            FindSimilarMatchFaceCollection.Add(faceSimilarResults);
                            /*MediaPlayer player = new MediaPlayer();
                            player.Open(new Uri(media_name[candidate1.FaceId].Substring(0, media_name[candidate1.FaceId].Length - 4) + ".WAV", UriKind.Relative));
                            player.Play();*/
                            Thread.Sleep(5000);

                        }
                        catch (FaceAPIException ex)
                        {
                            MainWindow.Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                        }
                    }
                }
            }
            GC.Collect();
            OpenFaceButton.IsEnabled = false;
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

        /// <summary>
        /// Pick image folder and detect all faces in these images
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void FolderPicker_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            string subscriptionKey = mainWindow._scenariosControl.SubscriptionKey;
            string endpoint = mainWindow._scenariosControl.SubscriptionEndpoint;
            var faceServiceClient = new FaceServiceClient(subscriptionKey, endpoint);
            /*try
            {
                MainWindow.Log("Request: Face List {0} will be used to build a person database. Checking whether the face list exists.", _faceListName);

                await faceServiceClient.GetFaceListAsync(_faceListName);
                groupExists = true;
                MainWindow.Log("Response: Face List {0} exists.", _faceListName);
            }
            catch (FaceAPIException ex)
            {
                if (ex.ErrorCode != "FaceListNotFound")
                {
                    MainWindow.Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                    return;
                }
                else
                {
                    MainWindow.Log("Response: Face List {0} did not exist previously.", _faceListName);
                }
            }

            if (groupExists)
            {
                var cleanFaceList = System.Windows.MessageBox.Show(string.Format("Requires a clean up for face list \"{0}\" before setting up a new face list. Click OK to proceed, face list \"{0}\" will be cleared.", _faceListName), "Warning", MessageBoxButton.OKCancel);
                if (cleanFaceList == MessageBoxResult.OK)
                {
                    await faceServiceClient.DeleteFaceListAsync(_faceListName);
                }
                else
                {
                    return;
                }
            }*/
            OpenCameraButton.IsEnabled = false;
            OpenFaceButton.IsEnabled = false;
            // Show folder picker
            //System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            //var result = dlg.ShowDialog();
            //string file_path = @"D:\microsoftAPI\cognitive-Face-Windows\Data\PersonGroup\Family1-Mom";
            string file_path = @"E:\hackathon\ls\cognitive-Face-Windows\data1";
            bool forceContinue = false;


            // if (result == System.Windows.Forms.DialogResult.OK)
            if (System.IO.Directory.Exists(file_path))
            {
                // Enumerate all ".jpg" files in the folder, call detect
                List<Task> tasks = new List<Task>();
                FacesCollection.Clear();
                //DetectionFacesCollection.Clear();
                TargetFaces.Clear();
                FindSimilarMatchPersonCollection.Clear();
                FindSimilarMatchFaceCollection.Clear();
                SelectedFile = null;


                // Set the suggestion count is intent to minimum the data preparation step only,
                // it's not corresponding to service side constraint
                const int SuggestionCount = 10;
                int processCount = 0;

                MainWindow.Log("Request: Preparing, detecting faces in chosen folder.");

                //await faceServiceClient.CreateFaceListAsync(_faceListName, _faceListName, "face list for sample");

                var imageList =
                    new ConcurrentBag<string>(
                        Directory.EnumerateFiles(file_path/*dlg.SelectedPath*/, "*.*", SearchOption.AllDirectories)
                            .Where(s => s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".png") || s.ToLower().EndsWith(".bmp") || s.ToLower().EndsWith(".gif")));

                string img;
                int invalidImageCount = 0;
                int i = 0;
                while (imageList.TryTake(out img))
                {
                    tasks.Add(Task.Factory.StartNew(
                        async (obj) =>
                        {
                            var imgPath = obj as string;
                            // Call detection
                            using (var fStream = File.OpenRead(imgPath))
                            {
                                try
                                {
                                    /*var faces =
                                        await faceServiceClient.AddFaceToFaceListAsync(_faceListName, fStream);*/
                                    // ProjectOxford.Face.Contract.Face[] faces = await faceServiceClient.DetectAsync(fStream, false, true, new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Glasses, FaceAttributeType.HeadPose, FaceAttributeType.FacialHair, FaceAttributeType.Emotion, FaceAttributeType.Hair, FaceAttributeType.Makeup, FaceAttributeType.Occlusion, FaceAttributeType.Accessories, FaceAttributeType.Noise, FaceAttributeType.Exposure, FaceAttributeType.Blur });
                                    var renderingImage = UIHelper.LoadImageAppliedOrientation(imgPath);
                                    var imageInfo = UIHelper.GetImageInfoForRendering(renderingImage);
                                    var faces1 = await faceServiceClient.DetectAsync(fStream);
                                    // ObservableCollection<Face> detection_tmp = new ObservableCollection<Face>();

                                    //faceServiceClient.
                                    // Update detected faces on UI
                                    //faces[0].FaceRectangle
                                    foreach (var face in faces1)
                                    {

                                        //      detection_tmp.Add(face);
                                        //DetectionFacesCollection.
                                        //_faceListName = _faceListName + "-" + face.FaceId;
                                        faceid_list[i] = face.FaceId;
                                        media_name.Add(face.FaceId.ToString(), imgPath);
                                        i++;
                                        //MainWindow.Log(" faceId", _faceListName);
                                        // _faceListName.
                                        //faceServiceClient.a
                                        //var face_list = await faceServiceClient.AddFaceToFaceListAsync(_faceListName, File.OpenRead(face.ImageFile));
                                    }
                                    return new Tuple<string, ClientContract.Face[]>(imgPath, faces1);
                                    /*foreach (var face in faces)
                                    {
                                        
                                        
                                    }*/
                                }
                                catch (FaceAPIException ex)
                                {
                                    // if operation conflict, retry.
                                    if (ex.ErrorCode.Equals("ConcurrentOperationConflict"))
                                    {
                                        imageList.Add(imgPath);
                                        return null;
                                    }
                                    // if operation cause rate limit exceed, retry.
                                    else if (ex.ErrorCode.Equals("RateLimitExceeded"))
                                    {
                                        imageList.Add(imgPath);
                                        return null;
                                    }
                                    /*else if (ex.ErrorMessage.Contains("more than 1 face in the image."))
                                    {
                                        Interlocked.Increment(ref invalidImageCount);
                                    }*/
                                    // Here we simply ignore all detection failure in this sample
                                    // You may handle these exceptions by check the Error.Error.Code and Error.Message property for ClientException object
                                    return new Tuple<string, ClientContract.Face[]>(imgPath, null);
                                }
                            }
                        },
                        img).Unwrap().ContinueWith((detectTask) =>
                        {
                            var res = detectTask?.Result;
                            if (res?.Item2 == null)
                            {
                                return;
                            }

                            // Update detected faces on UI
                            this.Dispatcher.Invoke(
                            new Action
                                <ObservableCollection<Face>, string, ClientContract.Face[]>(
                                UIHelper.UpdateFace),
                            FacesCollection,
                            res.Item1,
                            res.Item2);
                        }));

                    processCount++;

                    if (processCount >= SuggestionCount && !forceContinue)
                    {
                        var continueProcess =
                            System.Windows.Forms.MessageBox.Show(
                                "The images loaded have reached the recommended count, may take long time if proceed. Would you like to continue to load images?",
                                "Warning", System.Windows.Forms.MessageBoxButtons.YesNo);
                        if (continueProcess == System.Windows.Forms.DialogResult.Yes)
                        {
                            forceContinue = true;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (tasks.Count >= _maxConcurrentProcesses || imageList.IsEmpty)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                }
                if (invalidImageCount > 0)
                {
                    MainWindow.Log("Warning: more or less than one face is detected in {0} images, can not add to face list.", invalidImageCount);
                }
                MainWindow.Log("Response: Success. Total {0} faces are detected.", FacesCollection.Count);
            }
            else
            {
                MainWindow.Log("cannot open file");
            }
            GC.Collect();
            //OpenFaceButton.IsEnabled = true;
            OpenFaceButton.IsEnabled = true;
            OpenCameraButton.IsEnabled = true;
        }

        /// <summary>
        /// open camera dialog
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private async void OpenCamera_Click(object sender, RoutedEventArgs e)
        {
            //OpenFaceButton.IsEnabled = false;
            CameraOpen camera = new CameraOpen();
            camera.ShowDialog();
            //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.DefaultExt = ".jpg";
            //dlg.Filter = "Image files (*.jpg, *.png, *.bmp, *.gif) | *.jpg; *.png; *.bmp; *.gif";
            //var filePicker = dlg.ShowDialog();

            //if (filePicker.HasValue && filePicker.Value)
            //{
                // User picked image
                // Clear previous detection and find similar results
                TargetFaces.Clear();
                FindSimilarMatchPersonCollection.Clear();
                FindSimilarMatchFaceCollection.Clear();
                var sw = Stopwatch.StartNew();

                var pickedImagePath = @"D:\3.jpg";//dlg.FileName;
                var renderingImage = UIHelper.LoadImageAppliedOrientation(pickedImagePath);
                var imageInfo = UIHelper.GetImageInfoForRendering(renderingImage);
                SelectedFile = renderingImage;

                // Detect all faces in the picked image
                using (var fStream = File.OpenRead(pickedImagePath))
                {
                    MainWindow.Log("Request: Detecting faces in {0}", SelectedFile);

                    MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
                    string subscriptionKey = mainWindow._scenariosControl.SubscriptionKey;
                    string endpoint = mainWindow._scenariosControl.SubscriptionEndpoint;
                    var faceServiceClient = new FaceServiceClient(subscriptionKey, endpoint);
                    var faces = await faceServiceClient.DetectAsync(fStream);

                    // Update detected faces on UI
                    foreach (var face in UIHelper.CalculateFaceRectangleForRendering(faces, MaxImageSize, imageInfo))
                    {
                        TargetFaces.Add(face);
                    }

                    MainWindow.Log("Response: Success. Detected {0} face(s) in {1}", faces.Length, SelectedFile);

                    // Find two modes similar faces for each face
                    foreach (var f in faces)
                    {
                        var faceId = f.FaceId;
                        MainWindow.Log("Request: Finding similar faces in Personal Match Mode for face {0}", faceId);

                        try
                        {
                            // Default mode, call find matchPerson similar REST API, the result contains all the face ids which is personal similar to the query face
                            const int requestCandidatesCount = 4;
                            var result = await faceServiceClient.FindSimilarAsync(faceId, faceid_list, requestCandidatesCount);
                            //faceServiceClient.F                           
                            // Update find matchPerson similar results collection for rendering
                            var personSimilarResult = new FindSimilarResult();
                            personSimilarResult.Faces = new ObservableCollection<Face>();
                            personSimilarResult.QueryFace = new Face()
                            {
                                ImageFile = SelectedFile,
                                Top = f.FaceRectangle.Top,
                                Left = f.FaceRectangle.Left,
                                Width = f.FaceRectangle.Width,
                                Height = f.FaceRectangle.Height,
                                FaceId = faceId.ToString(),
                            };
                            foreach (var fr in result)
                            {
                                var candidateFace = FacesCollection.First(ff => ff.FaceId == fr.FaceId.ToString());
                                Face newFace = new Face();
                                newFace.ImageFile = candidateFace.ImageFile;
                                newFace.Confidence = fr.Confidence;
                                newFace.FaceId = candidateFace.FaceId;
                                personSimilarResult.Faces.Add(newFace);
                            }

                            MainWindow.Log("Response: Found {0} similar faces for face {1}", personSimilarResult.Faces.Count, faceId);

                            FindSimilarMatchPersonCollection.Add(personSimilarResult);
                        }
                        catch (FaceAPIException ex)
                        {
                            MainWindow.Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                        }

                        try
                        {
                            // Call find facial match similar REST API, the result faces the top N with the highest similar confidence 
                            const int requestCandidatesCount = 4;
                            var result = await faceServiceClient.FindSimilarAsync(faceId, faceid_list, FindSimilarMatchMode.matchFace, requestCandidatesCount);

                            // Update "matchFace" similar results collection for rendering
                            var faceSimilarResults = new FindSimilarResult();
                            faceSimilarResults.Faces = new ObservableCollection<Face>();
                            faceSimilarResults.QueryFace = new Face()
                            {
                                ImageFile = SelectedFile,
                                Top = f.FaceRectangle.Top,
                                Left = f.FaceRectangle.Left,
                                Width = f.FaceRectangle.Width,
                                Height = f.FaceRectangle.Height,
                                FaceId = faceId.ToString(),
                            };
                            foreach (var fr in result)
                            {
                                var candidateFace = FacesCollection.First(ff => ff.FaceId == fr.FaceId.ToString());
                                Face newFace = new Face();
                                newFace.ImageFile = candidateFace.ImageFile;
                                //Bitmap imag = new Bitmap();
                                //(candidateFace.ImageFile);
                                //g2.
                                // MainWindow.Log("Response: Found {0} similar faces for face {1}", , faceId);
                                newFace.Confidence = fr.Confidence;
                                newFace.Top = candidateFace.Top;
                                newFace.Left = candidateFace.Left;
                                newFace.Width = candidateFace.Width;
                                newFace.Height = candidateFace.Height;
                                newFace.FaceId = fr.FaceId.ToString();//candidateFace.FaceId;
                                faceSimilarResults.Faces.Add(newFace);

                            }
                            var candidate1 = FacesCollection.First(ff => ff.FaceId == result[0].FaceId.ToString());
                            Bitmap graph = new Bitmap(UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Width, UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Height);
                            Graphics g2 = Graphics.FromImage(graph);

                            g2.DrawImage(UIHelper.ImageSourceToBitmap(candidate1.ImageFile), 0, 0);
                            // Rectangle zuibiao = new Rectangle(f.FaceRectangle.Left, f.FaceRectangle.Top, f.FaceRectangle.Width, f.FaceRectangle.Height);
                            Rectangle zuibiao = new Rectangle(candidate1.Left, candidate1.Top, candidate1.Width, candidate1.Height);
                            //g2.DrawImageUnscaled(UIHelper.ImageSourceToBitmap(candidateFace.ImageFile),0,0);
                            g2.DrawImage(UIHelper.ImageSourceToBitmap(SelectedFile), zuibiao, f.FaceRectangle.Left, f.FaceRectangle.Top, f.FaceRectangle.Width, f.FaceRectangle.Height, GraphicsUnit.Pixel);
                            System.Drawing.Image saveImage = System.Drawing.Image.FromHbitmap(graph.GetHbitmap());
                            saveImage.Save(@"E:\hackathon\ls\cognitive-Face-Windows\data1\image1.jpg", ImageFormat.Jpeg);

                            Bitmap graph1 = new Bitmap(UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Width, UIHelper.ImageSourceToBitmap(candidate1.ImageFile).Height);
                            Graphics g3 = Graphics.FromImage(graph1);

                            g3.DrawImage(UIHelper.ImageSourceToBitmap(candidate1.ImageFile), 0, 0);
                            System.Drawing.Image saveImage1 = System.Drawing.Image.FromHbitmap(graph1.GetHbitmap());
                            saveImage1.Save(@"E:\hackathon\ls\cognitive-Face-Windows\image1.jpg", ImageFormat.Jpeg);
                            MainWindow.Log("Response: Found {0} similar faces for face {1}", faceSimilarResults.Faces.Count, faceId);
                        MergeImage1 = getMergedPicture(@"D:\3.jpg", @"E:\hackathon\ls\cognitive-Face-Windows\image1.jpg");
                        //MergeImage1 = getMergedPicture("D:\\3.jpg", "D:\\1.jpg");
                        FindSimilarMatchFaceCollection.Add(faceSimilarResults);
                        /* MediaPlayer player = new MediaPlayer();
                         player.Open(new Uri(media_name[candidate1.FaceId].Substring(0, media_name[candidate1.FaceId].Length - 4) + ".WAV", UriKind.Relative));
                         player.Play();*/
                        Thread.Sleep(4000);
                        }
                        catch (FaceAPIException ex)
                        {
                            MainWindow.Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                        }
                    }
                }
            //}
            //GC.Collect();
           // OpenFaceButton.IsEnabled = false;
            GC.Collect();

        }

        #endregion Methods

        #region Nested Types

        /// <summary>
        /// Find similar result for UI binding
        /// </summary>
        public class FindSimilarResult : INotifyPropertyChanged
        {
            #region Fields

            /// <summary>
            /// Similar faces collection
            /// </summary>
            private ObservableCollection<Face> _faces;
            
            /// <summary>
            /// Query face
            /// </summary>
            private Face _queryFace;

            #endregion Fields

            #region Events

            /// <summary>
            /// Implement INotifyPropertyChanged interface
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            #endregion Events

            #region Properties
            
            /// <summary>
            /// Gets or sets similar faces collection
            /// </summary>
            public ObservableCollection<Face> Faces
            {
                get
                {
                    return _faces;
                }

                set
                {
                    _faces = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Faces"));
                    }
                }
            }
            
            /// <summary>
            /// Gets or sets query face
            /// </summary>
            public Face QueryFace
            {
                get
                {
                    return _queryFace;
                }

                set
                {
                    _queryFace = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("QueryFace"));
                    }
                }
            }

            #endregion Properties
        }

        #endregion Nested Types        
    }
}