using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CitizenRequestsAdmin.Models;



namespace CitizenRequestsAdmin
{
    public partial class ChatWindow : Window
    {
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";
        private System.Windows.Threading.DispatcherTimer _chatRefreshTimer;
        private int _applicationId;
        private int _adminId;

        public ChatWindow(int applicationId, int adminId)
        {
            InitializeComponent();
            _applicationId = applicationId;
            _adminId = adminId; // Устанавливаем ID админа

            // Настраиваем таймер на обновление чата каждые 5 секунд
            _chatRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            _chatRefreshTimer.Interval = TimeSpan.FromSeconds(5);
            _chatRefreshTimer.Tick += (s, e) => LoadChat();
            _chatRefreshTimer.Start();

            LoadApplicationDetails();
            LoadChat();
            PrepareAutoGreeting();
        }


        private void PrepareAutoGreeting()
        {
            // Заполняем поле ввода, но не отправляем
            ResponseTextBox.Text = "Здравствуйте, чем могу помочь?";
        }


//private void ChatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//{
//    if (ChatListBox.SelectedItem is ChatMessage selectedMessage)
//    {
//        _selectedMessage = selectedMessage;

//        // Пройтись по всем элементам и сбросить выделение
//        foreach (var item in ChatListBox.Items)
//        {
//            if (ChatListBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem listBoxItem &&
//                listBoxItem.ContentTemplate.FindName("MessageBorder", listBoxItem) is Border border)
//            {
//                border.BorderBrush = Brushes.Transparent;
//            }
//        }

//        // Подсветить выбранный элемент
//        if (ChatListBox.ItemContainerGenerator.ContainerFromItem(_selectedMessage) is ListBoxItem selectedItem &&
//            selectedItem.ContentTemplate.FindName("MessageBorder", selectedItem) is Border selectedBorder)
//        {
//            selectedBorder.BorderBrush = Brushes.Red;
//        }
//    }
//}



        private void LoadApplicationDetails()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT Description, Status FROM Applications WHERE ApplicationId = @ApplicationId";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ApplicationTextBlock.Text = reader["Description"].ToString();
                            string status = reader["Status"].ToString();
                            StatusComboBox.SelectedItem = FindStatusItem(status);
                        }
                    }
                }
            }
        }

        private object FindStatusItem(string status)
        {
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content.ToString() == status)
                    return item;
            }
            return null;
        }

        // Обновленный метод LoadChat
        private void LoadChat()
        {
            ChatListBox.Items.Clear();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT r.ResponseId, r.IsFromCitizen, " +
                               "CASE WHEN r.IsFromCitizen = 1 THEN c.FullName ELSE ad.FullName END AS SenderName, " +
                               "r.ResponseText, r.ResponseDate " +
                               "FROM Responses r " +
                               "LEFT JOIN Admins ad ON r.AdminId = ad.AdminId " +
                               "LEFT JOIN Citizens c ON r.ApplicationId = c.CitizenId " +
                               "WHERE r.ApplicationId = @ApplicationId ORDER BY r.ResponseDate";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ChatListBox.Items.Add(new Models.ChatMessage
                            {
                                ResponseId = Convert.ToInt32(reader["ResponseId"]),
                                IsFromCitizen = Convert.ToBoolean(reader["IsFromCitizen"]),
                                SenderName = reader["SenderName"].ToString(),
                                MessageText = reader["ResponseText"].ToString(),
                                Timestamp = Convert.ToDateTime(reader["ResponseDate"])
                            });
                        }
                    }
                }
            }
            ScrollToBottom();
        }


        private void ScrollToBottom()
        {
            if (ChatListBox.Items.Count > 0)
            {
                ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
            }
        }


        //private void SendAutoGreeting()
        //{
        //    using (SqlConnection connection = new SqlConnection(_connectionString))
        //    {
        //        connection.Open();
        //        string query = "INSERT INTO Responses (ApplicationId, AdminId, ResponseText, ResponseDate, IsFromCitizen) " +
        //                       "VALUES (@ApplicationId, @AdminId, @ResponseText, GETDATE(), 0)";
        //        using (SqlCommand cmd = new SqlCommand(query, connection))
        //        {
        //            cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
        //            cmd.Parameters.AddWithValue("@AdminId", _adminId);
        //            cmd.Parameters.AddWithValue("@ResponseText", "Здравствуйте, чем могу помочь?");
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //    LoadChat();
        //}

        private void SendResponse_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResponseTextBox.Text))
            {
                MessageBox.Show("Нельзя отправить пустое сообщение!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Responses (ApplicationId, AdminId, ResponseText, ResponseDate, IsFromCitizen) " +
                               "VALUES (@ApplicationId, @AdminId, @ResponseText, GETDATE(), 0)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
                    cmd.Parameters.AddWithValue("@AdminId", _adminId);
                    cmd.Parameters.AddWithValue("@ResponseText", ResponseTextBox.Text);
                    cmd.ExecuteNonQuery();
                }
            }

            ResponseTextBox.Clear();
            LoadChat();
        }

        private void DeleteResponse_Click(object sender, RoutedEventArgs e)
        {
            if (ChatListBox.SelectedItem is ChatMessage selectedMessage)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM Responses WHERE ResponseId = @ResponseId";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ResponseId", selectedMessage.ResponseId);
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadChat();
            }
            else
            {
                MessageBox.Show("Выберите сообщение для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveStatus_Click(object sender, RoutedEventArgs e)
        {
            string newStatus = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(newStatus)) return;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "UPDATE Applications SET Status = @Status WHERE ApplicationId = @ApplicationId";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@ApplicationId", _applicationId);
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Статус обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }


    }
}