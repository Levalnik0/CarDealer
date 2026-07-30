using System.Linq;
using System.Windows;

namespace CarDealer
{
    public partial class LoginWindow : Window
    {
        CarDealershipEntities1 db = new CarDealershipEntities1();

        public LoginWindow()
        {
            InitializeComponent();
        }

        // Пароль вводится в PasswordBox, а по галочке подменяется
        // обычным TextBox, чтобы его можно было прочитать.
        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = new MainWindow();
            this.Close();
            main.Show();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text;
            string password = PasswordBox.Visibility == Visibility.Visible
                ? PasswordBox.Password
                : PasswordTextBox.Text;

            var user = db.Users.FirstOrDefault(u => u.Login == login && u.Password == password);

            if (user == null)
            {
                MessageBox.Show("Неверный логин или пароль");
                return;
            }

            // Роль определяет, какое рабочее окно откроется после входа
            switch (user.RoleId)
            {
                case 1:
                    AdminWindow admin = new AdminWindow(user.FullName);
                    admin.Show();
                    this.Close();
                    break;

                case 2:
                    ConsultantWindow consultant = new ConsultantWindow(user.FullName);
                    consultant.Show();
                    this.Close();
                    break;

                case 3:
                    CreditWindow credit = new CreditWindow(user.FullName);
                    credit.Show();
                    this.Close();
                    break;

                case 4:
                    TradeInWindow trade = new TradeInWindow(user.FullName);
                    trade.Show();
                    this.Close();
                    break;

                default:
                    MessageBox.Show("Неизвестная роль");
                    break;
            }
        }
    }
}