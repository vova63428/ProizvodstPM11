using System;
using System.Data;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;

namespace ArendatorPraktPm11
{
    public partial class ReportWindow : System.Windows.Window  // Явно указываем System.Windows.Window
    {
        System.Data.DataTable reportData;

        public ReportWindow(System.Data.DataTable data, string title)
        {
            InitializeComponent();
            reportData = data;
            lblTitle.Text = title;
            dgReport.ItemsSource = reportData.DefaultView;
        }

        private void btnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Excel.Application excelApp = new Excel.Application();
                Excel.Workbook workbook = excelApp.Workbooks.Add();
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.ActiveSheet;

                // Заголовок
                worksheet.Cells[1, 1] = lblTitle.Text;
                ((Excel.Range)worksheet.Cells[1, 1]).Font.Bold = true;
                ((Excel.Range)worksheet.Cells[1, 1]).Font.Size = 16;

                // Заголовки колонок
                for (int i = 0; i < reportData.Columns.Count; i++)
                {
                    worksheet.Cells[3, i + 1] = reportData.Columns[i].ColumnName;
                    ((Excel.Range)worksheet.Cells[3, i + 1]).Font.Bold = true;
                }

                // Данные
                for (int i = 0; i < reportData.Rows.Count; i++)
                {
                    for (int j = 0; j < reportData.Columns.Count; j++)
                    {
                        worksheet.Cells[i + 4, j + 1] = reportData.Rows[i][j].ToString();
                    }
                }

                worksheet.Columns.AutoFit();
                excelApp.Visible = true;

                MessageBox.Show("Отчёт открыт в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\nВозможно, Excel не установлен.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            string text = lblTitle.Text + "\n\n";
            foreach (System.Data.DataColumn col in reportData.Columns)
            {
                text += col.ColumnName + "\t";
            }
            text += "\n";
            foreach (System.Data.DataRow row in reportData.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    text += item.ToString() + "\t";
                }
                text += "\n";
            }
            Clipboard.SetText(text);
            MessageBox.Show("Отчёт скопирован в буфер обмена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}