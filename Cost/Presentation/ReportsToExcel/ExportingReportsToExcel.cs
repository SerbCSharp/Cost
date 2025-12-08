using Cost.Domain;
using Cost.Infrastructure.Repositories.Models;
using Cost.Infrastructure.Repositories.Models.ContractsCounterparties;
using Cost.Infrastructure.Repositories.Models.OperationsTmp;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Cost.Presentation.ReportsToExcel
{
    public class ExportingReportsToExcel
    {
        public void Cost(List<Domain.Cost> cost) // Стоимость строительства
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\Cost.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
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
            sheet.Cells[1, 11].Value = "Ставка НДС";
            sheet.Cells[1, 12].Value = "ГП";
            sheet.Cells[1, 13].Value = "ГУ";
            sheet.Cells[1, 14].Value = "Общая площадь";
            sheet.Cells[1, 15].Value = "Стоимость строительства";
            sheet.Cells[1, 16].Value = "Наименование";
            sheet.Cells[1, 17].Value = "Стоимость строительства с ∆НДС";
            sheet.Cells[1, 18].Value = "Оплата фактическая";
            sheet.Cells[1, 19].Value = "Остаток оплат до сдачи объекта";
            sheet.Cells[1, 1, 1, 19].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

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
                sheet.Cells[row, column + 17].Formula = $"O{row}*(1.2-K{row})";
                sheet.Cells[row, column + 18].Formula = $"IF(J{row}=\"Подрядчик\",F{row}-E{row}*(L{row}+M{row}),0)";
                sheet.Cells[row, column + 19].Formula = $"=IF(J{row}=\"Подрядчик\",O{row}-O{row}*(L{row}+M{row})-R{row},0)";
                //sheet.Cells[row, column + 20].Value = item.ContractId;
                //sheet.Cells[row, column + 21].Value = item.NumberAA;
                row++;
            }

            sheet.Cells[row, column + 4].Formula = $"=SUBTOTAL(9,D2:D{row - 1})";
            sheet.Cells[row, column + 5].Formula = $"=SUBTOTAL(9,E2:E{row - 1})";
            sheet.Cells[row, column + 6].Formula = $"=SUBTOTAL(9,F2:F{row - 1})";
            sheet.Cells[row, column + 15].Formula = $"=SUBTOTAL(9,O2:O{row - 1})";
            sheet.Cells[row, column + 17].Formula = $"=SUBTOTAL(9,Q2:Q{row - 1})";
            sheet.Cells[row, column + 19].Formula = $"=SUBTOTAL(9,S2:S{row - 1})";
            sheet.Cells[row, 2, row, 19].Style.Font.Bold = true;


            sheet.Cells[1, 1, row, 19].AutoFitColumns();
            sheet.Column(1).Width = 50;
            sheet.Column(2).Width = 50;
            sheet.Column(7).Width = 50;
            sheet.Column(8).Width = 50;
            sheet.Column(16).Hidden = true;
            sheet.Column(18).Hidden = true;

            var range = sheet.Cells[1, 1, row - 1, 19];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 3, row, 3].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 4, row, 6].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 11, row, 13].Style.Numberformat.Format = "0%";
            sheet.Cells[2, 14, row, 15].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 17, row, 19].Style.Numberformat.Format = "### ### ### ##0.00";

            sheet.View.FreezePanes(2, 2);

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void WeDoNotHaveTheseContracts(IEnumerable<Contracts> contracts)
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\WeDoNotHaveTheseContracts1.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets["Новые договора"];
            if (sheet == null)
                sheet = package.Workbook.Worksheets.Add("Новые договора");

            sheet.Cells.Style.Font.Name = "Times New Roman";
            sheet.Cells.Style.Font.Size = 13;

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
                row++;
            }
            sheet.Cells[1, 1, row, 7].AutoFitColumns();

            var range = sheet.Cells[1, 1, row - 1, 7];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 6, row, 6].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 7, row, 7].Style.Numberformat.Format = "### ### ### ##0.00";

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void ReconciliationStatement(List<ReconciliationStatement> reconciliationStatement)
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\Transcript1.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
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

        public void IncomeAndExpenses(List<IncomeAndExpenses> incomeAndExpenses)
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\IncomeAndExpenses1.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets["IncomeAndExpenses"];
            if (sheet == null)
                sheet = package.Workbook.Worksheets.Add("IncomeAndExpenses");

            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            // Шапка
            sheet.Cells[1, 1].Value = "Дата";
            sheet.Cells[1, 2].Value = "Выполнение";
            sheet.Cells[1, 3].Value = "Оплата";
            sheet.Cells[1, 4].Value = "Сумма НДС";
            sheet.Cells[1, 5].Value = "Сумма НДС (счет-фактура)";
            sheet.Cells[1, 6].Value = "Документ";
            sheet.Cells[1, 7].Value = "ContractId";
            sheet.Cells[1, 8].Value = "Контрагент";
            sheet.Cells[1, 9].Value = "Договор";
            sheet.Cells[1, 10].Value = "Дата договора";
            sheet.Cells[1, 11].Value = "Сумма договора";
            sheet.Cells[1, 12].Value = "Статус договора";
            sheet.Cells[1, 13].Value = "ГП";
            sheet.Cells[1, 14].Value = "ГУ";
            sheet.Cells[1, 15].Value = "Ставка НДС";
            sheet.Cells[1, 16].Value = "Литер";
            sheet.Cells[1, 17].Value = "Статья затрат";
            sheet.Cells[1, 18].Value = "Подрядчик/Поставщик";
            sheet.Cells[1, 19].Value = "Литер из оплат";
            sheet.Cells[1, 20].Value = "Статья затрат из оплат";

            sheet.Cells[1, 1, 1, 20].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 20].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in incomeAndExpenses)
            {
                sheet.Cells[row, column + 1].Value = item.Date;
                sheet.Cells[row, column + 2].Value = item.Receipt;
                sheet.Cells[row, column + 3].Value = item.Payment;
                sheet.Cells[row, column + 4].Value = item.DocumentNDSAmount;
                sheet.Cells[row, column + 5].Value = item.InvoiceReceivedNDS;
                sheet.Cells[row, column + 6].Value = item.DocumentName;
                sheet.Cells[row, column + 7].Value = item.ContractId;
                sheet.Cells[row, column + 8].Value = item.Contractor;
                sheet.Cells[row, column + 9].Value = item.Number;
                sheet.Cells[row, column + 10].Value = item.DateContract;
                sheet.Cells[row, column + 11].Value = item.SumContract;
                sheet.Cells[row, column + 12].Value = item.ContractClosed;
                sheet.Cells[row, column + 13].Value = item.GeneralContracting;
                sheet.Cells[row, column + 14].Value = item.WarrantyLien;
                sheet.Cells[row, column + 15].Value = item.RateNDS;
                sheet.Cells[row, column + 16].Value = item.ConstructionObject;
                sheet.Cells[row, column + 17].Value = item.CostItem;
                sheet.Cells[row, column + 18].Value = item.ContractorOrSupplier;
                sheet.Cells[row, column + 19].Value = item.LiterPayment;
                sheet.Cells[row, column + 20].Value = item.CostItemPayment;
                row++;
            }
            sheet.Cells[row, column + 2].Formula = $"=SUBTOTAL(9,B2:B{row - 1})";
            sheet.Cells[row, column + 3].Formula = $"=SUBTOTAL(9,C2:C{row - 1})";
            sheet.Cells[row, column + 4].Formula = $"=SUBTOTAL(9,D2:D{row - 1})";
            sheet.Cells[row, column + 5].Formula = $"=SUBTOTAL(9,E2:E{row - 1})";

            sheet.Cells[row, 2, row, 20].Style.Font.Bold = true;
            sheet.Cells[1, 1, row, 20].AutoFitColumns();
            sheet.Column(7).Hidden = true;
            sheet.Column(6).Width = 50;

            var range = sheet.Cells[1, 1, row - 1, 20];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 1, row, 1].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 2, row, 5].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 10, row, 10].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 11, row, 11].Style.Numberformat.Format = "### ### ### ##0.00";
            sheet.Cells[2, 13, row, 15].Style.Numberformat.Format = "0%";

            range.AutoFilter = true;
            sheet.View.FreezePanes(2, 1);

            package.SaveAs(new FileInfo(filePath));
        }

        public void ContractsFrom1C(List<ContractsCounterpartiesValue> Contracts) // 
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\Contracts1.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Договоры из 1С");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            // Шапка
            sheet.Cells[1, 1].Value = "Код договора из 1С";
            sheet.Cells[1, 2].Value = "Подрядчик";
            sheet.Cells[1, 3].Value = "Номер договора";
            sheet.Cells[1, 4].Value = "Номер ДС";
            sheet.Cells[1, 5].Value = "Наименование";
            sheet.Cells[1, 6].Value = "Дата договора";
            sheet.Cells[1, 7].Value = "Сумма договора";
            sheet.Cells[1, 8].Value = "Ставка НДС";
            sheet.Cells[1, 9].Value = "ГП";
            sheet.Cells[1, 10].Value = "ГУ";
            sheet.Cells[1, 11].Value = "Подрядчик/Поставщик";
            sheet.Cells[1, 12].Value = "Литер";
            sheet.Cells[1, 13].Value = "Статья затрат";
            sheet.Cells[1, 14].Value = "Комментарий";
            sheet.Cells[1, 15].Value = "Статус";
            sheet.Cells[1, 16].Value = "Тип договора";
            sheet.Cells[1, 1, 1, 16].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in Contracts)
            {
                sheet.Cells[row, column + 1].Value = item.CounterpartyAgreementId;
                sheet.Cells[row, column + 2].Value = item.Contractor;
                sheet.Cells[row, column + 3].Value = item.Number;
                sheet.Cells[row, column + 4].Value = "";
                sheet.Cells[row, column + 5].Value = item.Name;
                sheet.Cells[row, column + 6].Value = item.Date;
                sheet.Cells[row, column + 7].Value = item.Sum;
                sheet.Cells[row, column + 8].Value = item.RateNDS;
                sheet.Cells[row, column + 9].Value = item.TypeCalculation;
                sheet.Cells[row, column + 10].Value = "";
                sheet.Cells[row, column + 11].Value = "";
                sheet.Cells[row, column + 12].Value = item.ConstructionProjects;
                sheet.Cells[row, column + 13].Value = item.CostItems;
                sheet.Cells[row, column + 14].Value = "";
                sheet.Cells[row, column + 15].Value = item.ContractClosed;
                sheet.Cells[row, column + 16].Value = item.TypeAgreement;
                row++;
            }

            sheet.Cells[1, 1, row, 16].AutoFitColumns();

            var range = sheet.Cells[1, 1, row - 1, 16];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 6, row, 6].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 7, row, 7].Style.Numberformat.Format = "### ### ### ##0.00";

            sheet.View.FreezePanes(2, 2);

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void Operations(List<OperationsTmpValue> operations)
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\Operations1.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets["Бухгалтерские операции"];
            if (sheet == null)
                sheet = package.Workbook.Worksheets.Add("Бухгалтерские операции");

            sheet.Cells.Style.Font.Name = "Times New Roman";
            sheet.Cells.Style.Font.Size = 13;

            // Шапка
            sheet.Cells[1, 1].Value = "Код из 1С";
            sheet.Cells[1, 2].Value = "Номер";
            sheet.Cells[1, 3].Value = "Дата";
            sheet.Cells[1, 4].Value = "Сумма";
            sheet.Cells[1, 5].Value = "Содержание";
            sheet.Cells[1, 6].Value = "Комментарий";
            sheet.Cells[1, 1, 1, 6].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in operations)
            {
                sheet.Cells[row, column + 1].Value = item.Ref_Key;
                sheet.Cells[row, column + 2].Value = item.Number;
                sheet.Cells[row, column + 3].Value = item.Date;
                sheet.Cells[row, column + 4].Value = item.Sum;
                sheet.Cells[row, column + 5].Value = item.Content;
                sheet.Cells[row, column + 6].Value = item.Comment;
                row++;
            }
            sheet.Cells[1, 1, row, 6].AutoFitColumns();

            var range = sheet.Cells[1, 1, row - 1, 6];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 3, row, 3].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 4, row, 4].Style.Numberformat.Format = "### ### ### ##0.00";

            range.AutoFilter = true;

            package.SaveAs(new FileInfo(filePath));
        }

        public void Payments(List<LiterAndCostItemInPayments> payments) // Оплаты
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\Payments1.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Оплаты");
            sheet.Cells.Style.Font.Name = "Calibri";
            sheet.Cells.Style.Font.Size = 11;

            // Шапка
            sheet.Cells[1, 1].Value = "PaymentId";
            sheet.Cells[1, 2].Value = "Number";
            sheet.Cells[1, 3].Value = "Date";
            sheet.Cells[1, 4].Value = "Сумма";
            sheet.Cells[1, 5].Value = "NDS";
            sheet.Cells[1, 6].Value = "Литер";
            sheet.Cells[1, 7].Value = "Статья затрат";
            sheet.Cells[1, 8].Value = "PurposePayment";
            sheet.Cells[1, 9].Value = "Контрагент";
            sheet.Cells[1, 1, 1, 9].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var row = 2;
            var column = 0;
            foreach (var item in payments)
            {
                sheet.Cells[row, column + 1].Value = item.PaymentId;
                sheet.Cells[row, column + 2].Value = item.Number;
                sheet.Cells[row, column + 3].Value = item.Date;
                sheet.Cells[row, column + 4].Value = item.PaymentAmount;
                sheet.Cells[row, column + 5].Value = item.PaymentNDSAmount;
                sheet.Cells[row, column + 6].Value = item.Liter;
                sheet.Cells[row, column + 7].Value = item.CostItems;
                sheet.Cells[row, column + 8].Value = item.PurposePayment;
                sheet.Cells[row, column + 9].Value = item.Contractor;
                sheet.Cells[row, column + 10].Value = item.CostItemsInAgreement;
                sheet.Cells[row, column + 11].Value = item.ContractorOrSupplier;
                sheet.Cells[row, column + 12].Value = item.ContractId;
                row++;
            }
            sheet.Cells[row, column + 4].Formula = $"=SUBTOTAL(9,D2:D{row - 1})";
            sheet.Cells[row, 2, row, 9].Style.Font.Bold = true;
            sheet.Cells[1, 1, row, 9].AutoFitColumns();
            sheet.Column(1).Hidden = true;
            sheet.Column(6).Width = 30;
            sheet.Column(7).Width = 50;
            sheet.Column(8).Width = 50;
            sheet.Column(9).Width = 30;

            var range = sheet.Cells[1, 1, row - 1, 9];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            sheet.Cells[2, 3, row, 3].Style.Numberformat.Format = "dd.mm.yyyy";
            sheet.Cells[2, 4, row, 5].Style.Numberformat.Format = "### ### ### ##0.00";

            range.AutoFilter = true;
            sheet.View.FreezePanes(2, 1);

            package.SaveAs(new FileInfo(filePath));
        }
    }
}
