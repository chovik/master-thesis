using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using FaceDetection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BibNumberDetectionUI
{
    public class RGBSample
    {
        public byte Red { get; set; }

        public byte Green { get; set; }

        public byte Blue { get; set; }

        private bool _isVisited = false;

        public bool IsVisited
        {
            get { return _isVisited; }
            set { _isVisited = value; }
        }

        private List<Point> _points;

        public List<Point> Points
        {
            get
            {
                if (_points == null)
                {
                    _points = new List<Point>();
                }

                return _points;
            }
        }


    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Mat> _processingImageWorkflow;

        public ObservableCollection<Mat> ProcessingImageWorkflow
        {
            get
            {
                if (_processingImageWorkflow == null)
                {
                    _processingImageWorkflow = new ObservableCollection<Mat>();
                }

                return _processingImageWorkflow;
            }
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">The Emgu CV Image</param>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                //DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _addImageToTheList = mat =>
            {
                ProcessingImageWorkflow.Add(mat.Clone());
            };
        }
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Run();
            //OpenFileDialog ofd = new OpenFileDialog();

            //var fileSelected = ofd.ShowDialog();
            //if(fileSelected.HasValue
            //    && fileSelected.Value)
            //{
            //    //DetectFace.Detect()
            //}
        }

        static System.Drawing.Rectangle ComputeTorsoArea(System.Drawing.Rectangle faceArea, System.Drawing.Size imageRectangle)
        {
            double torsoWidth = faceArea.Width * (7.0 / 3.0);
            double torsoHeight = faceArea.Height * 3.0;
            double faceCenterX = faceArea.X + (faceArea.Width * 0.5);
            double faceCenterY = faceArea.Y + (faceArea.Height * 0.5);

            double torsoHalfWidth = torsoWidth * 0.5;
            double torsoHalfHeight = torsoHeight * 0.5;

            double torsoX = faceCenterX - torsoHalfWidth;
            double torsoY = faceCenterY + (2 * 0.5 * faceArea.Height);

            if (torsoX < 0)
            {
                var diffX = -torsoX;
                torsoX = 0;
                torsoWidth -= diffX;
            }

            if (torsoY < 0)
            {
                var diffY = -torsoY;
                torsoY = 0;
                torsoHeight -= diffY;
            }

            //if (torsoX + torsoWidth > imageRectangle.Width)
            //{
            //    var diffY = -torsoY;
            //    torsoY = 0;
            //    torsoHeight -= diffY;
            //}


            //if (torsoY + torsoHeight > imageRectangle.Height)
            //{
            //    torsoWidth
            //}

            System.Drawing.Rectangle torsoRectangle = new System.Drawing.Rectangle((int)Math.Round(torsoX), (int)Math.Round(torsoY), (int)Math.Round(torsoWidth), (int)Math.Round(torsoHeight));

            if (torsoRectangle.Right > imageRectangle.Width)
            {
                double rightDiff = torsoRectangle.Right - imageRectangle.Width;
                torsoRectangle.Width -= (int)Math.Round(rightDiff);
            }

            if (torsoRectangle.Bottom > imageRectangle.Height)
            {
                double bottomDiff = torsoRectangle.Bottom - imageRectangle.Height;
                torsoRectangle.Height -= (int)Math.Round(bottomDiff);
            }

            return torsoRectangle;
        }

        Action<Mat> _addImageToTheList = null;

        private int CalculateDistanceFromEdgePixel(int rowIndex, int columnIndex, Matrix<byte> edgeMatrix, int xDirection = 0, int yDirection = 0)
        {
            var pixelValue = edgeMatrix[rowIndex, columnIndex];

            if (pixelValue > 0)
            {
                return 0;
            }
            else
            {
                var currentValue = pixelValue;
                int i = 0;

                while (currentValue != 255)
                {
                    i++;
                    currentValue = edgeMatrix[rowIndex + yDirection * i, columnIndex + xDirection * i];
                }

                return i;
            }
        }

        private double ComputeIntersectionSurfaceValue(int rowIndex, int columnIndex, Matrix<byte> edgeMatrix, Matrix<byte> grayMatrix)
        {
            //            x1px2y1y2+x1ݔ2py1y2+x1x2ݕ1pݕଶ+x1x2y1y2p
            //x2y1y2+x1y1y2+x1x2y2+x1x2y1
            var x1 = CalculateDistanceFromEdgePixel(rowIndex, columnIndex, edgeMatrix, -1, 0);
            var x2 = CalculateDistanceFromEdgePixel(rowIndex, columnIndex, edgeMatrix, 1, 0);
            var y1 = CalculateDistanceFromEdgePixel(rowIndex, columnIndex, edgeMatrix, 0, -1);
            var y2 = CalculateDistanceFromEdgePixel(rowIndex, columnIndex, edgeMatrix, 0, 1);

            //if(x1 > 1)
            //{
            //    x1--;
            //}

            //if (x2 > 1)
            //{
            //    x2--;
            //}

            //if (y1 > 1)
            //{
            //    y1--;
            //}

            //if (y2 > 1)
            //{
            //    y2--;
            //}

            var x1p = grayMatrix[rowIndex, columnIndex - x1];
            var x2p = grayMatrix[rowIndex, columnIndex + x2];
            var y1p = grayMatrix[rowIndex - y1, columnIndex];
            var y2p = grayMatrix[rowIndex + y2, columnIndex];

            var value = (x1p * x2 * y1 * y2) + (x1 * x2p * y1 * y2) + (x1 * x2 * y1p * y2) + (x1 * x2 * y1 * y2p);
            value = value / ((x2 * y1 * y2) + (x1 * y1 * y2) + (x1 * x2 * y2) + (x1 * x2 * y1));

            return value;

        }

        private Matrix<double> CreateIntersectionSurfaceMatrix(Matrix<byte> edgeMatrix, Matrix<byte> grayMatrix, Matrix<double> skeletonMatrix)
        {
            Matrix<double> matrix = skeletonMatrix.Clone();

            for (int rowIndex = 0; rowIndex < matrix.Rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < matrix.Cols; columnIndex++)
                {
                    var edgeVal = edgeMatrix[rowIndex, columnIndex];

                    if (edgeVal <= 0)
                    {
                        matrix[rowIndex, columnIndex] = ComputeIntersectionSurfaceValue(rowIndex, columnIndex, edgeMatrix, grayMatrix);
                    }
                    //else
                    //{
                    //    matrix[rowIndex, columnIndex] = 255;
                    //}
                }
            }

            return matrix;
        }

        async Task BinaryImage(Mat gray, int number, Mat canny = null, Mat color = null)
        {
            if(gray.Width > 160)
            {
                CvInvoke.MedianBlur(gray, gray, 5);
            }
            
            var image = gray;
            image.Save("image-" + number + ".bmp");

             
    
            using(var gausssian1 = new Image<Gray, byte>(image.Size))
            {
                using (var gausssian2 = new Image<Gray, byte>(image.Size))
                {
                    CvInvoke.GaussianBlur(gray, gausssian1, new System.Drawing.Size(5, 5), 0);
                    CvInvoke.GaussianBlur(gray, gausssian2, new System.Drawing.Size(1, 1), 0);
                    var result = gausssian1 - gray.ToImage<Gray, byte>();
                    result.Save("gauss-dog-" + number + ".bmp");
               
            

            var imageData = EdgePreservingSmoothingBW(gray, 5);

            using (Matrix<byte> edgeSmoothingImage = new Matrix<byte>(imageData))
            //using (Mat image2 = new Mat(@"IMG_0041-Gray.jpg", LoadImageType.Grayscale))
            using (Mat cannyImage = new Mat())
            {
                await Dispatcher.BeginInvoke(_addImageToTheList,
                    edgeSmoothingImage.Mat);

                edgeSmoothingImage.Save("edgeSmoothingImage" + number + ".jpg");

                await Dispatcher.BeginInvoke(_addImageToTheList,
                    image);
                //await Dispatcher.BeginInvoke(_addImageToTheList,
                //    image2);

                var increasedContrasstArray = ChangeContrast(image, 80);

                using (var changedContrastImg = new Image<Gray, byte>(increasedContrasstArray))
                {
                    await Dispatcher.BeginInvoke(_addImageToTheList,
                    changedContrastImg.Mat);

                    changedContrastImg.Save("changedContrastImg" + number + ".jpg");

                    //CvInvoke.Threshold(changedContrastImg, cannyImage, 200, 255, ThresholdType.Binary);

                    //using(Mat sobel = new Mat())
                    //{

                    //}

                    Matrix<byte> sobelMatrix = new Matrix<byte>(image.Size);

                    var sobelX = new Mat(changedContrastImg.Size, DepthType.Cv8U, 1);
                    var sobelY = new Mat(changedContrastImg.Size, DepthType.Cv8U, 1);

                    CvInvoke.Sobel(gray, sobelX, DepthType.Cv8U, 1, 1);

                    sobelX.Save("sobelX-" + number + ".bmp");

                    CvInvoke.Sobel(gray, sobelX, DepthType.Cv8U, 1, 0);
                    CvInvoke.Sobel(gray, sobelY, DepthType.Cv8U, 0, 1);



                    for (int rowIndex = 0; rowIndex < changedContrastImg.Rows; rowIndex++)
                    {
                        for (int columnIndex = 0; columnIndex < changedContrastImg.Cols; columnIndex++)
                        {
                            var rX = sobelX.GetData(rowIndex, columnIndex)[0];
                            var rY = sobelY.GetData(rowIndex, columnIndex)[0];
                            sobelMatrix[rowIndex, columnIndex] = ToByte(Math.Sqrt(rX * rX + rY * rY));
                        }
                    }

                    //CvInvoke.Threshold(sobelMatrix, sobelMatrix, 170, 255, ThresholdType.Binary);

                    await Dispatcher.BeginInvoke(_addImageToTheList,
                            sobelMatrix.Mat);

                    sobelMatrix.Save("sobelMatrix" + number + ".bmp");

                    CvInvoke.Laplacian(image, sobelMatrix, DepthType.Cv8U, 3, 1, 0, BorderType.Default);

                    

                        sobelMatrix.Save("laplacian" + number + ".bmp");

                    //CvInvoke.Threshold(sobelMatrix, sobelMatrix, 170, 255, ThresholdType.Binary);//170-190
                    //CvInvoke.AdaptiveThreshold(sobelMatrix, sobelMatrix, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 11, 2);

                        for (int row = 0; row < sobelMatrix.Rows; row++)
                        {
                            for (int column = 0; column < sobelMatrix.Cols; column++)
                            {
                                sobelMatrix[row, column] = result.Data[row, column, 0];
                            }
                        }

                        var edgeSmoothBWAray = ToArray(sobelMatrix.Mat);

                    var maxArray = new byte[sobelMatrix.Rows, sobelMatrix.Cols];

                    for(int row = 0; row < sobelMatrix.Rows; row++)
                    {
                        for(int column = 0; column < sobelMatrix.Cols; column++)
                        {
                            var val = sobelMatrix[row, column];

                            if(val > 0)
                            {
                                if(row > 0
                                    && row < sobelMatrix.Rows - 1
                                    && column > 0
                                    && column < sobelMatrix.Cols - 1)
                                {
                                    var neighbours = GetNeighbours(new System.Drawing.Point(column, row));

                                    double maxValue = double.MinValue;

                                    foreach (var p in neighbours)
                                    {
                                        var value = sobelMatrix[p.Y, p.X];

                                        if (value > maxValue)
                                        {
                                            maxValue = value;
                                        }
                                    }

                                    maxArray[row, column] = ToByte(maxValue);
                                }
                            }
                        }
                    }

                    //var temp = maxArray.Clone() as byte[,];

                    //for (int row = 0; row < sobelMatrix.Rows; row++)
                    //{
                    //    for (int column = 0; column < sobelMatrix.Cols; column++)
                    //    {
                    //        var val = temp[row, column];

                    //        if (val > 0)
                    //        {
                    //            if (row > 0
                    //                && row < sobelMatrix.Rows - 1
                    //                && column > 0
                    //                && column < sobelMatrix.Cols - 1)
                    //            {
                    //                var neighbours = GetNeighbours(new System.Drawing.Point(column, row));

                    //                double maxValue = double.MinValue;

                    //                foreach (var p in neighbours)
                    //                {
                    //                    var value = temp[p.Y, p.X];

                    //                    if (value > maxValue)
                    //                    {
                    //                        maxValue = value;
                    //                    }
                    //                }

                    //                maxArray[row, column] = ToByte(maxValue);
                    //            }
                    //        }
                    //    }
                    //}

                    //temp = maxArray.Clone() as byte[,];

                    //for (int row = 0; row < sobelMatrix.Rows; row++)
                    //{
                    //    for (int column = 0; column < sobelMatrix.Cols; column++)
                    //    {
                    //        var val = temp[row, column];

                    //        if (val > 0)
                    //        {
                    //            if (row > 0
                    //                && row < sobelMatrix.Rows - 1
                    //                && column > 0
                    //                && column < sobelMatrix.Cols - 1)
                    //            {
                    //                var neighbours = GetNeighbours(new System.Drawing.Point(column, row));

                    //                double maxValue = double.MinValue;

                    //                foreach (var p in neighbours)
                    //                {
                    //                    var value = temp[p.Y, p.X];

                    //                    if (value > maxValue)
                    //                    {
                    //                        maxValue = value;
                    //                    }
                    //                }

                    //                maxArray[row, column] = ToByte(maxValue);
                    //            }
                    //        }
                    //    }
                    //}

                    var maxMatrix = new Matrix<byte>(maxArray);
                    maxMatrix.Save("maxArray" + number + ".bmp");

                    var edgeMatrix = new Matrix<byte>(edgeSmoothBWAray);

                    edgeMatrix.Save("laplacian-edgeMatrix" + number + ".bmp");

                    var thresholdEdge = CustomThreshold(sobelMatrix, 13);



                    Matrix<byte> thresholdEdgeMatrix = new Matrix<byte>(thresholdEdge);

                    thresholdEdgeMatrix.Save("laplacian-threshold-2-" + number + ".bmp");

                    sobelMatrix.Save("laplacian-threshold" + number + ".bmp");

                    CvInvoke.Canny(changedContrastImg, cannyImage, 150, 224, 3, false);
                    await Dispatcher.BeginInvoke(_addImageToTheList,
                        cannyImage);


                    Matrix<byte> mask = new Matrix<byte>(image.Size);

                    int dilSize = 2;
                    Mat se1 = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(2 * dilSize + 1, 2 * dilSize + 1), new System.Drawing.Point(dilSize, dilSize));
                    dilSize = 1;
                    Mat se2 = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(2 * dilSize + 1, 2 * dilSize + 1), new System.Drawing.Point(dilSize, dilSize));
                    //CvInvoke.MorphologyEx(sobelMatrix, mask, MorphOp.Close, se1, new System.Drawing.Point(0, 0), 1, BorderType.Default, new MCvScalar(255, 0, 0, 255));

                    //await Dispatcher.BeginInvoke(_addImageToTheList,
                    //            mask.Mat);


                    CvInvoke.MorphologyEx(sobelMatrix, mask, MorphOp.Open, se2, new System.Drawing.Point(0, 0), 1, BorderType.Default, new MCvScalar(255, 0, 0, 255));
                    //CvInvoke.Erode(sobelMatrix, sobelMatrix, se1, new System.Drawing.Point(0, 0), 1, BorderType.Default, new MCvScalar(255, 0, 0, 255));
                    //await Dispatcher.BeginInvoke(_addImageToTheList,
                    //                mask.Mat);

                    var maskedSobel = new Matrix<byte>(image.Size);

                    for (int rowIndex = 0; rowIndex < image.Rows; rowIndex++)
                    {
                        for (int columnIndex = 0; columnIndex < image.Cols; columnIndex++)
                        {
                            maskedSobel[rowIndex, columnIndex] = ToByte(thresholdEdgeMatrix[rowIndex, columnIndex] * (mask[rowIndex, columnIndex] / 255));
                        }
                    }
                    //CvInvoke.Threshold(sobelMatrix, sobelMatrix, 160, 255, ThresholdType.Binary);

                    //await Dispatcher.BeginInvoke(_addImageToTheList,
                    //        maskedSobel.Mat);


                    //CvInvoke.Erode(sobelMatrix, sobelMatrix, aaa, new System.Drawing.Point(0, 0), 1, BorderType.Default, new MCvScalar(255, 0, 0, 255));

                    //await Dispatcher.BeginInvoke(_addImageToTheList,
                    //        sobelMatrix.Mat);

                    cannyImage.Save("canny" + number + ".bmp");

                    Matrix<byte> imageMatrix = new Matrix<byte>(image.Size);
                    image.CopyTo(imageMatrix);

                    Matrix<byte> cannyMatrix = new Matrix<byte>(cannyImage.Size);

                    //CvInvoke.Threshold(sobelMatrix, sobelMatrix, 80, 255, ThresholdType.Binary);

                    if (canny == null)
                    {
                        thresholdEdgeMatrix.CopyTo(cannyMatrix);
                    }
                    else
                    {
                        thresholdEdgeMatrix.CopyTo(cannyMatrix);
                    }




                    var skeletonMatrix = new Matrix<double>(image.Size);
                    var skeletonMatrixByte = new Matrix<byte>(image.Size);
                    //imageMatrix.Mul(cannyMatrix);

                    for (int rowIndex = 0; rowIndex < image.Rows; rowIndex++)
                    {
                        cannyMatrix[rowIndex, 0] = 255;
                        cannyMatrix[rowIndex, image.Cols - 1] = 255;
                    }

                    for (int columnIndex = 0; columnIndex < image.Cols; columnIndex++)
                    {
                        cannyMatrix[0, columnIndex] = 255;
                        cannyMatrix[image.Rows - 1, columnIndex] = 255;
                    }

                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                    cannyMatrix.Mat);

                    for (int rowIndex = 0; rowIndex < image.Rows; rowIndex++)
                    {
                        for (int columnIndex = 0; columnIndex < image.Cols; columnIndex++)
                        {
                            var skeletonvalue = imageMatrix[rowIndex, columnIndex] * (cannyMatrix[rowIndex, columnIndex] / 255);
                            skeletonMatrix[rowIndex, columnIndex] = skeletonvalue;
                            skeletonMatrixByte[rowIndex, columnIndex] = ToByte(skeletonvalue);
                        }
                    }

                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                skeletonMatrixByte.Mat);

                    skeletonMatrixByte.Save("skeletonMatrixByte" + number + ".bmp");

                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(sobelMatrix, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);

                        foreach (var contour in contours.ToArrayOfArray())
                        {
                            int meanColor = 0;

                            foreach (var point in contour)
                            {
                                meanColor += image.GetData(point.Y, point.X)[0];
                            }

                            meanColor = meanColor / contour.Length;

                            foreach (var point in contour)
                            {
                                skeletonMatrix[point.Y, point.X] = meanColor;
                                skeletonMatrixByte[point.Y, point.X] = ToByte(meanColor);
                            }
                        }
                        //CvInvoke.DrawContours(smoothImage.Mat, contours, -1, new MCvScalar(255, 0, 0), -1, LineType.EightConnected, null, 200);

                    }

                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                    skeletonMatrixByte.Mat);

                    skeletonMatrixByte.Save("skeletonMatrixByte-norm" + number + ".bmp");



                    var intersectionSurfaceMatrix = CreateIntersectionSurfaceMatrix(cannyMatrix, skeletonMatrixByte, skeletonMatrix);

                    var intersectionMatrixByte = new Matrix<byte>(intersectionSurfaceMatrix.Size);

                    for (int rowIndex = 0; rowIndex < image.Rows; rowIndex++)
                    {
                        for (int columnIndex = 0; columnIndex < image.Cols; columnIndex++)
                        {
                            intersectionMatrixByte[rowIndex, columnIndex] = ToByte(intersectionSurfaceMatrix[rowIndex, columnIndex]);
                        }
                    }

                    await Dispatcher.BeginInvoke(_addImageToTheList,
                            intersectionMatrixByte.Mat);
                    intersectionMatrixByte.Save("intersectionMatrixByte" + number + ".bmp");

                    var doubleImageMatrix = imageMatrix.Convert<double>();
                    var differenceMatrix = intersectionSurfaceMatrix - doubleImageMatrix;

                    var binary1Matrix = new Matrix<byte>(image.Size);
                    var binary2Matrix = new Matrix<byte>(image.Size);

                    double pt = 15;
                    double nt = 15;

                    for (int rowIndex = 0; rowIndex < image.Rows; rowIndex++)
                    {
                        for (int columnIndex = 0; columnIndex < image.Cols; columnIndex++)
                        {
                            var diffValue = differenceMatrix[rowIndex, columnIndex];
                            if (diffValue > pt
                                || cannyMatrix[rowIndex, columnIndex] == 255)
                            {
                                binary1Matrix[rowIndex, columnIndex] = 255;
                            }
                            else
                            {
                                binary1Matrix[rowIndex, columnIndex] = 0;
                            }

                            if (diffValue < -nt
                                || cannyMatrix[rowIndex, columnIndex] == 255)
                            {
                                binary2Matrix[rowIndex, columnIndex] = 255;
                            }
                            else
                            {
                                binary2Matrix[rowIndex, columnIndex] = 0;
                            }
                        }
                    }

                    VectorOfVectorOfPoint contoursVector = new VectorOfVectorOfPoint();
                    //var connectedComponents = FindBlobs(binary1Matrix.Mat, number);
                    CvInvoke.FindContours(binary1Matrix.Mat.Clone(), contoursVector, null, RetrType.List, ChainApproxMethod.ChainApproxNone);

                    var minWidth = 0.03 * gray.Width;
                    var maxWidth = 0.25 * gray.Width;

                    var minHeight = 0.04 * gray.Height;
                    var maxHeight = 0.25 * gray.Height;

                    Mat detectedDigits = new Mat();

                    gray.ConvertTo(detectedDigits, DepthType.Cv32S);

                    CvInvoke.DrawContours(gray, contoursVector, -1, new MCvScalar(100, 100, 100));
                    CvInvoke.DrawContours(color, contoursVector, -1, new MCvScalar(0, 255, 0));

                    for (int compIndex = 0; compIndex < contoursVector.Size; compIndex++)
                    {
                        var component = contoursVector[compIndex];
                        var compRectangle = CvInvoke.BoundingRectangle(component);

                        var ratio = (double)compRectangle.Width / compRectangle.Height;
                        var inversedRatio = (double) 1 / ratio;
                        

                        if(compRectangle.Width >= minWidth
                            && compRectangle.Width <= maxWidth
                            && compRectangle.Height >= minHeight
                            && compRectangle.Height <= maxHeight
                            && (ratio <= 0.9
                            && inversedRatio <= 4))
                        {
                            using (Emgu.CV.OCR.Tesseract t = new Emgu.CV.OCR.Tesseract("D:/Emgu/emgucv-windows-universal 3.0.0.2157/bin/tessdata/", "eng", OcrEngineMode.TesseractOnly, "1234567890"))
                            {
                                var outerRectangle = compRectangle;
                                int margin = 4;
                                outerRectangle.X = outerRectangle.X - margin;

                                if(outerRectangle.X < 0)
                                {
                                    outerRectangle.X = 0;
                                }

                                outerRectangle.Y = outerRectangle.Y - margin;

                                if (outerRectangle.Y < 0)
                                {
                                    outerRectangle.Y = 0;
                                }

                                outerRectangle.Width += (2 * margin);
                                outerRectangle.Height += (2 * margin);

                                var diffWidth =  gray.Width - outerRectangle.Right;

                                if (diffWidth < 0)
                                {
                                    outerRectangle.Width += diffWidth;
                                }

                                var diffHeight = gray.Height - outerRectangle.Bottom;

                                if (diffHeight < 0)
                                {
                                    outerRectangle.Height += diffHeight;
                                }

                                var characterMask = new Matrix<byte>(binary1Matrix.Size);
                                characterMask.SetZero();
                                CvInvoke.DrawContours(characterMask.Mat, contoursVector, compIndex, new MCvScalar(255), -1);

                                var characterLargeMatrix = new Matrix<byte>(binary1Matrix.Size);
                                binary1Matrix.Mat.CopyTo(characterLargeMatrix, characterMask.Mat);
                                var characterMatrix = characterLargeMatrix.GetSubRect(outerRectangle).Mat;
                                //var cropCharacterMatrix = new Mat();
                                //characterMatrix.CopyTo(cropCharacterMatrix, characterMask.Mat);

                                using (var covar = new Mat())
                                {
                                    using (var meanMat = new Mat())
                                    {
                                        using (var eigenValues = new Mat())
                                        {
                                            using (var eigenVectors = new Mat())
                                            {

                                                double meanX = 0;
                                                double meanY = 0;
                                                int count = 0;
                                                for (int row = 0; row < characterMatrix.Rows; row++)
                                                {
                                                    for (int column = 0; column < characterMatrix.Cols; column++)
                                                    {
                                                        var val = characterMatrix.GetData(row, column)[0];

                                                        if (val > 0)
                                                        {
                                                            meanX += column;
                                                            meanY += row;
                                                            count++;
                                                        }
                                                    }
                                                }

                                                if (count > 0)
                                                {
                                                    meanX = (double)meanX / count;
                                                    meanY = (double)meanY / count;
                                                }

                                                var covXYSum = 0.0;
                                                var covXSum = 0.0;
                                                var covYSum = 0.0;

                                                for (int row = 0; row < characterMatrix.Rows; row++)
                                                {
                                                    for (int column = 0; column < characterMatrix.Cols; column++)
                                                    {
                                                        var val = characterMatrix.GetData(row, column)[0];

                                                        if (val > 0)
                                                        {
                                                            var diffX = column - meanX;
                                                            var diffY = row - meanY;

                                                            covXYSum += (diffX * diffY);
                                                            covXSum += (diffX * diffX);
                                                            covYSum += (diffY * diffY);
                                                        }
                                                    }
                                                }

                                                var covarianceXY = covXYSum / count;
                                                var covarianceX = covXSum / count;
                                                var covarianceY = covYSum / count;

                                                var covarianceMatrix = new Matrix<double>(2, 2, 1);

                                                covarianceMatrix[0, 0] = covarianceX;
                                                covarianceMatrix[0, 1] = covarianceXY;
                                                covarianceMatrix[1, 0] = covarianceXY;
                                                covarianceMatrix[1, 1] = covarianceY;


                                                // CvInvoke.CalcCovarMatrix(characterMatrix, covar, meanMat, CovarMethod.Rows | CovarMethod.Normal);
                                                CvInvoke.Eigen(covarianceMatrix.Mat, eigenValues, eigenVectors);

                                                //if(eigenValues.Data != null)
                                                //{
                                                var max = int.MinValue;
                                                int maxIndex = -1;
                                                for (int eigenIndex = 0; eigenIndex < eigenValues.Rows; eigenIndex++)
                                                {
                                                    var val = eigenValues.GetData(eigenIndex, 0)[0];

                                                    if (val > max)
                                                    {
                                                        max = val;
                                                        maxIndex = eigenIndex;
                                                    }
                                                    //var value = eigenValues.To[eigenIndex];
                                                }

                                                if (maxIndex >= 0)
                                                {
                                                    var eigenVectorX = eigenVectors.GetData(maxIndex, 0)[0];
                                                    var eigenVectorY = eigenVectors.GetData(maxIndex, 1)[0];

                                                    var angle = Math.Atan2(eigenVectorY, eigenVectorX);
                                                    //Shift the angle to the [0, 2pi] interval instead of [-pi, pi]
                                                    if (angle < 0)
                                                    {
                                                        angle += 6.28318530718;
                                                    }
                                                    //Conver to degrees instead of radians
                                                    angle = 180 * angle / 3.14159265359;

                                                    var rotateAngle = Math.Abs(90 - angle);
                                                    CvInvoke.GetRotationMatrix2D(new System.Drawing.PointF((float)(Math.Ceiling(compRectangle.Width / 2f)), (float)(Math.Ceiling(compRectangle.Height / 2f))), rotateAngle, 1, covar);
                                                    Debug.WriteLine("ANGLE - " + angle + " - " + compIndex);

                                                    characterMatrix.Save("rotated-" + number + "-" + compIndex + "-char.bmp");
                                                    using (var rotatedMatrix = new Mat())
                                                    {
                                                        var translateMatrix = new Matrix<double>(new double[,] { { 1, 0, 5 }, { 0, 1, 5 } });
                                                        CvInvoke.WarpAffine(characterMatrix, rotatedMatrix, covar, new System.Drawing.Size(characterMatrix.Cols + 10, characterMatrix.Rows + 10));
                                                        CvInvoke.WarpAffine(rotatedMatrix, rotatedMatrix, translateMatrix, rotatedMatrix.Size);

                                                        t.Recognize(rotatedMatrix);
                                                        var characters = t.GetCharacters();



                                                        if (characters.Length > 0)
                                                        {
                                                            int ix = 0;
                                                            foreach (var c in characters)
                                                            {


                                                                Debug.WriteLine("Detected Number " + number + " | " + ix + " : " + c.Text + " - " + c.Cost);
                                                                CvInvoke.PutText(detectedDigits, c.Text, new System.Drawing.Point(compRectangle.Left, compRectangle.Top), FontFace.HersheyComplex, 0.7, new Bgr(0, 255, 0).MCvScalar);
                                                                ix++;
                                                            }

                                                        }

                                                        //CvInvoke.PutText(detectedDigits, angle.ToString("F0"), new System.Drawing.Point(compRectangle.Left, compRectangle.Top), FontFace.HersheyComplex, 0.4, new Bgr(0, 255, 0).MCvScalar);
                                                        rotatedMatrix.Save("rotated-" + number + "-" + compIndex  + ".bmp");
                                                    }
                                                }
                                                //}

                                            }
                                        }
                                    }

                                }

                                //gray.ToImage<Bgr, byte>()
                               
                            }
                            CvInvoke.Rectangle(detectedDigits, compRectangle, new MCvScalar(100, 100, 100));
                        }
                    }

                    gray.Save("gray-" + number + ".bmp");
                    color.Save("color-" + number + ".bmp");

                    detectedDigits.Save("detectedDigits" + number + ".bmp");

                        await Dispatcher.BeginInvoke(_addImageToTheList,
                            binary1Matrix.Mat);

                    binary1Matrix.Save("binary1Matrix" + number + ".bmp");

                    await Dispatcher.BeginInvoke(_addImageToTheList,
                        binary2Matrix.Mat);

                    binary2Matrix.Save("binary2Matrix" + number + ".bmp");
                }


                //CvInvoke.BilateralFilter(image, cannyImage, 4, 4, 4);
                //CvInvoke.Laplacian(image, cannyImage, DepthType.Cv8U);
                //CvInvoke.Threshold(cannyImage, cannyImage, 40, 255, ThresholdType.Binary);
                //await Dispatcher.BeginInvoke(_addImageToTheList,
                //    cannyImage);


                //using(Mat skeletonMat  = new Mat())
                //{
                //    skeletonMat =
                //}
            }
                }
            }
        }

        byte[,] CustomThreshold(Matrix<byte> img, int size)
        {
            var globalMean = 0;
            var globalCount = 0;
            var globalMax = 0;

            for (int rowIndex = 0; rowIndex < img.Rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < img.Cols; columnIndex++)
                {
                    var val = img[rowIndex, columnIndex];

                    if (val > 0)
                    {
                        if (val > globalMax)
                        {
                            globalMax = val;
                        }

                        globalMean += val;
                        globalCount++;
                    }
                }
            }

            globalMean = globalMean / globalCount;



            byte[,] fitleredValues = new byte[img.Rows, img.Cols];

            var xIterations = Math.Truncate(((double)img.Cols / size));
            var yIterations = Math.Truncate(((double)img.Rows / size));

            for (int yAreaIndex = 0; yAreaIndex < yIterations; yAreaIndex++)
            {
                for (int xAreaIndex = 0; xAreaIndex < xIterations; xAreaIndex++)
                {
                    var mean = 0;
                    int count = 0;
                    var max = 0;
                    for (int row = yAreaIndex * size; row < (yAreaIndex + 1) * size; row++)
                    {
                        for (int column = xAreaIndex * size; column < (xAreaIndex + 1) * size; column++)
                        {
                            if (row >= img.Rows
                                || column >= img.Cols)
                            {

                            }
                            else
                            {
                                var val = img[row, column];
                                if (val > 0)
                                {
                                    if (val > max)
                                    {
                                        max = val;
                                    }

                                    mean += val;
                                    count++;
                                }
                            }
                        }
                    }


                    if (count > 0)
                    {
                        mean = mean / count;
                    }

                    var threshold = 0.65 * globalMean;// *1.3;


                    //if (threshold > globalMax * 0.8)
                    //{
                    //    threshold = globalMax * 0.8;
                    //}

                    for (int row = yAreaIndex * size; row < (yAreaIndex + 1) * size; row++)
                    {
                        for (int column = xAreaIndex * size; column < (xAreaIndex + 1) * size; column++)
                        {
                            if (row >= img.Rows
                                || column >= img.Cols)
                            {

                            }
                            else
                            {
                                var val = img[row, column];
                                if (val < threshold
                                    || threshold == 0)
                                {
                                    fitleredValues[row, column] = 0;
                                }
                                else
                                {
                                    fitleredValues[row, column] = 255;
                                }
                            }
                        }

                    }
                }
            }


            return fitleredValues;
        }


        async Task Run()
        {
            await Task.Run(async () =>
                {
                    using (Mat image = new Mat(@"Koice-66.jpg", LoadImageType.AnyColor))
                    {//Read the files as an 8-bit Bgr image  
                        //Mat sharpImage = new Mat();

                        //await BinaryImage(image);

                        //return;
                        
                        
                        Action updateListAction = () =>
                            {
                                ProcessingImageWorkflow.Clear();
                                ProcessingImageWorkflow.Add(image);
                            };
                        //using (var filterImage = new Mat())
                        //{
                        //    using (var filterImage2 = new Mat())
                        //{

                        await Dispatcher.BeginInvoke(updateListAction,
                            null);


                        //var rows = image.Rows;
                        //var cols = image.Cols;

                        //    var colorConvertArray = new double[,] 
                        //    {
                        //        {0.2989360212937753847527155, 0.5870430744511212909351327,  0.1140209042551033243121518},
                        //        {0.5,                         0.5,                         -1},
                        //        {1,                          -1,                            0}
                        //    };

                        //    var reshapedArrayLength = rows * cols;

                        //    var reshapedArray = new double[reshapedArrayLength, 3];

                        //Matrix<double> colorconvert = new Matrix<double>(colorConvertArray);



                        //for(int rowIndex = 0; rowIndex < rows; rowIndex++)
                        //{
                        //    for(int colIndex = 0; colIndex < cols; colIndex++)
                        //    {
                        //        reshapedArray[(rowIndex * cols) + colIndex, 0] = image.GetData(rowIndex, colIndex)[0];
                        //        reshapedArray[(rowIndex * cols) + colIndex, 1] = image.GetData(rowIndex, colIndex)[1];
                        //        reshapedArray[(rowIndex * cols) + colIndex, 2] = image.GetData(rowIndex, colIndex)[2];
                        //    }
                        //}

                        //Matrix<double> pic = new Matrix<double>(rows * cols, 3, 1);
                        //var img = pic * colorconvert;

                        //Mat aaa = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(5,9), new System.Drawing.Point(2, 4));
                        //CvInvoke.Dilate(filterImage2, filterImage, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                        //await Dispatcher.BeginInvoke(_addImageToTheList,
                        //    filterImage);

                        //CvInvoke.Erode(filterImage2, filterImage2, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                        //await Dispatcher.BeginInvoke(_addImageToTheList,
                        //    filterImage2);

                        //CvInvoke.Subtract(filterImage, filterImage2, filterImage);//, filterImage, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                        //await Dispatcher.BeginInvoke(_addImageToTheList,
                        //    filterImage);

                        //CvInvoke.Threshold(filterImage, filterImage, 80, 240, ThresholdType.BinaryInv);// 3, false);//, filterImage, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                        //await Dispatcher.BeginInvoke(_addImageToTheList,
                        //    filterImage);


                        //CvInvoke.MorphologyEx(filterImage, filterImage, Emgu.CV.CvEnum.MorphOp.Tophat, aaa, new System.Drawing.Point(1, 1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(255, 0, 0, 255));  

                        //CvInvoke.MorphologyEx(filterImage, filterImage, MorphOp.Tophat, null, new System.Drawing.Point(), 2, BorderType.Replicate, new MCvScalar());


                        //await Dispatcher.BeginInvoke(_addImageToTheList,
                        //    filterImage);

                        //CvInvoke.BilateralFilter(image, filterImage, 2, 4, 4);

                        //await Dispatcher.BeginInvoke(_addImageToTheList,
                        //    filterImage);

                        //ImageViewer.Show(image, String.Format(
                        //                                      "Canny Gray"));
                        //CvInvoke.GaussianBlur(image, sharpImage, new System.Drawing.Size(5, 5), 5);
                        ////ImageViewer.Show(sharpImage, String.Format(
                        ////                                      "Canny Gray"));
                        //CvInvoke.AddWeighted(image, 1.5, sharpImage, -0.5, 0, sharpImage);
                        //image = sharpImage;

                        //ImageViewer.Show(image, String.Format(
                        //                                      "Canny Gray"));
                        long detectionTime;
                        List<System.Drawing.Rectangle> faces = new List<System.Drawing.Rectangle>();
                        List<System.Drawing.Rectangle> eyes = new List<System.Drawing.Rectangle>();

                        //The cuda cascade classifier doesn't seem to be able to load "haarcascade_frontalface_default.xml" file in this release
                        //disabling CUDA module for now
                        bool tryUseCuda = false;
                        bool tryUseOpenCL = true;

                        DetectFace.Detect(
                        image, "haarcascade_frontalface_alt2.xml", "haarcascade_eye.xml",
                        faces, eyes,
                        tryUseCuda,
                        tryUseOpenCL,
                        out detectionTime);

                        int index = 0;
                        foreach (System.Drawing.Rectangle face in faces)
                        {
                            index++;

                            //if (index > 1)
                            //{
                            //    break;
                            //}

                            CvInvoke.Rectangle(image, face, new Bgr(System.Drawing.Color.Red).MCvScalar, 2);


                            System.Drawing.Rectangle torsoRectangle = ComputeTorsoArea(face, image.Size);

                            using (Mat torsoMat = new Mat(image, torsoRectangle))
                            {
                                await Dispatcher.BeginInvoke(_addImageToTheList,
                                                                 torsoMat);

                                using (Mat grayMat = new Mat())
                                {
                                    CvInvoke.CvtColor(torsoMat, grayMat, ColorConversion.Bgr2Gray);

                                    var grayClone = grayMat.Clone();

                                    //CvInvoke.Invert(grayMat, grayMat, DecompMethod.Normal);
                                    await BinaryImage(grayMat, index, null, torsoMat.Clone());

                                   // var inverseMat = grayMat.Clone();
                                    Matrix<byte> inverseMatrix = new Matrix<byte>(grayMat.Size);

                                    for (int row = 0; row < grayMat.Rows; row++ )
                                    {
                                        for(var column = 0; column < grayMat.Cols; column++)
                                        {
                                            inverseMatrix[row, column] = ToByte(255 - grayClone.GetData(row, column)[0]);
                                        }
                                    }
                                        //CvInvoke.Invert(grayMat, grayMat, DecompMethod.Normal);
                                    index++;
                                    await BinaryImage(inverseMatrix.Mat, index, null, torsoMat.Clone());
                                }


                                //using (Mat hsvMat = new Mat())  
                                //{





                                //var changedContrast = ChangeContrast(torsoMat, 127);

                                //using (Image<Bgr, byte> contrast = new Image<Bgr, byte>(changedContrast))
                                //{
                                //    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                 contrast.Mat);

                                //CvInvoke.CvtColor(torsoMat, hsvMat, ColorConversion.Bgr2Hsv);
                                // CvInvoke.MedianBlur(torsoMat, torsoMat, 3);
                                //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                             torsoMat);

                                //CvInvoke.CvtColor(torsoMat, filterImage2, ColorConversion.Bgr2Gray);
                                //CvInvoke.MedianBlur(filterImage2, filterImage2, 5);

                                //Mat aaa = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(5, 9), new System.Drawing.Point(2, 4));
                                //CvInvoke.Dilate(filterImage2, filterImage, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                                //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //    filterImage);

                                //CvInvoke.Erode(filterImage2, filterImage2, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                                //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //    filterImage2);

                                //CvInvoke.Subtract(filterImage, filterImage2, filterImage);//, filterImage, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                                //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //    filterImage);

                                //CvInvoke.Threshold(filterImage, filterImage2, 80, 240, ThresholdType.BinaryInv);// 3, false);//, filterImage, aaa, new System.Drawing.Point(-1, -1), 1, BorderType.Replicate, new MCvScalar(255, 0, 0, 255));

                                //CvInvoke.Threshold(filterImage, filterImage, 80, 240, ThresholdType.Binary);

                                //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //        filterImage2);

                                //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //    filterImage);

                                //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //    torsoMat);
                                ////ProcessingImageWorkflow.Add(ToBitmapSource(torsoMat));
                                //var data = EdgePreservingSmoothing(torsoMat, 5);

                                //using (Image<Bgr, byte> edgeImage = new Image<Bgr, byte>(data))
                                //{
                                //    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                           edgeImage.Mat);

                                //    var changedContrast = ChangeContrast(torsoMat, 127);

                                //    using (Image<Bgr, byte> contrast = new Image<Bgr, byte>(changedContrast))
                                //    {
                                //        await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                     contrast.Mat);

                                //        var subSampleData = await SubSampling(edgeImage.Mat);

                                //        using (Image<Bgr, byte> smoothImage = new Image<Bgr, byte>(subSampleData))
                                //        {
                                //            smoothImage.Save("output1.jpg");
                                //            var changedContrast2 = ChangeContrast(smoothImage.Mat, 20);

                                //            var edge = ComputeEdgeMagnitudes(smoothImage.Mat);
                                //            var rows = edge.GetLength(0);
                                //            var cols = edge.GetLength(1);

                                //            byte[, ,] newEdge = new byte[rows, cols, 1];

                                //            for (int row = 0; row < edge.GetLength(0); row++)
                                //            {
                                //                for (int col = 0; col < edge.GetLength(1); col++)
                                //                {
                                //                    newEdge[row, col, 0] = ToByte(edge[row, col, 0]);
                                //                }
                                //            }


                                //            Image<Gray, Byte> edRGB = new Image<Gray, Byte>(newEdge);

                                //            await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                         edRGB.Mat);

                                //            using (Image<Bgr, byte> contrast2 = new Image<Bgr, byte>(changedContrast2))
                                //            {
                                //                await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                             contrast2.Mat);

                                //                using (var canny = new Mat())
                                //                {
                                //                    using (var gray = new Mat())
                                //                    {
                                //                        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                                //                        {
                                //                            using (VectorOfVectorOfPoint filteredContours = new VectorOfVectorOfPoint())
                                //                            {
                                //                                var rgb = smoothImage.Mat.Split();

                                //                                await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                   rgb[0]);

                                //                                await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                   rgb[1]);

                                //                                await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                   rgb[2]);

                                //                                using (Mat invertedMat = new Mat())
                                //                                {
                                //                                    Mat xSobel = new Mat();
                                //                                    Mat ySobel = new Mat();
                                //                                    //CvInvoke.con

                                //                                    CvInvoke.CvtColor(smoothImage, gray, ColorConversion.Bgr2Gray);

                                //                                    //CvInvoke.MedianBlur(gray, gray, 5);
                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                       gray);

                                //                                    CvInvoke.Sobel(gray, xSobel, DepthType.Cv8U, 1, 0);
                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                       xSobel);


                                //                                    CvInvoke.Sobel(gray, ySobel, DepthType.Cv8U, 0, 1);
                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                       ySobel);



                                //                                    byte[, ,] edgeSobel = new byte[xSobel.Rows, xSobel.Cols, 1];

                                //                                    for (int row = 0; row < xSobel.Rows; row++)
                                //                                    {
                                //                                        for (int col = 0; col < xSobel.Cols; col++)
                                //                                        {
                                //                                            var magX = xSobel.GetData(row, col)[0];
                                //                                            var magY = ySobel.GetData(row, col)[0];
                                //                                            var magnitude = Math.Sqrt(magX * magX + magY * magY);
                                //                                            edgeSobel[row, col, 0] = ToByte(magnitude);
                                //                                        }
                                //                                    }


                                //                                    using (Image<Gray, Byte> edgeSobelImage = new Image<Gray, byte>(edgeSobel))
                                //                                    {

                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                   edgeSobelImage.Mat);

                                //                                    //CvInvoke.Threshold(edgeSobelImage.Mat, canny, 100, 255, ThresholdType.Binary);

                                //                                    //await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                    //           canny);

                                //                                    //CvInvoke.AddWeighted(xSobel, 0.5, ySobel, 0.5, 0, gray);


                                //                                    //CvInvoke.Invert(gray, invertedMat, DecompMethod.Eig);

                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                           gray);

                                //                                    await BinaryImage(gray, edgeSobelImage.Mat);

                                //                                    //CvInvoke.AdaptiveThreshold(gray, gray, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 11, 2);
                                //                                    //CvInvoke.Threshold(smoothImage.Mat, gray, 150, 255, ThresholdType.Binary);

                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                       gray);
                                //                                    //CvInvoke.Laplacian(gray, canny, DepthType.Cv8U);
                                //                                    //CvInvoke.Threshold(gray, invertedMat, 173, 256, ThresholdType.Binary);
                                //                                    CvInvoke.Canny(gray, canny, 70, 240, 3, false);

                                //                                    CvInvoke.FindContours(canny, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);

                                //                                    for (int i = 0; i < contours.Size; i++)
                                //                                    {
                                //                                        var contour = contours[i];
                                //                                        var contourArea = CvInvoke.ContourArea(contour);

                                //                                        //if (contourArea > face.Height * face.Width * 0.4
                                //                                        //    && contourArea < face.Height * face.Width * 2)//face.Height * face.Width * 0.7)
                                //                                        //{
                                //                                        filteredContours.Push(contour);
                                //                                        //}
                                //                                    }

                                //                                    LineSegment2D[] lines = CvInvoke.HoughLinesP(
                                //                                                           canny,
                                //                                                           1, //Distance resolution in pixel-related units
                                //                                                           Math.PI / 180, //Angle resolution measured in radians.
                                //                                                           25, //threshold
                                //                                                           5, //min Line width
                                //                                                           10); //gap between lines

                                //                                    foreach (var line in lines)
                                //                                    {
                                //                                        //CvInvoke.Line(smoothImage.Mat, line.P1, line.P2, new Bgr(System.Drawing.Color.DarkBlue).MCvScalar, 1);
                                //                                    }

                                //                                    CvInvoke.DrawContours(smoothImage.Mat, filteredContours, -1, new MCvScalar(255, 0, 0), -1, LineType.EightConnected, null, 200);

                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                       smoothImage.Mat);

                                //                                    await Dispatcher.BeginInvoke(_addImageToTheList,
                                //                                                           canny);
                                //                                }
                                //                            }
                                //                        }
                                //                    }
                                //                    }
                                //                }

                                //            }
                                //        }
                                //    }
                                //}

                                //ProcessingImageWorkflow.Add(ToBitmapSource(torsoMat.Clone()));


                            }
                        }


                        //ImageViewer.Show(preprocessed, String.Format(
                        //                               "Img Gray"));

                        //using (Mat gray = new Mat())
                        //{
                        //    using (Mat canny = new Mat())
                        //    {
                        //        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                        //        {
                        //            var rgbTorsoMats = preprocessed.Split();

                        //            var rMat = rgbTorsoMats[0];
                        //            var gMat = rgbTorsoMats[1];
                        //            var bMat = rgbTorsoMats[2];

                        //            //var b = bMat.GetData(0, 0);

                        //            foreach (var rgbMat in rgbTorsoMats)
                        //            {
                        //                //CvInvoke.CvtColor(torsoMat, gray, ColorConversion.Bgr2Gray);
                        //                //CvInvoke.CLAHE(gray, 2, new Size(8, 8), gray);
                        //                CvInvoke.Canny(rgbMat, canny, 170, 100, 3, false);

                        //                LineSegment2D[] lines = CvInvoke.HoughLinesP(
                        //                   canny,
                        //                   1, //Distance resolution in pixel-related units
                        //                   Math.PI / 90, //Angle resolution measured in radians.
                        //                   90, //threshold
                        //                   10, //min Line width
                        //                   0); //gap between lines

                        //                //foreach (var line in lines)
                        //                //{
                        //                //    CvInvoke.Line(canny, line.P1, line.P2, new Bgr(Color.YellowGreen).MCvScalar, 1);
                        //                //}

                        //                //int[,] hierachy = CvInvoke.FindContourTree(canny, contours, ChainApproxMethod.ChainApproxSimple);
                        //                //ImageViewer.Show(canny, String.Format(
                        //                //                "Canny Gray"));
                        //            }


                        //            //FindLicensePlate(contours, hierachy, 0, gray, canny, licensePlateImagesList, filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);
                        //        }
                        //    }
                        //}

                        //CvInvoke.Rectangle(image, torsoRectangle, new Bgr(Color.Green).MCvScalar, 2);

                        //}

                        //}





                        //display the image 
                        //ImageViewer.Show(image, String.Format(
                        //"Completed face and eye detection using {0} in {1} milliseconds",
                        //(tryUseCuda && CudaInvoke.HasCuda) ? "GPU"
                        //: (tryUseOpenCL && CvInvoke.HaveOpenCLCompatibleGpuDevice) ? "OpenCL"
                        //: "CPU",
                        //detectionTime));
                        //    }
                        //}
                    }
                });


        }


        public async Task<byte[, ,]> SubSampling(Mat img)
        {
            //ProcessingImageWorkflow.Add(ToBitmapSource(img));
            //img = new Mat("diplomka.png", LoadImageType.Color);
            var colorChannels = img.Split();

            var rSobelX = new Mat(img.Size, DepthType.Cv8U, 1);
            var rSobelY = new Mat(img.Size, DepthType.Cv8U, 1);
            var gSobelX = new Mat(img.Size, DepthType.Cv8U, 1);
            var gSobelY = new Mat(img.Size, DepthType.Cv8U, 1);
            var bSobelX = new Mat(img.Size, DepthType.Cv8U, 1);
            var bSobelY = new Mat(img.Size, DepthType.Cv8U, 1);

            CvInvoke.Sobel(colorChannels[2], rSobelX, DepthType.Cv8U, 1, 0);
            CvInvoke.Sobel(colorChannels[2], rSobelY, DepthType.Cv8U, 0, 1);

            CvInvoke.Sobel(colorChannels[1], gSobelX, DepthType.Cv8U, 1, 0);
            CvInvoke.Sobel(colorChannels[1], gSobelY, DepthType.Cv8U, 0, 1);

            CvInvoke.Sobel(colorChannels[0], bSobelX, DepthType.Cv8U, 1, 0);
            CvInvoke.Sobel(colorChannels[0], bSobelY, DepthType.Cv8U, 0, 1);


            int cols = img.Cols;
            int rows = img.Rows;

            var edgeValues = new double[rows, cols];
            var edgeValuesTest = new byte[rows, cols, 1];

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                {
                    var rX = rSobelX.GetData(rowIndex, columnIndex)[0];
                    var rY = rSobelY.GetData(rowIndex, columnIndex)[0];
                    var gX = gSobelX.GetData(rowIndex, columnIndex)[0];
                    var gY = gSobelY.GetData(rowIndex, columnIndex)[0];
                    var bX = bSobelX.GetData(rowIndex, columnIndex)[0];
                    var bY = bSobelY.GetData(rowIndex, columnIndex)[0];

                    var edgeRed = Math.Sqrt(rX * rX + rY * rY);
                    var edgeGreen = Math.Sqrt(gX * gX + gY * gY);
                    var edgeBlue = Math.Sqrt(bX * bX + bY * bY);

                    var edgeValue = (new double[] { edgeRed, edgeGreen, edgeBlue }).Max();
                    edgeValuesTest[rowIndex, columnIndex, 0] = ToByte(edgeValue);
                    edgeValues[rowIndex, columnIndex] = edgeValue;
                }
            }

            Image<Gray, byte> edGE = new Image<Gray, byte>(edgeValuesTest);

            await Dispatcher.BeginInvoke(_addImageToTheList,
                                    edGE.Mat);

            var samples = new byte[rows, cols, 3];

            var samplesList = new List<RGBSample>();
            var samplesHistogram = new RGBSample[256, 256, 256];

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                {
                    var isLocalMinima = false;

                    if (rowIndex > 0
                        && rowIndex < rows - 1
                        && columnIndex > 0
                        && columnIndex < cols - 1)
                    {
                        isLocalMinima = IsLocalMinima(new System.Drawing.Point(rowIndex, columnIndex), edgeValues);
                    }

                    if (!isLocalMinima)
                    {
                        samples[rowIndex, columnIndex, 2] = 255;
                        samples[rowIndex, columnIndex, 1] = 255;
                        samples[rowIndex, columnIndex, 0] = 255;
                    }
                    else
                    {
                        var bytes = img.GetData(rowIndex, columnIndex);

                        var r = bytes[2];
                        var g = bytes[1];
                        var b = bytes[0];

                        samples[rowIndex, columnIndex, 2] = r;
                        samples[rowIndex, columnIndex, 1] = g;
                        samples[rowIndex, columnIndex, 0] = b;

                        if (samplesHistogram[r, g, b] == null)
                        {
                            var sample = new RGBSample() { Red = r, Green = g, Blue = b };
                            samplesHistogram[r, g, b] = sample;
                            samplesList.Add(sample);
                        }

                        samplesHistogram[r, g, b].Points.Add(new Point(rowIndex, columnIndex));
                    }
                }
            }


            Image<Bgr, byte> img4 = new Image<Bgr, byte>(samples);
            await Dispatcher.BeginInvoke(_addImageToTheList,
                                    img4.Mat);

            var visitedSamples = new List<byte[]>();

            var h = 32;

            var meanSamples = new List<RGBSample>();

            foreach (var sample in samplesList)
            {
                //visitedSamples.Add(sample);

                if (!sample.IsVisited)
                {
                    sample.IsVisited = true;

                    double meanRed = 0;
                    double meanGreen = 0;
                    double meanBlue = 0;

                    double totalCount = 0;

                    for (int r = sample.Red - h; r <= sample.Red + h; r++)
                    {
                        for (int g = sample.Green - h; g <= sample.Green + h; g++)
                        {
                            for (int b = sample.Blue - h; b <= sample.Blue + h; b++)
                            {
                                if (r >= 0 && r < 255
                                    && b >= 0 && b < 255
                                    && g >= 0 && g < 255)
                                {
                                    var rgbSample = samplesHistogram[r, g, b];
                                    if (rgbSample != null)
                                    {
                                        rgbSample.IsVisited = true;
                                        var count = rgbSample.Points.Count;
                                        meanRed += r * count;
                                        meanGreen += g * count;
                                        meanBlue += b * count;

                                        totalCount += count;
                                    }
                                }
                            }
                        }
                    }

                    var meanSample = new RGBSample() { Red = (byte)Math.Round(meanRed / totalCount), Green = (byte)Math.Round(meanGreen / totalCount), Blue = (byte)Math.Round(meanBlue / totalCount) };
                    meanSamples.Add(meanSample);
                }

            }

            var rgbHistogram = new RGBSample[256, 256, 256];
            var rMat = colorChannels[2];
            var gMat = colorChannels[1];
            var bMat = colorChannels[0];

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                {
                    var r = rMat.GetData(rowIndex, columnIndex)[0];
                    var g = gMat.GetData(rowIndex, columnIndex)[0];
                    var b = bMat.GetData(rowIndex, columnIndex)[0];

                    if (rgbHistogram[r, g, b] == null)
                    {
                        var sample = new RGBSample() { Red = r, Green = g, Blue = b };
                        rgbHistogram[r, g, b] = sample;
                    }

                    rgbHistogram[r, g, b].Points.Add(new Point(rowIndex, columnIndex));
                }
            }

            //CvInvoke.MeanShift(img4, )

            foreach (var mean in meanSamples)
            {
                Debug.WriteLine("Mean R: " + mean.Red + " G: " + mean.Green + " B: " + mean.Blue);
            }

            var finalColors = new List<RGBSample>();

            foreach (var meanSample in meanSamples)
            {
                meanSample.IsVisited = true;

                double meanRed = 0;
                double meanGreen = 0;
                double meanBlue = 0;

                double totalCount = 0;
                double dm = double.MaxValue;
                var currentSample = meanSample;

                while (dm > 3)
                {
                    for (int r = currentSample.Red - h; r <= currentSample.Red + h; r++)
                    {
                        for (int g = currentSample.Green - h; g <= currentSample.Green + h; g++)
                        {
                            for (int b = currentSample.Blue - h; b <= currentSample.Blue + h; b++)
                            {
                                if (r >= 0 && r < 256
                                    && b >= 0 && b < 256
                                    && g >= 0 && g < 256)
                                {
                                    var rgbSample = rgbHistogram[r, g, b];
                                    if (rgbSample != null)
                                    {
                                        rgbSample.IsVisited = true;
                                        var count = rgbSample.Points.Count;


                                        meanRed += r * count;
                                        if (meanRed < 0)
                                        {
                                            Debug.Assert(true);
                                        }

                                        meanGreen += g * count;
                                        meanBlue += b * count;

                                        totalCount += count;
                                    }
                                }
                            }
                        }
                    }

                    meanRed = meanRed / totalCount;
                    meanGreen = meanGreen / totalCount;
                    meanBlue = meanBlue / totalCount;

                    dm = Math.Abs(currentSample.Red - meanRed)
                        + Math.Abs(currentSample.Green - meanGreen)
                        + Math.Abs(currentSample.Blue - meanBlue);

                    currentSample = new RGBSample() { Red = (byte)Math.Round(meanRed), Green = (byte)Math.Round(meanGreen), Blue = (byte)Math.Round(meanBlue) };

                    totalCount = 0;
                    meanRed = 0;
                    meanGreen = 0;
                    meanBlue = 0;
                }

                finalColors.Add(currentSample);

                Debug.WriteLine("Final R: " + currentSample.Red + " G: " + currentSample.Green + " B: " + currentSample.Blue);
            }


            byte[, ,] finalImageData = new byte[rows, cols, 3];

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                {
                    var rOld = rMat.GetData(rowIndex, columnIndex)[0];
                    var gOld = gMat.GetData(rowIndex, columnIndex)[0];
                    var bOld = bMat.GetData(rowIndex, columnIndex)[0];

                    double min = double.MaxValue;
                    int minIndex = -1;
                    int i = 0;

                    foreach (var final in finalColors)
                    {
                        var diff = (Math.Abs(final.Red - rOld)
                            + Math.Abs(final.Green - gOld)
                            + Math.Abs(final.Blue - bOld));

                        if (diff < min)
                        {
                            minIndex = i;
                            min = diff;
                        }

                        i++;
                    }

                    if (minIndex >= 0)
                    {
                        finalImageData[rowIndex, columnIndex, 0] = finalColors[minIndex].Blue;
                        finalImageData[rowIndex, columnIndex, 1] = finalColors[minIndex].Green;
                        finalImageData[rowIndex, columnIndex, 2] = finalColors[minIndex].Red;
                    }
                }
            }

            Image<Bgr, byte> img2 = new Image<Bgr, byte>(finalImageData);

            await Dispatcher.BeginInvoke(_addImageToTheList,
                                    img2.Mat);


            byte[, ,] postProcessedData = null;

            //using(var edgeMat = new Mat())
            //{
            //CvInvoke.Canny(img2.Mat, edgeMat, 80, 240, 3, false);

            //await Dispatcher.BeginInvoke(_addImageToTheList,
            //                        edgeMat);

            postProcessedData = PostProcessing(img2.Mat);

            Image<Bgr, byte> img3 = new Image<Bgr, byte>(postProcessedData);

            await Dispatcher.BeginInvoke(_addImageToTheList,
                                    img3.Mat);
            //}

            return postProcessedData;
        }

        public static double[, ,] ComputeEdgeMagnitudes(Mat img)
        {
            var colorChannels = img.Split();

            var rSobelX = new Mat(img.Size, DepthType.Cv8U, 1);
            var rSobelY = new Mat(img.Size, DepthType.Cv8U, 1);
            var gSobelX = new Mat(img.Size, DepthType.Cv8U, 1);
            var gSobelY = new Mat(img.Size, DepthType.Cv8U, 1);
            var bSobelX = new Mat(img.Size, DepthType.Cv8U, 1);
            var bSobelY = new Mat(img.Size, DepthType.Cv8U, 1);

            CvInvoke.Sobel(colorChannels[2], rSobelX, DepthType.Cv8U, 1, 0);
            CvInvoke.Sobel(colorChannels[2], rSobelY, DepthType.Cv8U, 0, 1);

            CvInvoke.Sobel(colorChannels[1], gSobelX, DepthType.Cv8U, 1, 0);
            CvInvoke.Sobel(colorChannels[1], gSobelY, DepthType.Cv8U, 0, 1);

            CvInvoke.Sobel(colorChannels[0], bSobelX, DepthType.Cv8U, 1, 0);
            CvInvoke.Sobel(colorChannels[0], bSobelY, DepthType.Cv8U, 0, 1);


            int cols = img.Cols;
            int rows = img.Rows;

            var edgeValues = new double[rows, cols, 1];

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                {
                    var rX = rSobelX.GetData(rowIndex, columnIndex)[0];
                    var rY = rSobelY.GetData(rowIndex, columnIndex)[0];
                    var gX = gSobelX.GetData(rowIndex, columnIndex)[0];
                    var gY = gSobelY.GetData(rowIndex, columnIndex)[0];
                    var bX = bSobelX.GetData(rowIndex, columnIndex)[0];
                    var bY = bSobelY.GetData(rowIndex, columnIndex)[0];

                    var edgeRed = Math.Sqrt(rX * rX + rY * rY);
                    var edgeGreen = Math.Sqrt(gX * gX + gY * gY);
                    var edgeBlue = Math.Sqrt(bX * bX + bY * bY);

                    var edgeValue = (new double[] { edgeRed, edgeGreen, edgeBlue }).Max();
                    edgeValues[rowIndex, columnIndex, 0] = edgeValue;
                }
            }

            return edgeValues;
        }

        public static byte[, ,] PostProcessing(Mat img)
        {
            var edgeData = ComputeEdgeMagnitudes(img);
            int rows = img.Rows;
            int cols = img.Cols;
            var colors = img.NumberOfChannels;
            byte[, ,] newData = new byte[rows, cols, colors];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (row == 0
                            || row == rows - 1
                            || col == 0
                            || col == cols - 1)
                    {
                        newData[row, col, 2] = img.GetData(row, col)[2];
                        newData[row, col, 1] = img.GetData(row, col)[1];
                        newData[row, col, 0] = img.GetData(row, col)[0];
                    }
                    else
                    {
                        Rgb newValue = PostProcessPixel(new System.Drawing.Point(col, row), img, edgeData);
                        newData[row, col, 2] = ToByte(newValue.Red);
                        newData[row, col, 1] = ToByte(newValue.Green);
                        newData[row, col, 0] = ToByte(newValue.Blue);
                    }


                }
            }

            return newData;
        }

        public static byte ToByte(double d)
        {
            int round = (int)Math.Round(d);

            if (round > 255)
            {
                round = 255;
            }
            else if (round < 0)
            {
                round = 0;
            }

            return (byte)round;
        }

        public static Rgb PostProcessPixel(System.Drawing.Point centralPixel, Mat data, double[, ,] edgeMat)
        {
            System.Drawing.Point[] neighbours = GetNeighbours(centralPixel);

            if (neighbours != null)
            {
                var centralRgb = GetRgb(centralPixel, data);

                var rgb1 = GetRgb(neighbours[0], data);
                var rgb2 = GetRgb(neighbours[1], data);
                var rgb3 = GetRgb(neighbours[2], data);
                var rgb4 = GetRgb(neighbours[3], data);
                var rgb5 = GetRgb(neighbours[4], data);
                var rgb6 = GetRgb(neighbours[5], data);
                var rgb7 = GetRgb(neighbours[6], data);
                var rgb8 = GetRgb(neighbours[7], data);

                var d0 = Math.Abs(rgb4.Red - rgb5.Red) + Math.Abs(rgb4.Green - rgb5.Green) + Math.Abs(rgb4.Blue - rgb5.Blue);
                var d45 = Math.Abs(rgb6.Red - rgb3.Red) + Math.Abs(rgb6.Green - rgb3.Green) + Math.Abs(rgb6.Blue - rgb3.Blue);
                var d90 = Math.Abs(rgb7.Red - rgb2.Red) + Math.Abs(rgb7.Green - rgb2.Green) + Math.Abs(rgb7.Blue - rgb2.Blue);
                var d135 = Math.Abs(rgb8.Red - rgb1.Red) + Math.Abs(rgb8.Green - rgb1.Green) + Math.Abs(rgb8.Blue - rgb1.Blue);

                var dList = new[] { d0, d45, d90, d135 };

                var maxD = dList.Max();

                System.Drawing.Point point = new System.Drawing.Point();


                if (maxD == d0)
                {
                    var distance1 = ColorDistance(rgb4, centralRgb);
                    var distance2 = ColorDistance(rgb5, centralRgb);

                    if (distance1 < distance2)
                    {
                        point = neighbours[3];
                    }
                    else
                    {
                        point = neighbours[4];
                    }
                }
                else if (maxD == d45)
                {
                    var distance1 = ColorDistance(rgb6, centralRgb);
                    var distance2 = ColorDistance(rgb3, centralRgb);

                    if (distance1 < distance2)
                    {
                        point = neighbours[5];
                    }
                    else
                    {
                        point = neighbours[2];
                    }
                }
                else if (maxD == d90)
                {
                    var distance1 = ColorDistance(rgb7, centralRgb);
                    var distance2 = ColorDistance(rgb2, centralRgb);

                    if (distance1 < distance2)
                    {
                        point = neighbours[6];
                    }
                    else
                    {
                        point = neighbours[1];
                    }
                }
                else if (maxD == d135)
                {
                    var distance1 = ColorDistance(rgb8, centralRgb);
                    var distance2 = ColorDistance(rgb1, centralRgb);

                    if (distance1 < distance2)
                    {
                        point = neighbours[7];
                    }
                    else
                    {
                        point = neighbours[0];
                    }
                }

                var edgeVal = edgeMat[point.Y, point.X, 0];
                var centerEdgeVal = edgeMat[centralPixel.Y, centralPixel.X, 0];

                if (centerEdgeVal > edgeVal)
                {
                    return GetRgb(point, data);
                }
            }

            return GetRgb(centralPixel, data);
        }

        public static double ColorDistance(System.Drawing.Point p1, System.Drawing.Point p2, byte[, ,] data)
        {
            var rgb1 = GetRgb(p1, data);
            var rgb2 = GetRgb(p2, data);

            return ColorDistance(rgb1, rgb2);
        }

        public static double ColorDistance(Rgb rgb1, Rgb rgb2)
        {
            var val = Math.Abs(rgb1.Red - rgb2.Red) + Math.Abs(rgb1.Green - rgb2.Green) + Math.Abs(rgb1.Blue - rgb2.Blue);

            return val;
        }

        public static Rgb GetRgb(System.Drawing.Point point, byte[, ,] data)
        {
            Rgb rgb = new Rgb();

            rgb.Red = data[point.Y, point.X, 2];
            rgb.Green = data[point.Y, point.X, 1];
            rgb.Blue = data[point.Y, point.X, 0];

            return rgb;
        }

        public static Rgb GetRgb(System.Drawing.Point point, Mat img)
        {
            Rgb rgb = new Rgb();

            rgb.Red = img.GetData(point.Y, point.X)[2];
            rgb.Green = img.GetData(point.Y, point.X)[1];
            rgb.Blue = img.GetData(point.Y, point.X)[0];

            return rgb;

        }

        public static bool IsLocalMinima(System.Drawing.Point centralPixel, double[,] edgeValues)
        {
            System.Drawing.Point[] neighbours = GetNeighbours(centralPixel);

            double minValue = double.MaxValue;

            foreach (var p in neighbours)
            {
                var value = edgeValues[p.X, p.Y];

                if (value < minValue)
                {
                    minValue = value;
                }
            }

            var centralValue = edgeValues[centralPixel.X, centralPixel.Y];

            return centralValue <= minValue;
        }

        public static byte[,] EdgePreservingSmoothingBW(Mat img, int numberOfCycles = 5)
        {
            byte[,] resultValues = new byte[img.Rows, img.Cols];

            for (int i = 0; i < 1; i++)
            {

                int cols = img.Cols;
                int rows = img.Rows;

                //result image will be smaller because the pixels on the border have less then 8 neighbours. 
                //we are going to ignore them
                Mat result = new Mat(img.Rows - 2, img.Cols - 2, img.Depth, 1);


                for (int rowIndex = 0; rowIndex < rows; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                    {
                        if (rowIndex == 0
                            || rowIndex == rows - 1
                            || columnIndex == 0
                            || columnIndex == cols - 1)
                        {
                            //pixel is on the border
                            resultValues[rowIndex, columnIndex] = 0;
                            continue;
                        }
                        var oldValue = img.GetData(rowIndex, columnIndex)[0];
                        if (oldValue == 0)
                        {
                            //var newValue = ComputeManhattanColorDistancesBW(img, new System.Drawing.Point(rowIndex, columnIndex), 10);
                            resultValues[rowIndex, columnIndex] = Convert.ToByte(oldValue);
                        }
                        else
                        {
                            var newValue = ComputeManhattanColorDistancesBW(img, new System.Drawing.Point(rowIndex, columnIndex), 10);
                            resultValues[rowIndex, columnIndex] = Convert.ToByte(newValue);
                        }

                    }
                }

                //Image<Gray, byte> img2 = new Image<Gray, byte>(resultValues);
                //img = img2.Mat;
            }





            //ImageViewer.Show(img2, String.Format(
            //                                      "Img Gray"));

            return resultValues;
        }

        public static byte[,] ToArray(Mat img)
        {
            byte[,] resultValues = new byte[img.Rows, img.Cols];

            for (int i = 0; i < 1; i++)
            {

                int cols = img.Cols;
                int rows = img.Rows;

                //result image will be smaller because the pixels on the border have less then 8 neighbours. 
                //we are going to ignore them
      
                for (int rowIndex = 0; rowIndex < rows; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                    {

                        var oldValue = img.GetData(rowIndex, columnIndex)[0];
 
                            //var newValue = ComputeManhattanColorDistancesBW(img, new System.Drawing.Point(rowIndex, columnIndex), 10);
                        resultValues[rowIndex, columnIndex] = Convert.ToByte(oldValue);
     

                    }
                }

                //Image<Gray, byte> img2 = new Image<Gray, byte>(resultValues);
                //img = img2.Mat;
            }





            //ImageViewer.Show(img2, String.Format(
            //                                      "Img Gray"));

            return resultValues;
        }



        public static byte[, ,] EdgePreservingSmoothing(Mat img, int numberOfCycles = 5)
        {
            byte[, ,] resultValues = new byte[img.Rows, img.Cols, 3];

            for (int i = 0; i < 1; i++)
            {
                var colorChannels = img.Split();

                var rChannel = colorChannels[2];
                var gChannel = colorChannels[1];
                var bChannel = colorChannels[0];

                int cols = img.Cols;
                int rows = img.Rows;

                //result image will be smaller because the pixels on the border have less then 8 neighbours. 
                //we are going to ignore them
                Mat result = new Mat(img.Rows - 2, img.Cols - 2, img.Depth, 1);


                for (int rowIndex = 0; rowIndex < rows; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < cols; columnIndex++)
                    {
                        if (rowIndex == 0
                            || rowIndex == rows - 1
                            || columnIndex == 0
                            || columnIndex == cols - 1)
                        {
                            //pixel is on the border
                            resultValues[rowIndex, columnIndex, 0] = 0;
                            resultValues[rowIndex, columnIndex, 1] = 0;
                            resultValues[rowIndex, columnIndex, 2] = 0;
                            continue;
                        }

                        var newValue = ComputeManhattanColorDistances(rChannel, gChannel, bChannel, new System.Drawing.Point(rowIndex, columnIndex), 10);
                        resultValues[rowIndex, columnIndex, 0] = Convert.ToByte(newValue[2]);
                        resultValues[rowIndex, columnIndex, 1] = Convert.ToByte(newValue[1]);
                        resultValues[rowIndex, columnIndex, 2] = Convert.ToByte(newValue[0]);
                    }
                }
                Image<Bgr, byte> img2 = new Image<Bgr, byte>(resultValues);
                img = img2.Mat;
            }





            //ImageViewer.Show(img2, String.Format(
            //                                      "Img Gray"));

            return resultValues;
        }

        public static System.Drawing.Point[] GetNeighbours(System.Drawing.Point centralPixel)
        {
            System.Drawing.Point[] neighbours = new System.Drawing.Point[8];
            //ROW 1
            neighbours[0] = new System.Drawing.Point(centralPixel.X - 1, centralPixel.Y - 1);
            neighbours[1] = new System.Drawing.Point(centralPixel.X, centralPixel.Y - 1);
            neighbours[2] = new System.Drawing.Point(centralPixel.X + 1, centralPixel.Y - 1);

            //ROW 2
            neighbours[3] = new System.Drawing.Point(centralPixel.X - 1, centralPixel.Y);
            //Point p5 = new Point(centralPixel.X, centralPixel.Y); / centralPixel
            neighbours[4] = new System.Drawing.Point(centralPixel.X + 1, centralPixel.Y);

            //ROW 3
            neighbours[5] = new System.Drawing.Point(centralPixel.X - 1, centralPixel.Y + 1);
            neighbours[6] = new System.Drawing.Point(centralPixel.X, centralPixel.Y + 1);
            neighbours[7] = new System.Drawing.Point(centralPixel.X + 1, centralPixel.Y + 1);

            return neighbours;
        }

        public const double REF_X = 95.047; // Observer= 2°, Illuminant= D65
        public const double REF_Y = 100.000;
        public const double REF_Z = 108.883;

        static double[] bgr2xyz(byte[] bgr)
        {
            double[] xyz = new double[3];

            double r = (double)bgr[2] / 255.0;
            double g = (double)bgr[1] / 255.0;
            double b = (double)bgr[0] / 255.0;
            if (r > 0.04045)
                r = Math.Pow((r + 0.055) / 1.055, 2.4);
            else
                r = r / 12.92;
            if (g > 0.04045)
                g = Math.Pow((g + 0.055) / 1.055, 2.4);
            else
                g = g / 12.92;
            if (b > 0.04045)
                b = Math.Pow((b + 0.055) / 1.055, 2.4);
            else
                b = b / 12.92;
            r *= 100.0;
            g *= 100.0;
            b *= 100.0;
            xyz[0] = r * 0.4124 + g * 0.3576 + b * 0.1805;
            xyz[1] = r * 0.2126 + g * 0.7152 + b * 0.0722;
            xyz[2] = r * 0.0193 + g * 0.1192 + b * 0.9505;

            return xyz;
        }

        static double[] xyz2lab(double[] xyz)
        {
            double[] lab = new double[3];

            double x = xyz[0] / REF_X;
            double y = xyz[1] / REF_X;
            double z = xyz[2] / REF_X;
            if (x > 0.008856)
                x = Math.Pow(x, .3333333333);
            else
                x = (7.787 * x) + (16.0 / 116.0);
            if (y > 0.008856)
                y = Math.Pow(y, .3333333333);
            else
                y = (7.787 * y) + (16.0 / 116.0);
            if (z > 0.008856)
                z = Math.Pow(z, .3333333333);
            else
                z = (7.787 * z) + (16.0 / 116.0);
            lab[0] = (116.0 * y) - 16.0;
            lab[1] = 500.0 * (x - y);
            lab[2] = 200.0 * (y - z);

            return lab;
        }

        static double[] lab2lch(double[] lab)
        {
            double[] lch = new double[3];

            lch[0] = lab[0];
            lch[1] = Math.Sqrt((lab[1] * lab[1]) + (lab[2] * lab[2]));
            lch[2] = Math.Atan2(lab[2], lab[1]);

            return lch;
        }

        static double deltaE2000(byte[] bgr1, byte[] bgr2)
        {
            double[] xyz1, xyz2, lab1, lab2, lch1, lch2;
            xyz1 = bgr2xyz(bgr1);
            xyz2 = bgr2xyz(bgr2);
            lab1 = xyz2lab(xyz1);
            lab2 = xyz2lab(xyz2);
            lch1 = lab2lch(lab1);
            lch2 = lab2lch(lab2);
            return deltaE2000(lch1, lch2);
        }


        static double deltaE2000(double[] lch1, double[] lch2)
        {
            double avg_L = (lch1[0] + lch2[0]) * 0.5;
            double delta_L = lch2[0] - lch1[0];
            double avg_C = (lch1[1] + lch2[1]) * 0.5;
            double delta_C = lch1[1] - lch2[1];
            double avg_H = (lch1[2] + lch2[2]) * 0.5;
            if (Math.Abs(lch1[2] - lch2[2]) > Math.PI)
            {
                avg_H += Math.PI;
            }

            double delta_H = lch2[2] - lch1[2];
            if (Math.Abs(delta_H) > Math.PI)
            {
                if (lch2[2] <= lch1[2])
                {
                    delta_H += Math.PI * 2.0;
                }
                else
                {
                    delta_H -= Math.PI * 2.0;
                }
            }

            delta_H = Math.Sqrt(lch1[1] * lch2[1]) * Math.Sin(delta_H) * 2.0;
            double T = 1.0 -
                    0.17 * Math.Cos(avg_H - Math.PI / 6.0) +
                    0.24 * Math.Cos(avg_H * 2.0) +
                    0.32 * Math.Cos(avg_H * 3.0 + Math.PI / 30.0) -
                    0.20 * Math.Cos(avg_H * 4.0 - Math.PI * 7.0 / 20.0);
            double SL = avg_L - 50.0;
            SL *= SL;
            SL = SL * 0.015 / Math.Sqrt(SL + 20.0) + 1.0;
            double SC = avg_C * 0.045 + 1.0;
            double SH = avg_C * T * 0.015 + 1.0;
            double delta_Theta = avg_H / 25.0 - Math.PI * 11.0 / 180.0;
            delta_Theta = Math.Exp(delta_Theta * -delta_Theta) * (Math.PI / 6.0);
            double RT = Math.Pow(avg_C, 7.0);
            RT = Math.Sqrt(RT / (RT + 6103515625.0)) * Math.Sin(delta_Theta) * -2.0; // 6103515625 = 25^7
            delta_L /= SL;
            delta_C /= SC;
            delta_H /= SH;
            return Math.Sqrt(delta_L * delta_L + delta_C * delta_C + delta_H * delta_H + RT * delta_C * delta_H);
        }
        public static int[] ComputeManhattanColorDistances(Mat rChannel, Mat gChannel, Mat bChannel, System.Drawing.Point centralPixel, double p)
        {
            System.Drawing.Point[] neighbours = GetNeighbours(centralPixel);

            var redCenterByte = rChannel.GetData(centralPixel.X, centralPixel.Y);
            var greenCenterByte = bChannel.GetData(centralPixel.X, centralPixel.Y);
            var blueCenterByte = bChannel.GetData(centralPixel.X, centralPixel.Y);

            var redCenter = (byte)Convert.ToInt32(redCenterByte[0]);
            var greenCenter = (byte)Convert.ToInt32(greenCenterByte[0]);
            var blueCenter = (byte)Convert.ToInt32(blueCenterByte[0]);

            var coefficients = new double[8];

            for (int dIndex = 0; dIndex < 8; dIndex++)
            {
                var point = neighbours[dIndex];
                var redByte = rChannel.GetData(point.X, point.Y);
                var greenByte = gChannel.GetData(point.X, point.Y);
                var blueByte = bChannel.GetData(point.X, point.Y);

                var red = (byte)Convert.ToInt32(redByte[0]);
                var green = (byte)Convert.ToInt32(greenByte[0]);
                var blue = (byte)Convert.ToInt32(blueByte[0]);



                var d = (double)(Math.Abs(redCenter - red) + Math.Abs(greenCenter - green) + Math.Abs(blueCenter - blue)) / (3 * 255);
                var delta2000 = deltaE2000(new byte[] { blue, green, red }, new byte[] { blueCenter, greenCenter, redCenter });
                //Debug.WriteLine("d: " + d);
                //var deltaPercentage = delta2000/100.0;
                //Debug.Assert(deltaPercentage <= 1);

                var c = Math.Pow((1 - d), p);

                coefficients[dIndex] = c;
                //distances.[0]
            }

            var sumCoefficients = coefficients.Sum();

            int newRed = 0;
            int newGreen = 0;
            int newBlue = 0;

            for (int dIndex = 0; dIndex < 8; dIndex++)
            {
                var point = neighbours[dIndex];
                var redByte = rChannel.GetData(point.X, point.Y);
                var greenByte = gChannel.GetData(point.X, point.Y);
                var blueByte = bChannel.GetData(point.X, point.Y);

                var red = Convert.ToInt32(redByte[0]);
                var green = Convert.ToInt32(greenByte[0]);
                var blue = Convert.ToInt32(blueByte[0]);

                newRed += (int)Math.Round(coefficients[dIndex] * (1 / sumCoefficients) * red);
                newGreen += (int)Math.Round(coefficients[dIndex] * (1 / sumCoefficients) * green);
                newBlue += (int)Math.Round(coefficients[dIndex] * (1 / sumCoefficients) * blue);
            }

            if (newRed > 255)
            {
                newRed = 255;
            }

            if (newGreen > 255)
            {
                newGreen = 255;
            }

            if (newBlue > 255)
            {
                newBlue = 255;
            }

            return new int[] { newRed, newGreen, newBlue };
        }

        public static int ComputeManhattanColorDistancesBW(Mat gray, System.Drawing.Point centralPixel, double p)
        {
            System.Drawing.Point[] neighbours = GetNeighbours(centralPixel);

            var centerByte = gray.GetData(centralPixel.X, centralPixel.Y);

            var center = (byte)Convert.ToInt32(centerByte[0]);

            var coefficients = new double[8];

            for (int dIndex = 0; dIndex < 8; dIndex++)
            {
                var point = neighbours[dIndex];
                var byteVal = gray.GetData(point.X, point.Y);

                var val = (byte)Convert.ToInt32(byteVal[0]);



                var d = (double)(Math.Abs(center - val) / 255);
                //var delta2000 = deltaE2000(new byte[] { blue, green, red }, new byte[] { blueCenter, greenCenter, redCenter });
                //Debug.WriteLine("d: " + d);
                //var deltaPercentage = delta2000/100.0;
                //Debug.Assert(deltaPercentage <= 1);

                var c = Math.Pow((1 - d), p);

                coefficients[dIndex] = c;
                //distances.[0]
            }

            var sumCoefficients = coefficients.Sum();

            int newVal = 0;

            for (int dIndex = 0; dIndex < 8; dIndex++)
            {
                var point = neighbours[dIndex];
                var valByte = gray.GetData(point.X, point.Y);

                var val = Convert.ToInt32(valByte[0]);

                newVal += (int)Math.Round(coefficients[dIndex] * (1 / sumCoefficients) * val);
            }

            if (newVal > 255)
            {
                newVal = 255;
            }

            return newVal;
        }

        private void ResultButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button == null)
            {
                return;
            }

            EdgeDetectionWindow details = new EdgeDetectionWindow(button.DataContext as Mat);
            //details.DataContext = button.DataContext; ;
            details.Show();
        }

        public byte[, ,] ChangeContrast(Mat img, double a, double b = 128)
        {
            //a = 50;
            byte[, ,] data = new byte[img.Rows, img.Cols, img.NumberOfChannels];

            var factor = (259 * (a + 255)) / (255 * (259 - a));


            //if (img.NumberOfChannels == 3)
            //{
            for (int row = 0; row < img.Rows; row++)
            {
                for (int column = 0; column < img.Cols; column++)
                {
                    for (int channel = 0; channel < img.NumberOfChannels; channel++)
                    {
                        var oldValue = img.GetData(row, column)[channel];
                        var val = factor * (oldValue - b) + b;
                        var newValue = Math.Truncate(val);

                        if (newValue > 256)
                        {
                            newValue = 255;
                        }
                        else if (newValue < 0)
                        {
                            newValue = 0;
                        }

                        data[row, column, channel] = (byte)newValue;
                    }
                }
                //}
            }

            return data;
        }

        VectorOfVectorOfPoint FindBlobs(Mat binary, int index)
        {
            VectorOfVectorOfPoint blobs = new VectorOfVectorOfPoint();

            // Fill the label_image with the blobs
            // 0  - background
            // 1  - unlabelled foreground
            // 2+ - labelled foreground

            Mat label_image = new Mat();
            binary.ConvertTo(label_image, DepthType.Cv8U);

            int label_count = 2; // starts at 2 because 0,1 are used already

            for (int y = 0; y < label_image.Rows; y++)
            {

                for (int x = 0; x < label_image.Cols; x++)
                {
                    var val = label_image.GetData(y, x)[0];

                    if (val != 255)
                    {
                        continue;
                    }
 
                    System.Drawing.Rectangle rect;
                    CvInvoke.FloodFill(label_image, new Mat(), new System.Drawing.Point(x, y), new MCvScalar(label_count), out rect, new MCvScalar(0), new MCvScalar(0), Connectivity.FourConnected, FloodFillType.Default);
                    //cv::floodFill(label_image, cv::Point(x,y), label_count, &rect, 0, 0, 4);

                    VectorOfPoint blob = new VectorOfPoint();

                    for (int i = rect.Y; i < (rect.Y + rect.Height); i++)
                    {

                        for (int j = rect.X; j < (rect.X + rect.Width); j++)
                        {
                            var val2 = label_image.GetData(y, x)[0];
                            if (val2 != label_count)
                            {
                                continue;
                            }

                            blob.Push(new System.Drawing.Point[] { new System.Drawing.Point(j, i) });
                        }
                    }

                    blobs.Push(blob);

                    label_count++;
                }
            }

            label_image.Save("labeled" + index + ".bmp");

            return blobs;
        }
    }
}
