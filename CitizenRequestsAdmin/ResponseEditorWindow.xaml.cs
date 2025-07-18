using System;
using System.Data.SqlClient;
using System.Windows;

namespace CitizenRequestsAdmin
{
    public partial class ResponseEditorWindow : Window
    {
        private readonly int _applicationId;
        private readonly int _adminId;
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";

        public ResponseEditorWindow(int applicationId, int adminId)
        {
            InitializeComponent();
            _applicationId = applicationId;
            _adminId = adminId;
            LoadResponse();
        }

        private void LoadResponse()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT ResponseText FROM Responses WHERE ApplicationId = @ApplicationId AND IsFromCitizen = 0", connection);
                cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ResponseTextBox.Text = reader["ResponseText"].ToString();
                }
                reader.Close();
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void SaveResponse_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(
                    @"IF EXISTS (SELECT 1 FROM Responses WHERE ApplicationId = @ApplicationId AND IsFromCitizen = 0)
                        UPDATE Responses SET ResponseText = @ResponseText, SentAt = @SentAt, SenderId = @SenderId
                        WHERE ApplicationId = @ApplicationId AND IsFromCitizen = 0
                      ELSE
                        INSERT INTO Responses (ApplicationId, ResponseText, SentAt, IsFromCitizen, SenderId)
                        VALUES (@ApplicationId, @ResponseText, @SentAt, 0, @SenderId)", connection);
                cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
                cmd.Parameters.AddWithValue("@ResponseText", ResponseTextBox.Text);
                cmd.Parameters.AddWithValue("@SentAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@SenderId", _adminId);
                cmd.ExecuteNonQuery();
            }
            Close();
        }
    }
}