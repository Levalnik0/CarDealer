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
    public partial class ConsultantWindow : Window
    {
        CarDealershipEntities1 db = new CarDealershipEntities1();

        CustomerRequests currentRequest;
        Cars currentCar;

        public ConsultantWindow(string fullName)
        {
            InitializeComponent();
            UserNameText.Text = $"Здравствуйте, {fullName}";
            LoadRequests();
        }

        // В очередь консультанта попадают только свежие брони: заявки,
        // ушедшие в кредит или trade-in, ведут другие сотрудники, а закрытые
        // остаются в базе ради истории продаж.
        void LoadRequests()
        {
            RequestsList.ItemsSource = db.CustomerRequests
                .Where(r => r.Status == "Забронирован")
                .ToList();
        }

        private void RequestsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            currentRequest = RequestsList.SelectedItem as CustomerRequests;

            if (currentRequest == null) return;

            currentCar = db.Cars.FirstOrDefault(c => c.Id == currentRequest.CarId);

            ShowCarInfo();
        }

        void ShowCarInfo()
        {
            if (currentCar == null) return;

            CarInfo.Text =
                $"Марка: {currentCar.Manufacturer}\n" +
                $"Модель: {currentCar.Model}\n" +
                $"Цвет: {currentCar.Color}\n" +
                $"Кузов: {currentCar.BodyType}\n" +
                $"Пробег: {currentCar.Mileage}\n" +
                $"Цена: {currentCar.Price}";
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (currentRequest == null) return;

            var result = MessageBox.Show("Клиент отказался?", "Подтверждение", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                // Машина возвращается на витрину
                var car = db.Cars.FirstOrDefault(c => c.Id == currentRequest.CarId);

                car.StatusId = 1;

                currentRequest.Status = "Отказ";
                db.SaveChanges();

                ClearUI();
                LoadRequests();
            }
        }

        private void Buy_Click(object sender, RoutedEventArgs e)
        {
            if (currentRequest == null || currentCar == null) return;

            PurchaseWindow window = new PurchaseWindow();

            if (window.ShowDialog() == true)
            {
                if (window.Result == "Покупка")
                {
                    bool saved = CreateDkp(currentRequest, currentCar);
                    if (!saved) return;

                    currentCar.StatusId = 3; // продана

                    // Заявка не удаляется, а закрывается: сумма сделки нужна
                    // администратору для отчёта по выручке.
                    currentRequest.Status = "Завершена";
                    currentRequest.FinalPrice = currentCar.Price;

                    db.SaveChanges();

                    MessageBox.Show("Сделка завершена");

                    ClearUI();
                    LoadRequests();
                }

                else if (window.Result == "Кредит")
                {
                    currentRequest.Status = "Кредит";

                    db.SaveChanges();

                    MessageBox.Show("Заявка передана кредитному консультанту");

                    ClearUI();
                    LoadRequests();
                }

                else if (window.Result == "TradeIn")
                {
                    currentRequest.Status = "TradeIn";

                    db.SaveChanges();

                    MessageBox.Show("Заявка передана оценщику");

                    ClearUI();
                    LoadRequests();
                }
            }
        }

        // Договор купли-продажи в PDF. Возвращает false, если пользователь
        // закрыл диалог сохранения — тогда сделку не проводим.
        bool CreateDkp(CustomerRequests request, Cars car)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF (*.pdf)|*.pdf";
            dialog.FileName = $"ДКП_{car.Manufacturer}_{car.Model}";

            if (dialog.ShowDialog() != true)
                return false;

            string path = dialog.FileName;

            PdfWriter writer = new PdfWriter(path);
            PdfDocument pdf = new PdfDocument(writer);
            Document doc = new Document(pdf);

            // Встроенные шрифты iText не знают кириллицу, поэтому берём Arial
            string fontPath = @"C:\Windows\Fonts\arial.ttf";
            PdfFont font = PdfFontFactory.CreateFont(fontPath, "Identity-H");
            doc.SetFont(font);

            doc.Add(new Paragraph("ДОГОВОР КУПЛИ-ПРОДАЖИ").SetFontSize(16));
            doc.Add(new Paragraph($"Дата: {DateTime.Now:dd.MM.yyyy}"));

            doc.Add(new Paragraph("\nПокупатель:"));
            doc.Add(new Paragraph(request.FullName));
            doc.Add(new Paragraph($"Телефон: {request.Phone}"));

            doc.Add(new Paragraph("\nАвтомобиль:"));
            doc.Add(new Paragraph($"{car.Manufacturer} {car.Model}"));
            doc.Add(new Paragraph($"VIN: {car.VIN}"));
            doc.Add(new Paragraph($"Цена: {car.Price}"));

            doc.Add(new Paragraph("\nПодпись покупателя: __________"));

            doc.Close();

            MessageBox.Show($"ДКП сохранён:\n{path}");

            return true;
        }

        // Подбор альтернативы, если выбранная машина клиенту не подошла:
        // критерии сравнения задаются галочками.
        private void FindSimilar_Click(object sender, RoutedEventArgs e)
        {
            if (currentCar == null) return;

            bool brand = CheckBrand.IsChecked == true;
            bool model = CheckModel.IsChecked == true;
            bool color = CheckColor.IsChecked == true;
            bool body = CheckBody.IsChecked == true;
            bool priceCheck = CheckPrice.IsChecked == true;

            CarInfo.Text = "";
            SimilarCarsList.ItemsSource = null;

            var query = db.Cars.Where(c => c.StatusId == 1 && c.Id != currentCar.Id);

            if (brand)
                query = query.Where(c => c.Manufacturer == currentCar.Manufacturer);

            if (model)
                query = query.Where(c => c.Model == currentCar.Model);

            if (color)
                query = query.Where(c => c.Color == currentCar.Color);

            if (body)
                query = query.Where(c => c.BodyType == currentCar.BodyType);

            if (priceCheck && currentCar.Price != null)
            {
                decimal price = currentCar.Price.Value;

                query = query.Where(c => c.Price >= price * 0.8m &&
                                         c.Price <= price * 1.2m);
            }

            var result = query.ToList();

            SimilarCarsList.ItemsSource = result;

            MessageBox.Show(result.Count > 0
                ? $"Найдено: {result.Count}"
                : "Похожих машин не найдено");
        }

        private void SimilarCarsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var car = SimilarCarsList.SelectedItem as Cars;

            if (car == null) return;

            currentCar = car;
            ShowCarInfo();
        }

        void ClearUI()
        {
            currentRequest = null;
            currentCar = null;

            RequestsList.SelectedItem = null;

            CarInfo.Text = "";
            SimilarCarsList.ItemsSource = null;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }
    }
}