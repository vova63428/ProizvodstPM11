using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace ArendatorPraktPm11
{
    public partial class EditWindow : Window
    {
        string connectionString;
        string tableName;
        int? recordId;
        bool isAddMode;
        List<string> fieldNames = new List<string>();
        List<UIElement> inputControls = new List<UIElement>();
        Dictionary<string, string> foreignKeyTables = new Dictionary<string, string>();
        int currentUserId = 1;

        Dictionary<string, string> idColumnNames = new Dictionary<string, string>
        {
            {"contracts", "id_contract"},
            {"objects", "id_object"},
            {"tenants", "id_tenant"},
            {"employees", "id_employee"},
            {"positions", "id_position"},
            {"payments", "id_payment"},
            {"users", "id_user"},
            {"audit_log", "id_log"},
            {"departments", "id_department"}
        };

        HashSet<string> identityTables = new HashSet<string>
        {
            "users",
            "audit_log",
            "departments"
        };

        public EditWindow(string connStr, string table, int? id = null, int userId = 1)
        {
            InitializeComponent();
            connectionString = connStr;
            tableName = table;
            recordId = id;
            isAddMode = !id.HasValue;
            currentUserId = userId;

            if (tableName == "contracts")
            {
                foreignKeyTables["id_object"] = "objects";
                foreignKeyTables["id_tenant"] = "tenants";
                foreignKeyTables["id_responsible_employee"] = "employees";
            }
            else if (tableName == "employees")
            {
                foreignKeyTables["id_position"] = "positions";
                foreignKeyTables["department"] = "departments";
            }
            else if (tableName == "payments")
            {
                foreignKeyTables["id_contract"] = "contracts";
                foreignKeyTables["id_employee_who_accepted"] = "employees";
            }

            lblTitle.Text = isAddMode ? "➕ Добавление новой записи" : "✏️ Редактирование записи";
            LoadFields();
        }

        private void LoadFields()
        {
            spFields.Children.Clear();
            fieldNames.Clear();
            inputControls.Clear();

            string sql = $"SELECT TOP 0 * FROM {tableName}";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTable schemaTable = reader.GetSchemaTable();

                    for (int i = 0; i < schemaTable.Rows.Count; i++)
                    {
                        string fieldName = schemaTable.Rows[i]["ColumnName"].ToString();
                        string dataType = schemaTable.Rows[i]["DataTypeName"].ToString();

                        string correctIdName = GetIdColumnName(tableName);

                        if (fieldName == correctIdName && identityTables.Contains(tableName))
                            continue;

                        if (fieldName == "id_user" || fieldName == "id_log")
                            continue;

                        fieldNames.Add(fieldName);

                        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                        stack.Children.Add(new TextBlock
                        {
                            Text = GetRussianName(fieldName),
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 0, 0, 5)
                        });

                        UIElement control;

                        if (foreignKeyTables.ContainsKey(fieldName))
                        {
                            var comboBox = new ComboBox
                            {
                                Name = $"cmb_{fieldName}",
                                Height = 32,
                                DisplayMemberPath = "DisplayName",
                                SelectedValuePath = "Id",
                                Tag = fieldName
                            };
                            LoadComboBoxData(comboBox, foreignKeyTables[fieldName]);
                            control = comboBox;
                        }
                        else if (dataType.ToLower().Contains("date"))
                        {
                            var datePicker = new DatePicker { Name = $"dp_{fieldName}", Height = 32 };
                            control = datePicker;
                        }
                        else
                        {
                            var textBox = new TextBox { Name = $"txt_{fieldName}", Height = 32, Padding = new Thickness(8, 0, 0, 0) };
                            control = textBox;
                        }

                        stack.Children.Add(control);
                        inputControls.Add(control);
                        spFields.Children.Add(stack);
                    }
                }
            }

            if (!isAddMode && recordId.HasValue)
            {
                LoadExistingData();
            }
        }

        private string GetIdColumnName(string table)
        {
            if (idColumnNames.ContainsKey(table))
                return idColumnNames[table];
            return $"id_{table}";
        }

        private int GetNextId(string tableName)
        {
            string idColumn = GetIdColumnName(tableName);
            string sql = $"SELECT ISNULL(MAX({idColumn}), 0) + 1 FROM {tableName}";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private void LoadComboBoxData(ComboBox comboBox, string refTable)
        {
            string displayField = "";
            string idField = GetIdColumnName(refTable);

            if (refTable == "objects") displayField = "address";
            else if (refTable == "tenants") displayField = "short_name";
            else if (refTable == "employees") displayField = "last_name + ' ' + first_name";
            else if (refTable == "positions") displayField = "position_name";
            else if (refTable == "contracts") displayField = "contract_number";
            else if (refTable == "departments")
            {
                idField = "id_department";
                displayField = "department_name";
            }
            else displayField = "name";

            string sql = $"SELECT {idField} AS Id, {displayField} AS DisplayName FROM {refTable}";
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }

                comboBox.ItemsSource = dt.DefaultView;
                comboBox.SelectedValuePath = "Id";
                comboBox.DisplayMemberPath = "DisplayName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных для {refTable}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadExistingData()
        {
            string idColumn = GetIdColumnName(tableName);
            string sql = $"SELECT * FROM {tableName} WHERE {idColumn} = @id";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", recordId.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            for (int i = 0; i < fieldNames.Count; i++)
                            {
                                string fieldName = fieldNames[i];
                                object value = reader[fieldName];
                                UIElement control = inputControls[i];

                                if (control is TextBox textBox)
                                {
                                    textBox.Text = value != DBNull.Value ? value.ToString() : "";
                                }
                                else if (control is ComboBox comboBox)
                                {
                                    if (value != DBNull.Value)
                                    {
                                        try { comboBox.SelectedValue = Convert.ToInt32(value); } catch { }
                                    }
                                }
                                else if (control is DatePicker datePicker)
                                {
                                    if (value != DBNull.Value)
                                        datePicker.SelectedDate = Convert.ToDateTime(value);
                                }
                            }
                        }
                    }
                }
            }
        }

        private string GetRussianName(string fieldName)
        {
            var names = new Dictionary<string, string>
            {
                {"contract_number", "Номер договора"},
                {"id_object", "Объект"},
                {"id_tenant", "Арендатор"},
                {"id_responsible_employee", "Ответственный сотрудник"},
                {"start_date", "Дата начала"},
                {"end_date", "Дата окончания"},
                {"monthly_rate", "Ставка (руб)"},
                {"payment_day", "День оплаты"},
                {"contract_status", "Статус"},
                {"notes", "Примечания"},
                {"address", "Адрес"},
                {"cadastral_number", "Кадастровый номер"},
                {"type", "Тип"},
                {"total_area", "Площадь"},
                {"purpose", "Назначение"},
                {"condition", "Состояние"},
                {"is_rented_now", "В аренде"},
                {"short_name", "Наименование"},
                {"full_name", "Полное имя"},
                {"inn", "ИНН"},
                {"ogrn", "ОГРН"},
                {"phone", "Телефон"},
                {"email", "Email"},
                {"legal_address", "Юридический адрес"},
                {"registration_date", "Дата регистрации"},
                {"last_name", "Фамилия"},
                {"first_name", "Имя"},
                {"middle_name", "Отчество"},
                {"id_position", "Должность"},
                {"department", "Отдел"},
                {"hire_date", "Дата приёма"},
                {"login", "Логин"},
                {"password", "Пароль"},
                {"is_active", "Активен"},
                {"role", "Роль"},
                {"payment_date", "Дата платежа"},
                {"period_month", "Период"},
                {"amount", "Сумма"},
                {"payment_type", "Тип платежа"},
                {"is_penalty", "Пеня"},
                {"receipt_number", "Номер квитанции"},
                {"id_contract", "Договор"},
                {"id_employee_who_accepted", "Кто принял"},
                {"position_name", "Название должности"},
                {"salary_grade", "Категория"},
                {"access_level", "Уровень доступа"},
                {"base_salary", "Базовый оклад"},
                {"department_name", "Название отдела"},
                {"id_department", "ID отдела"}
            };
            return names.ContainsKey(fieldName) ? names[fieldName] : fieldName;
        }

        private object GetValueFromControl(UIElement control)
        {
            if (control is TextBox textBox)
                return string.IsNullOrWhiteSpace(textBox.Text) ? null : textBox.Text;
            if (control is ComboBox comboBox)
                return comboBox.SelectedValue;
            if (control is DatePicker datePicker)
                return datePicker.SelectedDate;
            return null;
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

        private void SyncEmployeeToUsers(SqlConnection conn, int employeeId)
        {
            string getEmployee = "SELECT login, password, last_name, first_name, middle_name, email, phone, role FROM employees WHERE id_employee = @id";
            using (SqlCommand cmd = new SqlCommand(getEmployee, conn))
            {
                cmd.Parameters.AddWithValue("@id", employeeId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string login = reader.GetString(0);
                        string password = reader.GetString(1);
                        string lastName = reader.GetString(2);
                        string firstName = reader.GetString(3);
                        string middleName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        string email = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        string phone = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        string role = reader.GetString(7);
                        reader.Close();

                        string userRole = "property_worker";
                        if (role == "admin") userRole = "admin";
                        else if (role == "accountant") userRole = "accountant";

                        string checkUser = "SELECT COUNT(*) FROM users WHERE login = @login";
                        using (SqlCommand checkCmd = new SqlCommand(checkUser, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@login", login);
                            int exists = (int)checkCmd.ExecuteScalar();

                            if (exists > 0)
                            {
                                string updateUser = @"UPDATE users SET password = @password, full_name = @full_name, email = @email, phone = @phone, role = @role WHERE login = @login";
                                using (SqlCommand updateCmd = new SqlCommand(updateUser, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@login", login);
                                    updateCmd.Parameters.AddWithValue("@password", password);
                                    updateCmd.Parameters.AddWithValue("@full_name", $"{lastName} {firstName}{(string.IsNullOrEmpty(middleName) ? "" : " " + middleName)}");
                                    updateCmd.Parameters.AddWithValue("@email", email);
                                    updateCmd.Parameters.AddWithValue("@phone", phone);
                                    updateCmd.Parameters.AddWithValue("@role", userRole);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string insertUser = @"INSERT INTO users (login, password, full_name, email, phone, role, is_blocked, created_at) 
                                                      VALUES (@login, @password, @full_name, @email, @phone, @role, 0, GETDATE())";
                                using (SqlCommand insertCmd = new SqlCommand(insertUser, conn))
                                {
                                    insertCmd.Parameters.AddWithValue("@login", login);
                                    insertCmd.Parameters.AddWithValue("@password", password);
                                    insertCmd.Parameters.AddWithValue("@full_name", $"{lastName} {firstName}{(string.IsNullOrEmpty(middleName) ? "" : " " + middleName)}");
                                    insertCmd.Parameters.AddWithValue("@email", email);
                                    insertCmd.Parameters.AddWithValue("@phone", phone);
                                    insertCmd.Parameters.AddWithValue("@role", userRole);
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SyncTenantToUsers(SqlConnection conn, int tenantId)
        {
            string getTenant = "SELECT short_name, email, phone FROM tenants WHERE id_tenant = @id";
            using (SqlCommand cmd = new SqlCommand(getTenant, conn))
            {
                cmd.Parameters.AddWithValue("@id", tenantId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string shortName = reader.GetString(0);
                        string email = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string phone = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        reader.Close();

                        string login = "tenant_" + tenantId;

                        string checkUser = "SELECT COUNT(*) FROM users WHERE login = @login";
                        using (SqlCommand checkCmd = new SqlCommand(checkUser, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@login", login);
                            int exists = (int)checkCmd.ExecuteScalar();

                            if (exists > 0)
                            {
                                string updateUser = "UPDATE users SET full_name = @full_name, email = @email, phone = @phone WHERE login = @login";
                                using (SqlCommand updateCmd = new SqlCommand(updateUser, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@login", login);
                                    updateCmd.Parameters.AddWithValue("@full_name", shortName);
                                    updateCmd.Parameters.AddWithValue("@email", email);
                                    updateCmd.Parameters.AddWithValue("@phone", phone);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string insertUser = @"INSERT INTO users (login, password, full_name, email, phone, role, is_blocked, created_at) 
                                                      VALUES (@login, 'tenant123', @full_name, @email, @phone, 'tenant', 0, GETDATE())";
                                using (SqlCommand insertCmd = new SqlCommand(insertUser, conn))
                                {
                                    insertCmd.Parameters.AddWithValue("@login", login);
                                    insertCmd.Parameters.AddWithValue("@full_name", shortName);
                                    insertCmd.Parameters.AddWithValue("@email", email);
                                    insertCmd.Parameters.AddWithValue("@phone", phone);
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int newRecordId = 0;
                    string idColumn = GetIdColumnName(tableName);

                    if (isAddMode)
                    {
                        if (!identityTables.Contains(tableName))
                        {
                            int newId = GetNextId(tableName);

                            // Формируем список полей для INSERT (без дублирования ID)
                            List<string> insertFields = new List<string>();
                            List<string> insertParams = new List<string>();

                            insertFields.Add(idColumn);
                            insertParams.Add("@newId");

                            for (int i = 0; i < fieldNames.Count; i++)
                            {
                                if (fieldNames[i] == idColumn) continue;
                                insertFields.Add(fieldNames[i]);
                                insertParams.Add("@" + fieldNames[i]);
                            }

                            string insertSql = $"INSERT INTO {tableName} ({string.Join(", ", insertFields)}) VALUES ({string.Join(", ", insertParams)})";
                            using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@newId", newId);
                                for (int i = 0; i < fieldNames.Count; i++)
                                {
                                    if (fieldNames[i] == idColumn) continue;
                                    object value = GetValueFromControl(inputControls[i]);
                                    cmd.Parameters.AddWithValue("@" + fieldNames[i], value ?? DBNull.Value);
                                }
                                cmd.ExecuteNonQuery();
                            }
                            newRecordId = newId;
                        }
                        else
                        {
                            string insertSql = $"INSERT INTO {tableName} ({string.Join(", ", fieldNames)}) VALUES ({string.Join(", ", fieldNames.ConvertAll(f => "@" + f))}); SELECT SCOPE_IDENTITY();";
                            using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                            {
                                for (int i = 0; i < fieldNames.Count; i++)
                                {
                                    object value = GetValueFromControl(inputControls[i]);
                                    cmd.Parameters.AddWithValue("@" + fieldNames[i], value ?? DBNull.Value);
                                }
                                newRecordId = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }

                        WriteToAuditLog("INSERT", tableName, newRecordId, null, "Добавлена новая запись");

                        if (tableName == "employees")
                        {
                            SyncEmployeeToUsers(conn, newRecordId);
                        }
                        else if (tableName == "tenants")
                        {
                            SyncTenantToUsers(conn, newRecordId);
                        }

                        MessageBox.Show("Запись успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string oldValues = "";
                        string getOldSql = $"SELECT * FROM {tableName} WHERE {idColumn} = @id";
                        using (SqlCommand cmd = new SqlCommand(getOldSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", recordId.Value);
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

                        string updateSql = $"UPDATE {tableName} SET {string.Join(", ", fieldNames.ConvertAll(f => f + " = @" + f))} WHERE {idColumn} = @id";
                        using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                        {
                            for (int i = 0; i < fieldNames.Count; i++)
                            {
                                object value = GetValueFromControl(inputControls[i]);
                                cmd.Parameters.AddWithValue("@" + fieldNames[i], value ?? DBNull.Value);
                            }
                            cmd.Parameters.AddWithValue("@id", recordId.Value);
                            cmd.ExecuteNonQuery();
                        }

                        WriteToAuditLog("UPDATE", tableName, recordId.Value, oldValues, "Запись отредактирована");

                        if (tableName == "employees")
                        {
                            SyncEmployeeToUsers(conn, recordId.Value);
                        }
                        else if (tableName == "tenants")
                        {
                            SyncTenantToUsers(conn, recordId.Value);
                        }

                        MessageBox.Show("Запись успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}