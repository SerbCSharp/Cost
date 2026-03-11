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
using Cost.Infrastructure.Repositories.Models.Receipts;
using Cost.Infrastructure.Repositories.Models.Selling;
using Cost.Infrastructure.Repositories.Models.SupplierPaymentInvoice;

namespace Cost.Application
{
    public interface IGettingData
    {
        decimal StartBalance { get; } // Баланс по 51 счету на StartDate
        DateOnly StartDate { get; } // С какой даты считаем CashFlow

        Task<DebitToCurrentAccount> DebitToCurrentAccountAsync(); // Списание с расчетного счета
        Task<AdditionalInformation> AdditionalInformationAsync(); // Дополнительные сведения
        Task<NomenclatureGroups> NomenclatureGroupsAsync(); // Номенклатурные группы
        Task<CostItems> CostItemsAsync(); // Статьи затрат
        IEnumerable<ExpensePaymentsFromExcel> ExpensePaymentsFromExcel(); // Литер и статья затрат в старых оплатах
        Task<SupplierPaymentInvoice> SupplierPaymentInvoiceAsync(); // Счет на оплату поставщика
        Task<DepositToCurrentAccount> DepositToCurrentAccountAsync(); // Поступление на расчетный счет
        Task<BuyerPaymentInvoice> BuyerPaymentInvoiceAsync(); // Счет на оплату покупателю
        Task<Counterparties> CounterpartiesAsync(); // Контрагенты
        Task<ContractsCounterparties> ContractsCounterpartiesAsync(); // Договоры контрагентов
        IEnumerable<Contracts> GetContracts(); // Договора
        Task<DebtAdjustment> DebtAdjustmentAsync(); // Корректировка долга




        Task<Receipts> ReceiptGoodsServicesAsync(); // Поступление товаров и услуг
        Task<Receipts> ReceiptProcessingAsync(); // Поступление из переработки
        Task<Selling> SellingAsync(); // Реализация
        List<Facility> GetFacility(); // Объекты строительства
        List<Operations> GetOperations(); // Бухгалтерские операции
        Task<ImplementationConstructionWorks> ImplementationConstructionWorksAsync(); // Реализация строительных работ
        List<AreaOfActivityInPayments> GetLiterAndCostItemInAreaOfActivity(); // AreaOfActivity по литеру и статье затрат в оплатах
        Task<ActOfCompletion> ActOfCompletionAsync(); // Акты об окончании СМР
        Task<string> TmpAsync();
    }
}
