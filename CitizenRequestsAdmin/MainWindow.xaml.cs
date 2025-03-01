using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
//using WPF_UI;
using System.Windows.Controls;

namespace CitizenRequestsAdmin
{
    public partial class MainWindow : Window
    {
        private readonly string _connectionString = "Server=TEMHANLAPTOP\\TDG2022;Database=CitizenRequestsDB;Integrated Security=True;";
        private int selectedApplicationId = -1;
        private int selectedCitizenId = -1;
        private int selectedCategoryId = -1;
        private int _adminId;
        public MainWindow()
        {
            InitializeComponent();
            //ApplicationThemeManager.Apply(this);
            ShowLoginWindow();
            Loaded += (s, e) => LoadData();
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
        }
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl) // Проверяем, что событие сработало именно на TabControl
            {
                LoadData();
            }
        }

        private void ShowLoginWindow()
        {
            LoginWindow loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                _adminId = loginWindow.AdminId; // Получаем ID администратора после входа
                LoadData();
            }
            else
            {
                Close();
            }
        }

        private void LoadData()
        {
            LoadApplications();
            LoadCitizens();
            LoadCategories();
        }

        private void LoadApplications()
        {
            if (ApplicationsGrid == null) return; // Проверяем, что DataGrid уже загружен

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
            SELECT a.ApplicationId, c.FullName, cat.CategoryName, a.Status, a.SubmissionDate, a.Description
            FROM Applications a
            JOIN Citizens c ON a.CitizenId = c.CitizenId
            JOIN Categories cat ON a.CategoryId = cat.CategoryId
            ORDER BY a.SubmissionDate DESC";

                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                ApplicationsGrid.ItemsSource = dt.DefaultView;

                LoadCategoriesForFilter(connection);
            }
        }


        private void LoadCategoriesForFilter(SqlConnection connection)
        {
            SqlDataAdapter adapter = new SqlDataAdapter("SELECT DISTINCT CategoryName FROM Categories", connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            CategoryFilterBox.Items.Clear();
            CategoryFilterBox.Items.Add("Все");

            foreach (DataRow row in dt.Rows)
            {
                CategoryFilterBox.Items.Add(row["CategoryName"].ToString());
            }

            CategoryFilterBox.SelectedIndex = 0;
        }

        // Фильтрация по статусу и категории
        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (ApplicationsGrid == null || ApplicationsGrid.ItemsSource == null)
                return;

            if (ApplicationsGrid.ItemsSource is DataView view)
            {
                string filter = "";

                string search = SearchBox?.Text?.Trim();
                if (!string.IsNullOrEmpty(search))
                {
                    filter += $"(FullName LIKE '%{search}%' OR Description LIKE '%{search}%')";
                }

                string selectedStatus = (StatusFilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (selectedStatus != "Все" && !string.IsNullOrEmpty(selectedStatus))
                {
                    if (!string.IsNullOrEmpty(filter)) filter += " AND ";
                    filter += $"Status = '{selectedStatus}'";
                }

                string selectedCategory = CategoryFilterBox.SelectedItem?.ToString();
                if (selectedCategory != "Все" && !string.IsNullOrEmpty(selectedCategory))
                {
                    if (!string.IsNullOrEmpty(filter)) filter += " AND ";
                    filter += $"CategoryName = '{selectedCategory}'";
                }

                view.RowFilter = filter;
            }
        }

        // Очистка фильтров
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StatusFilterBox.SelectedIndex = 0;
            CategoryFilterBox.SelectedIndex = 0;
        }


        private void LoadCitizens()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Citizens WHERE 1=1";

                if (CitizenSearchBox != null && !string.IsNullOrWhiteSpace(CitizenSearchBox.Text))
                {
                    query += " AND FullName LIKE @SearchText";
                }
                if (RegistrationDateFilter != null && RegistrationDateFilter.SelectedDate.HasValue)
                {
                    query += " AND CreatedAt >= @RegDate";
                }

                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                if (CitizenSearchBox != null && !string.IsNullOrWhiteSpace(CitizenSearchBox.Text))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@SearchText", "%" + CitizenSearchBox.Text + "%");
                }
                if (RegistrationDateFilter != null && RegistrationDateFilter.SelectedDate.HasValue)
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@RegDate", RegistrationDateFilter.SelectedDate.Value);
                }

                DataTable dt = new DataTable();
                adapter.Fill(dt);
                CitizensGrid.ItemsSource = dt.DefaultView;
            }
        }

        // Поиск граждан
        private void CitizenSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCitizens();
        }

        // Фильтрация по дате регистрации
        private void FilterCitizens(object sender, SelectionChangedEventArgs e)
        {
            LoadCitizens();
        }

        // Очистить фильтр
        private void ClearCitizenFilter_Click(object sender, RoutedEventArgs e)
        {
            if (CitizenSearchBox != null) CitizenSearchBox.Text = "";
            if (RegistrationDateFilter != null) RegistrationDateFilter.SelectedDate = null;
            LoadCitizens();
        }

        // Выбор гражданина
        private void CitizensGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CitizensGrid.SelectedItem == null)
            {
                // Если ничего не выбрано, можно вывести предупреждение или просто завершить метод
                //MessageBox.Show("Пожалуйста, выберите гражданина из списка.");
                return;
            }

            if (CitizensGrid.SelectedItem is DataRowView row)
            {
                selectedCitizenId = Convert.ToInt32(row["CitizenId"]);
            }
        }

        // Удаление гражданина
        private void DeleteCitizen_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCitizenId == -1) return;

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить гражданина?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("DELETE FROM Citizens WHERE CitizenId = @CitizenId", connection);
                    cmd.Parameters.AddWithValue("@CitizenId", selectedCitizenId);
                    cmd.ExecuteNonQuery();
                }
                LoadCitizens();
            }
        }

        // Открытие окна редактирования
        private void EditCitizen_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCitizenId == -1) return;

            EditCitizenWindow editWindow = new EditCitizenWindow(selectedCitizenId);
            editWindow.ShowDialog();
            LoadCitizens();
        }


        private void LoadCategories(string filter = "")
        {
            var categories = new List<Category>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    "SELECT CategoryId, CategoryName, Description FROM Categories WHERE CategoryName LIKE @Filter",
                    connection
                );
                command.Parameters.AddWithValue("@Filter", "%" + filter + "%");
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    categories.Add(new Category
                    {
                        CategoryId = reader.GetInt32(0),
                        CategoryName = reader.GetString(1),
                        Description = reader.GetString(2)
                    });
                }
            }

            // Привязка списка категорий к DataGrid
            CategoriesGrid.ItemsSource = categories;

            // Если необходимо, можно также использовать DataGridTemplateColumn для редактируемых колонок.
            foreach (var column in CategoriesGrid.Columns)
            {
                if (column.Header.ToString() == "CategoryName" || column.Header.ToString() == "Description")
                {
                    column.IsReadOnly = false;  // Разрешаем редактирование в этих колонках.
                }
            }
        }



        private void CategorySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCategories(CategorySearchBox.Text);
        }

        private void ClearCategoryFilter_Click(object sender, RoutedEventArgs e)
        {
            CategorySearchBox.Text = "";
            LoadCategories();
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new EditCategoryWindow();
            addWindow.ShowDialog();
            LoadCategories();
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCategoryId == -1)
            {
                MessageBox.Show("Выберите категорию для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new EditCategoryWindow(selectedCategoryId);
            editWindow.ShowDialog();
            LoadCategories();
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is Category selectedCategory)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить категорию?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand("DELETE FROM Categories WHERE CategoryId = @CategoryId", connection);
                        cmd.Parameters.AddWithValue("@CategoryId", selectedCategory.CategoryId);
                        cmd.ExecuteNonQuery();
                    }
                    LoadCategories();
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


        private void ApplicationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ApplicationsGrid.SelectedItem is DataRowView row)
            {
                selectedApplicationId = Convert.ToInt32(row["ApplicationId"]);
            }
        }



        private void DeleteApplication_Click(object sender, RoutedEventArgs e)
        {
            if (selectedApplicationId == -1) return;

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить обращение?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("DELETE FROM Applications WHERE ApplicationId = @ApplicationId", connection);
                    cmd.Parameters.AddWithValue("@ApplicationId", selectedApplicationId);
                    cmd.ExecuteNonQuery();
                }
                LoadApplications();
            }
        }

        private void OpenChat_Click(object sender, RoutedEventArgs e)
        {
            if (selectedApplicationId == -1) return;

            ChatWindow chatWindow = new ChatWindow(selectedApplicationId, _adminId);
            chatWindow.ShowDialog();
            //LoadResponses();
        }

        private void EditResponse_Click(object sender, RoutedEventArgs e)
        {
            if (selectedApplicationId == -1) return;

            ResponseEditorWindow responseEditor = new ResponseEditorWindow(selectedApplicationId);
            responseEditor.ShowDialog();
            //LoadResponses();
        }


        public class Category
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public string Description { get; set; }
        }

    }
}
