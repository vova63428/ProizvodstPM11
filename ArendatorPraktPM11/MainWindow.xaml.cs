using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace ArendatorPraktPm11
{
    public partial class MainWindow : Window
    {
        string connectionString;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                connectionString = ConfigurationManager.ConnectionStrings["MunicipalRentDB"].ConnectionString;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка чтения конфигурации: {ex.Message}", "Критическая ошибка");
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string loginOrEmail = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            var selectedItem = cmbRole.SelectedItem as ComboBoxItem;
            string userType = selectedItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(loginOrEmail) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин/email и пароль");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (userType == "tenant")
                    {
                        // Вход для арендатора: проверяем И login, И email
                        string sql = @"SELECT u.id_user, u.full_name, t.id_tenant, t.short_name
                                       FROM users u
                                       LEFT JOIN tenants t ON u.email = t.email
                                       WHERE (u.login = @login OR u.email = @login) 
                                         AND u.password = @password 
                                         AND u.role = 'tenant' 
                                         AND u.is_blocked = 0";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@login", loginOrEmail);
                            cmd.Parameters.AddWithValue("@password", password);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int userId = reader.GetInt32(0);
                                    string userFullName = reader.GetString(1);
                                    int tenantId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                                    string tenantName = reader.IsDBNull(3) ? userFullName : reader.GetString(3);

                                    reader.Close();

                                    var tenantWindow = new TenantMainWindow(connectionString, tenantId, tenantName);
                                    tenantWindow.Show();
                                    this.Close();
                                }
                                else
                                {
                                    ShowError("Неверный логин/email или пароль, или аккаунт заблокирован.");
                                }
                            }
                        }
                    }
                    else if (userType == "employee")
                    {
                        // Вход для сотрудника: проверяем И login, И email
                        string sql = @"SELECT id_employee, last_name, first_name, role
                                       FROM employees
                                       WHERE (login = @login OR email = @login) 
                                         AND password = @password 
                                         AND is_active = 1";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@login", loginOrEmail);
                            cmd.Parameters.AddWithValue("@password", password);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int empId = reader.GetInt32(0);
                                    string lastName = reader.GetString(1);
                                    string firstName = reader.GetString(2);
                                    string role = reader.GetString(3);
                                    reader.Close();

                                    if (role == "admin")
                                    {
                                        var adminWindow = new AdminMainWindow(connectionString, empId, $"{firstName} {lastName}");
                                        adminWindow.Show();
                                    }
                                    else
                                    {
                                        var employeeWindow = new EmployeeMainWindow(connectionString, empId, $"{firstName} {lastName}");
                                        employeeWindow.Show();
                                    }
                                    this.Close();
                                }
                                else
                                {
                                    ShowError("Сотрудник не найден или неверные данные.");
                                }
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ShowError($"Ошибка подключения к базе данных:\n{ex.Message}\n\nПроверьте строку подключения в App.config");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;
        }
    }
}