using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgeDetection
{
    public class ConvolutionMatrix
    {
        private int _rows;

        public int Rows
        {
            get { return _rows; }
            set { _rows = value; }
        }

        private int _cols;

        public int Cols
        {
            get { return _cols; }
            set { _cols = value; }
        }
        
        
        private ObservableCollection<BindableValue> _values;

        public ObservableCollection<BindableValue> Values
        {
            get 
            { 
                if(_values == null)
                {
                    _values = new ObservableCollection<BindableValue>();
                }

                return _values; 
            }
        }

        public ConvolutionMatrix(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;

            InitializeValues();
        }

        public void InitializeValues()
        {
            Values.Clear();

            var count = Rows * Cols;


            for (int i = 0; i < count; i++)
            {
                Values.Add(new BindableValue(0.0));
            }
        }

        public double[,] GetData()
        {
            var count = Rows * Cols;
            double[,] data = new double[Rows, Cols];

            for (int i = 0; i < count; i++)
            {
                var val = Values[i];
                var row = i / Cols;
                var column = i % Rows;
                data[row, column] = val.Value;
            }

            return data;
        }
    }
}
