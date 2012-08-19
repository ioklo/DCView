﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DCView
{
    public class Gallery
    {
        public string ID { get; private set; }
        public string Name { get; private set; }

        public Gallery(string id, string name)
        {
            this.ID = id;
            this.Name = name;            
        }
    }
}
