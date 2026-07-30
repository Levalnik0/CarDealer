using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;

namespace CarDealer
{
    public partial class CreditWindow : Window
    {
        CarDealershipEntities1 db = new CarDealershipEntities1();

        CustomerRequests currentRequest;
        Cars currentCar;

        // Цена, от которой считается кредит. Если машину брали по trade-in,
        // это уже уменьшенная сумма, а не прайс из каталога.
        decimal currentPrice = 0;

        public CreditWindow(string fullName)
        {
            InitializeComponent();

            UserNameText.Text = $"Здравствуйте, {fullName}";

            YearsBox.ItemsSource = new int[] { 1, 2, 3, 4, 5 };
            YearsBox.SelectedIndex = 2;

            LoadRequests();
        }

        void LoadRequests()
        {
            CreditRequestsList.ItemsSource = db.CustomerRequests
                .Where(r => r.Status == "Кредит")
                .ToList();
        }

        private void CreditRequestsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            currentRequest = CreditRequestsList.SelectedItem as CustomerRequests;
            if (currentRequest == null) return;

            currentCar = db.Cars.FirstOrDefault(c => c.Id == currentRequest.CarId);

            ClientBox.Text = currentRequest.FullName;
            CarBox.Text = currentCar.Manufacturer + " " + currentCar.Model;

            // FinalPrice заполняет оценщик, если была сдача авто в зачёт
            if (currentRequest.FinalPrice != null)
                currentPrice = currentRequest.FinalPrice.Value;
            else
                currentPrice = currentCar.Price ?? 0;

            PriceBox.Text = currentPrice.ToString();

            Recalculate();
        }

        private void CalcChanged(object sender, EventArgs e)
        {
            Recalculate();
        }

        // Пересчитывается при каждом изменении условий
        void Recalculate()
        {
            if (currentCar == null) return;

            decimal price = currentPrice;

            decimal initial = 0;
            decimal.TryParse(InitialBox.Text, out initial);

            int years = (int)YearsBox.SelectedItem;
            int months = years * 12;

            bool insurance = InsuranceBox.IsChecked == true;

            decimal rate = CalculateRate(price, initial, years, insurance);

            decimal insuranceSum = insurance ? price * 0.06m : 0;
            decimal loan = price - initial + insuranceSum;

            decimal payment = CalcAnnuity(loan, rate, months);

            decimal total = payment * months;
            decimal overpay = total - (price - initial);

            RateBox.Text = rate.ToString("0.00");
            PaymentText.Text = payment.ToString("N0") + " ₽";
            OverpayText.Text = overpay.ToString("N0") + " ₽";
        }

        // Ставка складывается из базовых 22% и набора надбавок и скидок:
        // короткий срок, крупный взнос и страховка её снижают.
        decimal CalculateRate(decimal price, decimal initial, int years, bool insurance)
        {
            decimal percent = price == 0 ? 0 : (initial / price) * 100;

            decimal rate = 22;

            if (years <= 2) rate -= 2;
            if (percent > 30) rate -= 3;
            if (percent < 10) rate += 4;

            if (insurance) rate -= 2;
            else rate += 3;

            if (rate < 15) rate = 15;
            if (rate > 35) rate = 35;

            return rate;
        }

        // Аннуитетный платёж по классической формуле.
        // Считаем в double: у decimal нет возведения в степень.
        decimal CalcAnnuity(decimal sum, decimal rate, int months)
        {
            if (sum <= 0) return 0;

            double i = (double)(rate / 12 / 100);
            double n = months;
            double s = (double)sum;

            double pay = s * (i * Math.Pow(1 + i, n)) / (Math.Pow(1 + i, n) - 1);

            return (decimal)pay;
        }

        private void GiveCredit_Click(object sender, RoutedEventArgs e)
        {
            if (currentRequest == null || currentCar == null) return;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF (*.pdf)|*.pdf";
            dialog.FileName = $"Кредит_{currentCar.Manufacturer}_{currentCar.Model}";

            if (dialog.ShowDialog() != true)
                return;

            string path = dialog.FileName;

            PdfWriter writer = new PdfWriter(path);
            PdfDocument pdf = new PdfDocument(writer);
            Document doc = new Document(pdf);

            // Кириллица в PDF требует внешнего шрифта
            string fontPath = @"C:\Windows\Fonts\arial.ttf";
            PdfFont font = PdfFontFactory.CreateFont(fontPath, "Identity-H");
            doc.SetFont(font);

            doc.Add(new Paragraph("КРЕДИТНЫЙ ДОГОВОР").SetFontSize(16));
            doc.Add(new Paragraph($"Дата: {DateTime.Now:dd.MM.yyyy}\n"));

            doc.Add(new Paragraph("Клиент:"));
            doc.Add(new Paragraph(currentRequest.FullName));
            doc.Add(new Paragraph($"Телефон: {currentRequest.Phone}\n"));

            doc.Add(new Paragraph("Автомобиль:"));
            doc.Add(new Paragraph($"{currentCar.Manufacturer} {currentCar.Model}"));
            doc.Add(new Paragraph($"Цена: {currentPrice}"));
            doc.Add(new Paragraph($"VIN: {currentCar.VIN}\n"));

            doc.Add(new Paragraph("Условия кредита:"));
            doc.Add(new Paragraph($"Первоначальный взнос: {InitialBox.Text}"));
            doc.Add(new Paragraph($"Срок: {YearsBox.SelectedItem} лет"));
            doc.Add(new Paragraph($"Ставка: {RateBox.Text}%"));
            doc.Add(new Paragraph($"Ежемесячный платеж: {PaymentText.Text}"));
            doc.Add(new Paragraph($"Переплата: {OverpayText.Text}\n"));

            doc.Add(new Paragraph("Подпись клиента: __________"));

            doc.Close();

            // Договор подписан — машина уходит с витрины, заявка закрывается.
            // FinalPrice фиксирует сумму сделки для отчёта по выручке.
            currentCar.StatusId = 3;

            currentRequest.Status = "Завершена";
            currentRequest.FinalPrice = currentPrice;

            db.SaveChanges();

            MessageBox.Show($"Кредит оформлен!\nДокумент сохранён:\n{path}");

            ClearForm();
            LoadRequests();
        }

        void ClearForm()
        {
            currentRequest = null;
            currentCar = null;
            currentPrice = 0;

            CreditRequestsList.SelectedItem = null;

            ClientBox.Text = "";
            CarBox.Text = "";
            PriceBox.Text = "";

            InitialBox.Text = "";

            YearsBox.SelectedIndex = 2;

            InsuranceBox.IsChecked = false;

            RateBox.Text = "";
            PaymentText.Text = "";
            OverpayText.Text = "";
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }
    }
}