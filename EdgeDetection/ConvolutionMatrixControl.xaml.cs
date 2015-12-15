using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for ConvolutionMatrixControl.xaml
    /// </summary>
    public partial class ConvolutionMatrixControl : UserControl
    {
        public double CellWidth
        {
            get { return (double)GetValue(CellWidthProperty); }
            set { SetValue(CellWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CellWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CellWidthProperty =
            DependencyProperty.Register("CellWidth", typeof(double), typeof(ConvolutionMatrixControl), new PropertyMetadata(10.0));



        public ConvolutionMatrix Matrix
        {
            get { return (ConvolutionMatrix)GetValue(MatrixProperty); }
            set { SetValue(MatrixProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Matrix.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MatrixProperty =
            DependencyProperty.Register("Matrix", typeof(ConvolutionMatrix), typeof(ConvolutionMatrixControl), new PropertyMetadata(null, OnMatrixChanged));

        public static void OnMatrixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var convolutionMatrixControl = d as ConvolutionMatrixControl;

            if (convolutionMatrixControl != null)
            {
                convolutionMatrixControl.DataContext = e.NewValue;
                convolutionMatrixControl.ComputeCellWidth();
            }
        }

        

        
        
        public ConvolutionMatrixControl()
        {
            InitializeComponent();

            this.SizeChanged += ConvolutionMatrixControl_SizeChanged;
            ComputeCellWidth();
        }

        void ConvolutionMatrixControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ComputeCellWidth();
        }

        private void ComputeCellWidth()
        {
            if(Matrix == null)
            {
                return;
            }

            var newWidth = this.Width;

            var newCellWidth = newWidth / Matrix.Cols;
            CellWidth = newCellWidth;
        }
    }
}
