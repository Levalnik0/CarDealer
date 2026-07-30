using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CarDealer
{
    public partial class AdminWindow : Window
    {
        CarDealershipEntities1 db = new CarDealershipEntities1();

        Users selectedUser;
        byte[] selectedImage;

        public AdminWindow(string fullName)
        {
            InitializeComponent();

            UserNameText.Text = $"Здравствуйте, {fullName}";

            LoadUsers();
            LoadRoles();
            LoadCars();

            DateFromPicker.SelectedDate = DateTime.Now.AddMonths(-1);
            DateToPicker.SelectedDate = DateTime.Now;

            LoadAnalytics();
            
            BodyBox.ItemsSource = new string[]
            {
                "Седан","Хэтчбек","Кроссовер","Внедорожник","Купе"
            };

            NewPhoneBox.Text = "+7";
        }

        private void AnalyticsDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAnalytics();
        }


        void LoadAnalytics()
        {
            DateTime dateFrom = DateFromPicker.SelectedDate ?? new DateTime(2000, 1, 1);
            DateTime dateTo = DateToPicker.SelectedDate ?? DateTime.Now;

            dateTo = dateTo.Date.AddDays(1);

            var requests = db.CustomerRequests
                .Where(x => x.CreatedDate >= dateFrom &&
                            x.CreatedDate < dateTo)
                .ToList();

            TotalCarsText.Text = db.Cars.Count().ToString();

            AvailableCarsText.Text = db.Cars.Count(c => c.StatusId == 1).ToString();

            RequestsText.Text = requests.Count.ToString();

            SoldCarsText.Text = requests.Count.ToString();

            decimal revenue = requests.Sum(x => x.FinalPrice ?? 0);

            RevenueText.Text = revenue.ToString("N0") + " ₽";

            // Три самые востребованные марки за период
            var topBrands = requests
                .Join(db.Cars,
                      r => r.CarId,
                      c => c.Id,
                      (r, c) => c.Manufacturer)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select((g, index) =>
                    $"{index + 1} место - {g.Key} ({g.Count()})")
                .ToList();

            TopBrandsList.ItemsSource = topBrands;
        }

        void LoadUsers()
        {
            UsersList.ItemsSource = db.Users.Where(u => u.RoleId != 1).ToList();
        }

        void LoadRoles()
        {
            RoleBox.ItemsSource = db.Roles.Where(r => r.Id != 1).ToList();
            RoleBox.DisplayMemberPath = "Name";
            RoleBox.SelectedValuePath = "Id";
        }

        void LoadCars()
        {
            CarsAdminList.ItemsSource = db.Cars.ToList();
        }

        private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = UsersList.SelectedItem as Users;

            if (selectedUser != null)
            {
                NewNameBox.Text = selectedUser.FullName;
                NewPhoneBox.Text = selectedUser.Phone;
                RoleBox.SelectedValue = selectedUser.RoleId;
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewNameBox.Text) ||
                    string.IsNullOrWhiteSpace(NewPhoneBox.Text) ||
                    RoleBox.SelectedValue == null)
                {
                    MessageBox.Show("Заполните все поля.");
                    return;
                }

                Users user = new Users
                {
                    FullName = NewNameBox.Text,
                    Phone = NewPhoneBox.Text,
                    RoleId = (int)RoleBox.SelectedValue
                };

                db.Users.Add(user);
                db.SaveChanges();

                user.Login = $"work{user.Id}";
                user.Password = $"work{user.Id}";

                db.SaveChanges();

                MessageBox.Show(
                    $"Сотрудник успешно добавлен.\n\nЛогин: {user.Login}\nПароль: {user.Password}");

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null) return;

            var res = MessageBox.Show("Уволить сотрудника?", "Подтверждение",
                MessageBoxButton.YesNo);

            if (res == MessageBoxResult.Yes)
            {
                db.Users.Remove(selectedUser);
                db.SaveChanges();
                LoadUsers();
            }
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        }

        private void Phone_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!NewPhoneBox.Text.StartsWith("+7"))
                NewPhoneBox.Text = "+7";

            if (NewPhoneBox.Text.Length > 12)
                NewPhoneBox.Text = NewPhoneBox.Text.Substring(0, 12);

            NewPhoneBox.SelectionStart = NewPhoneBox.Text.Length;
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Image|*.jpg;*.png";

            if (dialog.ShowDialog() == true)
                selectedImage = File.ReadAllBytes(dialog.FileName);
        }

        private void AddCar_Click(object sender, RoutedEventArgs e)
        {
            int count = db.Cars.Count() + 1;

            Cars car = new Cars
            {
                Manufacturer = BrandBoxCar.Text,
                Model = ModelBox.Text,
                Year = int.Parse(YearBoxCar.Text),
                Mileage = 0,
                Color = ColorBoxCar.Text,
                Price = decimal.Parse(PriceBox.Text),
                BodyType = BodyBox.Text,
                VIN = $"VIN{count:0000}",
                Photo = selectedImage,
                StatusId = 1
            };

            db.Cars.Add(car);
            db.SaveChanges();

            LoadCars();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }
    }
}