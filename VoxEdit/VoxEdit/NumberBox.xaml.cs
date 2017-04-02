using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace VoxEdit
{
    /// <summary>
    /// Interaction logic for NumberBox.xaml
    /// </summary>
    public partial class NumberBox : UserControl
    {
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumberBox), new PropertyMetadata(0));


        public NumberBox()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void IncrementValueClick(object sender, RoutedEventArgs e)
        {
            int multiplier = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift)) { multiplier = 10; }

            Value += multiplier;
        }
        private void DecrementValueClick(object sender, RoutedEventArgs e)
        {
            int multiplier = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift)) { multiplier = 10; }

            Value -= multiplier;
        }

    }
    public class StringToNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string intAsString = value as string;
            if(intAsString != null)
            {
                if(int.TryParse(intAsString,out int result))
                {
                    return result;
                }
            }
            throw new ArgumentException("Could not parse value");
        }
    }
}
