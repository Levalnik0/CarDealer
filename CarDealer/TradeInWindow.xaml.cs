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
    public partial class TradeInWindow : Window
    {
        CarDealershipEntities1 db = new CarDealershipEntities1();

        CustomerRequests currentRequest;
        Cars newCar;

        byte[] photo;

        public TradeInWindow(string name)
        {
            InitializeComponent();
            UserNameText.Text = $"Здравствуйте, {name}";
            LoadRequests();
        }

        void LoadRequests()
        {
            RequestsList.ItemsSource = db.CustomerRequests
                .Where(r => r.Status == "TradeIn")
                .ToList();
        }

        private void RequestsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            currentRequest = RequestsList.SelectedItem as CustomerRequests;
            if (currentRequest == null) return;

            newCar = db.Cars.FirstOrDefault(c => c.Id == currentRequest.CarId);

            NewCarInfo.Text =
                $"{newCar.Manufacturer} {newCar.Model}\nЦена: {newCar.Price}";
        }

        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == true)
            {
                photo = System.IO.File.ReadAllBytes(dialog.FileName);
                PhotoText.Text = "Фото добавлено";
            }
        }

        private void CalcFinal(object sender, EventArgs e)
        {
            if (newCar == null) return;

            decimal oldPrice = 0;
            decimal.TryParse(PriceBox.Text, out oldPrice);

            decimal final = (newCar.Price ?? 0) - oldPrice;

            FinalPriceText.Text = final.ToString("N0") + " ₽";
        }

        // Оценщик закрывает заявку одним из трёх способов: клиент платит
        // разницу сразу, берёт её в кредит или пока ничего не решает.
        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (currentRequest == null || newCar == null) return;

            decimal oldPrice = decimal.TryParse(PriceBox.Text, out decimal p) ? p : 0;
            decimal final = (newCar.Price ?? 0) - oldPrice;

            var result = MessageBox.Show(
                "Да - покупка\nНет - кредит\nОтмена - ничего",
                "Завершение Trade-In",
                MessageBoxButton.YesNoCancel);

            // Принятая машина попадает в каталог в любом случае
            AddUsedCar(oldPrice);

            if (result == MessageBoxResult.Yes)
            {
                CreateTradeDkp(oldPrice, final);
                CompleteSale();
            }

            else if (result == MessageBoxResult.No)
            {
                currentRequest.FinalPrice = final;
                currentRequest.Status = "Кредит";

                db.SaveChanges();

                MessageBox.Show($"Передано в кредит\nСумма: {final}");

                Clear();
                LoadRequests();
            }
        }

        // Сдаваемая машина сразу становится доступной для продажи
        void AddUsedCar(decimal price)
        {
            Cars usedCar = new Cars
            {
                Manufacturer = BrandBox.Text,
                Model = ModelBox.Text,
                Year = int.TryParse(YearBox.Text, out int y) ? y : 0,
                Mileage = int.TryParse(MileageBox.Text, out int m) ? m : 0,
                Color = ColorBox.Text,
                Price = price,
                StatusId = 1,
                Photo = photo
            };

            db.Cars.Add(usedCar);
        }

        void CreateTradeDkp(decimal oldPrice, decimal final)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF (*.pdf)|*.pdf";

            if (dialog.ShowDialog() != true) return;

            string path = dialog.FileName;

            PdfWriter writer = new PdfWriter(path);
            PdfDocument pdf = new PdfDocument(writer);
            Document doc = new Document(pdf);

            // Кириллица в PDF требует внешнего шрифта
            string fontPath = @"C:\Windows\Fonts\arial.ttf";
            PdfFont font = PdfFontFactory.CreateFont(fontPath, "Identity-H");
            doc.SetFont(font);

            doc.Add(new Paragraph("ДОГОВОР TRADE-IN").SetFontSize(16));

            doc.Add(new Paragraph($"Клиент: {currentRequest.FullName}"));

            doc.Add(new Paragraph("\nПокупаемое авто:"));
            doc.Add(new Paragraph($"{newCar.Manufacturer} {newCar.Model}"));
            doc.Add(new Paragraph($"Цена: {newCar.Price}"));

            doc.Add(new Paragraph("\nПринятое авто:"));
            doc.Add(new Paragraph($"{BrandBox.Text} {ModelBox.Text}"));
            doc.Add(new Paragraph($"Год: {YearBox.Text}"));
            doc.Add(new Paragraph($"Пробег: {MileageBox.Text}"));
            doc.Add(new Paragraph($"Оценка: {oldPrice}"));

            doc.Add(new Paragraph("\nИТОГ:"));
            doc.Add(new Paragraph($"К оплате: {final}"));

            doc.Add(new Paragraph("\nПодпись: __________"));

            doc.Close();

            MessageBox.Show("ДКП сохранён");
        }

        void CompleteSale()
        {
            newCar.StatusId = 3; // продана

            db.CustomerRequests.Remove(currentRequest);

            db.SaveChanges();

            MessageBox.Show("Trade-In завершён");

            Clear();
            LoadRequests();
        }

        void Clear()
        {
            currentRequest = null;
            newCar = null;

            RequestsList.SelectedItem = null;

            NewCarInfo.Text = "";
            FinalPriceText.Text = "";

            BrandBox.Text = "";
            ModelBox.Text = "";
            YearBox.Text = "";
            MileageBox.Text = "";
            ColorBox.Text = "";
            PriceBox.Text = "";
            PhotoText.Text = "";
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }
    }
}