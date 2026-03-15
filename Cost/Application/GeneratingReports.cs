using Cost.Domain;
using Cost.Infrastructure.Repositories.Models.ActOfCompletion;
using Cost.Presentation.DTO.Request;
using Cost.Presentation.ReportsToExcel;

namespace Cost.Application
{
    public class GeneratingReports
    {
        private readonly IGettingDataFactory _gettingDataFactory;
        private readonly ExportingReportsToExcel _exportingReportsToExcel;

        public GeneratingReports(IGettingDataFactory gettingDataFactory, ExportingReportsToExcel exportingReportsToExcel)
        {
            _gettingDataFactory = gettingDataFactory;
            _exportingReportsToExcel = exportingReportsToExcel;
        }

        public async Task<IEnumerable<Payment>> ExpensePaymentsAsync(Organizations organization) // Расходные оплаты
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            var payments = (await gettingData.DebitToCurrentAccountAsync()).Value;
            var multiplePayments = payments.Where(x => x.PaymentDetails.Length > 0)
                .SelectMany(y => y.PaymentDetails, (x, y) => new { payment = x, PaymentDetails = y })
                .Select(z => new Payment
                {
                    PaymentId = z.payment.PaymentId,
                    Date = DateOnly.FromDateTime(z.payment.Date),
                    PaymentDetailsId = z.PaymentDetails.PaymentInvoiceId,
                    ContractId = z.PaymentDetails.ContractId,
                    PaymentAmount = z.PaymentDetails.PaymentAmount,
                    PaymentPurpose = z.payment.PaymentPurpose,
                    TypeOperation = z.payment.TypeOperation
                });
            var singlePayment = payments.Where(x => x.PaymentDetails.Length == 0)
                .Select(y => new Payment
                {
                    PaymentId = y.PaymentId,
                    Date = DateOnly.FromDateTime(y.Date),
                    PaymentDetailsId = null,
                    ContractId = y.ContractId,
                    PaymentAmount = y.PaymentAmount,
                    PaymentPurpose = y.PaymentPurpose,
                    TypeOperation = y.TypeOperation
                });
            var allPayments = multiplePayments.Concat(singlePayment);

            var supplierPaymentInvoice = (await gettingData.SupplierPaymentInvoiceAsync()).Value;
            var paymentsPlusSupplierPaymentInvoice = from vAllPayments in allPayments
                                                     join vSupplierPaymentInvoice in supplierPaymentInvoice
                                                     on vAllPayments.PaymentDetailsId equals vSupplierPaymentInvoice.SupplierPaymentInvoiceId into leftJoin
                                                     from subvSupplierPaymentInvoice in leftJoin.DefaultIfEmpty()
                                                     select new { vAllPayments, subvSupplierPaymentInvoice };

            var additionalInformation = (await gettingData.AdditionalInformationAsync()).Value;
            var literId = additionalInformation.Where(x => x.ValueType.Contains("НоменклатурныеГруппы", StringComparison.OrdinalIgnoreCase));
            var paymentsPlusLiterId = from vPaymentsPlusSupplierPaymentInvoice in paymentsPlusSupplierPaymentInvoice
                                      join vLiterId in literId
                                      on vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentDetailsId equals vLiterId.ADObject into leftJoin
                                      from subvLiterId in leftJoin.DefaultIfEmpty()
                                      select new { vPaymentsPlusSupplierPaymentInvoice, subvLiterId?.ADValue };
            var costItemId = additionalInformation.Where(x => x.ValueType.Contains("СтатьиЗатрат", StringComparison.OrdinalIgnoreCase));
            var paymentsPlusLiterIdPlusCostItemId = from vPaymentsPlusLiterId in paymentsPlusLiterId
                                                    join vCostItemId in costItemId
                                                    on vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentDetailsId equals vCostItemId.ADObject into leftJoin
                                                    from subvCostItemId in leftJoin.DefaultIfEmpty()
                                                    select new { vPaymentsPlusLiterId, subvCostItemId?.ADValue };
            var nomenclatureGroups = (await gettingData.NomenclatureGroupsAsync()).Value;
            var plusLiterName = from vPaymentsPlusLiterIdPlusCostItemId in paymentsPlusLiterIdPlusCostItemId
                                join vNomenclatureGroups in nomenclatureGroups
                                on vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.ADValue equals vNomenclatureGroups.Ref_Key into leftJoin
                                from subvNomenclatureGroups in leftJoin.DefaultIfEmpty()
                                select new { vPaymentsPlusLiterIdPlusCostItemId, subvNomenclatureGroups?.Description };
            var costItems = (await gettingData.CostItemsAsync()).Value;
            var plusCostItemName = from vPlusLiterName in plusLiterName
                                   join vCostItems in costItems
                                   on vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.ADValue equals vCostItems.Ref_Key into leftJoin
                                   from subvCostItems in leftJoin.DefaultIfEmpty()
                                   select new { vPlusLiterName, subvCostItems?.Description };

            // Объекты и статьи затрат по старым оплатам
            var expensePaymentsFromExcel = gettingData.ExpensePaymentsFromExcel();
            var plusExpensePaymentsFromExcel = from vPlusCostItemName in plusCostItemName
                                               join vExpensePaymentsFromExcel in expensePaymentsFromExcel
                                               on vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentId
                                               equals vExpensePaymentsFromExcel.PaymentId into leftJoin
                                               from subvExpensePaymentsFromExcel in leftJoin.DefaultIfEmpty()
                                               select new { vPlusCostItemName, subvExpensePaymentsFromExcel };

            var result = plusExpensePaymentsFromExcel.Select(x => new Payment
            {
                PaymentId = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentId,
                Date = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.Date,
                PaymentAmount = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentAmount,
                ContractId = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.ContractId,
                Liter = string.IsNullOrEmpty(x.subvExpensePaymentsFromExcel?.Liter) ? x.vPlusCostItemName.vPlusLiterName.Description : x.subvExpensePaymentsFromExcel?.Liter,
                CostItem = string.IsNullOrEmpty(x.subvExpensePaymentsFromExcel?.CostItems) ? x.vPlusCostItemName.Description : x.subvExpensePaymentsFromExcel?.CostItems,
                PaymentPurpose = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentPurpose,
                TypeOperation = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.TypeOperation,
                CommentFromPaymentInvoice = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.subvSupplierPaymentInvoice?.Comment,
                PaymentDetailsId = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentDetailsId
            }).OrderBy(x => x.Date);

            return result;
        }

        public async Task<IEnumerable<Payment>> IncomePaymentsAsync(Organizations organization) // Доходные оплаты
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var payments = (await gettingData.DepositToCurrentAccountAsync()).Value;
            var multiplePayments = payments.Where(x => x.PaymentDetails.Length > 0)
                .SelectMany(y => y.PaymentDetails, (x, y) => new { payment = x, PaymentDetails = y })
                .Select(z => new Payment
                {
                    PaymentId = z.payment.PaymentId,
                    Date = DateOnly.FromDateTime(z.payment.Date),
                    PaymentDetailsId = z.PaymentDetails.PaymentInvoiceId,
                    ContractId = z.PaymentDetails.ContractId,
                    PaymentAmount = z.PaymentDetails.PaymentAmount,
                    PaymentPurpose = z.payment.PaymentPurpose,
                    TypeOperation = z.payment.TypeOperation
                });
            var singlePayment = payments.Where(x => x.PaymentDetails.Length == 0)
                .Select(y => new Payment
                {
                    PaymentId = y.PaymentId,
                    Date = DateOnly.FromDateTime(y.Date),
                    PaymentDetailsId = null,
                    ContractId = y.ContractId,
                    PaymentAmount = y.PaymentAmount,
                    PaymentPurpose = y.PaymentPurpose,
                    TypeOperation = y.TypeOperation
                });
            var allPayments = multiplePayments.Concat(singlePayment);

            var buyerPaymentInvoice = (await gettingData.BuyerPaymentInvoiceAsync()).Value;
            var paymentsPlusSupplierPaymentInvoice = from vAllPayments in allPayments
                                                     join vbuyerPaymentInvoice in buyerPaymentInvoice
                                                     on vAllPayments.PaymentDetailsId equals vbuyerPaymentInvoice.BuyerPaymentInvoiceId into leftJoin
                                                     from subvbuyerPaymentInvoice in leftJoin.DefaultIfEmpty()
                                                     select new { vAllPayments, subvbuyerPaymentInvoice?.Comment };

            var result = paymentsPlusSupplierPaymentInvoice.Select(x => new Payment
            {
                PaymentId = x.vAllPayments.PaymentId,
                Date = x.vAllPayments.Date,
                PaymentAmount = x.vAllPayments.PaymentAmount,
                ContractId = x.vAllPayments.ContractId,
                PaymentPurpose = x.vAllPayments.PaymentPurpose,
                TypeOperation = x.vAllPayments.TypeOperation,
                CommentFromPaymentInvoice = x.Comment,
                PaymentDetailsId = x.vAllPayments.PaymentDetailsId
            }).OrderBy(x => x.Date);

            return result;
        }

        public async Task<IEnumerable<Contracts>> WeDoNotHaveTheseContractsAsync(Organizations organization) // Отсутствующие у нас договора
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var contractsCounterparties = (await gettingData.ContractsCounterpartiesAsync());
            var contractsCounterpartiesValue = contractsCounterparties.Value
                .Where(x => int.Parse(x.Code.Substring(x.Code.Length - 6)) > contractsCounterparties.CodeContract);

            // Контрагенты + договора
            var counterparties = await gettingData.CounterpartiesAsync();
            var contractsFrom1C = counterparties.Value.Join(contractsCounterpartiesValue, counterparties => counterparties.Ref_Key,
                contractsCounterparties => contractsCounterparties.ContractorId,
                (x, y) => new Contracts
                {
                    ContractId = y.ContractorId,
                    Contractor = x.Description,
                    Number = y.Number,
                    Name = y.Name,
                    Date = DateOnly.FromDateTime(y.Date ?? new DateTime()),
                    Sum = y.Sum ?? 0,
                    Code = y.Code
                });

            var contractsFromExcel = gettingData.GetContracts();

            return contractsFrom1C.Except(contractsFromExcel);
        }

        public async Task<IEnumerable<(Payment, Contracts)>> PaymentsAsync(Organizations organization) // Расходные оплаты + договора
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var payments = await ExpensePaymentsAsync(organization);
            var contracts = gettingData.GetContracts();
            var paymentsPluscontracts = from vPayments in payments
                                        join vContracts in contracts
                                        on vPayments.ContractId equals vContracts.ContractId into leftJoin
                                        from subvContracts in leftJoin.DefaultIfEmpty()
                                        select (vPayments, subvContracts);

            return paymentsPluscontracts;
        }

        public async Task<IEnumerable<AccountingTransaction>> AccountingTransactionAsync(Organizations organization) // Корректировка долга
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var debtAdjustment = (await gettingData.DebtAdjustmentAsync()).Value.ToList();

            // Убираем из Корректировки долга проводки по одному договору в одном документе Корректировка долга
            foreach (var item in debtAdjustment)
            {
                if (item.AccountsPayable.Length > 0 && item.AccountsReceivable.Length > 0
                    && item.AccountsPayable.First().ContractId == item.AccountsReceivable.First().ContractId)
                {
                    item.DeletionMark = true;
                }
                if (item.AccountsPayable.Length > 0 && item.AccountsPayable.First().ContractId == item.AccountsPayable.First().CorContractId)
                {
                    item.DeletionMark = true;
                }
                if (item.AccountsReceivable.Length > 0 && item.AccountsReceivable.First().ContractId == item.AccountsReceivable.First().CorContractId)
                {
                    item.DeletionMark = true;
                }
            }
            debtAdjustment.RemoveAll(x => x.DeletionMark);
            _exportingReportsToExcel.Browse(debtAdjustment);

            var multiplePayable = debtAdjustment.SelectMany(x => x.AccountsPayable, (x, y) => new { debtAdjustment = x, accountsPayable = y })
                .Select(z => new AccountingTransaction
                {
                    Date = DateOnly.FromDateTime(z.debtAdjustment.Date),
                    ContractId = z.accountsPayable.ContractId,
                    Debit = z.accountsPayable.Sum
                });
            var singlePayable = debtAdjustment.Where(x => x.AccountsPayable.Length == 0)
                .SelectMany(x => x.AccountsReceivable, (x, y) => new { debtAdjustment = x, accountsReceivable = y })
                .Select(z => new AccountingTransaction
                {
                    Date = DateOnly.FromDateTime(z.debtAdjustment.Date),
                    ContractId = z.accountsReceivable.CorContractId,
                    Debit = z.accountsReceivable.Sum
                });
            var allPayable = multiplePayable.Concat(singlePayable);

            var multipleReceivable = debtAdjustment.SelectMany(x => x.AccountsReceivable, (x, y) => new { debtAdjustment = x, accountsReceivable = y })
                .Select(z => new AccountingTransaction
                {
                    Date = DateOnly.FromDateTime(z.debtAdjustment.Date),
                    ContractId = z.accountsReceivable.ContractId,
                    Credit = z.accountsReceivable.Sum
                });
            var singleReceivable = debtAdjustment.Where(x => x.AccountsReceivable.Length == 0)
                .SelectMany(x => x.AccountsPayable, (x, y) => new { debtAdjustment = x, accountsPayable = y })
                .Select(z => new AccountingTransaction
                {
                    Date = DateOnly.FromDateTime(z.debtAdjustment.Date),
                    ContractId = z.accountsPayable.CorContractId,
                    Credit = z.accountsPayable.Sum
                });
            var allReceivable = multipleReceivable.Concat(singleReceivable);

            return allPayable.Concat(allReceivable);
        }

        public async Task<IEnumerable<IncomeAndExpenses>> IncomeAndExpensesAsync(Organizations organization) // Доходы и расходы
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var expensePayments = (await ExpensePaymentsAsync(organization))
                .Select(x => new IncomeAndExpenses()
                {
                    Date = x.Date,
                    Debit = x.PaymentAmount,
                    ContractId = x.ContractId,
                    Liter = x.Liter,
                    CostItem = x.CostItem,
                    TypeOperation = x.TypeOperation,
                    PaymentPurpose = x.PaymentPurpose,
                    DocumentName = "Списание с расчетного счета"
                });

            var receiptGoodsServices = (await gettingData.ReceiptGoodsServicesAsync()).Value
                .Select(x => new IncomeAndExpenses()
                {
                    Date = DateOnly.FromDateTime(x.Date),
                    Credit = x.DocumentAmount,
                    ContractId = x.ContractId,
                    DocumentName = "Поступление товаров и услуг"
                });
            var plusReceiptGoodsServices = expensePayments.Concat(receiptGoodsServices);

            var receiptProcessing = (await gettingData.ReceiptProcessingAsync()).Value
                .Select(x => new IncomeAndExpenses()
                {
                    Date = DateOnly.FromDateTime(x.Date),
                    Credit = x.DocumentAmount,
                    ContractId = x.ContractId,
                    DocumentName = "Поступление из переработки"
                });
            var plusReceiptProcessing = plusReceiptGoodsServices.Concat(receiptProcessing);

            var saleGoodsServices = (await gettingData.SaleGoodsServicesAsync()).Value
                .Select(x => new IncomeAndExpenses()
                {
                    Date = DateOnly.FromDateTime(x.Date),
                    Debit = x.DocumentAmount,
                    ContractId = x.ContractId,
                    DocumentName = "Реализация товаров и услуг"
                });
            var plusSaleGoodsServices = plusReceiptProcessing.Concat(saleGoodsServices);

            var accountingTransactions = (await AccountingTransactionAsync(organization))
                .Select(x => new IncomeAndExpenses()
                {
                    Date = x.Date,
                    Debit = x.Debit,
                    Credit = x.Credit,
                    ContractId = x.ContractId,
                    DocumentName = "Корректировка долга"
                });
            var plusAccountingTransactions = plusSaleGoodsServices.Concat(accountingTransactions);

            var incomePayments = (await IncomePaymentsAsync(organization))
                .Select(x => new IncomeAndExpenses
                {
                    Date = x.Date,
                    Credit = x.PaymentAmount,
                    ContractId = x.ContractId,
                    TypeOperation = x.TypeOperation,
                    PaymentPurpose = x.PaymentPurpose,
                    DocumentName = "Поступление на расчетный счет"
                });
            var plusIncomePayments = plusAccountingTransactions.Concat(incomePayments);

            var operations = gettingData.GetOperations();
            var operationDebit = operations
                .Select(y => new IncomeAndExpenses
                {
                    Date = y.Date,
                    Debit = y.Sum,
                    ContractId = y.ContractDebit,
                    DocumentName = "Операция"
                });
            var plusOperationDebit = plusIncomePayments.Concat(operationDebit);

            var operationCredit = operations
                .Select(y => new IncomeAndExpenses
                {
                    Date = y.Date,
                    Credit = y.Sum,
                    ContractId = y.ContractCredit,
                    DocumentName = "Операция"
                });
            var plusOperationCredit = plusOperationDebit.Concat(operationCredit);

            var implementationConstructionWorks = (await gettingData.ImplementationConstructionWorksAsync()).Value
                .Select(y => new IncomeAndExpenses
                {
                    Date = DateOnly.FromDateTime(y.Date),
                    Debit = y.DocumentAmount,
                    ContractId = y.ContractId,
                    DocumentName = "Реализация строительных работ и услуг"
                });
            var plusImplementationConstructionWorks = implementationConstructionWorks != null ? plusOperationCredit.Concat(implementationConstructionWorks)
                                                                                              : plusOperationCredit;

            var incomeAndExpenses = plusImplementationConstructionWorks
                .Select(x => new IncomeAndExpenses
                {
                    ContractId = x.ContractId,
                    DocumentName = x.DocumentName,
                    Credit = x.Credit,
                    Debit = x.Debit,
                    Date = x.Date,
                    CostItem = x.CostItem,
                    Liter = x.Liter,
                    TypeOperation = x.TypeOperation,
                    PaymentPurpose = x.PaymentPurpose
                });

            return incomeAndExpenses.OrderBy(x => x.Date);
        }

        public async Task<(IEnumerable<CashFlow>, decimal)> CashFlowAsync(Organizations organization, DateOnly startDate, DateOnly endDate) // ДДС
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = (await IncomeAndExpensesAsync(organization)).Where(w => w.Date >= gettingData.StartDate
                && (w.DocumentName == "Списание с расчетного счета" || w.DocumentName == "Поступление на расчетный счет"));

            var contracts = gettingData.GetContracts();
            var plusContracts = from vIncomeAndExpenses in incomeAndExpenses
                                join vContracts in contracts
                                on vIncomeAndExpenses.ContractId equals vContracts.ContractId into leftJoin
                                from subvContracts in leftJoin.DefaultIfEmpty()
                                select (vIncomeAndExpenses, subvContracts);

            var incomeAndExpensesNotEmpty = plusContracts.Where(x => !string.IsNullOrEmpty(x.subvContracts?.AreaOfActivity))
                .Select(y => new CashFlow
                {
                    Date = y.vIncomeAndExpenses.Date,
                    Receipt = y.vIncomeAndExpenses.Credit,
                    Payment = y.vIncomeAndExpenses.Debit,
                    TypeOperation = y.vIncomeAndExpenses.TypeOperation,
                    AreaOfActivity = y.subvContracts.AreaOfActivity,
                    Liter = y.vIncomeAndExpenses.Liter,
                    CostItem = y.vIncomeAndExpenses.CostItem,
                    ContractId = y.vIncomeAndExpenses.ContractId,
                    Contractor = y.subvContracts.Contractor,
                    Number = y.subvContracts.Number,
                    PaymentPurpose = y.vIncomeAndExpenses.PaymentPurpose
                });

            var incomeAndExpensesEmpty = plusContracts.Where(x => string.IsNullOrEmpty(x.subvContracts?.AreaOfActivity));
            var literAndCostItemInAreaOfActivity = gettingData.GetLiterAndCostItemInAreaOfActivity();
            var incomeAndExpensesEmptyPlusAreaOfActivity = from income in incomeAndExpensesEmpty
                                                           join areaOfActivity in literAndCostItemInAreaOfActivity
                                                           on income.vIncomeAndExpenses.Liter + income.vIncomeAndExpenses.CostItem equals areaOfActivity.Liter + areaOfActivity.CostItems
                                                           into tmp
                                                           from subareaOfActivity in tmp.DefaultIfEmpty()
                                                           select new CashFlow
                                                           {
                                                               Date = income.vIncomeAndExpenses.Date,
                                                               Receipt = income.vIncomeAndExpenses.Credit,
                                                               Payment = income.vIncomeAndExpenses.Debit,
                                                               PaymentPurpose = income.vIncomeAndExpenses.PaymentPurpose,
                                                               TypeOperation = income.vIncomeAndExpenses.TypeOperation,
                                                               AreaOfActivity = subareaOfActivity != null ? subareaOfActivity.AreaOfActivity : income.vIncomeAndExpenses.TypeOperation,
                                                               Liter = income.vIncomeAndExpenses.Liter,
                                                               CostItem = income.vIncomeAndExpenses.CostItem,
                                                               ContractId = income.vIncomeAndExpenses.ContractId,
                                                               Contractor = income.subvContracts?.Contractor,
                                                               Number = income.subvContracts?.Number
                                                           };

            var result = incomeAndExpensesNotEmpty.Concat(incomeAndExpensesEmptyPlusAreaOfActivity);
            _exportingReportsToExcel.Browse(result); // Source

            // -------------------------------------------------------

            var startCashFlow = result.Where(z => z.Date < startDate)
                                                 .GroupBy(x => x.AreaOfActivity)
                                                 .Select(y => new CashFlow
                                                 {
                                                     AreaOfActivity = y.Key,
                                                     Receipt = y.Sum(z => z.Receipt),
                                                     Payment = y.Sum(z => z.Payment),
                                                 });

            var startBalance = gettingData.StartBalance;

            foreach (var item in startCashFlow)
            {
                startBalance = startBalance + item.Receipt - item.Payment;
            }

            // -------------------------------------------------------

            var cashFlow = result.Where(z => z.Date >= startDate
                                                     && z.Date <= endDate)
                                            .GroupBy(x => x.AreaOfActivity)
                                            .Select(y => new CashFlow
                                            {
                                                AreaOfActivity = y.Key,
                                                Receipt = y.Sum(z => z.Receipt),
                                                Payment = y.Sum(z => z.Payment),
                                            })
                                            .Where(z => z.AreaOfActivity != "ПереводСДругогоСчета"
                                                     && z.AreaOfActivity != "ПереводНаДругойСчет")
                                            .OrderBy(or => or.AreaOfActivity);

            var tuple = (cashFlow, startBalance);

            return tuple;
        }

        public async Task<IEnumerable<Expense>> ExpenseAsync(Organizations organization) // Стоимость строительства объектов
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization);

            _exportingReportsToExcel.Browse(incomeAndExpenses);

            var contracts = gettingData.GetContracts();
            var plusContracts = from vIncomeAndExpenses in incomeAndExpenses
                                join vContracts in contracts
                                on vIncomeAndExpenses.ContractId equals vContracts.ContractId into leftJoin
                                from subvContracts in leftJoin.DefaultIfEmpty()
                                select (vIncomeAndExpenses, subvContracts);

            _exportingReportsToExcel.Browse(plusContracts);

            var contractsContractors = plusContracts.Where(x => x.subvContracts.ContractorOrSupplier == "Подрядчик")
                                          .GroupBy(y => y.subvContracts.ContractId)
                                          .Select(z => new Expense
                                          {
                                              ContractId = z.Key,
                                              Receipt = z.Sum(s => s.vIncomeAndExpenses.Credit),
                                              Payment = z.Sum(s => s.vIncomeAndExpenses.Debit),
                                              Contractor = z.FirstOrDefault().subvContracts.Contractor,
                                              Number = z.FirstOrDefault().subvContracts.Number,
                                              RateNDS = z.FirstOrDefault().subvContracts.RateNDS,
                                              GeneralContracting = z.FirstOrDefault().subvContracts.GeneralContracting,
                                              Liter = z.FirstOrDefault().subvContracts.Liter,
                                              ContractClosed = z.FirstOrDefault().subvContracts.ContractClosed,
                                              ContractorOrSupplier = z.FirstOrDefault().subvContracts.ContractorOrSupplier,
                                              CostItem = z.FirstOrDefault().subvContracts.CostItem,
                                              Date = z.FirstOrDefault().subvContracts.Date,
                                              Sum = z.FirstOrDefault().subvContracts.Sum,
                                              SecurityDeposit = z.FirstOrDefault().subvContracts.SecurityDeposit,
                                              Name = z.FirstOrDefault().subvContracts.Name
                                          });

            _exportingReportsToExcel.Browse(contractsContractors);

            var builder = contractsContractors.Where(y => y.NumberAA != "Гарантийное удержание").GroupBy(x => x.Contractor + x.Number).Select(y => new Expense
            {
                ContractId = y?.FirstOrDefault(z => string.IsNullOrEmpty(z?.NumberAA)).ContractId,
                Contractor = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Contractor,
                Number = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Number,
                Date = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Date,
                Sum = y.Sum(z => z.Sum),
                Liter = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA))?.Liter,
                CostItem = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).CostItem,
                Receipt = y.Sum(z => z.Receipt),
                Payment = y.Sum(z => z.Payment),
                ContractClosed = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ContractClosed,
                ContractorOrSupplier = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ContractorOrSupplier,
                GeneralContracting = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).GeneralContracting,
                RateNDS = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).RateNDS,
                Name = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Name,
                SecurityDeposit = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).SecurityDeposit,
                TotalArea = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).TotalArea,
                AmountUntil2026 = y.Sum(z => z.AmountUntil2026),
                RateNDS2026 = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).RateNDS2026
            }).ToList();

            builder.ForEach(item =>
            {
                if (item.ContractClosed == "Закрыт" || item.ContractClosed == "Расторгнут" || item.Receipt > item.Sum)
                {
                    item.ConstructionCost = item.Receipt;
                }
                else
                {
                    item.ConstructionCost = item.Sum ?? 0;
                }

                item.ConstructionCostNDS = item.AmountUntil2026 * (1.2M - item.RateNDS) + (item.ConstructionCost - item.AmountUntil2026) * (1.22M - item.RateNDS2026);
                item.InputNDS = item.AmountUntil2026 * item.RateNDS / (1 + item.RateNDS) + (item.Receipt - item.AmountUntil2026) * item.RateNDS2026 / (1 + item.RateNDS2026);
                item.Expenses = item.Receipt - item.Receipt * item.GeneralContracting - item.InputNDS;
            });

            var supplierAll = plusContracts.Where(x => x.subvContracts.ContractorOrSupplier == "Поставщик")
                .GroupBy(y => new { y.vIncomeAndExpenses.ContractId, y.vIncomeAndExpenses.Liter, y.vIncomeAndExpenses.CostItem, y.vIncomeAndExpenses.Date.Year })
                .Where(w => !string.IsNullOrEmpty(w.Key.Liter)).Select(z => new Expense
                {
                    ContractId = z.Key.ContractId,
                    Receipt = 0,
                    Payment = z.Sum(s => s.vIncomeAndExpenses.Debit),
                    Contractor = z.FirstOrDefault().subvContracts.Contractor,
                    Number = z.FirstOrDefault().subvContracts.Number,
                    RateNDS = z.FirstOrDefault().subvContracts.RateNDS,
                    RateNDS2026 = z.FirstOrDefault().subvContracts.RateNDS2026,
                    GeneralContracting = z.FirstOrDefault().subvContracts.GeneralContracting,
                    Liter = z.Key.Liter,
                    ContractClosed = z.FirstOrDefault().subvContracts.ContractClosed,
                    ContractorOrSupplier = z.FirstOrDefault().subvContracts.ContractorOrSupplier,
                    CostItem = z.Key.CostItem,
                    Date = z.FirstOrDefault().subvContracts.Date,
                    Sum = 0,
                    SecurityDeposit = z.FirstOrDefault().subvContracts.SecurityDeposit,
                    Name = z.FirstOrDefault().subvContracts.Name,
                    ConstructionCost = z.Sum(s => s.vIncomeAndExpenses.Debit),
                    Year = z.Key.Year
                }).Where(w => w.Payment != 0);

            var deliveriesThrough2026 = supplierAll.Where(x => x.Year < 2026).ToList();
            deliveriesThrough2026.ForEach(item =>
            {
                item.ConstructionCostNDS = item.ConstructionCost * (1.2M - item.RateNDS);
                item.InputNDS = item.Payment * item.RateNDS / (1 + item.RateNDS);
                item.Expenses = item.Payment - item.InputNDS;
            });
            var deliveriesAfter2025 = supplierAll.Where(x => x.Year >= 2026).ToList();
            deliveriesAfter2025.ForEach(item =>
            {
                item.ConstructionCostNDS = item.ConstructionCost * (1.22M - item.RateNDS2026);
                item.InputNDS = item.Payment * item.RateNDS2026 / (1 + item.RateNDS2026);
                item.Expenses = item.Payment - item.InputNDS;
            });

            var supplier = deliveriesThrough2026.Concat(deliveriesAfter2025);
            var builderPlusSupplier = builder.Concat(supplier);

            var facility = gettingData.GetFacility();
            var totalAreas = facility.GroupBy(y => y.ObjectNameIn1C).Select(x => new { ObjectNameIn1C = x.Key, x.FirstOrDefault().TotalArea });

            var plusFacility = from vBuilderPlusSupplier in builderPlusSupplier
                               join vTotalAreas in totalAreas
                               on vBuilderPlusSupplier.Liter equals vTotalAreas.ObjectNameIn1C into leftJoin
                               from subvTotalAreas in leftJoin.DefaultIfEmpty()
                               select new Expense
                               {
                                   ContractId = vBuilderPlusSupplier.ContractId,
                                   Receipt = vBuilderPlusSupplier.Receipt,
                                   Payment = vBuilderPlusSupplier.Payment,
                                   Contractor = vBuilderPlusSupplier.Contractor,
                                   Number = vBuilderPlusSupplier.Number,
                                   RateNDS = vBuilderPlusSupplier.RateNDS,
                                   GeneralContracting = vBuilderPlusSupplier.GeneralContracting,
                                   Liter = vBuilderPlusSupplier.Liter,
                                   ContractClosed = vBuilderPlusSupplier.ContractClosed,
                                   ContractorOrSupplier = vBuilderPlusSupplier.ContractorOrSupplier,
                                   CostItem = vBuilderPlusSupplier.CostItem,
                                   Date = vBuilderPlusSupplier.Date,
                                   Sum = vBuilderPlusSupplier.Sum,
                                   SecurityDeposit = vBuilderPlusSupplier.SecurityDeposit,
                                   Name = vBuilderPlusSupplier.Name,
                                   ConstructionCost = vBuilderPlusSupplier.ConstructionCost,
                                   TotalArea = subvTotalAreas?.TotalArea ?? 0,
                                   ConstructionCostNDS = vBuilderPlusSupplier.ConstructionCostNDS,
                                   InputNDS = vBuilderPlusSupplier.InputNDS,
                                   Expenses = vBuilderPlusSupplier.Expenses,
                                   AmountUntil2026 = vBuilderPlusSupplier.AmountUntil2026,
                                   RateNDS2026 = vBuilderPlusSupplier.RateNDS2026,
                               };

            return plusFacility.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number);
        }

        public async Task<IEnumerable<Expense>> CurrentDebtAsync(Organizations organization) // Текущая задолженность
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var expense = await ExpenseAsync(organization);
            foreach (var item in expense)
            {
                if (item.Liter.Contains("Смородина", StringComparison.OrdinalIgnoreCase))
                {
                    item.ResidentialComplex = "Смородина";
                    item.Number = item.Contractor + "   " + item.Number;
                    if (item.ContractorOrSupplier == "Подрядчик")
                    {
                        if (item.ContractClosed == "Закрыт" || item.ContractClosed == "Расторгнут")
                            item.CurrentDebt = item.Receipt - item.Receipt * item.GeneralContracting - item.Payment;
                        else
                            item.CurrentDebt = item.Receipt - item.Receipt * (item.GeneralContracting + item.SecurityDeposit) - item.Payment;
                    }
                }

                if (item.Liter.Contains("Кипарис", StringComparison.OrdinalIgnoreCase))
                {
                    item.ResidentialComplex = "Кипарис";
                    item.Number = item.Contractor + "   " + item.Number;
                    if (item.ContractorOrSupplier == "Подрядчик")
                    {
                        if (item.ContractClosed == "Закрыт" || item.ContractClosed == "Расторгнут")
                            item.CurrentDebt = item.Receipt - item.Receipt * item.GeneralContracting - item.Payment;
                        else
                            item.CurrentDebt = item.Receipt - item.Receipt * (item.GeneralContracting + item.SecurityDeposit) - item.Payment;
                    }
                }
            }
            return expense.Where(x => !string.IsNullOrEmpty(x.ResidentialComplex))
                       .OrderBy(y => y.ResidentialComplex)
                       .ThenBy(t => t.Liter)
                       .ThenBy(z => z.ContractorOrSupplier)
                       .ThenBy(o => o.CostItem);
        }

        public async Task<IEnumerable<ReconciliationStatement>> ReconciliationStatementAsync(string contractName, Organizations organization, string contractor) // Акт сверки
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization);
            var contracts = gettingData.GetContracts();
            var plusContracts = from vIncomeAndExpenses in incomeAndExpenses
                                join vContracts in contracts
                                on vIncomeAndExpenses.ContractId equals vContracts.ContractId into leftJoin
                                from subvContracts in leftJoin.DefaultIfEmpty()
                                select (vIncomeAndExpenses, subvContracts);

            var reconciliationStatement = plusContracts.GroupBy(y => y.vIncomeAndExpenses.ContractId)
                  .Select(z => new ReconciliationStatement
                  {
                      ContractId = z.Key,
                      Credit = z.Sum(s => s.vIncomeAndExpenses.Credit),
                      Debit = z.Sum(s => s.vIncomeAndExpenses.Debit),
                      Contractor = z.FirstOrDefault().subvContracts.Contractor,
                      Date = z.FirstOrDefault().subvContracts.Date,
                      Sum = z.FirstOrDefault().subvContracts.Sum,
                      Name = z.FirstOrDefault().subvContracts.Name,
                      DocumentName = z.FirstOrDefault().vIncomeAndExpenses.DocumentName
                  });

            return reconciliationStatement.Where(x => x.Name == contractName && x.Contractor == contractor);
        }

        public async Task<IEnumerable<Income>> IncomeAsync(Organizations organization) // Доходы от строительства объектов
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization);

            _exportingReportsToExcel.Browse(incomeAndExpenses);

            var contracts = gettingData.GetContracts();
            var plusContracts = from vIncomeAndExpenses in incomeAndExpenses
                                join vContracts in contracts
                                on vIncomeAndExpenses.ContractId equals vContracts.ContractId into leftJoin
                                from subvContracts in leftJoin.DefaultIfEmpty()
                                select (vIncomeAndExpenses, subvContracts);

            _exportingReportsToExcel.Browse(plusContracts);

            var buyersContracts = plusContracts.Where(x => x.subvContracts.ContractorOrSupplier == "Покупатель")
                              .GroupBy(y => y.subvContracts.ContractId)
                              .Select(z => new Income
                              {
                                  ContractId = z.Key,
                                  Receipt = z.Sum(s => s.vIncomeAndExpenses.Credit),
                                  Payment = z.Sum(s => s.vIncomeAndExpenses.Debit),
                                  Contractor = z.FirstOrDefault().subvContracts.Contractor,
                                  Number = z.FirstOrDefault().subvContracts.Number,
                                  Liter = z.FirstOrDefault().subvContracts.Liter,
                                  Date = z.FirstOrDefault().subvContracts.Date,
                                  Sum = z.FirstOrDefault().subvContracts.Sum,
                                  Name = z.FirstOrDefault().subvContracts.Name,
                                  AmountUntil2026 = z.FirstOrDefault().subvContracts.AmountUntil2026
                              });

            _exportingReportsToExcel.Browse(buyersContracts);

            var income = buyersContracts.GroupBy(x => x.Contractor + x.Number)
                              .Select(y => new Income
                              {
                                  Receipt = y.Sum(s => s.Receipt),
                                  Payment = y.Sum(s => s.Payment),
                                  ContractId = y?.FirstOrDefault().ContractId,
                                  Contractor = y.FirstOrDefault().Contractor,
                                  Number = y.FirstOrDefault().Number,
                                  Date = y.FirstOrDefault().Date,
                                  Sum = y.Sum(z => z.Sum),
                                  Liter = y.FirstOrDefault()?.Liter,
                                  Name = y.FirstOrDefault().Name,
                                  AmountUntil2026 = y.Sum(s => s.AmountUntil2026)
                              }).ToList();

            income.ForEach(item =>
            {
                item.OutgoingNDS = item.AmountUntil2026 * 0.2M + (item.Receipt - item.AmountUntil2026) * 0.22M;
            });

            return income.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number);
        }

        public async Task<IEnumerable<ActOfCompletionValue>> ActOfCompletionAsync(Organizations organization) // Акты об окончании СМР
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            return (await gettingData.ActOfCompletionAsync()).Value;
        }
    }
}