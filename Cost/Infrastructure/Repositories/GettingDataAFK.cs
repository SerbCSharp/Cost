using Cost.Application;
using Cost.Domain;
using Cost.Infrastructure.Repositories.Models;
using Cost.Infrastructure.Repositories.Models.AdditionalInformation;
using Cost.Infrastructure.Repositories.Models.BillPayment;
using Cost.Infrastructure.Repositories.Models.ConstructionProjects;
using Cost.Infrastructure.Repositories.Models.ContractsCounterparties;
using Cost.Infrastructure.Repositories.Models.CostItems;
using Cost.Infrastructure.Repositories.Models.Counterparties;
using Cost.Infrastructure.Repositories.Models.DebtAdjustment;
using Cost.Infrastructure.Repositories.Models.InvoiceReceived;
using Cost.Infrastructure.Repositories.Models.NomenclatureGroups;
using Cost.Infrastructure.Repositories.Models.OperationsTmp;
using Cost.Infrastructure.Repositories.Models.Payments;
using Cost.Infrastructure.Repositories.Models.Receipts;
using Cost.Infrastructure.Repositories.Models.ReceiptToCurrentAccount;
using Cost.Infrastructure.Repositories.Models.Selling;
using Cost.Infrastructure.Repositories.Models.TypesCalculations;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using System.Data;
using System.Net.Http.Headers;
using System.Text;

namespace Cost.Infrastructure.Repositories
{
    public class GettingDataAFK : IGettingData
    {
        private readonly HttpClient httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Base1CConfiguration _base1CConfiguration;
        public GettingDataAFK(IOptions<Base1CConfiguration> base1CConfiguration, IHttpClientFactory httpClientFactory)
        {
            _base1CConfiguration = base1CConfiguration.Value;
            string username = _base1CConfiguration.Username;
            string password = _base1CConfiguration.Password;
            string credentials = $"{username}:{password}";
            byte[] byteArray = Encoding.ASCII.GetBytes(credentials);
            string base64Credentials = Convert.ToBase64String(byteArray);
            _httpClientFactory = httpClientFactory;
            httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);
        }

        public async Task<Counterparties> CounterpartiesAsync() // Контрагенты
        {
            var counterpartiesUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Catalog_Контрагенты?$format=json&$select=Ref_Key,Description,Parent_Key";
            using HttpResponseMessage counterpartiesResponse = await httpClient.GetAsync(counterpartiesUrl);
            return await counterpartiesResponse.Content.ReadFromJsonAsync<Counterparties>();
        }

        public async Task<ContractsCounterparties> ContractsCounterpartiesAsync() // Договоры контрагентов
        {
            var contractsCounterpartiesUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Catalog_ДоговорыКонтрагентов?$format=json";
            using HttpResponseMessage contractsCounterpartiesResponse = await httpClient.GetAsync(contractsCounterpartiesUrl);
            return await contractsCounterpartiesResponse.Content.ReadFromJsonAsync<ContractsCounterparties>();
        }

        public async Task<Receipts> ReceiptGoodsServicesAsync() // Поступление товаров и услуг
        {
            var receiptsUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_ПоступлениеТоваровУслуг?$format=json&$select=Ref_Key,Date,Posted,СуммаДокумента,ДоговорКонтрагента_Key";
            using HttpResponseMessage receiptsResponse = await httpClient.GetAsync(receiptsUrl);
            return await receiptsResponse.Content.ReadFromJsonAsync<Receipts>();
        }

        public async Task<Payments> PaymentsAsync() // Списание с расчетного счета
        {
            var paymentsUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_СписаниеСРасчетногоСчета?$format=json&$select=Ref_Key,Date,Posted,СуммаДокумента,ДоговорКонтрагента_Key,DeletionMark,РасшифровкаПлатежа";
            using HttpResponseMessage paymentsResponse = await httpClient.GetAsync(paymentsUrl);
            return await paymentsResponse.Content.ReadFromJsonAsync<Payments>();
        }

        public async Task<ReceiptToCurrentAccount> ReceiptToCurrentAccountAsync() // Поступление на расчетный счет
        {
            var receiptToCurrentAccountUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_ПоступлениеНаРасчетныйСчет?$format=json";
            using HttpResponseMessage receiptToCurrentAccountResponse = await httpClient.GetAsync(receiptToCurrentAccountUrl);
            return await receiptToCurrentAccountResponse.Content.ReadFromJsonAsync<ReceiptToCurrentAccount>();
        }

        public async Task<NomenclatureGroups> NomenclatureGroupsAsync() // Номенклатурные группы
        {
            var nomenclatureGroupsUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Catalog_НоменклатурныеГруппы?$format=json";
            using HttpResponseMessage nomenclatureGroupsResponse = await httpClient.GetAsync(nomenclatureGroupsUrl);
            return await nomenclatureGroupsResponse.Content.ReadFromJsonAsync<NomenclatureGroups>();
        }

        public async Task<ConstructionProjects> ConstructionProjectsAsync() // Объекты Строительства
        {
            var constructionProjectsUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Catalog_ОбъектыСтроительства?$format=json";
            using HttpResponseMessage constructionProjectsResponse = await httpClient.GetAsync(constructionProjectsUrl);
            return await constructionProjectsResponse.Content.ReadFromJsonAsync<ConstructionProjects>();
        }

        public async Task<CostItems> CostItemsAsync() // Статьи затрат
        {
            var costItemsUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Catalog_СтатьиЗатрат?$format=json";
            using HttpResponseMessage costItemsResponse = await httpClient.GetAsync(costItemsUrl);
            return await costItemsResponse.Content.ReadFromJsonAsync<CostItems>();
        }

        public async Task<TypesCalculations> TypesCalculationsAsync() // Виды взаиморасчетов
        {
            var typesCalculationsUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Catalog_ВидыВзаиморасчетов?$format=json";
            using HttpResponseMessage typesCalculationsResponse = await httpClient.GetAsync(typesCalculationsUrl);
            return await typesCalculationsResponse.Content.ReadFromJsonAsync<TypesCalculations>();
        }

        public async Task<DebtAdjustment> DebtAdjustmentAsync() // Корректировка долга
        {
            var debtAdjustmentUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_КорректировкаДолга?$format=json";
            using HttpResponseMessage debtAdjustmentResponse = await httpClient.GetAsync(debtAdjustmentUrl);
            return await debtAdjustmentResponse.Content.ReadFromJsonAsync<DebtAdjustment>();
        }

        public async Task<Receipts> ReceiptProcessingAsync() // Поступление из переработки
        {
            var receiptsUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_ПоступлениеИзПереработки?$format=json";
            using HttpResponseMessage receiptsResponse = await httpClient.GetAsync(receiptsUrl);
            return await receiptsResponse.Content.ReadFromJsonAsync<Receipts>();
        }

        public async Task<Selling> SellingAsync() // Реализация
        {
            var sellingUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_РеализацияТоваровУслуг?$format=json";
            using HttpResponseMessage sellingResponse = await httpClient.GetAsync(sellingUrl);
            return await sellingResponse.Content.ReadFromJsonAsync<Selling>();
        }

        public async Task<AdditionalInformation> AdditionalInformationAsync() // Дополнительные сведения
        {
            var additionalInformationUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/InformationRegister_ДополнительныеСведения?$format=json";
            using HttpResponseMessage additionalInformationResponse = await httpClient.GetAsync(additionalInformationUrl);
            return await additionalInformationResponse.Content.ReadFromJsonAsync<AdditionalInformation>();
        }

        public async Task<InvoiceReceived> InvoiceReceivedAsync() // Счета-фактуры полученные
        {
            var invoiceReceivedUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_СчетФактураПолученный?$format=json";
            using HttpResponseMessage invoiceReceivedResponse = await httpClient.GetAsync(invoiceReceivedUrl);
            return await invoiceReceivedResponse.Content.ReadFromJsonAsync<InvoiceReceived>();
        }

        public List<Facility> GetFacility() // Объекты строительства
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\AFKDevelopment\\Catalogs.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            FileInfo fileInfo = new FileInfo(filePath);
            using var package = new ExcelPackage(fileInfo);
            var sheet = package.Workbook.Worksheets[Name: "Objects"];
            DataTable dataTable = new DataTable();

            for (int i = sheet.Dimension.Start.Column; i <= sheet.Dimension.End.Column; i++)
            {
                if (sheet.Cells[1, i].Value.ToString() == "TotalArea")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(decimal));
                else
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString());
            }

            for (int i = 2; i <= sheet.Dimension.End.Row; i++)
            {
                DataRow dataRow = dataTable.NewRow();
                for (int j = 1; j <= sheet.Dimension.End.Column; j++)
                {
                    dataRow[j - 1] = sheet.Cells[i, j].Value;
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable.AsEnumerable().Select(row => new Facility
            {
                Liter = row.Field<string>("Liter"),
                Name = row.Field<string>("Name"),
                ObjectNameIn1C = row.Field<string>("ObjectNameIn1C"),
                TotalArea = row.Field<decimal>("TotalArea")
            }).ToList();
        }

        public List<Contracts> GetContracts() // Договора
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\AFK\\Catalogs.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            FileInfo fileInfo = new FileInfo(filePath);
            using var package = new ExcelPackage(fileInfo);
            var sheet = package.Workbook.Worksheets[Name: "Contracts"];
            DataTable dataTable = new DataTable();

            for (int i = sheet.Dimension.Start.Column; i <= sheet.Dimension.End.Column; i++)
            {
                if (sheet.Cells[1, i].Value.ToString() == "Дата договора")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(DateTime));
                else if (sheet.Cells[1, i].Value.ToString() == "Сумма договора")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(decimal));
                else if (sheet.Cells[1, i].Value.ToString() == "ГП")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(decimal));
                else if (sheet.Cells[1, i].Value.ToString() == "ГУ")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(decimal));
                else if (sheet.Cells[1, i].Value.ToString() == "Ставка НДС")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(decimal));
                else
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString());
            }

            for (int i = 2; i <= sheet.Dimension.End.Row; i++)
            {
                DataRow dataRow = dataTable.NewRow();
                for (int j = 1; j <= sheet.Dimension.End.Column; j++)
                {
                    dataRow[j - 1] = sheet.Cells[i, j].Value;
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable.AsEnumerable().Select(row => new Contracts
            {
                ContractId = row.Field<string>("Код договора из 1С"),
                Contractor = row.Field<string>("Подрядчик"),
                Number = row.Field<string>("Номер договора"),
                NumberAA = row.Field<string>("Номер ДС"),
                Date = row.Field<DateTime>("Дата договора"),
                Sum = row.Field<decimal>("Сумма договора"),
                RateNDS = row.Field<decimal>("Ставка НДС"),
                GeneralContracting = row.Field<decimal>("ГП"),
                WarrantyLien = row.Field<decimal>("ГУ"),
                ContractorOrSupplier = row.Field<string>("Подрядчик/Поставщик"),
                ConstructionObject = row.Field<string>("Литер"),
                CostItem = row.Field<string>("Статья затрат"),
                Name = row.Field<string>("Наименование"),
                ContractClosed = row.Field<string>("Статус")
            }).ToList();
        }

        public List<Operations> GetOperations() // Бухгалтерские операции
        {
            string filePath = "\\\\AFK-Nas1\\Share\\ВЕГА1\\Кагерман\\Сергей\\AFK\\Catalogs.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            FileInfo fileInfo = new FileInfo(filePath);
            using var package = new ExcelPackage(fileInfo);
            var sheet = package.Workbook.Worksheets[Name: "Operations"];
            DataTable dataTable = new DataTable();

            for (int i = sheet.Dimension.Start.Column; i <= sheet.Dimension.End.Column; i++)
            {
                if (sheet.Cells[1, i].Value.ToString() == "Дата")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(DateTime));
                else if (sheet.Cells[1, i].Value.ToString() == "Сумма")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(decimal));
                else
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString());
            }

            for (int i = 2; i <= sheet.Dimension.End.Row; i++)
            {
                DataRow dataRow = dataTable.NewRow();
                for (int j = 1; j <= sheet.Dimension.End.Column; j++)
                {
                    dataRow[j - 1] = sheet.Cells[i, j].Value;
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable.AsEnumerable().Select(row => new Operations
            {
                OperationId = row.Field<string>("Код из 1С"),
                Number = row.Field<string>("Номер"),
                Date = row.Field<DateTime>("Дата"),
                Sum = row.Field<decimal>("Сумма"),
                ContractDebit = row.Field<string>("Договор Дебет"),
                ContractCredit = row.Field<string>("Договор Кредит"),
                ContractorDebit = row.Field<string>("Контрагент Дебет"),
                ContractorCredit = row.Field<string>("Контрагент Кредит"),
            }).ToList();
        }

        public async Task<string> TmpAsync()
        {
            //var operationUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_КорректировкаПоступления?$format=json";
            var operationUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_ПоступлениеТоваровУслуг?$format=json";
            using HttpResponseMessage operationResponse = await httpClient.GetAsync(operationUrl);
            string content1 = await operationResponse.Content.ReadAsStringAsync();
            Console.WriteLine(content1);
            return content1;
        }

        public async Task<OperationsTmp> OperationAsync() // Операции
        {
            var operationUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/Document_ОперацияБух?$format=json";
            using HttpResponseMessage operationResponse = await httpClient.GetAsync(operationUrl);
            return await operationResponse.Content.ReadFromJsonAsync<OperationsTmp>();
        }

        public Task<BillPayment> BillPaymentAsync()
        {
            throw new NotImplementedException();
        }

        public List<LiterAndCostItemInPayments> GetLiterAndCostItemInPayments()
        {
            throw new NotImplementedException();
        }
    }
}
