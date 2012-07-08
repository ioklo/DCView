using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace MyApps.DCView
{
    public class Gallery : INotifyPropertyChanged
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public bool Adult { get; set; }

        public string PCSite
        {
            get
            {
                return "gall.dcinside.com";
            }
        }

        public string Site
        {
            get
            {
                return "m.dcinside.com";
            }
        }

        public bool IsFavorite
        {
            get
            {
                return isFavorite;
            }
            set
            {
                isFavorite = value;
                
                if( PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsFavorite"));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private bool isFavorite = false;
    }
}
