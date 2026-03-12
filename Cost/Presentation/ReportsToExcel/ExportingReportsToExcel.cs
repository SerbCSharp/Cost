using Cost.Domain;
using Cost.Infrastructure.Repositories.Models.ActOfCompletion;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Reflection;

namespace Cost.Presentation.ReportsToExcel
{
    public class ExportingReportsToExcel
    {
        public ExportingReportsToExcel()
        {
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
        }

        public void  Browse<T>(IEnumerable<T> data) // Универсальный просмотрщик
        {
            string filePath = "C:\\Cost\\Browse.xlsx";
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Browse");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;
            sheet.View.FreezePanes(2, 1);

            var type = data.GetType().GetInterface("IEnumerable`1").GetGenericArguments()[0];
            var fields = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var countFields = fields.Length;

            // Шапка
            for (int i = 0; i < countFields; i++ )
            {
                sheet.Cells[1, i + 1].Value = fields[i].Name;
                switch (fields[i].PropertyType.Name)
                {
                    case "String":
                        sheet.Column(i + 1).Style.Numberformat.Format = "@";
                        break;
                    case "DateOnly":
                        sheet.Column(i + 1).Style.Numberformat.Format = "dd.mm.yyyy";
                        break;
                    case "Decimal":
                        sheet.Column(i + 1).Style.Numberformat.Format = "### ### ### ##0.00";
                        break;
                    default:
                        break;
                }
            }
            sheet.Cells[1, 1, 1, countFields].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, countFields].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            foreach (var item in data)
            {
                for (int i = 0; i < countFields; i++)
                {
                    sheet.Cells[row, i + 1].Value = fields[i].GetValue(item);
                }
                row++;
            }

            sheet.Cells[1, 1, row, 10].AutoFitColumns();

            var range = sheet.Cells[1, 1, row - 1, countFields];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void WeDoNotHaveTheseContracts(IEnumerable<Contracts> contracts)
        {
            string filePath = "C:\\Cost\\WeDoNotHaveTheseContracts.xlsx";
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Новые договора");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;
            sheet.View.FreezePanes(2, 1);

            // Шапка
            sheet.Cells[1, 1].Value = "Код договора из 1С";
            sheet.Cells[1, 2].Value = "Подрядчик";
            sheet.Cells[1, 3].Value = "Номер договора";
            sheet.Cells[1, 4].Value = "Номер ДС";
            sheet.Cells[1, 5].Value = "Наименование";
            sheet.Cells[1, 6].Value = "Дата договора";
            sheet.Cells[1, 7].Value = "Сумма договора";
            sheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in contracts)
            {
                sheet.Cells[row, column + 1].Value = item.ContractId;
                sheet.Cells[row, column + 2].Value = item.Contractor;
                sheet.Cells[row, column + 3].Value = item.Number;
                sheet.Cells[row, column + 4].Value = item.NumberAA;
                sheet.Cells[row, column + 5].Value = item.Name;
                sheet.Cells[row, column + 6].Value = item.Date;
                sheet.Cells[row, column + 7].Value = item.Sum;
                sheet.Cells[row, column + 8].Value = item.Code;
                row++;
            }
            sheet.Cells[1, 1, row, 7].AutoFitColumns();
            sheet.Cells[2, 6, row, 6].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 7, row, 7].Style.Numberformat.Format = "### ### ### ##0.00";

            var range = sheet.Cells[1, 1, row - 1, 7];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }







        public void Payments(IEnumerable<(Payment, Contracts)> payments) // Расходные оплаты
        {
            string filePath = "C:\\Cost\\Payments.xlsx";
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Расходные оплаты");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;
            sheet.View.FreezePanes(2, 1);

            // Шапка
            sheet.Cells[1, 1].Value = "Дата";
            sheet.Cells[1, 2].Value = "Сумма";
            sheet.Cells[1, 3].Value = "Литер";
            sheet.Cells[1, 4].Value = "Статья затрат";
            sheet.Cells[1, 5].Value = "PurposePayment";
            sheet.Cells[1, 6].Value = "Контрагент";
            sheet.Cells[1, 7].Value = "Договор";
            sheet.Cells[1, 8].Value = "ContractId";
            sheet.Cells[1, 9].Value = "Вид операции";
            sheet.Cells[1, 10].Value = "PaymentDetailsId";
            sheet.Cells[1, 11].Value = "CommentFromPaymentInvoice";
            sheet.Cells[1, 1, 1, 11].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in payments)
            {
                sheet.Cells[row, column + 1].Value = item.Item1.Date;
                sheet.Cells[row, column + 2].Value = item.Item1.PaymentAmount;
                sheet.Cells[row, column + 3].Value = item.Item1.Liter;
                sheet.Cells[row, column + 4].Value = item.Item1.CostItem;
                sheet.Cells[row, column + 5].Value = item.Item1.PaymentPurpose;
                sheet.Cells[row, column + 6].Value = item.Item2?.Contractor;
                sheet.Cells[row, column + 7].Value = item.Item2?.Number;
                sheet.Cells[row, column + 8].Value = item.Item1.ContractId;
                sheet.Cells[row, column + 9].Value = item.Item1.TypeOperation;
                sheet.Cells[row, column + 10].Value = item.Item1.PaymentDetailsId;
                sheet.Cells[row, column + 11].Value = item.Item1.CommentFromPaymentInvoice;
                row++;
            }
            sheet.Cells[1, 1, row, 11].AutoFitColumns();
            sheet.Cells[2, 1, row, 1].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 2, row, 2].Style.Numberformat.Format = "### ### ### ##0.00";

            var range = sheet.Cells[1, 1, row - 1, 11];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }













        //public void IncomeAndExpenses(IEnumerable<IncomeAndExpenses> incomeAndExpenses)
        //{
        //    string filePath = "C:\\Cost\\IncomeAndExpenses.xlsx";
        //    using var package = new ExcelPackage();

        //    var sheet = package.Workbook.Worksheets["IncomeAndExpenses"];
        //    if (sheet == null)
        //        sheet = package.Workbook.Worksheets.Add("IncomeAndExpenses");

        //    sheet.Cells.Style.Font.Name = "Calibri";
        //    sheet.Cells.Style.Font.Size = 11;

        //    // Шапка
        //    sheet.Cells[1, 1].Value = "Дата";
        //    sheet.Cells[1, 2].Value = "Выполнение";
        //    sheet.Cells[1, 3].Value = "Оплата";
        //    sheet.Cells[1, 4].Value = "Документ";
        //    sheet.Cells[1, 5].Value = "ContractId";
        //    //sheet.Cells[1, 6].Value = "Контрагент";
        //    //sheet.Cells[1, 7].Value = "Договор";
        //    //sheet.Cells[1, 8].Value = "ГП";
        //    //sheet.Cells[1, 9].Value = "ГУ";
        //    //sheet.Cells[1, 10].Value = "Ставка НДС";
        //    //sheet.Cells[1, 11].Value = "Расчетные ГП";
        //    //sheet.Cells[1, 12].Value = "Расчетная НДС";
        //    //sheet.Cells[1, 13].Value = "НДС к уплате (расчетный)";
        //    //sheet.Cells[1, 14].Value = "DocumentNDSAmount";
        //    //sheet.Cells[1, 15].Value = "InvoiceReceivedNDS";
        //    sheet.Cells[1, 16].Value = "Вид операции";
        //    //sheet.Cells[1, 17].Value = "Направление";
        //    sheet.Cells[1, 1, 1, 17].Style.Font.Bold = true;
        //    sheet.Cells[1, 1, 1, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        //    var row = 2;
        //    var column = 0;
        //    foreach (var item in incomeAndExpenses)
        //    {
        //        sheet.Cells[row, column + 1].Value = item.Date;
        //        sheet.Cells[row, column + 2].Value = item.Credit;
        //        sheet.Cells[row, column + 3].Value = item.Debit;
        //        sheet.Cells[row, column + 4].Value = item.DocumentName;
        //        sheet.Cells[row, column + 5].Value = item.ContractId;
        //        //sheet.Cells[row, column + 6].Value = item.Contractor;
        //        //sheet.Cells[row, column + 7].Value = item.Number;
        //        //sheet.Cells[row, column + 8].Value = item.GeneralContracting;
        //        //sheet.Cells[row, column + 9].Value = item.WarrantyLien;
        //        //sheet.Cells[row, column + 10].Value = item.RateNDS;
        //        //sheet.Cells[row, column + 11].Formula = $"IF(OR(D{row}=\"Поступление товаров и услуг\",D{row}=\"Поступление из переработки\"),B{row}*H{row},0)";
        //        //sheet.Cells[row, column + 12].Formula = $"IF(OR(D{row}=\"Поступление товаров и услуг\",D{row}=\"Поступление из переработки\"),B{row}*J{row}/(1+J{row}),0)";
        //        //sheet.Cells[row, column + 13].Formula = $"IF(OR(D{row}=\"Поступление товаров и услуг\",D{row}=\"Поступление из переработки\"),B{row}*(0.2-J{row}),0)";
        //        //sheet.Cells[row, column + 14].Value = item.DocumentNDSAmount;
        //        //sheet.Cells[row, column + 15].Value = item.InvoiceReceivedNDS;
        //        sheet.Cells[row, column + 16].Value = item.TypeOperation;
        //        //sheet.Cells[row, column + 17].Value = item.AreaOfActivity;
        //        row++;
        //    }
        //    //sheet.Cells[row, column + 2].Formula = $"=SUBTOTAL(9,B2:B{row - 1})";
        //    //sheet.Cells[row, column + 3].Formula = $"=SUBTOTAL(9,C2:C{row - 1})";
        //    //sheet.Cells[row, column + 11].Formula = $"=SUBTOTAL(9,K2:K{row - 1})";
        //    //sheet.Cells[row, column + 12].Formula = $"=SUBTOTAL(9,L2:L{row - 1})";
        //    //sheet.Cells[row, column + 13].Formula = $"=SUBTOTAL(9,M2:M{row - 1})";

        //    sheet.Cells[row, 2, row, 13].Style.Font.Bold = true;
        //    sheet.Cells[1, 1, row, 17].AutoFitColumns();
        //    sheet.Column(5).Hidden = true;
        //    sheet.Column(6).Width = 50;

        //    var range = sheet.Cells[1, 1, row - 1, 17];
        //    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        //    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        //    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        //    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

        //    sheet.Cells[2, 1, row, 1].Style.Numberformat.Format = "dd.mm.yyyy";
        //    sheet.Cells[2, 2, row, 3].Style.Numberformat.Format = "### ### ### ##0.00";
        //    sheet.Cells[2, 8, row, 10].Style.Numberformat.Format = "0%";
        //    //sheet.Cells[2, 11, row, 13].Style.Numberformat.Format = "### ### ### ##0.00";

        //    range.AutoFilter = true;
        //    sheet.View.FreezePanes(2, 1);

        //    package.SaveAs(new FileInfo(filePath));
        //}












        public void CurrentDebt(IEnumerable<Domain.Cost> cost) // Текущая задолженность
        {
            string filePath = "C:\\Cost\\CurrentDebt.xlsx";
            using var package = new ExcelPackage();

            var pivot = package.Workbook.Worksheets.Add("Текущая задолженность");
            var sheet = package.Workbook.Worksheets.Add("Data");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            sheet.View.FreezePanes(2, 1);

            // Шапка
            sheet.Cells[1, 1].Value = "ResidentialComplex";
            sheet.Cells[1, 2].Value = "ConstructionObject";
            sheet.Cells[1, 3].Value = "ContractorOrSupplier";
            sheet.Cells[1, 4].Value = "CostItem";
            sheet.Cells[1, 5].Value = "Contract";
            sheet.Cells[1, 6].Value = "ContractDate";
            sheet.Cells[1, 7].Value = "ContractAmount";
            sheet.Cells[1, 8].Value = "Receipt";
            sheet.Cells[1, 9].Value = "Payment";
            sheet.Cells[1, 10].Value = "CurrentDebt";
            sheet.Cells[1, 1, 1, 10].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            sheet.Columns[1, 5].Style.Numberformat.Format = "@";
            sheet.Column(6).Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Columns[7, 10].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Column(2).Width = 40;
            sheet.Column(4).Width = 80;

            var row = 2;
            var column = 0;
            foreach (var item in cost)
            {
                sheet.Cells[row, column + 1].Value = item.ResidentialComplex;
                sheet.Cells[row, column + 2].Value = item.ConstructionObject;
                sheet.Cells[row, column + 3].Value = item.ContractorOrSupplier;
                sheet.Cells[row, column + 4].Value = item.CostItem;
                sheet.Cells[row, column + 5].Value = item.Number;
                sheet.Cells[row, column + 6].Value = item.Date;
                sheet.Cells[row, column + 7].Value = item.Sum;
                sheet.Cells[row, column + 8].Value = item.Receipt;
                sheet.Cells[row, column + 9].Value = item.Payment;
                sheet.Cells[row, column + 10].Value = item.CurrentDebt;
                row++;
            }

            sheet.Cells[row, 2, row, 10].Style.Font.Bold = true;
            sheet.Cells[1, 1, row, 10].AutoFitColumns();

            var range = sheet.Cells[1, 1, row - 1, 10];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            range.AutoFilter = true;

            var customPivotTableStyle = pivot.Workbook.Styles.CreatePivotTableStyle("CurrentDebtStyle");
            customPivotTableStyle.HeaderRow.Style.Font.Bold = true;
            customPivotTableStyle.TotalRow.Style.Font.Bold = true;

            // Создание сводной таблицы
            var pivotTable = pivot.PivotTables.Add(pivot.Cells["A1"], range, "CurrentDebt");
            pivotTable.StyleName = "CurrentDebtStyle";
            pivotTable.ShowHeaders = false;
            pivotTable.ShowRowHeaders = false;
            pivotTable.DataOnRows = false;
            // pivotTable.RowGrandTotals = false;

            var styleWholeTable = pivotTable.Styles.AddWholeTable();
            styleWholeTable.Style.Font.Name = "Calibri";
            styleWholeTable.Style.Font.Size = 11;
            styleWholeTable.Style.NumberFormat.Format = "### ### ### ##0.00";
            styleWholeTable.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            styleWholeTable.Style.Border.Horizontal.Style = ExcelBorderStyle.Thin;
            styleWholeTable.Style.Border.Vertical.Style = ExcelBorderStyle.Thin;

            pivotTable.RowFields.Add(pivotTable.Fields["ResidentialComplex"]);
            pivotTable.RowFields.Add(pivotTable.Fields["ConstructionObject"]);
            pivotTable.RowFields.Add(pivotTable.Fields["ContractorOrSupplier"]);
            pivotTable.RowFields.Add(pivotTable.Fields["CostItem"]);
            pivotTable.RowFields.Add(pivotTable.Fields["Contract"]);
            pivotTable.DataFields.Add(pivotTable.Fields["ContractAmount"]);
            pivotTable.DataFields.Add(pivotTable.Fields["Receipt"]);
            pivotTable.DataFields.Add(pivotTable.Fields["Payment"]);
            pivotTable.DataFields.Add(pivotTable.Fields["CurrentDebt"]);

            pivotTable.DataFields[0].Name = "       Сумма договора      ";
            pivotTable.DataFields[1].Name = "         Выполнение        ";
            pivotTable.DataFields[2].Name = "           Оплата          ";
            pivotTable.DataFields[3].Name = "   Текущая задолженность   ";

            pivotTable.RowFields[0].Items.ShowDetails(false);
            pivotTable.RowFields[1].Items.ShowDetails(false);
            pivotTable.RowFields[2].Items.ShowDetails(false);
            pivotTable.RowFields[3].Items.ShowDetails(false);

            package.SaveAs(new FileInfo(filePath));
        }

        public void Cost(List<Domain.Cost> cost) // Стоимость строительства
        {
            string filePath = "C:\\Cost\\Cost.xlsx";
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Прямые затраты");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            // Шапка
            sheet.Cells[1, 1].Value = "Контрагент";
            sheet.Cells[1, 2].Value = "Договор";
            sheet.Cells[1, 3].Value = "Дата договора";
            sheet.Cells[1, 4].Value = "Сумма договора";
            sheet.Cells[1, 5].Value = "Выполнение";
            sheet.Cells[1, 6].Value = "Оплата";
            sheet.Cells[1, 7].Value = "Литер";
            sheet.Cells[1, 8].Value = "Статья затрат";
            sheet.Cells[1, 9].Value = "Статус договора";
            sheet.Cells[1, 10].Value = "Подрядчик/Поставщик";
            sheet.Cells[1, 11].Value = "НДС до 2026";
            sheet.Cells[1, 12].Value = "ГП";
            sheet.Cells[1, 13].Value = "ГУ";
            sheet.Cells[1, 14].Value = "Общая площадь";
            sheet.Cells[1, 15].Value = "Стоимость строительства";
            sheet.Cells[1, 16].Value = "Наименование";
            sheet.Cells[1, 17].Value = "Стоимость строительства с ∆НДС";
            sheet.Cells[1, 18].Value = "Оплата фактическая";
            sheet.Cells[1, 19].Value = "Остаток оплат до сдачи объекта";
            sheet.Cells[1, 20].Value = "ContractId";
            sheet.Cells[1, 21].Value = "Выполнение до 2026";
            sheet.Cells[1, 22].Value = "НДС с 2026";
            sheet.Cells[1, 23].Value = "Год оплаты";
            sheet.Cells[1, 24].Value = "Входящий НДС";
            sheet.Cells[1, 25].Value = "Выполнение за вычетом ГП и НДС";
            sheet.Cells[1, 1, 1, 25].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 25].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in cost)
            {
                sheet.Cells[row, column + 1].Value = item.Contractor;
                sheet.Cells[row, column + 2].Value = item.Number;
                sheet.Cells[row, column + 3].Value = item.Date;
                sheet.Cells[row, column + 4].Value = item.Sum;
                sheet.Cells[row, column + 5].Value = item.Receipt;
                sheet.Cells[row, column + 6].Value = item.Payment;
                sheet.Cells[row, column + 7].Value = item.ConstructionObject;
                sheet.Cells[row, column + 8].Value = item.CostItem;
                sheet.Cells[row, column + 9].Value = item.ContractClosed;
                sheet.Cells[row, column + 10].Value = item.ContractorOrSupplier;
                sheet.Cells[row, column + 11].Value = item.RateNDS;
                sheet.Cells[row, column + 12].Value = item.GeneralContracting;
                sheet.Cells[row, column + 13].Value = item.WarrantyLien;
                sheet.Cells[row, column + 14].Value = item.TotalArea;
                sheet.Cells[row, column + 15].Value = item.ConstructionCost;
                sheet.Cells[row, column + 16].Value = item.Name;
                sheet.Cells[row, column + 17].Value = item.ConstructionCostNDS;
                sheet.Cells[row, column + 18].Formula = $"IF(J{row}=\"Подрядчик\",F{row}-E{row}*(L{row}+M{row}),0)";
                sheet.Cells[row, column + 19].Formula = $"=IF(J{row}=\"Подрядчик\",O{row}-O{row}*(L{row}+M{row})-R{row},0)";
                sheet.Cells[row, column + 20].Value = item.ContractId;
                sheet.Cells[row, column + 21].Value = item.AmountUntil2026;
                sheet.Cells[row, column + 22].Value = item.RateNDS2026;
                sheet.Cells[row, column + 23].Value = item.Year;
                sheet.Cells[row, column + 24].Value = item.InputNDS;
                sheet.Cells[row, column + 25].Value = item.Expenses;
                row++;
            }

            sheet.Cells[row, column + 4].Formula = $"=SUBTOTAL(9,D2:D{row - 1})";
            sheet.Cells[row, column + 5].Formula = $"=SUBTOTAL(9,E2:E{row - 1})";
            sheet.Cells[row, column + 6].Formula = $"=SUBTOTAL(9,F2:F{row - 1})";
            sheet.Cells[row, column + 15].Formula = $"=SUBTOTAL(9,O2:O{row - 1})";
            sheet.Cells[row, column + 17].Formula = $"=SUBTOTAL(9,Q2:Q{row - 1})";
            sheet.Cells[row, column + 19].Formula = $"=SUBTOTAL(9,S2:S{row - 1})";
            sheet.Cells[row, column + 24].Formula = $"=SUBTOTAL(9,X2:X{row - 1})";
            sheet.Cells[row, column + 25].Formula = $"=SUBTOTAL(9,Y2:Y{row - 1})";
            sheet.Cells[row, 2, row, 25].Style.Font.Bold = true;


            sheet.Cells[1, 1, row, 25].AutoFitColumns();
            sheet.Column(1).Width = 50;
            sheet.Column(2).Width = 50;
            sheet.Column(7).Width = 50;
            sheet.Column(8).Width = 50;
            sheet.Column(16).Hidden = true;
            sheet.Column(18).Hidden = true;
            sheet.Column(20).Hidden = true;
            sheet.Column(23).Hidden = true;

            var range = sheet.Cells[1, 1, row - 1, 25];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 3, row, 3].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 4, row, 6].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 11, row, 13].Style.Numberformat.Format = "0%";
            sheet.Cells[2, 14, row, 15].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 17, row, 19].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 21, row, 21].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 22, row, 22].Style.Numberformat.Format = "0%";
            sheet.Cells[2, 24, row, 25].Style.Numberformat.Format = "### ### ### ##0.00";

            sheet.View.FreezePanes(2, 1);

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void Income(List<Income> income) // Доходы от строительства объектов
        {
            string filePath = "C:\\Cost\\Income.xlsx";
            //ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Маржинальный доход");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            // Шапка
            sheet.Cells[1, 1].Value = "Контрагент";
            sheet.Cells[1, 2].Value = "Договор";
            sheet.Cells[1, 3].Value = "Дата договора";
            sheet.Cells[1, 4].Value = "Сумма договора";
            sheet.Cells[1, 5].Value = "Выполнение";
            sheet.Cells[1, 6].Value = "Оплата";
            sheet.Cells[1, 7].Value = "Литер";
            sheet.Cells[1, 8].Value = "Наименование";
            sheet.Cells[1, 9].Value = "Исходящий НДС";
            sheet.Cells[1, 10].Value = "Выполнение без НДС";
            sheet.Cells[1, 11].Value = "ContractId";
            sheet.Cells[1, 12].Value = "Выполнение до 2026";
            sheet.Cells[1, 1, 1, 12].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in income)
            {
                sheet.Cells[row, column + 1].Value = item.Contractor;
                sheet.Cells[row, column + 2].Value = item.Number;
                sheet.Cells[row, column + 3].Value = item.Date;
                sheet.Cells[row, column + 4].Value = item.Sum;
                sheet.Cells[row, column + 5].Value = item.Receipt;
                sheet.Cells[row, column + 6].Value = item.Payment;
                sheet.Cells[row, column + 7].Value = item.ConstructionObject;
                sheet.Cells[row, column + 8].Value = item.Name;
                sheet.Cells[row, column + 9].Value = item.OutgoingNDS;
                sheet.Cells[row, column + 10].Formula = $"E{row}-I{row}";
                sheet.Cells[row, column + 11].Value = item.ContractId;
                sheet.Cells[row, column + 12].Value = item.AmountUntil2026;
                row++;
            }

            sheet.Cells[row, column + 4].Formula = $"=SUBTOTAL(9,D2:D{row - 1})";
            sheet.Cells[row, column + 5].Formula = $"=SUBTOTAL(9,E2:E{row - 1})";
            sheet.Cells[row, column + 6].Formula = $"=SUBTOTAL(9,F2:F{row - 1})";
            sheet.Cells[row, column + 9].Formula = $"=SUBTOTAL(9,I2:I{row - 1})";
            sheet.Cells[row, column + 10].Formula = $"=SUBTOTAL(9,J2:J{row - 1})";
            sheet.Cells[row, 2, row, 10].Style.Font.Bold = true;


            sheet.Cells[1, 1, row, 17].AutoFitColumns();
            sheet.Column(1).Width = 50;
            sheet.Column(2).Width = 50;
            sheet.Column(7).Width = 50;
            sheet.Column(8).Width = 50;
            sheet.Column(8).Hidden = true;
            sheet.Column(11).Hidden = true;

            var range = sheet.Cells[1, 1, row - 1, 12];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 3, row, 3].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 4, row, 6].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 9, row, 9].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 10, row, 10].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 12, row, 12].Style.Numberformat.Format = "### ### ### ##0.00";

            sheet.View.FreezePanes(2, 1);

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void ReconciliationStatement(List<ReconciliationStatement> reconciliationStatement)
        {
            string filePath = "C:\\Cost\\Transcript.xlsx";
            //ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets["Акт сверки по договору"];
            if (sheet == null)
                sheet = package.Workbook.Worksheets.Add("Акт сверки по договору");

            sheet.Cells.Style.Font.Name = "Times New Roman";
            sheet.Cells.Style.Font.Size = 13;

            var contract = reconciliationStatement.FirstOrDefault();

            // Шапка
            sheet.Cells[1, 1].Value = "Подрядчик:";
            sheet.Cells[2, 1].Value = "Договор:";
            sheet.Cells[3, 1].Value = "Сумма договора:";
            sheet.Cells[1, 2].Value = contract.Contractor;
            sheet.Cells[2, 2].Value = contract.Name;
            sheet.Cells[3, 2].Value = contract.Sum;
            sheet.Cells[3, 2, 3, 2].Style.Numberformat.Format = "### ### ### ##0.00";


            sheet.Cells[5, 1].Value = "Дата";
            sheet.Cells[5, 2].Value = "Дебет";
            sheet.Cells[5, 3].Value = "Кредит";
            sheet.Cells[5, 4].Value = "Документ";
            sheet.Cells[1, 1, 5, 4].Style.Font.Bold = true;
            sheet.Cells[5, 1, 5, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 6;
            var column = 0;
            foreach (var item in reconciliationStatement)
            {
                sheet.Cells[row, column + 1].Value = item.Date;
                sheet.Cells[row, column + 2].Value = item.Debit;
                sheet.Cells[row, column + 3].Value = item.Credit;
                sheet.Cells[row, column + 4].Value = item.DocumentName;
                row++;
            }
            sheet.Cells[row, column + 2].Formula = $"=SUM(B6:B{row-1})";
            sheet.Cells[row, column + 3].Formula = $"=SUM(C6:C{row - 1})";
            sheet.Cells[row, 2, row, 3].Style.Font.Bold = true;
            sheet.Cells[1, 1, row, 4].AutoFitColumns();
            sheet.Column(2).Width = 20;

            var range = sheet.Cells[5, 1, row - 1, 4];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[6, 1, row, 1].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[6, 2, row, 4].Style.Numberformat.Format = "### ### ### ##0.00";

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void CashFlow(List<CashFlow> cashFlow) // ДДС
        {
            string filePath = "C:\\Cost\\CashFlow.xlsx";
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("ДДС");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            var head = cashFlow.FirstOrDefault();

            // Шапка
            sheet.Cells[1, 1, 1, 4].Merge = true;
            sheet.Cells[1, 1].Value = $"ДДС по направлениям ({head.Organization})";
            sheet.Cells[1, 1].Style.Font.Size = 20;
            sheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            sheet.Cells[2, 2, 2, 4].Merge = true;
            sheet.Cells[2, 2].Value = $"с {head.StartDate.ToShortDateString()} по {head.EndDate.ToShortDateString()}";
            sheet.Cells[2, 2].Style.Font.Size = 16;
            sheet.Cells[2, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            sheet.Cells[4, 1, 6, 4].Style.Font.Bold = true;
            sheet.Cells[4, 2, 4, 3].Merge = true;
            sheet.Cells[4, 2].Value = "Сальдо на начало:";
            sheet.Cells[4, 2, 4, 4].Style.Font.Size = 12;
            sheet.Cells[4, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[4, 4].Value = head.StartBalance;
            sheet.Cells[4, 4].Style.Numberformat.Format = "### ### ### ##0.00";

            sheet.Cells[6, 1, 6, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[6, 1].Value = "Направления";
            sheet.Cells[6, 2].Value = "Поступления";
            sheet.Cells[6, 3].Value = "Выплаты";
            sheet.Cells[6, 4].Value = "Сальдо";

            var row = 7;
            var column = 0;
            foreach (var item in cashFlow)
            {
                sheet.Cells[row, column + 1].Value = item.AreaOfActivity;
                sheet.Cells[row, column + 2].Value = item.Receipt;
                sheet.Cells[row, column + 3].Value = item.Payment;
                sheet.Cells[row, column + 4].Formula = $"B{row}-C{row}";
                row++;
            }
            sheet.Cells[row, column + 2].Formula = $"=SUBTOTAL(9,B6:B{row - 1})";
            sheet.Cells[row, column + 3].Formula = $"=SUBTOTAL(9,C6:C{row - 1})";
            sheet.Cells[row, column + 4].Formula = $"=SUBTOTAL(9,D6:D{row - 1})";
            sheet.Cells[row, 2, row, 4].Style.Font.Bold = true;

            sheet.Cells[1, 1, row, 4].AutoFitColumns();
            sheet.Cells[7, 2, row, 4].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Column(4).Width = 15;

            var range = sheet.Cells[6, 1, row - 1, 4];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            range.AutoFilter = true;

            sheet.Cells[row + 2, 1, row + 2, 4].Style.Font.Bold = true;
            sheet.Cells[row + 2, 2, row + 2, 3].Merge = true;
            sheet.Cells[row + 2, 2].Value = "Сальдо на конец:";
            sheet.Cells[row + 2, 2, row + 2, 4].Style.Font.Size = 12;
            sheet.Cells[row + 2, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[row + 2, 4].Formula = $"=SUBTOTAL(9,D7:D{row - 1})+D4";
            sheet.Cells[row + 2, 4].Style.Numberformat.Format = "### ### ### ##0.00";

            package.SaveAs(new FileInfo(filePath));
        }

        public void ActOfCompletion(IEnumerable<ActOfCompletionValue> cost) // Акты об окончании СМР
        {
            string filePath = "C:\\Cost\\ActOfCompletion.xlsx";
            //ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Акты об окончании СМР");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            // Шапка
            sheet.Cells[1, 1].Value = "ContractId";
            sheet.Cells[1, 2].Value = "StartDate";
            sheet.Cells[1, 3].Value = "EndDate";
            sheet.Cells[1, 4].Value = "Comment";
            sheet.Cells[1, 1, 1, 4].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in cost)
            {
                sheet.Cells[row, column + 1].Value = item.ContractId;
                sheet.Cells[row, column + 2].Value = item.StartDate;
                sheet.Cells[row, column + 3].Value = item.EndDate;
                sheet.Cells[row, column + 4].Value = item.Comment;
                row++;
            }

            sheet.Cells[row, 2, row, 4].Style.Font.Bold = true;
            sheet.Cells[1, 1, row, 4].AutoFitColumns();

            var range = sheet.Cells[1, 1, row - 1, 4];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 2, row, 3].Style.Numberformat.Format = "dd.mm.yyyy";

            sheet.View.FreezePanes(2, 1);

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }
    }
}
