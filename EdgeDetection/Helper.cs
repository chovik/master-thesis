using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgeDetection
{
    public class Helper
    {
        public static byte ToUnsignedByte(double d)
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

        public static byte ToByte(double d)
        {
            int round = (int)Math.Round(d);

            if (round > 127)
            {
                round = 127;
            }
            else if (round < -127)
            {
                round = -127;
            }

            return (byte)round;
        }

        public static Matrix<byte> ApplyConvolutionMask(Mat img, double[,] convMat)
        {
            Matrix<byte> finalMatrix = new Matrix<byte>(img.Size);

            int rows = img.Rows;
            int cols = img.Cols;

            var maskRows = convMat.GetLength(0);
            var maskCols = convMat.GetLength(1);
            var halfMaskRows = (maskRows - 1) / 2;
            var halfMaskCols = (maskCols - 1) / 2;

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
                        double finalValue = 0.0;

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

                        finalMatrix[row, col] = Helper.ToUnsignedByte(finalValue);
                    }                  

                    
                }
            }

            return finalMatrix;
        }


        public static System.Drawing.Point[] GetNeighbours(System.Drawing.Point centralPixel, double[,] convMat)
        {
            var rows = convMat.GetLength(0);
            var cols = convMat.GetLength(1);
            var rowsHalf = (rows - 1) / 2;
            var colsHalf = (cols - 1) / 2;
            var firstRow = centralPixel.Y - rowsHalf;
            var firstColumn = centralPixel.X - colsHalf;
            var lastRow = centralPixel.Y + rowsHalf;
            var lastColumn = centralPixel.X + colsHalf;
            var currentRow = centralPixel.Y;
            var currentCol = centralPixel.X;

            System.Drawing.Point[] neighbours = new System.Drawing.Point[rows * cols];//-1
            int pointIndex = 0;

            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    //if(column != currentCol
                    //    || row != currentRow)
                    //{
                        var neighbourPoint = new System.Drawing.Point(column, row);
                        neighbours[pointIndex] = neighbourPoint;

                        pointIndex++;
                    //}
                }
            }

            return neighbours;
        }
    }
}
