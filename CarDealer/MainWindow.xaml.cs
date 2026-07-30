using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CarDealer
{
    public partial class MainWindow : Window
    {
        CarDealershipEntities1 db = new CarDealershipEntities1();

        public MainWindow()
        {
            InitializeComponent();

            LoadData();
            StartClock();
        }

        void LoadData()
        {
            // На витрину попадают только машины со статусом "Доступен"
            var cars = db.Cars.Where(c => c.StatusId == 1).ToList();

            CarsList.ItemsSource = cars;

            BrandBox.ItemsSource = cars
                .Select(c => c.Manufacturer)
                .Distinct()
                .ToList();

            ColorBox.ItemsSource = cars
                .Select(c => c.Color)
                .Distinct()
                .ToList();
        }

        void ApplyFilter()
        {
            var query = db.Cars.Where(c => c.StatusId == 1).AsQueryable();

            if (decimal.TryParse(PriceFromBox.Text, out decimal priceFrom))
                query = query.Where(c => c.Price >= priceFrom);

            if (decimal.TryParse(PriceToBox.Text, out decimal priceTo))
                query = query.Where(c => c.Price <= priceTo);

            if (int.TryParse(YearBox.Text, out int year))
                query = query.Where(c => c.Year == year);

            if (BrandBox.SelectedItem != null)
            {
                string brand = BrandBox.SelectedItem.ToString();
                query = query.Where(c => c.Manufacturer == brand);
            }

            if (ColorBox.SelectedItem != null)
            {
                string color = ColorBox.SelectedItem.ToString();
                query = query.Where(c => c.Color == color);
            }

            CarsList.ItemsSource = query.ToList();
        }

        void FilterChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            PriceFromBox.Text = "";
            PriceToBox.Text = "";
            YearBox.Text = "";

            BrandBox.SelectedIndex = -1;
            ColorBox.SelectedIndex = -1;

            LoadData();
        }

        void StartClock()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                TimeText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            };
            timer.Start();
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var car = button.DataContext as Cars;

            if (car == null) return;

            if (car.StatusId != 1)
            {
                MessageBox.Show("Авто уже недоступно");
                return;
            }

            BookingWindow window = new BookingWindow();

            if (window.ShowDialog() == true)
            {
                // Заявка от клиента — с ней дальше работает консультант
                CustomerRequests request = new CustomerRequests
                {
                    FullName = window.ClientName,
                    Phone = window.Phone,
                    CarId = car.Id,
                    Status = "Забронирован",
                    CreatedDate = DateTime.Now
                };

                db.CustomerRequests.Add(request);

                // Пока бронь активна, машина исчезает с витрины
                car.StatusId = 2;

                db.SaveChanges();

                MessageBox.Show(
                    $"Ваша машина {car.Manufacturer} {car.Model} забронирована на 24 часа"
                );

                LoadData();
            }
        }

        private void OpenLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow window = new LoginWindow();
            this.Close();
            window.ShowDialog();
           
        }
    }
}