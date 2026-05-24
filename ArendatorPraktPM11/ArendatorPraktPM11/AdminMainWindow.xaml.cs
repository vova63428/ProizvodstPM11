using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace ArendatorPraktPm11
{
    public partial class AdminMainWindow : Window
    {
        string connectionString;
        int employeeId;
        string employeeName;
        string currentTable = "";
        DataTable currentData = new DataTable();
        int currentUserId = 1;

        public AdminMainWindow(string connStr, int id, string name)
        {
            InitializeComponent();
            connectionString = connStr;
            employeeId = id;
            employeeName = name;
            lblAdminName.Text = employeeName;
            currentUserId = id;
        }

        private void lstTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTables.SelectedItem is ListBoxItem item && item.Tag is string tableName)
            {
                currentTable = tableName;
                RefreshData();

                string title = "";
                switch (currentTable)
                {
                    case "contracts": title = "📋 Договоры аренды"; break;
                    case "objects": title = "🏠 Объекты муниципального имущества"; break;
                    case "employees": title = "👥 Сотрудники администрации"; break;
                    case "tenants": title = "🏢 Арендаторы"; break;
                    case "payments": title = "💰 Платежи"; break;
                    case "users": title = "👤 Пользователи системы"; break;
                    case "positions": title = "📌 Справочник должностей"; break;
                    case "departments": title = "🏛️ Справочник отделов"; break;
                    case "audit_log": title = "📜 Журнал аудита"; break;
                }
                lblTitle.Text = title;
            }
        }

        private void RefreshData()
        {
            string sql = "";
            switch (currentTable)
            {
                case "contracts":
                    sql = @"SELECT 
                                c.id_contract AS 'ID',
                                c.contract_number AS 'Номер договора',
                                o.address AS 'Адрес объекта',
                                t.short_name AS 'Арендатор',
                                c.start_date AS 'Дата начала',
                                c.end_date AS 'Дата окончания',
                                c.monthly_rate AS 'Ставка (руб)',
                                c.contract_status AS 'Статус'
                           FROM contracts c
                           JOIN objects o ON c.id_object = o.id_object
                           JOIN tenants t ON c.id_tenant = t.id_tenant";
                    break;
                case "objects":
                    sql = @"SELECT 
                                id_object AS 'ID',
                                address AS 'Адрес',
                                type AS 'Тип',
                                total_area AS 'Площадь',
                                purpose AS 'Назначение',
                                condition AS 'Состояние',
                                CASE WHEN is_rented_now = 1 THEN N'Да' ELSE N'Нет' END AS 'В аренде'
                           FROM objects";
                    break;
                case "employees":
                    sql = @"SELECT 
                                id_employee AS 'ID',
                                last_name AS 'Фамилия',
                                first_name AS 'Имя',
                                middle_name AS 'Отчество',
                                login AS 'Логин',
                                password AS 'Пароль',
                                role AS 'Роль',
                                CASE WHEN is_active = 1 THEN N'Да' ELSE N'Нет' END AS 'Активен'
                           FROM employees";
                    break;
                case "tenants":
                    sql = @"SELECT 
                                id_tenant AS 'ID',
                                short_name AS 'Наименование',
                                type AS 'Тип',
                                inn AS 'ИНН',
                                phone AS 'Телефон',
                                email AS 'Email'
                           FROM tenants";
                    break;
                case "payments":
                    sql = @"SELECT 
                                p.id_payment AS 'ID',
                                p.payment_date AS 'Дата платежа',
                                p.amount AS 'Сумма',
                                c.contract_number AS 'Договор',
                                p.receipt_number AS 'Квитанция'
                           FROM payments p
                           JOIN contracts c ON p.id_contract = c.id_contract
                           ORDER BY p.payment_date DESC";
                    break;
                case "users":
                    sql = @"SELECT 
                                id_user AS 'ID',
                                login AS 'Логин',
                                password AS 'Пароль',
                                full_name AS 'ФИО',
                                email AS 'Email',
                                phone AS 'Телефон',
                                role AS 'Роль',
                                CASE WHEN is_blocked = 1 THEN N'Да' ELSE N'Нет' END AS 'Заблокирован',
                                last_login AS 'Последний вход',
                                created_at AS 'Дата регистрации'
                           FROM users
                           ORDER BY id_user";
                    break;
                case "positions":
                    sql = @"SELECT 
                                id_position AS 'ID',
                                position_name AS 'Должность',
                                salary_grade AS 'Категория',
                                access_level AS 'Уровень доступа',
                                base_salary AS 'Оклад'
                           FROM positions
                           ORDER BY id_position";
                    break;
                case "departments":
                    sql = @"SELECT 
                                id_department AS 'ID',
                                department_name AS 'Название отдела'
                           FROM departments
                           ORDER BY id_department";
                    break;
                case "audit_log":
                    sql = @"SELECT 
                                id_log AS 'ID',
                                id_user AS 'ID пользователя',
                                action_type AS 'Действие',
                                table_name AS 'Таблица',
                                record_id AS 'ID записи',
                                action_datetime AS 'Дата/время',
                                old_value AS 'Было',
                                new_value AS 'Стало',
                                ip_address AS 'IP адрес'
                           FROM audit_log
                           ORDER BY action_datetime DESC";
                    break;
                default:
                    return;
            }

            currentData = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(currentData);
                }
            }
            dgData.ItemsSource = currentData.DefaultView;
            lblStatus.Text = $"Загружено записей: {currentData.Rows.Count}";
        }

        private void WriteToAuditLog(string actionType, string tableName, int recordId, string oldValue, string newValue)
        {
            string sql = @"INSERT INTO audit_log (id_user, action_type, table_name, record_id, action_datetime, old_value, new_value, ip_address) 
                           VALUES (@id_user, @action_type, @table_name, @record_id, GETDATE(), @old_value, @new_value, @ip_address)";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id_user", currentUserId);
                    cmd.Parameters.AddWithValue("@action_type", actionType);
                    cmd.Parameters.AddWithValue("@table_name", tableName);
                    cmd.Parameters.AddWithValue("@record_id", recordId);
                    cmd.Parameters.AddWithValue("@old_value", string.IsNullOrEmpty(oldValue) ? (object)DBNull.Value : oldValue);
                    cmd.Parameters.AddWithValue("@new_value", string.IsNullOrEmpty(newValue) ? (object)DBNull.Value : newValue);
                    cmd.Parameters.AddWithValue("@ip_address", "127.0.0.1");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable))
            {
                MessageBox.Show("Сначала выберите таблицу", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new EditWindow(connectionString, currentTable, null, currentUserId);
            editWindow.Owner = this;
            if (editWindow.ShowDialog() == true)
            {
                RefreshData();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable))
            {
                MessageBox.Show("Сначала выберите таблицу", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgData.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView selectedRow = (DataRowView)dgData.SelectedItem;
            int recordId = Convert.ToInt32(selectedRow.Row[0]);

            var editWindow = new EditWindow(connectionString, currentTable, recordId, currentUserId);
            editWindow.Owner = this;
            if (editWindow.ShowDialog() == true)
            {
                RefreshData();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgData.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    DataRowView selectedRow = (DataRowView)dgData.SelectedItem;
                    int recordId = Convert.ToInt32(selectedRow.Row[0]);

                    string login = "";
                    string email = "";

                    if (currentTable == "employees")
                    {
                        if (selectedRow.Row.Table.Columns.Contains("Логин"))
                            login = selectedRow.Row["Логин"]?.ToString();
                        else if (selectedRow.Row.Table.Columns.Contains("login"))
                            login = selectedRow.Row["login"]?.ToString();
                    }

                    if (currentTable == "tenants")
                    {
                        if (selectedRow.Row.Table.Columns.Contains("Email"))
                            email = selectedRow.Row["Email"]?.ToString();
                        else if (selectedRow.Row.Table.Columns.Contains("email"))
                            email = selectedRow.Row["email"]?.ToString();
                    }

                    string oldValues = "";

                    string idColumnName = "";
                    switch (currentTable)
                    {
                        case "contracts": idColumnName = "id_contract"; break;
                        case "objects": idColumnName = "id_object"; break;
                        case "tenants": idColumnName = "id_tenant"; break;
                        case "employees": idColumnName = "id_employee"; break;
                        case "payments": idColumnName = "id_payment"; break;
                        case "users": idColumnName = "id_user"; break;
                        case "positions": idColumnName = "id_position"; break;
                        case "departments": idColumnName = "id_department"; break;
                        case "audit_log": idColumnName = "id_log"; break;
                        default: idColumnName = $"id_{currentTable}"; break;
                    }

                    string getOldSql = $"SELECT * FROM {currentTable} WHERE {idColumnName} = @id";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        using (SqlCommand cmd = new SqlCommand(getOldSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", recordId);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        oldValues += $"{reader.GetName(i)}: {reader[i]}; ";
                                    }
                                }
                            }
                        }

                        WriteToAuditLog("DELETE", currentTable, recordId, oldValues, null);

                        string deleteSql = $"DELETE FROM {currentTable} WHERE {idColumnName} = @id";
                        using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", recordId);
                            cmd.ExecuteNonQuery();
                        }

                        if (currentTable == "employees" && !string.IsNullOrEmpty(login))
                        {
                            string deleteUser = "DELETE FROM users WHERE login = @login";
                            using (SqlCommand cmd = new SqlCommand(deleteUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@login", login);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (currentTable == "tenants" && !string.IsNullOrEmpty(email))
                        {
                            string deleteUser = "DELETE FROM users WHERE email = @email AND role = 'tenant'";
                            using (SqlCommand cmd = new SqlCommand(deleteUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    RefreshData();
                    MessageBox.Show("Запись успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable))
            {
                MessageBox.Show("Сначала выберите таблицу", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reportSql = "";
            string reportTitle = "";

            switch (currentTable)
            {
                case "contracts":
                    reportSql = @"SELECT 
                                    COUNT(*) AS 'Всего договоров',
                                    SUM(CASE WHEN contract_status = 'active' THEN 1 ELSE 0 END) AS 'Действующих',
                                    SUM(CASE WHEN contract_status = 'ended' THEN 1 ELSE 0 END) AS 'Завершённых',
                                    SUM(monthly_rate) AS 'Общая сумма ставок (руб)'
                                  FROM contracts";
                    reportTitle = "Отчёт по договорам";
                    break;

                case "objects":
                    reportSql = @"SELECT 
                                    type AS 'Тип объекта',
                                    COUNT(*) AS 'Количество',
                                    SUM(CASE WHEN is_rented_now = 1 THEN 1 ELSE 0 END) AS 'В аренде',
                                    SUM(CASE WHEN is_rented_now = 0 THEN 1 ELSE 0 END) AS 'Свободно'
                                  FROM objects
                                  GROUP BY type";
                    reportTitle = "Отчёт по объектам";
                    break;

                case "payments":
                    reportSql = @"SELECT 
                                    YEAR(payment_date) AS 'Год',
                                    MONTH(payment_date) AS 'Месяц',
                                    COUNT(*) AS 'Количество платежей',
                                    SUM(amount) AS 'Общая сумма (руб)'
                                  FROM payments
                                  GROUP BY YEAR(payment_date), MONTH(payment_date)
                                  ORDER BY YEAR(payment_date) DESC, MONTH(payment_date) DESC";
                    reportTitle = "Отчёт по платежам";
                    break;

                case "employees":
                    reportSql = @"SELECT 
                                    role AS 'Роль',
                                    COUNT(*) AS 'Количество сотрудников'
                                  FROM employees
                                  WHERE is_active = 1
                                  GROUP BY role";
                    reportTitle = "Отчёт по сотрудникам";
                    break;

                case "tenants":
                    reportSql = @"SELECT 
                                    type AS 'Тип арендатора',
                                    COUNT(*) AS 'Количество'
                                  FROM tenants
                                  GROUP BY type";
                    reportTitle = "Отчёт по арендаторам";
                    break;

                case "users":
                    reportSql = @"SELECT 
                                    role AS 'Роль',
                                    COUNT(*) AS 'Количество пользователей',
                                    SUM(CASE WHEN is_blocked = 1 THEN 1 ELSE 0 END) AS 'Заблокировано'
                                  FROM users
                                  GROUP BY role";
                    reportTitle = "Отчёт по пользователям";
                    break;

                default:
                    MessageBox.Show("Для этой таблицы отчёт не настроен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
            }

            DataTable reportData = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(reportSql, conn))
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(reportData);
                }
            }

            ReportWindow reportWindow = new ReportWindow(reportData, reportTitle);
            reportWindow.ShowDialog();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}