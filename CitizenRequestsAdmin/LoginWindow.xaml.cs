using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CitizenRequestsAdmin
{
    public partial class LoginWindow : Window
    {
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";
        public int AdminId { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            int adminId = AuthenticateAdmin(email, password);
            if (adminId > 0)
            {
                AdminId = adminId;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Неверный email или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int AuthenticateAdmin(string email, string password)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT AdminId FROM Admins WHERE Email = @Email AND PasswordHash = HASHBYTES('SHA2_256', CONVERT(NVARCHAR(255), @Password))";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);

                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }
    }
}
