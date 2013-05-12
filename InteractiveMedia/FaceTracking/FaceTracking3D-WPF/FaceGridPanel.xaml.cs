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

namespace FaceTracking3D
{
    using System.IO;
    using System.Timers;
    using System.Windows.Media.Animation;

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
    /// Interaction logic for FaceGridPanel.xaml
    /// </summary>
    public partial class FaceGridPanel : UserControl
    {

        private const int IMAGE_MATRIX_WIDTH = 4;
        private const int IMAGE_MATRIX_HEIGHT = 4;

        private FaceImageGridUnit[,] faceGrid;

        private int currentMatrixRow;
        private int currentMatrixColumn;


        private bool renderEnabled;

        private static System.Timers.Timer aTimer;


        public FaceGridPanel()
        {
            InitializeComponent();

            initTimer();
            initializeFaceGrid();
            setupAnimation();
            renderEnabled = true;

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



        public void copyFaceImage(Viewport3D viewport3d)
        {

            if (renderEnabled)
                renderEnabled = false;
            else
                return;

            int width = 300;
            int height = 300;

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
            DoubleAnimation da = new DoubleAnimation(-200, 0, new Duration(TimeSpan.FromSeconds(3)));
            da.AutoReverse = true;

            BounceEase bounce = new BounceEase();
            bounce.Bounces = 4;
            bounce.Bounciness = 2;
            bounce.EasingMode = EasingMode.EaseOut;
            da.EasingFunction = bounce;

            TranslateTransform rt = new TranslateTransform();

            faceGrid[0, 0].ParentGrid.RenderTransform = rt;
            faceGrid[0, 0].ParentGrid.RenderTransformOrigin = new Point(0.5, 0.5);

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
            DoubleAnimation da = new DoubleAnimation(200, 0, new Duration(TimeSpan.FromSeconds(3)));
            da.AutoReverse = true;

            RotateTransform rt = new RotateTransform(30);
            FaceGrid.RenderTransform = rt;

            da.RepeatBehavior = RepeatBehavior.Forever;
            rt.BeginAnimation(RotateTransform.AngleProperty, da);
        }


    }
}
