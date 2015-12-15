using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace EdgeDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Matrix<byte> ApplyConvolutionMask(Mat img, Matrix<byte> convMat)
        {
            Matrix<byte> finalMatrix = new Matrix<byte>(img.Size);

            int rows = img.Rows;
            int cols = img.Cols;

            var maskRows = convMat.Rows;
            var maskCols = convMat.Cols;
            var halfMaskRows = (convMat.Rows - 1) / 2;
            var halfMaskCols = (convMat.Cols - 1) / 2;

            for(int row = 0; row < rows; row++)
            {
                for(int col = 0; col < cols; col++)
                {
                    if(!(row - halfMaskRows < 0
                        || row + halfMaskRows >= rows
                        || col - halfMaskCols < 0
                        || col + halfMaskCols >= cols))
                    {
                        var value = img.GetData(row, col)[0];
                        var currentPoint = new System.Drawing.Point(col, row);
                        var finalValue = 0;

                        var neighbours = GetNeighbours(currentPoint, convMat);

                        for(var neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
                        {
                            var neighbour = neighbours[neighbourIndex];
                            var neighbourValue = img.GetData(neighbour.Y, neighbour.X)[0];

                            var maskRow = neighbourIndex / maskCols;
                            var maskCol = neighbourIndex % maskRows;
                            var maskValue = convMat[maskRow, maskCol];

                            var convValue = neighbourValue * maskValue;
                            finalValue += convValue;
                        }

                        finalMatrix[row, col] = ToByte(finalValue);
                    }                  

                    
                }
            }

            return finalMatrix;
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

        public static System.Drawing.Point[] GetNeighbours(System.Drawing.Point centralPixel, Matrix<byte> convMat)
        {
            var rowsHalf = (convMat.Rows - 1) / 2;
            var colsHalf = (convMat.Cols - 1) / 2;
            var firstRow = centralPixel.Y - rowsHalf;
            var firstColumn = centralPixel.X - colsHalf;
            var lastRow = centralPixel.Y + rowsHalf;
            var lastColumn = centralPixel.X + colsHalf;
            var currentRow = centralPixel.Y;
            var currentCol = centralPixel.X;

            System.Drawing.Point[] neighbours = new System.Drawing.Point[convMat.Rows * convMat.Cols - 1];
            int pointIndex = 0;

            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    if(column != currentCol
                        || row != currentRow)
                    {
                        var neighbourPoint = new System.Drawing.Point(column, row);
                        neighbours[pointIndex] = neighbourPoint;

                        pointIndex++;
                    }
                }
            }

            return neighbours;
        }

        private void ConvolutionMatrixSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ConvolutionMatrixWIndow convolutionSettingsWindows = new ConvolutionMatrixWIndow();
            convolutionSettingsWindows.Show();
        }
    }
}
