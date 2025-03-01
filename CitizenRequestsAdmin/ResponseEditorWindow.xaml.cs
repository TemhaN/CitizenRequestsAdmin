using System;
using System.Data.SqlClient;
using System.Windows;

namespace CitizenRequestsAdmin
{
    public partial class ResponseEditorWindow : Window
    {
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";
        private int _applicationId;
        private int? _responseId = null;
        private int _adminId = 1; // Заменить на ID текущего администратора

        public ResponseEditorWindow(int applicationId, int? responseId = null)
        {
            InitializeComponent();
            _applicationId = applicationId;
            _responseId = responseId;

            if (_responseId.HasValue)
                LoadResponse();
        }

        private void LoadResponse()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("SELECT ResponseText FROM Responses WHERE ResponseId = @ResponseId", connection);
                cmd.Parameters.AddWithValue("@ResponseId", _responseId);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    ResponseTextBox.Text = result.ToString();
                }
            }
        }

        private void SaveResponse_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResponseTextBox.Text))
            {
                MessageBox.Show("Ответ не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                if (_responseId.HasValue)
                {
                    // Обновление существующего ответа
                    SqlCommand cmd = new SqlCommand("UPDATE Responses SET ResponseText = @ResponseText, ResponseDate = GETDATE() WHERE ResponseId = @ResponseId", connection);
                    cmd.Parameters.AddWithValue("@ResponseText", ResponseTextBox.Text);
                    cmd.Parameters.AddWithValue("@ResponseId", _responseId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    // Добавление нового ответа
                    SqlCommand cmd = new SqlCommand("INSERT INTO Responses (ApplicationId, AdminId, ResponseText, ResponseDate) VALUES (@ApplicationId, @AdminId, @ResponseText, GETDATE())", connection);
                    cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
                    cmd.Parameters.AddWithValue("@AdminId", _adminId);
                    cmd.Parameters.AddWithValue("@ResponseText", ResponseTextBox.Text);
                    cmd.ExecuteNonQuery();
                }
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
