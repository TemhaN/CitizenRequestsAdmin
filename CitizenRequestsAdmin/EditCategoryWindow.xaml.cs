using System;
using System.Data.SqlClient;
using System.Windows;

namespace CitizenRequestsAdmin
{
    public partial class EditCategoryWindow : Window
    {
        private readonly int? _categoryId;
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";

        public EditCategoryWindow(int? categoryId = null)
        {
            InitializeComponent();
            _categoryId = categoryId;

            if (_categoryId.HasValue)
            {
                LoadCategory();
            }
        }

        private void LoadCategory()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("SELECT CategoryName, Description FROM Categories WHERE CategoryId = @CategoryId", connection);
                cmd.Parameters.AddWithValue("@CategoryId", _categoryId.Value);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    CategoryNameBox.Text = reader["CategoryName"].ToString();
                    CategoryDescriptionBox.Text = reader["Description"].ToString();
                }
            }
        }

        private void SaveCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryNameBox.Text))
            {
                MessageBox.Show("Введите название категории!");
                return;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand cmd;
                if (_categoryId.HasValue)
                {
                    cmd = new SqlCommand("UPDATE Categories SET CategoryName = @CategoryName, Description = @Description WHERE CategoryId = @CategoryId", connection);
                    cmd.Parameters.AddWithValue("@CategoryId", _categoryId.Value);
                }
                else
                {
                    cmd = new SqlCommand("INSERT INTO Categories (CategoryName, Description) VALUES (@CategoryName, @Description)", connection);
                }
                cmd.Parameters.AddWithValue("@CategoryName", CategoryNameBox.Text);
                cmd.Parameters.AddWithValue("@Description", CategoryDescriptionBox.Text);
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
