using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace ArendatorPraktPm11
{
    public partial class TenantMainWindow : Window
    {
        string connectionString;
        int tenantId;
        string tenantName;

        public TenantMainWindow(string connStr, int id, string name)
        {
            InitializeComponent();
            connectionString = connStr;
            tenantId = id;
            tenantName = name;
            lblTenantName.Text = tenantName;
            LoadData();
        }

        private void LoadData()
        {
            // Загружаем договоры арендатора
            string contractsSql = @"SELECT 
                                        c.contract_number AS 'Номер договора',
                                        o.address AS 'Адрес объекта',
                                        o.type AS 'Тип объекта',
                                        c.monthly_rate AS 'Ставка (руб)',
                                        c.start_date AS 'Дата начала',
                                        c.end_date AS 'Дата окончания',
                                        c.contract_status AS 'Статус'
                                   FROM contracts c
                                   JOIN objects o ON c.id_object = o.id_object
                                   WHERE c.id_tenant = @tenantId";

            DataTable contractsTable = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(contractsSql, conn))
                {
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(contractsTable);
                    }
                }
            }
            dgContracts.ItemsSource = contractsTable.DefaultView;

            // Загружаем платежи арендатора
            string paymentsSql = @"SELECT 
                                        p.payment_date AS 'Дата платежа',
                                        p.amount AS 'Сумма (руб)',
                                        p.payment_type AS 'Способ оплаты',
                                        p.receipt_number AS 'Номер квитанции'
                                   FROM payments p
                                   JOIN contracts c ON p.id_contract = c.id_contract
                                   WHERE c.id_tenant = @tenantId
                                   ORDER BY p.payment_date DESC";

            DataTable paymentsTable = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(paymentsSql, conn))
                {
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(paymentsTable);
                    }
                }
            }
            dgPayments.ItemsSource = paymentsTable.DefaultView;
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}