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
using System.Windows.Shapes;

namespace EdgeDetection
{
    /// <summary>
    /// Interaction logic for ConvolutionMatrixWIndow.xaml
    /// </summary>
    public partial class ConvolutionMatrixWIndow : Window
    {
        private ObservableCollection<ConvolutionMatrix> _convolutionList;

        public ObservableCollection<ConvolutionMatrix> ConvolutionList
        {
            get
            {
                if(_convolutionList == null)
                {
                    _convolutionList = new ObservableCollection<ConvolutionMatrix>();
                }

                return _convolutionList; 
            }
        }
        
        public ConvolutionMatrixWIndow()
        {
            InitializeComponent();
            DataContext = this;

            for (int i = 0; i < 9; i++)
            {
                ConvolutionList.Add(new ConvolutionMatrix(3, 3));
            }                
        }
    }
}
