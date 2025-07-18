using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CitizenRequestsAdmin
{
    public partial class MainWindow : Window
    {
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";
        private int selectedApplicationId = -1;
        private int selectedCitizenId = -1;
        private int selectedCategoryId = -1;
        private int selectedPollId = -1;
        private int _adminId;
        private int _currentApplicationPage = 1;
        private int _currentCitizenPage = 1;
        private int _currentPollPage = 1;
        private const int _pageSize = 50;
        private int _totalApplications = 0;
        private int _totalCitizens = 0;
        private int _totalPolls = 0;
        private List<(int CategoryId, string CategoryName)> _cachedCategories = null;

        public MainWindow()
        {
            InitializeComponent();
            ShowLoginWindow();
            Loaded += async (s, e) => await LoadDataAsync();
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
        }

        private async void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                switch (tabControl.SelectedIndex)
                {
                    case 0: await LoadApplicationsAsync(); break;
                    case 1: await LoadCitizensAsync(); break;
                    case 2: await LoadCategoriesAsync(); break;
                    case 3: await LoadPollsAsync(); break;
                    case 4: await LoadStatisticsAsync(); break;
                }
            }
        }

        private void ShowLoginWindow()
        {
            LoginWindow loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                _adminId = loginWindow.AdminId;
            }
            else
            {
                Close();
            }
        }

        private async Task LoadDataAsync()
        {
            switch (MainTabControl.SelectedIndex)
            {
                case 0: await LoadApplicationsAsync(); break;
                case 1: await LoadCitizensAsync(); break;
                case 2: await LoadCategoriesAsync(); break;
                case 3: await LoadPollsAsync(); break;
                case 4: await LoadStatisticsAsync(); break;
            }
        }


        private async Task LoadApplicationsAsync()
        {
            if (ApplicationsList == null) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Debug.WriteLine($"Connected to: {connection.DataSource}, Database: {connection.Database}");

                    // Проверка количества записей в таблицах
                    string debugQuery = @"
                SELECT 
                    (SELECT COUNT(*) FROM Applications) AS TotalApplications,
                    (SELECT COUNT(*) FROM Citizens) AS TotalCitizens,
                    (SELECT COUNT(*) FROM Categories) AS TotalCategories,
                    (SELECT COUNT(*) FROM Responses WHERE IsFromCitizen = 0) AS TotalResponses,
                    (SELECT COUNT(*) FROM Applications a WHERE NOT EXISTS (
                        SELECT 1 FROM Responses r WHERE r.ApplicationId = a.ApplicationId AND r.IsFromCitizen = 0)) AS UnansweredApplications";
                    using (SqlCommand debugCommand = new SqlCommand(debugQuery, connection))
                    {
                        using (SqlDataReader reader = await debugCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Debug.WriteLine($"Debug: TotalApplications={reader.GetInt32(0)}, TotalCitizens={reader.GetInt32(1)}, TotalCategories={reader.GetInt32(2)}, TotalResponses={reader.GetInt32(3)}, UnansweredApplications={reader.GetInt32(4)}");
                            }
                            await reader.CloseAsync();
                        }
                    }

                    string searchText = SearchBox?.Text?.Trim() ?? "";
                    string selectedStatus = ((StatusFilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "").Trim();
                    string selectedCategory = (CategoryFilterBox.SelectedItem?.ToString() ?? "").Trim();

                    if (selectedStatus == "Все") selectedStatus = "";
                    if (selectedCategory == "Все") selectedCategory = "";

                    bool showUnansweredOnly = ShowUnansweredOnly?.IsChecked == true;

                    Debug.WriteLine($"Filters: SearchText={searchText}, Status={selectedStatus}, Category={selectedCategory}, ShowUnansweredOnly={showUnansweredOnly}");

                    string dataQuery = @"
                SELECT 
                a.ApplicationId, 
                ISNULL(c.FullName, 'Неизвестный гражданин') AS FullName, 
                ISNULL(cat.CategoryName, 'Без категории') AS CategoryName, 
                a.Status, 
                a.SubmissionDate, 
                a.Description,
                CASE 
                    WHEN EXISTS (SELECT 1 FROM Responses r WHERE r.ApplicationId = a.ApplicationId AND r.IsFromCitizen = 0) 
                    THEN 1 
                    ELSE 0 
                END AS IsAnswered
            FROM Applications a
            LEFT JOIN Citizens c ON a.CitizenId = c.CitizenId
            LEFT JOIN Categories cat ON a.CategoryId = cat.CategoryId
            WHERE 1=1
                AND (@SearchText = '' OR c.FullName LIKE '%' + @SearchText + '%' OR a.Description LIKE '%' + @SearchText + '%')
                AND (@Status = '' OR a.Status = @Status)
                AND (@Category = '' OR cat.CategoryName = @Category)
                AND (@ShowUnansweredOnly = 0 OR NOT EXISTS (
                    SELECT 1 FROM Responses r 
                    WHERE r.ApplicationId = a.ApplicationId AND r.IsFromCitizen = 0
                ))
            ORDER BY a.SubmissionDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    string countQuery = @"
                SELECT COUNT(*)
                FROM Applications a
                LEFT JOIN Citizens c ON a.CitizenId = c.CitizenId
                LEFT JOIN Categories cat ON a.CategoryId = cat.CategoryId
                WHERE 1=1
                    AND (@SearchText = '' OR c.FullName LIKE '%' + @SearchText + '%' OR a.Description LIKE '%' + @SearchText + '%')
                    AND (@Status = '' OR a.Status = @Status)
                    AND (@Category = '' OR cat.CategoryName = @Category)
                    AND (@ShowUnansweredOnly = 0 OR NOT EXISTS (
                        SELECT 1 FROM Responses r 
                        WHERE r.ApplicationId = a.ApplicationId AND r.IsFromCitizen = 0
                    ))";

                    Debug.WriteLine($"Count Query:\n{countQuery}\nSearchText='{searchText}', Status='{selectedStatus}', Category='{selectedCategory}', ShowUnansweredOnly={showUnansweredOnly}");

                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        countCommand.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = searchText;
                        countCommand.Parameters.Add("@Status", SqlDbType.NVarChar).Value = selectedStatus;
                        countCommand.Parameters.Add("@Category", SqlDbType.NVarChar).Value = selectedCategory;
                        countCommand.Parameters.Add("@ShowUnansweredOnly", SqlDbType.Bit).Value = showUnansweredOnly;
                        _totalApplications = (int)await countCommand.ExecuteScalarAsync();
                        Debug.WriteLine($"Total Applications: {_totalApplications}");
                    }

                    using (SqlCommand dataCommand = new SqlCommand(dataQuery, connection))
                    {
                        dataCommand.Parameters.Add("@Offset", SqlDbType.Int).Value = (_currentApplicationPage - 1) * _pageSize;
                        dataCommand.Parameters.Add("@PageSize", SqlDbType.Int).Value = _pageSize;
                        dataCommand.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = searchText;
                        dataCommand.Parameters.Add("@Status", SqlDbType.NVarChar).Value = selectedStatus;
                        dataCommand.Parameters.Add("@Category", SqlDbType.NVarChar).Value = selectedCategory;
                        dataCommand.Parameters.Add("@ShowUnansweredOnly", SqlDbType.Bit).Value = showUnansweredOnly;

                        using (SqlDataReader reader = await dataCommand.ExecuteReaderAsync())
                        {
                            DataTable dt = new DataTable();
                            dt.Columns.Add("ApplicationId", typeof(int));
                            dt.Columns.Add("FullName", typeof(string));
                            dt.Columns.Add("CategoryName", typeof(string));
                            dt.Columns.Add("Status", typeof(string));
                            dt.Columns.Add("SubmissionDate", typeof(DateTime));
                            dt.Columns.Add("Description", typeof(string));
                            dt.Columns.Add("IsAnswered", typeof(bool));

                            int rowCount = 0;
                            while (await reader.ReadAsync())
                            {
                                DataRow row = dt.NewRow();
                                row["ApplicationId"] = reader.GetInt32(0);
                                row["FullName"] = reader.GetString(1);
                                row["CategoryName"] = reader.GetString(2);
                                row["Status"] = reader.GetString(3);
                                row["SubmissionDate"] = reader.GetDateTime(4);
                                row["Description"] = reader.GetString(5);
                                row["IsAnswered"] = reader.GetInt32(6) == 1;
                                dt.Rows.Add(row);
                                rowCount++;
                            }
                            Debug.WriteLine($"Loaded {rowCount} rows for ApplicationsList");

                            int totalPages = (int)Math.Ceiling((double)_totalApplications / _pageSize);
                            PageInfoText.Text = $"Страница {_currentApplicationPage} из {totalPages}";
                            ApplicationsList.ItemsSource = dt.DefaultView;

                            //if (rowCount == 0)
                            //{
                            //    MessageBox.Show("Нет обращений, соответствующих фильтрам. Проверьте данные в таблицах Applications, Citizens, Categories, Responses.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            //}
                        }
                    }

                    await LoadCategoriesForFilterAsync(connection);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadApplicationsAsync Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка загрузки обращений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                Debug.WriteLine($"LoadApplicationsAsync completed in {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        private async void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentApplicationPage > 1)
            {
                _currentApplicationPage--;
                await LoadApplicationsAsync();
            }
        }

        private async void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_totalApplications / _pageSize);
            if (_currentApplicationPage < totalPages)
            {
                _currentApplicationPage++;
                await LoadApplicationsAsync();
            }
        }
        private async Task LoadCategoriesForFilterAsync(SqlConnection connection)
        {
            if (_cachedCategories != null && _cachedCategories.Any())
            {
                CategoryFilterBox.ItemsSource = new[] { "Все" }.Concat(_cachedCategories.Select(c => c.CategoryName));
                CategoryFilterBox.SelectedIndex = 0;
                return;
            }

            string query = "SELECT CategoryId, CategoryName FROM Categories ORDER BY CategoryName";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    _cachedCategories = new List<(int CategoryId, string CategoryName)>();
                    while (await reader.ReadAsync())
                    {
                        _cachedCategories.Add((reader.GetInt32(0), reader.GetString(1)));
                    }
                }
            }

            CategoryFilterBox.ItemsSource = new[] { "Все" }.Concat(_cachedCategories.Select(c => c.CategoryName));
            CategoryFilterBox.SelectedIndex = 0;
        }

        private async void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentApplicationPage = 1;
            await LoadApplicationsAsync();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentApplicationPage = 1;
            await LoadApplicationsAsync();
        }

        private async void UnansweredFilter_Checked(object sender, RoutedEventArgs e)
        {
            _currentApplicationPage = 1;
            await LoadApplicationsAsync();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StatusFilterBox.SelectedIndex = 0;
            CategoryFilterBox.SelectedIndex = 0;
            ShowUnansweredOnly.IsChecked = true;
            _currentApplicationPage = 1;
            _ = LoadApplicationsAsync();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                if (e.Delta > 0)
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 30);
                else
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 30);
                e.Handled = true;
            }
        }

        private async Task LoadCitizensAsync()
        {
            if (CitizensGrid == null) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string searchText = CitizenSearchBox?.Text?.Trim() ?? "";
                    DateTime? regDate = RegistrationDateFilter?.SelectedDate;

                    // Запрос для выборки данных
                    string dataQuery = @"
                SELECT 
                    CitizenId, 
                    FullName, 
                    Email,
                    PhoneNumber,
                    Address,
                    CreatedAt
                FROM Citizens
                WHERE 1=1
                    AND (@SearchText = '' OR FullName LIKE @SearchText)
                    AND (@RegDate IS NULL OR CreatedAt >= @RegDate)
                ORDER BY CitizenId
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    // Запрос для подсчета общего количества записей
                    string countQuery = @"
                SELECT COUNT(*)
                FROM Citizens
                WHERE 1=1
                    AND (@SearchText = '' OR FullName LIKE @SearchText)
                    AND (@RegDate IS NULL OR CreatedAt >= @RegDate)";

                    // Подсчет общего количества записей
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        countCommand.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");
                        countCommand.Parameters.AddWithValue("@RegDate", regDate.HasValue ? (object)regDate.Value : DBNull.Value);
                        _totalCitizens = (int)await countCommand.ExecuteScalarAsync();
                    }

                    // Выборка данных
                    using (SqlCommand dataCommand = new SqlCommand(dataQuery, connection))
                    {
                        dataCommand.Parameters.AddWithValue("@Offset", (_currentCitizenPage - 1) * _pageSize);
                        dataCommand.Parameters.AddWithValue("@PageSize", _pageSize);
                        dataCommand.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");
                        dataCommand.Parameters.AddWithValue("@RegDate", regDate.HasValue ? (object)regDate.Value : DBNull.Value);

                        using (SqlDataReader reader = await dataCommand.ExecuteReaderAsync())
                        {
                            DataTable dt = new DataTable();
                            dt.Columns.Add("CitizenId", typeof(int));
                            dt.Columns.Add("FullName", typeof(string));
                            dt.Columns.Add("Email", typeof(string));
                            dt.Columns.Add("PhoneNumber", typeof(string));
                            dt.Columns.Add("Address", typeof(string));
                            dt.Columns.Add("CreatedAt", typeof(DateTime));

                            while (await reader.ReadAsync())
                            {
                                DataRow row = dt.NewRow();
                                row["CitizenId"] = reader.GetInt32(0);
                                row["FullName"] = reader.GetString(1);
                                row["Email"] = reader.GetString(2);
                                row["PhoneNumber"] = reader.GetString(3);
                                row["Address"] = reader.GetString(4);
                                row["CreatedAt"] = reader.GetDateTime(5);
                                dt.Rows.Add(row);
                            }

                            int totalPages = (int)Math.Ceiling((double)_totalCitizens / _pageSize);
                            CitizenPageInfoText.Text = $"Страница {_currentCitizenPage} из {totalPages}";
                            CitizensGrid.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                Debug.WriteLine($"LoadCitizensAsync completed in {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        private async void PreviousCitizenPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCitizenPage > 1)
            {
                _currentCitizenPage--;
                await LoadCitizensAsync();
            }
        }

        private async void NextCitizenPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_totalCitizens / _pageSize);
            if (_currentCitizenPage < totalPages)
            {
                _currentCitizenPage++;
                await LoadCitizensAsync();
            }
        }

        private async void CitizenSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentCitizenPage = 1;
            await LoadCitizensAsync();
        }

        private async void FilterCitizens(object sender, SelectionChangedEventArgs e)
        {
            _currentCitizenPage = 1;
            await LoadCitizensAsync();
        }

        private async void ClearCitizenFilter_Click(object sender, RoutedEventArgs e)
        {
            if (CitizenSearchBox != null) CitizenSearchBox.Text = "";
            if (RegistrationDateFilter != null) RegistrationDateFilter.SelectedDate = null;
            _currentCitizenPage = 1;
            await LoadCitizensAsync();
        }

        private void CitizensGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CitizensGrid.SelectedItem is DataRowView row)
            {
                selectedCitizenId = Convert.ToInt32(row["CitizenId"]);
            }
            else
            {
                selectedCitizenId = -1;
            }
        }

        private async void DeleteCitizen_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCitizenId == -1) return;

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить гражданина?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Citizens WHERE CitizenId = @CitizenId", connection))
                    {
                        cmd.Parameters.AddWithValue("@CitizenId", selectedCitizenId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _currentCitizenPage = 1;
                await LoadCitizensAsync();
            }
        }

        private async void EditCitizen_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCitizenId == -1) return;

            EditCitizenWindow editWindow = new EditCitizenWindow(selectedCitizenId);
            editWindow.ShowDialog();
            await LoadCitizensAsync();
        }

        private async Task LoadCategoriesAsync(string filter = "")
        {
            if (CategoriesGrid == null) return;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT CategoryId, CategoryName, Description FROM Categories WHERE CategoryName LIKE @Filter ORDER BY CategoryName";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Filter", "%" + filter + "%");
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        var categories = new List<Category>();
                        while (await reader.ReadAsync())
                        {
                            categories.Add(new Category
                            {
                                CategoryId = reader.GetInt32(0),
                                CategoryName = reader.GetString(1),
                                Description = reader.GetString(2)
                            });
                        }
                        CategoriesGrid.ItemsSource = categories;
                    }
                }
            }

            foreach (var column in CategoriesGrid.Columns)
            {
                if (column.Header.ToString() == "CategoryName" || column.Header.ToString() == "Description")
                {
                    column.IsReadOnly = false;
                }
            }
        }

        private async void CategorySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadCategoriesAsync(CategorySearchBox.Text);
        }

        private async void ClearCategoryFilter_Click(object sender, RoutedEventArgs e)
        {
            CategorySearchBox.Text = "";
            await LoadCategoriesAsync();
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new EditCategoryWindow();
            addWindow.ShowDialog();
            _cachedCategories = null;
            await LoadCategoriesAsync();
        }

        private async void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCategoryId == -1)
            {
                MessageBox.Show("Выберите категорию для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new EditCategoryWindow(selectedCategoryId);
            editWindow.ShowDialog();
            _cachedCategories = null;
            await LoadCategoriesAsync();
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is Category selectedCategory)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить категорию?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Categories WHERE CategoryId = @CategoryId", connection))
                        {
                            cmd.Parameters.AddWithValue("@CategoryId", selectedCategory.CategoryId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    _cachedCategories = null;
                    await LoadCategoriesAsync();
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для удаления!");
            }
        }

        private void CategoriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is Category selectedCategory)
            {
                selectedCategoryId = selectedCategory.CategoryId;
            }
            else
            {
                selectedCategoryId = -1;
            }
        }

        private void ApplicationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ApplicationsList.SelectedItem is DataRowView row)
            {
                if (row["ApplicationId"] != DBNull.Value)
                {
                    selectedApplicationId = Convert.ToInt32(row["ApplicationId"]);
                }
                else
                {
                    selectedApplicationId = -1;
                }
            }
        }

        private void ApplicationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenChat_Click(sender, e);
        }

        private async void DeleteApplication_Click(object sender, RoutedEventArgs e)
        {
            if (selectedApplicationId == -1) return;

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить обращение?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Applications WHERE ApplicationId = @ApplicationId", connection))
                    {
                        cmd.Parameters.AddWithValue("@ApplicationId", selectedApplicationId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _currentApplicationPage = 1;
                await LoadApplicationsAsync();
            }
        }

        private void OpenChat_Click(object sender, RoutedEventArgs e)
        {
            if (selectedApplicationId == -1) return;

            ChatWindow chatWindow = new ChatWindow(selectedApplicationId, _adminId);
            chatWindow.ShowDialog();
        }

        private void EditResponse_Click(object sender, RoutedEventArgs e)
        {
            if (selectedApplicationId == -1) return;

            ResponseEditorWindow responseEditor = new ResponseEditorWindow(selectedApplicationId, _adminId);
            responseEditor.ShowDialog();
        }

        private async Task LoadPollsAsync(string filter = "")
        {
            if (PollsGrid == null) return;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string statusFilter = "";
                string selectedStatus = (PollStatusFilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все";
                if (selectedStatus == "Активные")
                    statusFilter = " AND IsActive = 1";
                else if (selectedStatus == "Завершённые")
                    statusFilter = " AND IsActive = 0";

                string query = @"
                    WITH PollData AS (
                        SELECT PollId, Title, Description, CreatedAt, EndsAt, IsActive,
                               COUNT(*) OVER () AS TotalCount
                        FROM Polls
                        WHERE Title LIKE @Filter" + statusFilter + @"
                        ORDER BY PollId
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                    )
                    SELECT PollId, Title, Description, CreatedAt, EndsAt, IsActive, TotalCount
                    FROM PollData";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Filter", "%" + filter + "%");
                    command.Parameters.AddWithValue("@Offset", (_currentPollPage - 1) * _pageSize);
                    command.Parameters.AddWithValue("@PageSize", _pageSize);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            _totalPolls = Convert.ToInt32(dt.Rows[0]["TotalCount"]);
                            int totalPages = (int)Math.Ceiling((double)_totalPolls / _pageSize);
                            PollPageInfoText.Text = $"Страница {_currentPollPage} из {totalPages}";
                        }
                        else
                        {
                            _totalPolls = 0;
                            PollPageInfoText.Text = $"Страница {_currentPollPage} из 1";
                        }

                        PollsGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
        }

        private async void PreviousPollPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPollPage > 1)
            {
                _currentPollPage--;
                await LoadPollsAsync(PollSearchBox.Text);
            }
        }

        private async void NextPollPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_totalPolls / _pageSize);
            if (_currentPollPage < totalPages)
            {
                _currentPollPage++;
                await LoadPollsAsync(PollSearchBox.Text);
            }
        }

        private async void PollSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPollPage = 1;
            await LoadPollsAsync(PollSearchBox.Text);
        }

        private async void PollFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPollPage = 1;
            await LoadPollsAsync(PollSearchBox.Text);
        }

        private async void ClearPollFilter_Click(object sender, RoutedEventArgs e)
        {
            PollSearchBox.Text = "";
            PollStatusFilterBox.SelectedIndex = 0;
            _currentPollPage = 1;
            await LoadPollsAsync();
        }

        private void PollsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PollsGrid.SelectedItem is DataRowView row)
            {
                selectedPollId = Convert.ToInt32(row["PollId"]);
            }
            else
            {
                selectedPollId = -1;
            }
        }

        private async void AddPoll_Click(object sender, RoutedEventArgs e)
        {
            EditPollWindow addWindow = new EditPollWindow();
            if (addWindow.ShowDialog() == true)
            {
                _currentPollPage = 1;
                await LoadPollsAsync();
            }
        }

        private async void EditPoll_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPollId == -1)
            {
                MessageBox.Show("Выберите опрос для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditPollWindow editWindow = new EditPollWindow(selectedPollId);
            if (editWindow.ShowDialog() == true)
            {
                await LoadPollsAsync();
            }
        }

        private async void DeletePoll_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPollId == -1)
            {
                MessageBox.Show("Выберите опрос для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить опрос?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM PollVotes WHERE PollId = @PollId; DELETE FROM Polls WHERE PollId = @PollId", connection))
                    {
                        cmd.Parameters.AddWithValue("@PollId", selectedPollId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _currentPollPage = 1;
                await LoadPollsAsync();
            }
        }

        private async Task LoadStatisticsAsync()
        {
            if (PollStatsComboBox == null || ApplicationStatusChart == null || CitizenRegistrationChart == null) return;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM Applications) AS TotalApplications,
                        (SELECT COUNT(*) FROM Citizens) AS TotalCitizens,
                        (SELECT COUNT(*) FROM Categories) AS TotalCategories;
                    SELECT PollId, Title FROM Polls;
                    SELECT Status, COUNT(*) AS Count FROM Applications GROUP BY Status;
                    SELECT FORMAT(CreatedAt, 'yyyy-MM') AS Month, COUNT(*) AS Count 
                    FROM Citizens 
                    GROUP BY FORMAT(CreatedAt, 'yyyy-MM') 
                    ORDER BY Month";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        // Total counts
                        if (await reader.ReadAsync())
                        {
                            TotalApplicationsText.Text = $"Обращений: {reader.GetInt32(0)}";
                            TotalCitizensText.Text = $"Граждан: {reader.GetInt32(1)}";
                            TotalCategoriesText.Text = $"Категорий: {reader.GetInt32(2)}";
                        }
                        await reader.NextResultAsync();

                        // Polls
                        PollStatsComboBox.Items.Clear();
                        while (await reader.ReadAsync())
                        {
                            PollStatsComboBox.Items.Add(new PollItem { PollId = reader.GetInt32(0), Title = reader.GetString(1) });
                        }
                        if (PollStatsComboBox.Items.Count > 0)
                        {
                            PollStatsComboBox.SelectedIndex = 0;
                        }
                        await reader.NextResultAsync();

                        // Application statuses
                        List<string> statuses = new List<string>();
                        List<double> statusCounts = new List<double>();
                        while (await reader.ReadAsync())
                        {
                            statuses.Add(reader.GetString(0));
                            statusCounts.Add(reader.GetInt32(1));
                        }
                        ApplicationStatusChart.Series = new SeriesCollection
                        {
                            new ColumnSeries
                            {
                                Title = "Обращения по статусам",
                                Values = new ChartValues<double>(statusCounts),
                                Fill = new SolidColorBrush(Colors.Purple)
                            }
                        };
                        ApplicationStatusChart.AxisX.Clear();
                        ApplicationStatusChart.AxisX.Add(new LiveCharts.Wpf.Axis
                        {
                            Title = "Статус",
                            Labels = statuses.ToArray()
                        });
                        ApplicationStatusChart.AxisY.Clear();
                        ApplicationStatusChart.AxisY.Add(new LiveCharts.Wpf.Axis
                        {
                            Title = "Количество обращений",
                            MinValue = 0
                        });
                        await reader.NextResultAsync();

                        // Citizen registrations
                        List<string> months = new List<string>();
                        List<double> regCounts = new List<double>();
                        while (await reader.ReadAsync())
                        {
                            months.Add(reader.GetString(0));
                            regCounts.Add(reader.GetInt32(1));
                        }
                        CitizenRegistrationChart.Series = new SeriesCollection
                        {
                            new ColumnSeries
                            {
                                Title = "Регистрации",
                                Values = new ChartValues<double>(regCounts),
                                Fill = new SolidColorBrush(Colors.Green)
                            }
                        };
                        CitizenRegistrationChart.AxisX.Clear();
                        CitizenRegistrationChart.AxisX.Add(new LiveCharts.Wpf.Axis
                        {
                            Title = "Месяц",
                            Labels = months.ToArray()
                        });
                        CitizenRegistrationChart.AxisY.Clear();
                        CitizenRegistrationChart.AxisY.Add(new LiveCharts.Wpf.Axis
                        {
                            Title = "Количество регистраций",
                            MinValue = 0
                        });
                    }
                }
            }
        }

        private async void PollStatsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PollStatsComboBox.SelectedItem == null) return;

            var selectedPoll = (PollItem)PollStatsComboBox.SelectedItem;
            int pollId = selectedPoll.PollId;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("SELECT VoteType, COUNT(*) AS Count FROM PollVotes WHERE PollId = @PollId GROUP BY VoteType", connection))
                {
                    cmd.Parameters.AddWithValue("@PollId", pollId);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        double[] votes = new double[2];
                        while (await reader.ReadAsync())
                        {
                            string voteType = reader.GetString(0);
                            int count = reader.GetInt32(1);
                            if (voteType == "За")
                                votes[0] = count;
                            else if (voteType == "Против")
                                votes[1] = count;
                        }

                        if (votes.All(v => v == 0))
                        {
                            MessageBox.Show("Нет голосов для этого опроса!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        PollStatsChart.Series = new SeriesCollection
                        {
                            new ColumnSeries
                            {
                                Title = "Голоса",
                                Values = new ChartValues<double>(votes),
                                Fill = new SolidColorBrush(Colors.Blue)
                            }
                        };

                        PollStatsChart.AxisX.Clear();
                        PollStatsChart.AxisX.Add(new LiveCharts.Wpf.Axis
                        {
                            Title = "Тип голоса",
                            Labels = new[] { "За", "Против" }
                        });

                        PollStatsChart.AxisY.Clear();
                        PollStatsChart.AxisY.Add(new LiveCharts.Wpf.Axis
                        {
                            Title = "Количество голосов",
                            MinValue = 0
                        });
                    }
                }
            }
        }

        public class PollItem
        {
            public int PollId { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }

        public class Category
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public string Description { get; set; }
        }
    }
}