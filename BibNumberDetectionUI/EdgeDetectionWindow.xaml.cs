using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace BibNumberDetectionUI
{
    /// <summary>
    /// Interaction logic for EdgeDetectionWindow.xaml
    /// </summary>
    public partial class EdgeDetectionWindow : Window, INotifyPropertyChanged
    {
        private BitmapSource _bitmapSource;

        public BitmapSource BitmapSource
        {
            get { return _bitmapSource; }
            set 
            { 
                _bitmapSource = value;
                OnPropertyChanged("BitmapSource");
            }
        }

        private Mat _sourceImage = null;

        public EdgeDetectionWindow(Mat sourceImage)
        {
            _sourceImage = sourceImage;
            InitializeComponent();
            DataContext = this;
            BitmapSource = MainWindow.ToBitmapSource(sourceImage);
        }

        private void UpdateImage()
        {
            if (_sourceImage == null)
            {
                return;
            }

            using (Mat edgeMat = new Mat())
            {
                using(Mat gray = new Mat())
                {
                    if (_sourceImage.NumberOfChannels == 3)
                    {
                        CvInvoke.CvtColor(_sourceImage, gray, ColorConversion.Bgr2Gray);
                        //CvInvoke.AdaptiveThreshold(gray, edgeMat, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 11, 2);
                        //CvInvoke.Threshold(_sourceImage, edgeMat, minimumTreshold.Value, maximumTreshold.Value, Emgu.CV.CvEnum.ThresholdType.BinaryInv);
                        CvInvoke.Canny(_sourceImage, edgeMat, minimumTreshold.Value, maximumTreshold.Value, 3, false);
                        BitmapSource = MainWindow.ToBitmapSource(edgeMat);
                    }
                    else
                    {
                        //CvInvoke.CvtColor(_sourceImage, gray, ColorConversion.Bgr2Gray);
                       // CvInvoke.AdaptiveThreshold(_sourceImage, edgeMat, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 11, 2);
                        //CvInvoke.Threshold(_sourceImage, edgeMat, minimumTreshold.Value, maximumTreshold.Value, Emgu.CV.CvEnum.ThresholdType.BinaryInv);
                        CvInvoke.Canny(_sourceImage, edgeMat, minimumTreshold.Value, maximumTreshold.Value, 3, false);
                        BitmapSource = MainWindow.ToBitmapSource(edgeMat);
                    }
                    
                    
                }                
            }
        }

        private void minimumTreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateImage();
        }

        private void maximumTreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateImage();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if(handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
