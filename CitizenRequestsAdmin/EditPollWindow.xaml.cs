using System;
using System.Data.SqlClient;
using System.Windows;

namespace CitizenRequestsAdmin
{
    public partial class EditPollWindow : Window
    {
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";
        private int? _pollId;

        public EditPollWindow(int? pollId = null)
        {
            InitializeComponent();
            _pollId = pollId;

            if (_pollId.HasValue)
            {
                LoadPollData();
            }
        }

        private void LoadPollData()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT Title, Description, EndsAt FROM Polls WHERE PollId = @PollId", connection);
                command.Parameters.AddWithValue("@PollId", _pollId);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    TitleTextBox.Text = reader["Title"].ToString();
                    DescriptionTextBox.Text = reader["Description"].ToString();
                    EndsAtDatePicker.SelectedDate = Convert.ToDateTime(reader["EndsAt"]);
                }
            }
        }

        private void SavePoll_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || string.IsNullOrWhiteSpace(DescriptionTextBox.Text) || !EndsAtDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EndsAtDatePicker.SelectedDate.Value < DateTime.Now)
            {
                MessageBox.Show("Дата окончания не может быть в прошлом!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command;

                if (_pollId.HasValue)
                {
                    command = new SqlCommand("UPDATE Polls SET Title = @Title, Description = @Description, EndsAt = @EndsAt WHERE PollId = @PollId", connection);
                    command.Parameters.AddWithValue("@PollId", _pollId);
                }
                else
                {
                    command = new SqlCommand("INSERT INTO Polls (Title, Description, CreatedAt, EndsAt, IsActive) VALUES (@Title, @Description, @CreatedAt, @EndsAt, @IsActive)", connection);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@IsActive", true);
                }

                command.Parameters.AddWithValue("@Title", TitleTextBox.Text);
                command.Parameters.AddWithValue("@Description", DescriptionTextBox.Text);
                command.Parameters.AddWithValue("@EndsAt", EndsAtDatePicker.SelectedDate.Value);

                command.ExecuteNonQuery();
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}