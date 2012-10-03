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
using System.Windows.Data;

namespace DCView.Util
{
    public static class SilverlightUtil
    {
        static public void RegisterForNotification(this FrameworkElement thisObject, string propertyName, FrameworkElement element, PropertyChangedCallback callback)
        {

            //Bind to a depedency property 
            Binding b = new Binding(propertyName) { Source = element };
            var prop = System.Windows.DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                thisObject.GetType(),
                new System.Windows.PropertyMetadata(callback));

            element.SetBinding(prop, b);
        }
    }
}
