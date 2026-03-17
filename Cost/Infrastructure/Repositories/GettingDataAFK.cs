using Cost.Application;
using Cost.Domain;
using Cost.Infrastructure.Repositories.Models;
using Cost.Infrastructure.Repositories.Models.ActOfCompletion;
using Cost.Infrastructure.Repositories.Models.AdditionalInformation;
using Cost.Infrastructure.Repositories.Models.BuyerPaymentInvoice;
using Cost.Infrastructure.Repositories.Models.ContractsCounterparties;
using Cost.Infrastructure.Repositories.Models.CostItems;
using Cost.Infrastructure.Repositories.Models.Counterparties;
using Cost.Infrastructure.Repositories.Models.DebitToCurrentAccount;
using Cost.Infrastructure.Repositories.Models.DebtAdjustment;
using Cost.Infrastructure.Repositories.Models.DepositToCurrentAccount;
using Cost.Infrastructure.Repositories.Models.ImplementationConstructionWorks;
using Cost.Infrastructure.Repositories.Models.NomenclatureGroups;
using Cost.Infrastructure.Repositories.Models.ReceiptGoodsServices;
using Cost.Infrastructure.Repositories.Models.ReceiptProcessing;
using Cost.Infrastructure.Repositories.Models.SaleGoodsServices;
using Cost.Infrastructure.Repositories.Models.SupplierPaymentInvoice;
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
        private const string ApiUrl = "http://localhost/afk_bs0_2020_new/odata/standard.odata/";

        public decimal StartBalance => 1016806.12M;
        public DateOnly StartDate => new(2026, 1, 1);

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
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
        }

        public async Task<DebitToCurrentAccount> DebitToCurrentAccountAsync() // Списание с расчетного счета
        {
            var paymentUrl = ApiUrl + "Document_СписаниеСРасчетногоСчета?$format=json"
                + "&$select=Ref_Key,Date,СуммаДокумента,ДоговорКонтрагента_Key,РасшифровкаПлатежа,НазначениеПлатежа,ВидОперации"
                + "&$filter=(DeletionMark eq false) and (Posted eq true)";
            using HttpResponseMessage paymentResponse = await httpClient.GetAsync(paymentUrl);
            return await paymentResponse.Content.ReadFromJsonAsync<DebitToCurrentAccount>();
        }

        public async Task<AdditionalInformation> AdditionalInformationAsync() // Дополнительные сведения
        {
            var additionalInformationUrl = ApiUrl + "InformationRegister_ДополнительныеСведения?$format=json"
                + "&$select=Объект,Значение,Значение_Type";
            using HttpResponseMessage additionalInformationResponse = await httpClient.GetAsync(additionalInformationUrl);
            return await additionalInformationResponse.Content.ReadFromJsonAsync<AdditionalInformation>();
        }

        public async Task<NomenclatureGroups> NomenclatureGroupsAsync() // Номенклатурные группы
        {
            var nomenclatureGroupsUrl = ApiUrl + "Catalog_НоменклатурныеГруппы?$format=json"
                + "&$select=Ref_Key,Description"
                + "&$filter=DeletionMark eq false";
            using HttpResponseMessage nomenclatureGroupsResponse = await httpClient.GetAsync(nomenclatureGroupsUrl);
            return await nomenclatureGroupsResponse.Content.ReadFromJsonAsync<NomenclatureGroups>();
        }

        public async Task<CostItems> CostItemsAsync() // Статьи затрат
        {
            var costItemsUrl = ApiUrl + "Catalog_СтатьиЗатрат?$format=json"
                + "&$select=Ref_Key,Description"
                + "&$filter=DeletionMark eq false";
            using HttpResponseMessage costItemsResponse = await httpClient.GetAsync(costItemsUrl);
            return await costItemsResponse.Content.ReadFromJsonAsync<CostItems>();
        }

        public IEnumerable<ExpensePaymentsFromExcel> ExpensePaymentsFromExcel() // Литер и статья затрат в старых оплатах
        {
            string filePath = "C:\\Cost\\AFKDevelopment\\Catalogs.xlsx";
            FileInfo fileInfo = new(filePath);
            using var package = new ExcelPackage(fileInfo);
            var sheet = package.Workbook.Worksheets[Name: "Payments"];
            DataTable dataTable = new();

            for (int i = sheet.Dimension.Start.Column; i <= sheet.Dimension.End.Column; i++)
            {
                if (sheet.Cells[1, i].Value.ToString() == "Date")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(DateTime));
                else if (sheet.Cells[1, i].Value.ToString() == "PaymentAmount")
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

            return dataTable.AsEnumerable().Select(row => new ExpensePaymentsFromExcel
            {
                Liter = row.Field<string>("Liter"),
                CostItems = row.Field<string>("CostItems"),
                PaymentId = row.Field<string>("PaymentId"),
                Date = DateOnly.FromDateTime(row.Field<DateTime>("Date")),
                PaymentAmount = row.Field<decimal>("PaymentAmount"),
                PurposePayment = row.Field<string>("PurposePayment"),
            });
        }

        public async Task<SupplierPaymentInvoice> SupplierPaymentInvoiceAsync() // Счет на оплату поставщика
        {
            var supplierPaymentInvoiceUrl = ApiUrl + "Document_СчетНаОплатуПоставщика?$format=json"
                + "&$select=Ref_Key,Комментарий"
                + "&$filter=DeletionMark eq false";
            using HttpResponseMessage supplierPaymentInvoiceResponse = await httpClient.GetAsync(supplierPaymentInvoiceUrl);
            return await supplierPaymentInvoiceResponse.Content.ReadFromJsonAsync<SupplierPaymentInvoice>();
        }

        public async Task<DepositToCurrentAccount> DepositToCurrentAccountAsync() // Поступление на расчетный счет
        {
            var depositToCurrentAccountUrl = ApiUrl + "Document_ПоступлениеНаРасчетныйСчет?$format=json"
                + "&$select=Ref_Key,Date,СуммаДокумента,ДоговорКонтрагента_Key,РасшифровкаПлатежа,НазначениеПлатежа,ВидОперации"
                + "&$filter=DeletionMark eq false and Posted eq true";
            using HttpResponseMessage depositToCurrentAccountResponse = await httpClient.GetAsync(depositToCurrentAccountUrl);
            return await depositToCurrentAccountResponse.Content.ReadFromJsonAsync<DepositToCurrentAccount>();
        }

        public async Task<BuyerPaymentInvoice> BuyerPaymentInvoiceAsync() // Счет на оплату покупателю
        {
            var buyerPaymentInvoiceUrl = ApiUrl + "Document_СчетНаОплатуПокупателю?$format=json"
                + "&$select=Ref_Key,Комментарий"
                + "&$filter=DeletionMark eq false";
            using HttpResponseMessage buyerPaymentInvoiceResponse = await httpClient.GetAsync(buyerPaymentInvoiceUrl);
            return await buyerPaymentInvoiceResponse.Content.ReadFromJsonAsync<BuyerPaymentInvoice>();
        }

        public async Task<Counterparties> CounterpartiesAsync() // Контрагенты
        {
            var counterpartiesUrl = ApiUrl + "Catalog_Контрагенты?$format=json"
                + "&$select=Ref_Key,Description";
            using HttpResponseMessage counterpartiesResponse = await httpClient.GetAsync(counterpartiesUrl);
            return await counterpartiesResponse.Content.ReadFromJsonAsync<Counterparties>();
        }

        public async Task<ContractsCounterparties> ContractsCounterpartiesAsync() // Договоры контрагентов
        {
            var contractsCounterpartiesUrl = ApiUrl + "Catalog_ДоговорыКонтрагентов?$format=json"
                + "&$select=Ref_Key,Номер,Description,Дата,Сумма,Owner_Key,Code"
                + "&$filter=DeletionMark eq false";
            using HttpResponseMessage contractsCounterpartiesResponse = await httpClient.GetAsync(contractsCounterpartiesUrl);
            var result = await contractsCounterpartiesResponse.Content.ReadFromJsonAsync<ContractsCounterparties>();
            result.CodeContract = 3760;
            return result;
        }

        public IEnumerable<Contracts> GetContracts() // Договора
        {
            string filePath = "C:\\Cost\\AFK\\Catalogs.xlsx";
            FileInfo fileInfo = new(filePath);
            using var package = new ExcelPackage(fileInfo);
            var sheet = package.Workbook.Worksheets[Name: "Contracts"];
            DataTable dataTable = new();

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
                else if (sheet.Cells[1, i].Value.ToString() == "AmountUntil2026")
                    dataTable.Columns.Add(sheet.Cells[1, i].Value.ToString(), typeof(decimal));
                else if (sheet.Cells[1, i].Value.ToString() == "RateNDS2026")
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
                Date = DateOnly.FromDateTime(row.Field<DateTime>("Дата договора")),
                Sum = row.Field<decimal>("Сумма договора"),
                RateNDS = row.Field<decimal>("Ставка НДС"),
                GeneralContracting = row.Field<decimal>("ГП"),
                SecurityDeposit = row.Field<decimal>("ГУ"),
                ContractorOrSupplier = row.Field<string>("Подрядчик/Поставщик"),
                Liter = row.Field<string>("Литер"),
                CostItem = row.Field<string>("Статья затрат"),
                Name = row.Field<string>("Наименование"),
                ContractClosed = row.Field<string>("Статус"),
                AmountUntil2026 = row.Field<decimal>("AmountUntil2026"),
                RateNDS2026 = row.Field<decimal>("RateNDS2026"),
                AreaOfActivity = row.Field<string>("Направление")
            });
        }

        public async Task<DebtAdjustment> DebtAdjustmentAsync() // Корректировка долга
        {
            var debtAdjustmentUrl = ApiUrl + "Document_КорректировкаДолга?$format=json"
                + "&$select=Date,DeletionMark,КредиторскаяЗадолженность,ДебиторскаяЗадолженность"
                + "&$filter=DeletionMark eq false and Posted eq true";
            using HttpResponseMessage debtAdjustmentResponse = await httpClient.GetAsync(debtAdjustmentUrl);
            return await debtAdjustmentResponse.Content.ReadFromJsonAsync<DebtAdjustment>();
        }

        public IEnumerable<Operations> GetOperations() // Бухгалтерские операции
        {
            string filePath = "C:\\Cost\\AFK\\Catalogs.xlsx";
            FileInfo fileInfo = new(filePath);
            using var package = new ExcelPackage(fileInfo);
            var sheet = package.Workbook.Worksheets[Name: "Operations"];
            DataTable dataTable = new();

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
                Date = DateOnly.FromDateTime(row.Field<DateTime>("Дата")),
                Sum = row.Field<decimal>("Сумма"),
                ContractDebit = row.Field<string>("Договор Дебет"),
                ContractCredit = row.Field<string>("Договор Кредит"),
            });
        }

        public async Task<ReceiptGoodsServices> ReceiptGoodsServicesAsync() // Поступление товаров и услуг
        {
            var receiptGoodsServicesUrl = ApiUrl + "Document_ПоступлениеТоваровУслуг?$format=json"
                + "&$select=Date,СуммаДокумента,ДоговорКонтрагента_Key"
                + "&$filter=DeletionMark eq false and Posted eq true";
            using HttpResponseMessage receiptGoodsServicesResponse = await httpClient.GetAsync(receiptGoodsServicesUrl);
            return await receiptGoodsServicesResponse.Content.ReadFromJsonAsync<ReceiptGoodsServices>();
        }

        public async Task<ReceiptProcessing> ReceiptProcessingAsync() // Поступление из переработки
        {
            var receiptProcessingUrl = ApiUrl + "Document_ПоступлениеИзПереработки?$format=json"
                + "&$select=Date,СуммаДокумента,ДоговорКонтрагента_Key"
                + "&$filter=DeletionMark eq false and Posted eq true";
            using HttpResponseMessage receiptProcessingResponse = await httpClient.GetAsync(receiptProcessingUrl);
            return await receiptProcessingResponse.Content.ReadFromJsonAsync<ReceiptProcessing>();
        }

        public async Task<SaleGoodsServices> SaleGoodsServicesAsync() // Реализация товаров и услуг
        {
            var saleGoodsServicesUrl = ApiUrl + "Document_РеализацияТоваровУслуг?$format=json"
                + "&$select=Date,СуммаДокумента,ДоговорКонтрагента_Key"
                + "&$filter=DeletionMark eq false and Posted eq true";
            using HttpResponseMessage saleGoodsServicesResponse = await httpClient.GetAsync(saleGoodsServicesUrl);
            return await saleGoodsServicesResponse.Content.ReadFromJsonAsync<SaleGoodsServices>();
        }

        public async Task<ImplementationConstructionWorks> ImplementationConstructionWorksAsync() // Реализация строительных работ
        {
            var implementationConstructionWorksUrl = ApiUrl + "Document_ИмпРеализацияСтроительныхРаботУслуг?$format=json"
                + "&$select=Date,СуммаДокумента,ДоговорКонтрагента_Key"
                + "&$filter=DeletionMark eq false and Posted eq true";
            using HttpResponseMessage implementationConstructionWorksResponse = await httpClient.GetAsync(implementationConstructionWorksUrl);
            return await implementationConstructionWorksResponse.Content.ReadFromJsonAsync<ImplementationConstructionWorks>();
        }

        public async Task<ActOfCompletion> ActOfCompletionAsync() // Акты об окончании СМР
        {
            var actOfCompletionUrl = ApiUrl + "Document_ИмпЗаказСМР?$format=json"
                + "&$select=ДатаНачала,ДатаОкончания,ДоговорКонтрагента_Key,Комментарий"
                + "&$filter=DeletionMark eq false and Posted eq true";
            using HttpResponseMessage actOfCompletionResponse = await httpClient.GetAsync(actOfCompletionUrl);
            return await actOfCompletionResponse.Content.ReadFromJsonAsync<ActOfCompletion>();
        }

        public IEnumerable<Facility> GetFacility() // Площади объектов строительства
        {
            throw new NotImplementedException();
        }

        public async Task<string> TmpAsync()
        {
            var tmpUrl = ApiUrl + "Document_СчетНаОплатуПоставщика?$format=json";
            using HttpResponseMessage tmpResponse = await httpClient.GetAsync(tmpUrl);
            string content = await tmpResponse.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            return content;
        }

        public IEnumerable<AreaOfActivityInPayments> GetLiterAndCostItemInAreaOfActivity() // AreaOfActivity по литеру и статье затрат в оплатах
        {
            string filePath = "C:\\Cost\\AFK\\Catalogs.xlsx";
            FileInfo fileInfo = new(filePath);
            using var package = new ExcelPackage(fileInfo);
            var sheet = package.Workbook.Worksheets[Name: "AreaOfActivity"];
            DataTable dataTable = new();

            for (int i = sheet.Dimension.Start.Column; i <= sheet.Dimension.End.Column; i++)
            {
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

            return dataTable.AsEnumerable().Select(row => new AreaOfActivityInPayments
            {
                Liter = row.Field<string>("Liter"),
                CostItems = row.Field<string>("CostItems"),
                AreaOfActivity = row.Field<string>("AreaOfActivity")
            });
        }
    }
}