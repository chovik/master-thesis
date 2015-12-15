using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgeDetection
{
    public class BindableValue : INotifyPropertyChanged
    {
        private double _value;

        public double Value
        {
            get { return _value; }
            set 
            { 
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        public BindableValue(double value)
        {
            this.Value = value;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if(handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
