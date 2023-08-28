using System;
using System.Windows;
using System.Windows.Controls;

namespace IpTvParser.Controls
{
    /// <summary>
    /// Interaction logic for NumberPicker.xaml
    /// </summary>
    public partial class NumberPicker : UserControl
    {
        public NumberPicker()
        {
            InitializeComponent();
        }


        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min", typeof(int), typeof(NumberPicker), new PropertyMetadata(0));

        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register("Max", typeof(int), typeof(NumberPicker), new PropertyMetadata(100));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumberPicker), new FrameworkPropertyMetadata(20));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(NumberPicker), new PropertyMetadata(string.Empty));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        public int MaxValue
        {
            get { return (int)GetValue(MaxProperty); }
            set
            {
                SetValue(MaxProperty, value);
                if (Value > value)
                    Value = value;
            }
        }
        public int MinValue
        {
            get { return (int)GetValue(MinProperty); }
            set
            {
                SetValue(MinProperty, value);
                if (value > Value)
                    Value = value;
            }
        }

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set
            {
                if (value > MaxValue)
                    return;
                if (value < MinValue)
                    return;
                SetValue(ValueProperty, value);
                ValueChanged?.Invoke(new NumberPickerValueChangedEventArg(value));
            }
        }

        private void IncrementButton_Click(object sender, RoutedEventArgs e)
        {
            Value++;
        }

        private void DecrementButton_Click(object sender, RoutedEventArgs e)
        {
            Value--;
        }

        public event Action<NumberPickerValueChangedEventArg> ValueChanged;
    }

    public class NumberPickerValueChangedEventArg : EventArgs
    {
        public NumberPickerValueChangedEventArg(int value) { Value = value; }

        public int Value { get; private set; }
    }
}
