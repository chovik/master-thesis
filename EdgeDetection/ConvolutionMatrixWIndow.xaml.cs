using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Win32;
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
            var convMatrix = new ConvolutionMatrix(3, 3);
            convMatrix.Values.Clear();
            convMatrix.Values.Add(new BindableValue(-1.0));
            convMatrix.Values.Add(new BindableValue( 0.0));
            convMatrix.Values.Add(new BindableValue( 1.0));

            convMatrix.Values.Add(new BindableValue(-2.0));
            convMatrix.Values.Add(new BindableValue( 0.0));
            convMatrix.Values.Add(new BindableValue( 2.0));

            convMatrix.Values.Add(new BindableValue(-1.0));
            convMatrix.Values.Add(new BindableValue( 0.0));
            convMatrix.Values.Add(new BindableValue( 1.0));

            var convMatrix2 = new ConvolutionMatrix(3, 3);
            convMatrix2.Values.Clear();
            convMatrix2.Values.Add(new BindableValue(-1.0));
            convMatrix2.Values.Add(new BindableValue(-2.0));
            convMatrix2.Values.Add(new BindableValue(-1.0));

            convMatrix2.Values.Add(new BindableValue( 0.0));
            convMatrix2.Values.Add(new BindableValue( 0.0));
            convMatrix2.Values.Add(new BindableValue( 0.0));

            convMatrix2.Values.Add(new BindableValue( 1.0));
            convMatrix2.Values.Add(new BindableValue( 2.0));
            convMatrix2.Values.Add(new BindableValue( 1.0));

            var convMatrix3 = new ConvolutionMatrix(3, 3);
            convMatrix3.Values.Clear();
            convMatrix3.Values.Add(new BindableValue( 0.0));
            convMatrix3.Values.Add(new BindableValue(-1.0));
            convMatrix3.Values.Add(new BindableValue(-2.0));

            convMatrix3.Values.Add(new BindableValue( 1.0));
            convMatrix3.Values.Add(new BindableValue( 0.0));
            convMatrix3.Values.Add(new BindableValue(-1.0));

            convMatrix3.Values.Add(new BindableValue( 2.0));
            convMatrix3.Values.Add(new BindableValue( 1.0));
            convMatrix3.Values.Add(new BindableValue( 0.0));

            var convMatrix4 = new ConvolutionMatrix(3, 3);
            convMatrix4.Values.Clear();
            convMatrix4.Values.Add(new BindableValue(1.0));
            convMatrix4.Values.Add(new BindableValue(0.0));
            convMatrix4.Values.Add(new BindableValue(-1.0));

            convMatrix4.Values.Add(new BindableValue(2.0));
            convMatrix4.Values.Add(new BindableValue(0.0));
            convMatrix4.Values.Add(new BindableValue(-2.0));

            convMatrix4.Values.Add(new BindableValue(1.0));
            convMatrix4.Values.Add(new BindableValue(0.0));
            convMatrix4.Values.Add(new BindableValue(-1.0));

            var convMatrix5 = new ConvolutionMatrix(3, 3);
            convMatrix5.Values.Clear();
            convMatrix5.Values.Add(new BindableValue(1.0));
            convMatrix5.Values.Add(new BindableValue(2.0));
            convMatrix5.Values.Add(new BindableValue(1.0));

            convMatrix5.Values.Add(new BindableValue(0.0));
            convMatrix5.Values.Add(new BindableValue(0.0));
            convMatrix5.Values.Add(new BindableValue(0.0));

            convMatrix5.Values.Add(new BindableValue(-1.0));
            convMatrix5.Values.Add(new BindableValue(-2.0));
            convMatrix5.Values.Add(new BindableValue(-1.0));

            var convMatrix6 = new ConvolutionMatrix(3, 3);
            convMatrix6.Values.Clear();
            convMatrix6.Values.Add(new BindableValue(0.0));
            convMatrix6.Values.Add(new BindableValue(1.0));
            convMatrix6.Values.Add(new BindableValue(2.0));

            convMatrix6.Values.Add(new BindableValue(-1.0));
            convMatrix6.Values.Add(new BindableValue(0.0));
            convMatrix6.Values.Add(new BindableValue(1.0));

            convMatrix6.Values.Add(new BindableValue(-2.0));
            convMatrix6.Values.Add(new BindableValue(-1.0));
            convMatrix6.Values.Add(new BindableValue(0.0));

            ConvolutionList.Add(convMatrix);
            ConvolutionList.Add(convMatrix2);
            ConvolutionList.Add(convMatrix3);

            ConvolutionList.Add(convMatrix4);
            ConvolutionList.Add(convMatrix5);
            ConvolutionList.Add(convMatrix6);


            for (int i = 0; i < (9-5); i++)
            {
                ConvolutionList.Add(new ConvolutionMatrix(3, 3));
            }                
        }

        public void SaveFinalImages()
        {
            OpenFileDialog ofd = new OpenFileDialog();

            var fileSelected = ofd.ShowDialog();
            if(fileSelected.HasValue
                && fileSelected.Value)
            {
                var file = ofd.FileName;
                var uri = new Uri(file);
                using (var image = new Mat(file, LoadImageType.Grayscale))
                {
                    int convIndex = 0;
                    foreach(var conv in ConvolutionList)
                    {
                        //var convMat = new Matrix<byte>(conv.Values.Select(v => Helper.ToByte(v.Value)).ToArray());
                        var convMat = conv.GetData();

                        var finalImage = Helper.ApplyConvolutionMask(image, convMat);
                        finalImage.Save("convolution-" + convIndex + ".bmp");
                        convIndex++;
                    }
                }
                
            }
            
        }

        private void SaveFinalImagesButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFinalImages();
        }

    }
}
