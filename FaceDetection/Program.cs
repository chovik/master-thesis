//----------------------------------------------------------------------------
//  Copyright (C) 2004-2015 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Cuda;
using Emgu.CV.Util;

using System.Linq;

namespace FaceDetection
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
                if(_points == null)
                {
                    _points = new List<Point>();
                }

                return _points; 
            }
        }
        

    }
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         Run();
      }

      static Rectangle ComputeTorsoArea(Rectangle faceArea)
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

          Rectangle torsoRectangle = new Rectangle((int)Math.Round(torsoX), (int)Math.Round(torsoY), (int)Math.Round(torsoWidth), (int)Math.Round(torsoHeight));

          return torsoRectangle;
      }

      static void Run()
      {
          Mat image = new Mat("IMG_0041.jpg", LoadImageType.Color); //Read the files as an 8-bit Bgr image  
          Mat sharpImage = new Mat();
          ImageViewer.Show(image, String.Format(
                                                "Canny Gray"));
          CvInvoke.GaussianBlur(image, sharpImage, new System.Drawing.Size(5, 5), 5);
          ImageViewer.Show(sharpImage, String.Format(
                                                "Canny Gray"));
          CvInvoke.AddWeighted(image, 1.5, sharpImage, -0.5, 0, sharpImage);
          image = sharpImage;

          ImageViewer.Show(image, String.Format(
                                                "Canny Gray"));
            long detectionTime;
            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> eyes = new List<Rectangle>();

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

            foreach (Rectangle face in faces)
            {
                CvInvoke.Rectangle(image, face, new Bgr(Color.Red).MCvScalar, 2);


                Rectangle torsoRectangle = ComputeTorsoArea(face);

                Mat torsoMat = new Mat(image, torsoRectangle);
                ImageViewer.Show(torsoMat, String.Format(
                                                "Img Gray"));

                for (int i = 0; i < 5; i++)
                {
                    torsoMat = EdgePreservingSmoothing(torsoMat);
                }

                ImageViewer.Show(torsoMat, String.Format(
                                               "Img Gray"));

                var preprocessed = SubSampling(torsoMat);

                //ImageViewer.Show(preprocessed, String.Format(
                //                               "Img Gray"));

                using (Mat gray = new Mat())
                {
                    using (Mat canny = new Mat())
                    {
                        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                        {
                            var rgbTorsoMats = preprocessed.Split();

                            var rMat = rgbTorsoMats[0];
                            var gMat = rgbTorsoMats[1];
                            var bMat = rgbTorsoMats[2];

                            //var b = bMat.GetData(0, 0);

                            foreach (var rgbMat in rgbTorsoMats)
                            {
                                //CvInvoke.CvtColor(torsoMat, gray, ColorConversion.Bgr2Gray);
                                //CvInvoke.CLAHE(gray, 2, new Size(8, 8), gray);
                                CvInvoke.Canny(rgbMat, canny, 170, 100, 3, false);

                                LineSegment2D[] lines = CvInvoke.HoughLinesP(
                                   canny,
                                   1, //Distance resolution in pixel-related units
                                   Math.PI / 90, //Angle resolution measured in radians.
                                   90, //threshold
                                   10, //min Line width
                                   0); //gap between lines

                                //foreach (var line in lines)
                                //{
                                //    CvInvoke.Line(canny, line.P1, line.P2, new Bgr(Color.YellowGreen).MCvScalar, 1);
                                //}

                                //int[,] hierachy = CvInvoke.FindContourTree(canny, contours, ChainApproxMethod.ChainApproxSimple);
                                ImageViewer.Show(canny, String.Format(
                                                "Canny Gray"));
                            }

                            
                            //FindLicensePlate(contours, hierachy, 0, gray, canny, licensePlateImagesList, filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);
                        }
                    }
                }

                //CvInvoke.Rectangle(image, torsoRectangle, new Bgr(Color.Green).MCvScalar, 2);

            }

            

          
            

            //display the image 
            ImageViewer.Show(image, String.Format(
            "Completed face and eye detection using {0} in {1} milliseconds", 
            (tryUseCuda && CudaInvoke.HasCuda) ? "GPU"
            : (tryUseOpenCL && CvInvoke.HaveOpenCLCompatibleGpuDevice) ? "OpenCL" 
            : "CPU",
            detectionTime));
      }

       public static Mat SubSampling(Mat img)
       {
           ImageViewer.Show(img, String.Format(
                                                 "Canny Gray"));
           //img = new Mat("diplomka.png", LoadImageType.Color);
            var colorChannels = img.Split();

            var rSobelX =  new Mat(img.Size, DepthType.Cv8U, 1);
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

            for(int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for(int columnIndex = 0; columnIndex < cols; columnIndex++)
                {
                    var rX = rSobelX.GetData(rowIndex, columnIndex)[0];
                    var rY = rSobelY.GetData(rowIndex, columnIndex)[0];
                    var gX = gSobelX.GetData(rowIndex, columnIndex)[0];
                    var gY = gSobelY.GetData(rowIndex, columnIndex)[0];
                    var bX = bSobelX.GetData(rowIndex, columnIndex)[0];
                    var bY = bSobelY.GetData(rowIndex, columnIndex)[0];

                    var edgeRed = Math.Sqrt(rX*rX + rY*rY);
                    var edgeGreen = Math.Sqrt(gX*gX + gY*gY);
                    var edgeBlue = Math.Sqrt(bX*bX + bY*bY);

                    var edgeValue = (new double[]{edgeRed, edgeGreen, edgeBlue}).Max();
                    edgeValues[rowIndex, columnIndex] = edgeValue;
                }
            }

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
                        && columnIndex < cols - 1 )
                    {
                        isLocalMinima = IsLocalMinima(new Point(rowIndex, columnIndex), edgeValues);
                    }                    

                    if(!isLocalMinima)
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

                        if(samplesHistogram[r , g, b] == null)
                        {
                            var sample =  new RGBSample() { Red = r, Green = g, Blue = b};
                            samplesHistogram[r, g, b] = sample;
                            samplesList.Add(sample);
                        }

                        samplesHistogram[r, g, b].Points.Add(new Point(rowIndex, columnIndex));
                    }
                }
            }


           Image<Bgr, byte> img4 = new Image<Bgr, byte>(samples);
            ImageViewer.Show(img4, String.Format(
                                                 "44 Gray"));

           var visitedSamples = new List<byte[]>();

           var h = 32;

           var meanSamples = new List<RGBSample>();

           foreach(var sample in samplesList)
           {
               //visitedSamples.Add(sample);

               if(!sample.IsVisited)
               {
                   sample.IsVisited = true;

                   double meanRed = 0;
                   double meanGreen = 0;
                   double meanBlue = 0;

                   double totalCount = 0;

                   for(int r = sample.Red - h; r <= sample.Red + h; r++)
                   {
                       for(int g = sample.Green - h; g <= sample.Green + h; g++)
                       {
                           for(int b = sample.Blue - h; b <= sample.Blue + h; b++)
                           {
                               if(r >= 0 && r < 255
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

                   var meanSample = new RGBSample() { Red = (byte)Math.Round(meanRed/totalCount), Green = (byte)Math.Round(meanGreen/totalCount), Blue = (byte)Math.Round(meanBlue/totalCount) };
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

                    if(rgbHistogram[r , g, b] == null)
                    {
                        var sample =  new RGBSample() { Red = r, Green = g, Blue = b};
                        rgbHistogram[r, g, b] = sample;
                    }

                    rgbHistogram[r, g, b].Points.Add(new Point(rowIndex, columnIndex));
                }
            }

           //CvInvoke.MeanShift(img4, )

           foreach(var mean in meanSamples)
           {
               Debug.WriteLine("Mean R: " + mean.Red + " G: " + mean.Green + " B: " + mean.Blue);
           }

           var finalColors = new List<RGBSample>();

           foreach(var meanSample in meanSamples)
           {
               meanSample.IsVisited = true;

               double meanRed = 0;
               double meanGreen = 0;
               double meanBlue = 0;

               double totalCount = 0;
               double dm = double.MaxValue;
               var currentSample = meanSample;

               while(dm > 3)
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
                                       if(meanRed < 0)
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


           byte[,,] finalImageData = new byte[rows, cols, 3];

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

                   foreach(var final in finalColors)
                   {
                       var diff = (Math.Abs(final.Red - rOld)
                           + Math.Abs(final.Green - gOld)
                           + Math.Abs(final.Blue - bOld));
                       
                       if(diff < min)
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
            ImageViewer.Show(img2, String.Format(
                                                 "Canny Gray"));

            return img2.Mat;
       }

       public static bool IsLocalMinima(Point centralPixel, double[,] edgeValues)
       {
           Point[] neighbours = GetNeighbours(centralPixel);

           double minValue = double.MaxValue;

           foreach(var p in neighbours)
           {
               var value = edgeValues[p.X, p.Y];

               if(value < minValue)
               {
                   minValue = value;
               }
           }

           var centralValue = edgeValues[centralPixel.X, centralPixel.Y];

           return centralValue <= minValue;
       }



      public static Mat EdgePreservingSmoothing(Mat img)
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
          byte[, ,] resultValues = new byte[img.Rows, img.Cols, 3];

          for(int rowIndex = 0; rowIndex < rows; rowIndex++)
          {
              for(int columnIndex = 0; columnIndex < cols; columnIndex++)
              {
                  if(rowIndex == 0
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

                  var newValue = ComputeManhattanColorDistances(rChannel, gChannel, bChannel, new Point(rowIndex, columnIndex), 10);
                  resultValues[rowIndex, columnIndex, 0] = Convert.ToByte(newValue[2]);
                  resultValues[rowIndex, columnIndex, 1] = Convert.ToByte(newValue[1]);
                  resultValues[rowIndex, columnIndex, 2] = Convert.ToByte(newValue[0]);
              }
          }

          Image<Bgr, byte> img2 = new Image<Bgr, byte>(resultValues);
          //ImageViewer.Show(img2, String.Format(
          //                                      "Img Gray"));

          return img2.Mat;
      }

      public static Point[] GetNeighbours(Point centralPixel)
      {
          Point[] neighbours = new Point[8];
          //ROW 1
          neighbours[0] = new Point(centralPixel.X - 1, centralPixel.Y - 1);
          neighbours[1] = new Point(centralPixel.X, centralPixel.Y - 1);
          neighbours[2] = new Point(centralPixel.X + 1, centralPixel.Y - 1);

          //ROW 2
          neighbours[3] = new Point(centralPixel.X - 1, centralPixel.Y);
          //Point p5 = new Point(centralPixel.X, centralPixel.Y); / centralPixel
          neighbours[4] = new Point(centralPixel.X + 1, centralPixel.Y);

          //ROW 3
          neighbours[5] = new Point(centralPixel.X - 1, centralPixel.Y + 1);
          neighbours[6] = new Point(centralPixel.X, centralPixel.Y + 1);
          neighbours[7] = new Point(centralPixel.X + 1, centralPixel.Y + 1);

          return neighbours;
      }

      public static int[] ComputeManhattanColorDistances(Mat rChannel, Mat gChannel, Mat bChannel, Point centralPixel, double p)
      {
          Point[] neighbours = GetNeighbours(centralPixel);

          var redCenterByte = rChannel.GetData(centralPixel.X, centralPixel.Y);
          var greenCenterByte = bChannel.GetData(centralPixel.X, centralPixel.Y);
          var blueCenterByte = bChannel.GetData(centralPixel.X, centralPixel.Y);

          var redCenter = Convert.ToInt32(redCenterByte[0]);
          var greenCenter = Convert.ToInt32(greenCenterByte[0]);
          var blueCenter = Convert.ToInt32(blueCenterByte[0]);

          var coefficients = new double[8];

          for(int dIndex = 0; dIndex < 8; dIndex++)
          {
              var point = neighbours[dIndex];
              var redByte = rChannel.GetData(point.X, point.Y);
              var greenByte = bChannel.GetData(point.X, point.Y);
              var blueByte = bChannel.GetData(point.X, point.Y);

              var red = Convert.ToInt32(redByte[0]);
              var green = Convert.ToInt32(greenByte[0]);
              var blue = Convert.ToInt32(blueByte[0]);

              

              var d = (double)(Math.Abs(redCenter - red) + Math.Abs(greenCenter - green) + Math.Abs(blueCenter - blue)) / (3 * 255);

              //Debug.WriteLine("d: " + d);


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
              var greenByte = bChannel.GetData(point.X, point.Y);
              var blueByte = bChannel.GetData(point.X, point.Y);

              var red = Convert.ToInt32(redByte[0]);
              var green = Convert.ToInt32(greenByte[0]);
              var blue = Convert.ToInt32(blueByte[0]);

              newRed += (int)Math.Round(coefficients[dIndex] * (1 / sumCoefficients) * red);
              newGreen += (int)Math.Round(coefficients[dIndex] * (1 / sumCoefficients) * green);
              newBlue += (int)Math.Round(coefficients[dIndex] * (1 / sumCoefficients) * blue);
          }

          if(newRed > 255)
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

          return new int[] { newRed, newGreen,  newBlue };
      }

   }
}