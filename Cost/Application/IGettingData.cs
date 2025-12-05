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

namespace Cost.Application
{
    public interface IGettingData
    {
        /// Контрагенты
        Task<Counterparties> CounterpartiesAsync();
        Task<ContractsCounterparties> ContractsCounterpartiesAsync(); // Договоры контрагентов
        Task<Receipts> ReceiptGoodsServicesAsync(); // Поступление товаров и услуг
        Task<Payments> PaymentsAsync(); // Списание с расчетного счета
        Task<ReceiptToCurrentAccount> ReceiptToCurrentAccountAsync(); // Поступление на расчетный счет
        Task<NomenclatureGroups> NomenclatureGroupsAsync(); // Номенклатурные группы
        Task<ConstructionProjects> ConstructionProjectsAsync(); // Объекты Строительства
        Task<CostItems> CostItemsAsync(); // Статьи затрат
        Task<TypesCalculations> TypesCalculationsAsync(); // Виды взаиморасчетов
        Task<DebtAdjustment> DebtAdjustmentAsync(); // Корректировка долга
        Task<Receipts> ReceiptProcessingAsync(); // Поступление из переработки
        Task<Selling> SellingAsync(); // Реализация
        Task<AdditionalInformation> AdditionalInformationAsync(); // Дополнительные сведения
        Task<InvoiceReceived> InvoiceReceivedAsync(); // Счета-фактуры полученные
        List<Facility> GetFacility(); // Объекты строительства
        List<Contracts> GetContracts(); // Договора
        List<Operations> GetOperations(); // Бухгалтерские операции
        Task<OperationsTmp> OperationAsync(); // Операции
        Task<BillPayment> BillPaymentAsync(); // Оплата счетов
        List<LiterAndCostItemInPayments> GetLiterAndCostItemInPayments(); // Литер и статья затрат в оплатах
        Task<string> TmpAsync();

    }
}
