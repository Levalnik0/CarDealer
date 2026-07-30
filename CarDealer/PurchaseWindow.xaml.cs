using System.Windows;

namespace CarDealer
{
    public partial class PurchaseWindow : Window
    {
        public string Result { get; private set; }

        public PurchaseWindow()
        {
            InitializeComponent();
        }

        private void Credit_Click(object sender, RoutedEventArgs e)
        {
            Result = "Кредит";
            DialogResult = true;
        }

        private void TradeIn_Click(object sender, RoutedEventArgs e)
        {
            Result = "TradeIn";
            DialogResult = true;
        }

        private void BuyNow_Click(object sender, RoutedEventArgs e)
        {
            Result = "Покупка";
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}