﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TexturedFaceMeshViewer.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace FaceTracking3D
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Media3D;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.FaceTracking;

    using Brushes = System.Windows.Media.Brushes;
    using Point = System.Windows.Point;
    using PointF = Microsoft.Kinect.Toolkit.FaceTracking.PointF;
    using Size = System.Windows.Size;

    public class FaceImageGridUnit
    {
        private System.Windows.Controls.Image imageObj;

        private RenderTargetBitmap bmp;

        private Grid parentGrid;

        public System.Windows.Controls.Image ImageObj
        {
            get
            {
                return this.imageObj;
            }
            set
            {
                this.imageObj = value;
            }
        }

        public RenderTargetBitmap Bmp
        {
            get
            {
                return this.bmp;
            }
            set
            {
                this.bmp = value;
            }
        }


        public Grid ParentGrid
        {
            get
            {
                return this.parentGrid;
            }
            set
            {
                this.parentGrid = value;
            }
        }
    }

    /// <summary>
    /// Interaction logic for TexturedFaceMeshViewer.xaml
    /// </summary>
    public partial class TexturedFaceMeshViewer : UserControl, IDisposable
    {
        private const int IMAGE_MATRIX_WIDTH = 4;
        private const int IMAGE_MATRIX_HEIGHT = 4;

        public static readonly DependencyProperty KinectProperty = DependencyProperty.Register(
            "Kinect", 
            typeof(KinectSensor), 
            typeof(TexturedFaceMeshViewer), 
            new UIPropertyMetadata(
                null, 
                (o, args) =>
                ((TexturedFaceMeshViewer)o).OnKinectChanged((KinectSensor)args.OldValue, (KinectSensor)args.NewValue)));

        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private byte[] colorImage;

        private ColorImageFormat colorImageFormat = ColorImageFormat.Undefined;

        private WriteableBitmap colorImageWritableBitmap;

        private short[] depthImage;

        private DepthImageFormat depthImageFormat = DepthImageFormat.Undefined;

        private FaceTracker faceTracker;

        private Skeleton[] skeletonData;

        private int trackingId = -1;

        private FaceTriangle[] triangleIndices;

        private LimitedConcurrencyLevelTaskScheduler scheduler;

        private TaskFactory taskFactory;

        private FaceImageGridUnit[,] faceGrid;

        private int currentMatrixRow;
        private int currentMatrixColumn;


        private bool renderEnabled;

        private static System.Timers.Timer aTimer;

        public TexturedFaceMeshViewer()
        {
            this.DataContext = this;
            this.InitializeComponent();
            initTimer();
            initializeFaceGrid();
            setupAnimation();
            renderEnabled = true;

        }

        private void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            property.SetValue(target, value, null);
        }

        private void initializeFaceGrid()
        {
            faceGrid = new FaceImageGridUnit[IMAGE_MATRIX_WIDTH , IMAGE_MATRIX_HEIGHT];
            for (int i = 0; i < IMAGE_MATRIX_WIDTH; i++)
                for (int j = 0; j < IMAGE_MATRIX_HEIGHT; j++)
                {
                    faceGrid[i,j] = new FaceImageGridUnit();
                    faceGrid[i,j].Bmp = new RenderTargetBitmap(150, 150, 96, 96, PixelFormats.Pbgra32);
                    
                    object element = FindName("FaceImage" + i + j);
                    faceGrid[i,j].ImageObj = (System.Windows.Controls.Image)element;

                    element = FindName("FaceImageGrid" + i + j);
                    faceGrid[i,j].ParentGrid = (Grid)element;
                }

            currentMatrixRow = 0;
            currentMatrixColumn = 0;

            
        }

        public KinectSensor Kinect
        {
            get
            {
                return (KinectSensor)this.GetValue(KinectProperty);
            }

            set
            {
                this.SetValue(KinectProperty, value);
            }
        }

        public void Dispose()
        {
            this.DestroyFaceTracker();
        }

        private void AllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs)
        {
            ColorImageFrame colorImageFrame = null;
            DepthImageFrame depthImageFrame = null;
            SkeletonFrame skeletonFrame = null;

            try
            {
                colorImageFrame = allFramesReadyEventArgs.OpenColorImageFrame();
                depthImageFrame = allFramesReadyEventArgs.OpenDepthImageFrame();
                skeletonFrame = allFramesReadyEventArgs.OpenSkeletonFrame();

                if (colorImageFrame == null || depthImageFrame == null || skeletonFrame == null)
                {
                    return;
                }

                // Check for changes in any of the data this function is receiving
                // and reset things appropriately.
                if (this.depthImageFormat != depthImageFrame.Format)
                {
                    this.DestroyFaceTracker();
                    this.depthImage = null;
                    this.depthImageFormat = depthImageFrame.Format;
                }

                if (this.colorImageFormat != colorImageFrame.Format)
                {
                    this.DestroyFaceTracker();
                    this.colorImage = null;
                    this.colorImageFormat = colorImageFrame.Format;
                    this.colorImageWritableBitmap = null;
                    this.ColorImage.Source = null;
                    this.theMaterial.Brush = null;
                }

                if (this.skeletonData != null && this.skeletonData.Length != skeletonFrame.SkeletonArrayLength)
                {
                    this.skeletonData = null;
                }

                // Create any buffers to store copies of the data we work with
                if (this.depthImage == null)
                {
                    this.depthImage = new short[depthImageFrame.PixelDataLength];
                }

                if (this.colorImage == null)
                {
                    this.colorImage = new byte[colorImageFrame.PixelDataLength];
                }

                if (this.colorImageWritableBitmap == null)
                {
                    this.colorImageWritableBitmap = new WriteableBitmap(
                        colorImageFrame.Width, colorImageFrame.Height, 96, 96, PixelFormats.Bgr32, null);
                    this.ColorImage.Source = this.colorImageWritableBitmap;
                    this.theMaterial.Brush = new ImageBrush(this.colorImageWritableBitmap)
                        {
                           ViewportUnits = BrushMappingMode.Absolute 
                        };
                }

                if (this.skeletonData == null)
                {
                    this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                }

                // Copy data received in this event to our buffers.
                colorImageFrame.CopyPixelDataTo(this.colorImage);
                depthImageFrame.CopyPixelDataTo(this.depthImage);
                skeletonFrame.CopySkeletonDataTo(this.skeletonData);
                this.colorImageWritableBitmap.WritePixels(
                    new Int32Rect(0, 0, colorImageFrame.Width, colorImageFrame.Height), 
                    this.colorImage, 
                    colorImageFrame.Width * Bgr32BytesPerPixel, 
                    0);

                // Find a skeleton to track.
                // First see if our old one is good.
                // When a skeleton is in PositionOnly tracking state, don't pick a new one
                // as it may become fully tracked again.
                Skeleton skeletonOfInterest =
                    this.skeletonData.FirstOrDefault(
                        skeleton =>
                        skeleton.TrackingId == this.trackingId
                        && skeleton.TrackingState != SkeletonTrackingState.NotTracked);

                if (skeletonOfInterest == null)
                {
                    // Old one wasn't around.  Find any skeleton that is being tracked and use it.
                    skeletonOfInterest =
                        this.skeletonData.FirstOrDefault(
                            skeleton => skeleton.TrackingState == SkeletonTrackingState.Tracked);

                    if (skeletonOfInterest != null)
                    {
                        // This may be a different person so reset the tracker which
                        // could have tuned itself to the previous person.
                        if (this.faceTracker != null)
                        {
                            this.faceTracker.ResetTracking();
                        }

                        this.trackingId = skeletonOfInterest.TrackingId;
                    }
                }

                bool displayFaceMesh = false;

                if (skeletonOfInterest != null && skeletonOfInterest.TrackingState == SkeletonTrackingState.Tracked)
                {
                    if (this.faceTracker == null)
                    {
                        try
                        {
                            this.faceTracker = new FaceTracker(this.Kinect);
                        }
                        catch (InvalidOperationException)
                        {
                            // During some shutdown scenarios the FaceTracker
                            // is unable to be instantiated.  Catch that exception
                            // and don't track a face.
                            Debug.WriteLine("AllFramesReady - creating a new FaceTracker threw an InvalidOperationException");
                            this.faceTracker = null;
                        }
                    }

                    if (this.faceTracker != null)
                    {
                        FaceTrackFrame faceTrackFrame = this.faceTracker.Track(
                            this.colorImageFormat,
                            this.colorImage,
                            this.depthImageFormat,
                            this.depthImage,
                            skeletonOfInterest);

                        if (faceTrackFrame.TrackSuccessful)
                        {
                            this.UpdateMesh(faceTrackFrame);

                            // Only display the face mesh if there was a successful track.
                            displayFaceMesh = true;
                        }
                    }
                }
                else
                {
                    this.trackingId = -1;
                }

                this.viewport3d.Visibility = displayFaceMesh ? Visibility.Visible : Visibility.Hidden;
                //this.viewport3d.Visibility = Visibility.Hidden;
            }
            finally
            {
                if (colorImageFrame != null)
                {
                    colorImageFrame.Dispose();
                }

                if (depthImageFrame != null)
                {
                    depthImageFrame.Dispose();
                }

                if (skeletonFrame != null)
                {
                    skeletonFrame.Dispose();
                }
            }
        }

        private void DestroyFaceTracker()
        {
            if (this.faceTracker != null)
            {
                this.faceTracker.Dispose();
                this.faceTracker = null;
            }
        }

        private void OnKinectChanged(KinectSensor oldSensor, KinectSensor newSensor)
        {
            if (oldSensor != null)
            {
                try
                {
                    oldSensor.AllFramesReady -= this.AllFramesReady;

                    this.DestroyFaceTracker();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (newSensor != null)
            {
                try
                {
                    this.faceTracker = new FaceTracker(this.Kinect);

                    newSensor.AllFramesReady += this.AllFramesReady;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        private void UpdateMesh(FaceTrackFrame faceTrackingFrame)
        {
            //Console.Out.WriteLine(" ###################### In UpdateMesh ############################# ");
            bool faceInCentre = true;

            EnumIndexableCollection<FeaturePoint, Vector3DF> shapePoints = faceTrackingFrame.Get3DShape();
            EnumIndexableCollection<FeaturePoint, PointF> projectedShapePoints = faceTrackingFrame.GetProjected3DShape();

            if (this.triangleIndices == null)
            {
                // Update stuff that doesn't change from frame to frame
                this.triangleIndices = faceTrackingFrame.GetTriangles();
                var indices = new Int32Collection(this.triangleIndices.Length * 3);
                foreach (FaceTriangle triangle in this.triangleIndices)
                {
                    indices.Add(triangle.Third);
                    indices.Add(triangle.Second);
                    indices.Add(triangle.First);
                }

                this.theGeometry.TriangleIndices = indices;
                this.theGeometry.Normals = null; // Let WPF3D calculate these.

                this.theGeometry.Positions = new Point3DCollection(shapePoints.Count);
                this.theGeometry.TextureCoordinates = new PointCollection(projectedShapePoints.Count);
                for (int pointIndex = 0; pointIndex < shapePoints.Count; pointIndex++)
                {
                    this.theGeometry.Positions.Add(new Point3D());
                    this.theGeometry.TextureCoordinates.Add(new Point());
                }
            }

            // Update the 3D model's vertices and texture coordinates
            for (int pointIndex = 0; pointIndex < shapePoints.Count; pointIndex++)
            {

                Vector3DF point = shapePoints[pointIndex];
                this.theGeometry.Positions[pointIndex] = new Point3D(point.X, point.Y, -point.Z);

                PointF projected = projectedShapePoints[pointIndex];

                this.theGeometry.TextureCoordinates[pointIndex] =
                    new Point(
                        projected.X/ (double)this.colorImageWritableBitmap.PixelWidth,
                        projected.Y/ (double)this.colorImageWritableBitmap.PixelHeight);

//                Console.Out.WriteLine("X = " + projected.X / (double)this.colorImageWritableBitmap.PixelWidth  + "Y = " + projected.Y / (double)this.colorImageWritableBitmap.PixelHeight);
                if (projected.X / (double)this.colorImageWritableBitmap.PixelWidth > .6 || 
                    projected.Y / (double)this.colorImageWritableBitmap.PixelHeight > .75) faceInCentre = false;
            }

            if (faceInCentre)
                copyFaceImage();
        }

        private void copyFaceImage()
        {

            if (renderEnabled) 
                renderEnabled = false;
            else
                return;

            int width = 600;
            int height = 600;

            viewport3d.Width = width;

            viewport3d.Height = height;

            viewport3d.Measure(new Size(width, height));

            viewport3d.Arrange(new System.Windows.Rect(0, 0, width, height));

//            bmp = new RenderTargetBitmap((int)viewport3d.ActualWidth, (int)viewport3d.ActualHeight, 96, 96, PixelFormats.Pbgra32);
//            bmp.Render(viewport3d);

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)viewport3d.ActualWidth,
                (int)viewport3d.ActualHeight, 96, 96, PixelFormats.Default);
            bmp.Render(viewport3d);
            //FaceImage.imageObj = bmp;
//            viewport3d.Visibility = Visibility.Collapsed;
//            FaceImage.Visibility = Visibility.Visible;

            faceGrid[currentMatrixRow, currentMatrixColumn].ImageObj.Source = bmp;
            faceGrid[currentMatrixRow, currentMatrixColumn].ImageObj.Visibility = Visibility.Visible;

            if (currentMatrixRow < 3) currentMatrixRow++;
            else
            {
                currentMatrixRow = 0;
                if (currentMatrixColumn < 3) currentMatrixColumn++;
                else
                    currentMatrixColumn = 0;
            }


            //this.saveFaceImageToFile(bmp);
            //taskFactory.StartNew(() => saveFaceImageToFile(bmp));
        }

        private void initTimer()
        {
            // Create a timer with a ten second interval.
            aTimer = new System.Timers.Timer(10000);

            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            // Set the Interval to 2 seconds (2000 milliseconds).
            aTimer.Interval = 3000;
            aTimer.Enabled = true;            
        }

        // Specify what you want to happen when the Elapsed event is  
        // raised. 
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            renderEnabled = true;
        }


        private void saveFaceImageToFile(RenderTargetBitmap bmp)
        {
            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bmp));

            using (Stream stm = File.Create(DateTime.Now.Ticks + "_temp.png"))
            {
                png.Save(stm);
            }
            
        }

        private void setupAnimation()
        {
            DoubleAnimation da = new DoubleAnimation(200, 0, new Duration(TimeSpan.FromSeconds(3)));
            da.AutoReverse = true;

            BounceEase bounce = new BounceEase();
            bounce.Bounces = 4;
            bounce.Bounciness = 2;
            bounce.EasingMode = EasingMode.EaseOut;
            da.EasingFunction = bounce;

            TranslateTransform rt = new TranslateTransform();

            faceGrid[0,0].ParentGrid.RenderTransform = rt;
            faceGrid[0,0].ParentGrid.RenderTransformOrigin = new Point(0.5, 0.5);

            for (int i = 0; i < IMAGE_MATRIX_WIDTH; i++)
                for (int j = 0; j < IMAGE_MATRIX_HEIGHT; j++)
                {
                    faceGrid[i, j].ParentGrid.RenderTransform = rt;
                    faceGrid[i, j].ParentGrid.RenderTransformOrigin = new Point(0.5, 0.5);
                }


            da.RepeatBehavior = RepeatBehavior.Forever;
            rt.BeginAnimation(TranslateTransform.XProperty, da);
            
        }


        private void setupPathAnimation()
        {
        }
    }
}