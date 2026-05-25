using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace ArendatorPraktPm11
{
    public partial class EmployeeMainWindow : Window
    {
        string connectionString;
        int employeeId;
        string employeeName;
        string currentTable = "";

        public EmployeeMainWindow(string connStr, int id, string name)
        {
            InitializeComponent();
            connectionString = connStr;
            employeeId = id;
            employeeName = name;
            lblEmployeeName.Text = employeeName;
        }

        private void lstTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTables.SelectedItem is ListBoxItem item && item.Tag is string tableName)
            {
                currentTable = tableName;
                LoadData(tableName);

                string title = "";
                switch (currentTable)
                {
                    case "contracts": title = "📋 Договоры аренды"; break;
                    case "objects": title = "🏠 Объекты муниципального имущества"; break;
                    case "tenants": title = "🏢 Арендаторы"; break;
                    case "payments": title = "💰 Платежи"; break;
                }
                lblTitle.Text = title;
            }
        }

        private void LoadData(string tableName)
        {
            string sql = "";
            switch (tableName)
            {
                case "contracts":
                    sql = @"SELECT 
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
                                address AS 'Адрес',
                                type AS 'Тип',
                                total_area AS 'Площадь (кв.м)',
                                purpose AS 'Назначение',
                                condition AS 'Состояние',
                                CASE WHEN is_rented_now = 1 THEN N'Да' ELSE N'Нет' END AS 'В аренде'
                           FROM objects";
                    break;
                case "tenants":
                    sql = @"SELECT 
                                short_name AS 'Наименование',
                                type AS 'Тип',
                                inn AS 'ИНН',
                                phone AS 'Телефон',
                                email AS 'Email'
                           FROM tenants";
                    break;
                case "payments":
                    sql = @"SELECT 
                                p.payment_date AS 'Дата платежа',
                                p.amount AS 'Сумма (руб)',
                                c.contract_number AS 'Договор',
                                p.payment_type AS 'Способ оплаты',
                                p.receipt_number AS 'Номер квитанции'
                           FROM payments p
                           JOIN contracts c ON p.id_contract = c.id_contract
                           ORDER BY p.payment_date DESC";
                    break;
                default:
                    return;
            }

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }
            dgData.ItemsSource = dt.DefaultView;
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable))
            {
                MessageBox.Show("Сначала выберите раздел", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                case "tenants":
                    reportSql = @"SELECT 
                                    type AS 'Тип арендатора',
                                    COUNT(*) AS 'Количество'
                                  FROM tenants
                                  GROUP BY type";
                    reportTitle = "Отчёт по арендаторам";
                    break;

                default:
                    MessageBox.Show("Для этого раздела отчёт не настроен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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