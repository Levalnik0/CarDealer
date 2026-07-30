using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CarDealer
{
    public partial class BookingWindow : Window
    {
        public string ClientName { get; private set; }
        public string Phone { get; private set; }

        public BookingWindow()
        {
            InitializeComponent();
            PhoneBox.Text = "+7";
        }

        // В номер телефона пускаем только цифры
        private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        }

        // Простая маска: +7 нельзя стереть, длина ограничена 12 символами
        private void PhoneBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!PhoneBox.Text.StartsWith("+7"))
                PhoneBox.Text = "+7";

            if (PhoneBox.Text.Length > 12)
                PhoneBox.Text = PhoneBox.Text.Substring(0, 12);

            PhoneBox.SelectionStart = PhoneBox.Text.Length;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) || PhoneBox.Text.Length < 12)
            {
                MessageBox.Show("Введите корректные данные");
                return;
            }

            ClientName = NameBox.Text;
            Phone = PhoneBox.Text;

            DialogResult = true;
        }
    }
}