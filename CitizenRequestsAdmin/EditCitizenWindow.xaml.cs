using System;
using System.Data.SqlClient;
using System.Windows;

namespace CitizenRequestsAdmin
{
    public partial class EditCitizenWindow : Window
    {
        private readonly int _citizenId;
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";

        public EditCitizenWindow(int citizenId)
        {
            InitializeComponent();
            _citizenId = citizenId;
            LoadCitizen();
        }

        private void LoadCitizen()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("SELECT FullName, Email, PhoneNumber, Address FROM Citizens WHERE CitizenId = @CitizenId", connection);
                cmd.Parameters.AddWithValue("@CitizenId", _citizenId);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    FullNameBox.Text = reader["FullName"].ToString();
                    EmailBox.Text = reader["Email"].ToString();
                    PhoneNumberBox.Text = reader["PhoneNumber"].ToString();
                    AddressBox.Text = reader["Address"].ToString();
                }
            }
        }

        private void SaveCitizen_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Citizens SET FullName = @FullName, Email = @Email, PhoneNumber = @PhoneNumber, Address = @Address WHERE CitizenId = @CitizenId", connection);
                cmd.Parameters.AddWithValue("@FullName", FullNameBox.Text);
                cmd.Parameters.AddWithValue("@Email", EmailBox.Text);
                cmd.Parameters.AddWithValue("@PhoneNumber", PhoneNumberBox.Text);
                cmd.Parameters.AddWithValue("@Address", AddressBox.Text);
                cmd.Parameters.AddWithValue("@CitizenId", _citizenId);
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Данные сохранены!");
            Close();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
