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
                    Date = DateOnly.FromDateTime(y.Date ?? new DateTime()) ,
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
                    TypeOperation = x.TypeOperation
                });

            return incomeAndExpenses.OrderBy(x => x.Date);
        }

















        public async Task<List<Domain.Cost>> CostAsync(Organizations organization) // Стоимость строительства объектов
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization);

            var contractor = incomeAndExpenses.Where(x => x.ContractorOrSupplier == "Подрядчик").GroupBy(y => y.ContractId).Select(z => new Domain.Cost
            {
                ContractId = z.Key,
                Receipt = z.Sum(s => s.Credit),
                Payment = z.Sum(s => s.Debit),
                Contractor = z.FirstOrDefault().Contractor,
                Number = z.FirstOrDefault().Number,
                RateNDS = z.FirstOrDefault().RateNDS,
                GeneralContracting = z.FirstOrDefault().GeneralContracting,
                ConstructionObject = z.FirstOrDefault().ConstructionObject,
                ContractClosed = z.FirstOrDefault().ContractClosed,
                ContractorOrSupplier = z.FirstOrDefault().ContractorOrSupplier,
                CostItem = z.FirstOrDefault().CostItem,
                Date = z.FirstOrDefault().Date,
                Sum = z.FirstOrDefault().SumContract,
                WarrantyLien = z.FirstOrDefault().WarrantyLien,
                Name = z.FirstOrDefault().Name
            });

            var contracts = gettingData.GetContracts().Where(x => x.ContractorOrSupplier == "Подрядчик");
            var contractsPlusContractor = from con in contracts
                                          join income in contractor
                                          on con.ContractId equals income.ContractId into tmp
                                          from subIncome in tmp.DefaultIfEmpty()
                                          select new Domain.Cost
                                          {
                                              ContractId = con.ContractId,
                                              Receipt = subIncome?.Receipt ?? 0,
                                              Payment = subIncome?.Payment ?? 0,
                                              Contractor = con.Contractor,
                                              Number = con.Number,
                                              RateNDS = con.RateNDS,
                                              GeneralContracting = con.GeneralContracting,
                                              ConstructionObject = con.Liter,
                                              ContractClosed = con.ContractClosed,
                                              ContractorOrSupplier = con.ContractorOrSupplier,
                                              CostItem = con.CostItem,
                                              Date = con.Date,
                                              Sum = con.Sum,
                                              WarrantyLien = con.WarrantyLien,
                                              Name = con.Name,
                                              NumberAA = con.NumberAA,
                                              AmountUntil2026 = con.AmountUntil2026,
                                              RateNDS2026 = con.RateNDS2026
                                          };

            var result = contractsPlusContractor.Where(y => y.NumberAA != "Гарантийное удержание").GroupBy(x => x.Contractor + x.Number).Select(y => new Domain.Cost
            {
                ContractId = y?.FirstOrDefault(z => string.IsNullOrEmpty(z?.NumberAA)).ContractId,
                Contractor = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Contractor,
                Number = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Number,
                Date = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Date,
                Sum = y.Sum(z => z.Sum),
                ConstructionObject = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA))?.ConstructionObject,
                CostItem = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).CostItem,
                Receipt = y.Sum(z => z.Receipt),
                Payment = y.Sum(z => z.Payment),
                ContractClosed = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ContractClosed,
                ContractorOrSupplier = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ContractorOrSupplier,
                GeneralContracting = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).GeneralContracting,
                RateNDS = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).RateNDS,
                Name = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Name,
                WarrantyLien = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).WarrantyLien,
                TotalArea = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).TotalArea,
                AmountUntil2026 = y.Sum(z => z.AmountUntil2026),
                RateNDS2026 = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).RateNDS2026
            }).ToList();

            result.ForEach(item =>
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

            var supplierAll = incomeAndExpenses.Where(x => x.ContractorOrSupplier == "Поставщик")
                .GroupBy(y => new { y.ContractId, y.LiterPayment, y.CostItemPayment, y.Date.Year }).Where(w => !string.IsNullOrEmpty(w.Key.LiterPayment)).Select(z => new Domain.Cost
                {
                    ContractId = z.Key.ContractId,
                    Receipt = 0,
                    Payment = z.Sum(s => s.Debit),
                    Contractor = z.FirstOrDefault().Contractor,
                    Number = z.FirstOrDefault().Number,
                    RateNDS = z.FirstOrDefault().RateNDS,
                    RateNDS2026 = z.FirstOrDefault().RateNDS2026,
                    GeneralContracting = z.FirstOrDefault().GeneralContracting,
                    ConstructionObject = z.Key.LiterPayment,
                    ContractClosed = z.FirstOrDefault().ContractClosed,
                    ContractorOrSupplier = z.FirstOrDefault().ContractorOrSupplier,
                    CostItem = z.Key.CostItemPayment,
                    Date = z.FirstOrDefault().Date,
                    Sum = 0,
                    WarrantyLien = z.FirstOrDefault().WarrantyLien,
                    Name = z.FirstOrDefault().Name,
                    ConstructionCost = z.Sum(s => s.Debit),
                    Year = z.Key.Year
                }).Where(w => w.Payment != 0).ToList();

            var supplierOld = supplierAll.Where(x => x.Year < 2026).ToList();
            supplierOld.ForEach(item =>
            {
                item.ConstructionCostNDS = item.ConstructionCost * (1.2M - item.RateNDS);
                item.InputNDS = item.Payment * item.RateNDS / (1 + item.RateNDS);
                item.Expenses = item.Payment - item.InputNDS;
            });
            var supplierNew = supplierAll.Where(x => x.Year >= 2026).ToList();
            supplierNew.ForEach(item =>
            {
                item.ConstructionCostNDS = item.ConstructionCost * (1.22M - item.RateNDS2026);
                item.InputNDS = item.Payment * item.RateNDS2026 / (1 + item.RateNDS2026);
                item.Expenses = item.Payment - item.InputNDS;
            });

            var supplier = supplierOld.Concat(supplierNew).ToList();
            var contractorOrSupplier = result.Concat(supplier).ToList();

            var facility = gettingData.GetFacility();
            var facilityGrouped = facility.GroupBy(y => y.ObjectNameIn1C).Select(x => new { ObjectNameIn1C = x.Key, x.FirstOrDefault().TotalArea });
            var PlusFacility = from income in contractorOrSupplier
                               join area in facilityGrouped
                               on income.ConstructionObject equals area.ObjectNameIn1C into tmp
                               from subArea in tmp.DefaultIfEmpty()
                               select new Domain.Cost
                               {
                                   ContractId = income.ContractId,
                                   Receipt = income.Receipt,
                                   Payment = income.Payment,
                                   Contractor = income.Contractor,
                                   Number = income.Number,
                                   RateNDS = income.RateNDS,
                                   GeneralContracting = income.GeneralContracting,
                                   ConstructionObject = income.ConstructionObject,
                                   ContractClosed = income.ContractClosed,
                                   ContractorOrSupplier = income.ContractorOrSupplier,
                                   CostItem = income.CostItem,
                                   Date = income.Date,
                                   Sum = income.Sum,
                                   WarrantyLien = income.WarrantyLien,
                                   Name = income.Name,
                                   NumberAA = income.NumberAA,
                                   ConstructionCost = income.ConstructionCost,
                                   TotalArea = subArea?.TotalArea ?? 0,
                                   Year = income.Year,
                                   ConstructionCostNDS = income.ConstructionCostNDS,
                                   InputNDS = income.InputNDS,
                                   Expenses = income.Expenses,
                                   AmountUntil2026 = income.AmountUntil2026,
                                   RateNDS2026 = income.RateNDS2026,
                               };

            return PlusFacility.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number).ToList();
        }













        public async Task<List<ReconciliationStatement>> ReconciliationStatementAsync(string contractName, Organizations organization, string contractor) // Акт сверки
        {
            return new List<ReconciliationStatement>();
        }













        //public async Task<List<Income>> IncomeAsync(Organizations organization) // Доходы от строительства объектов
        //{
        //    IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

        //    var incomeAndExpenses = await IncomeAndExpensesAsync(organization);

        //    var contractor = incomeAndExpenses.GroupBy(y => y.ContractId).Select(z => new Income
        //    {
        //        ContractId = z.Key,
        //        Payment = z.Sum(s => s.Credit),
        //        Receipt = z.Sum(s => s.Debit),
        //        Contractor = z.FirstOrDefault().Contractor,
        //        Number = z.FirstOrDefault().Number,
        //        ConstructionObject = z.FirstOrDefault().ConstructionObject,
        //        Date = z.FirstOrDefault().Date,
        //        Sum = z.FirstOrDefault().SumContract,
        //        Name = z.FirstOrDefault().Name
        //    });

        //    var contracts = gettingData.GetContracts().Where(x => x.ContractorOrSupplier == "Покупатель");
        //    var contractsPlusContractor = from con in contracts
        //                                  join income in contractor
        //                                  on con.ContractId equals income.ContractId into tmp
        //                                  from subIncome in tmp.DefaultIfEmpty()
        //                                  select new Income
        //                                  {
        //                                      ContractId = con.ContractId,
        //                                      Receipt = subIncome?.Receipt ?? 0,
        //                                      Payment = subIncome?.Payment ?? 0,
        //                                      Contractor = con.Contractor,
        //                                      Number = con.Number,
        //                                      ConstructionObject = con.Liter,
        //                                      Date = con.Date,
        //                                      Sum = con.Sum,
        //                                      Name = con.Name,
        //                                      NumberAA = con.NumberAA,
        //                                      AmountUntil2026 = con.AmountUntil2026,
        //                                  };

        //    var result = contractsPlusContractor.GroupBy(x => x.Contractor + x.Number).Select(y => new Income
        //    {
        //        ContractId = y?.FirstOrDefault().ContractId,
        //        Contractor = y.FirstOrDefault().Contractor,
        //        Number = y.FirstOrDefault().Number,
        //        Date = y.FirstOrDefault().Date,
        //        Sum = y.Sum(z => z.Sum),
        //        ConstructionObject = y.FirstOrDefault()?.ConstructionObject,
        //        Receipt = y.Sum(z => z.Receipt),
        //        Payment = y.Sum(z => z.Payment),
        //        Name = y.FirstOrDefault().Name,
        //        AmountUntil2026 = y.Sum(z => z.AmountUntil2026),
        //    }).ToList();

        //    result.ForEach(item =>
        //    {
        //        item.OutgoingNDS = item.AmountUntil2026 * 0.2M + (item.Receipt - item.AmountUntil2026) * 0.22M;
        //    });

        //    return result.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number).ToList();
        //}

        //public async Task<List<IncomeAndExpenses>> IncomeAndExpensesAsync(Organizations organization, DateOnly date, string costOrIncome = "") // Доходы и расходы по документам
        //{
        //    IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

        //    var plusLiterAndCostItemInPayments = (await ExpensePaymentsAsync(organization)).Where(x => x.Date >= date);

        //    var Payments = plusLiterAndCostItemInPayments.Select(p => new IncomeAndExpenses()
        //    {
        //        Date = p.Date,
        //        Debit = p.PaymentAmount,
        //        ContractId = p.ContractId,
        //        Liter = p.Liter,
        //        CostItem = p.CostItem,
        //        TypeOperation = p.TypeOperation,
        //        DocumentName = "Списание с расчетного счета"
        //    });

        //    var receiptGoodsServices = (await gettingData.ReceiptGoodsServicesAsync()).Value.Where(x => DateOnly.FromDateTime(x.Date) >= date);
        //    var ReceiptGoodsServices = receiptGoodsServices.Select(p => new IncomeAndExpenses()
        //    {
        //        Date = DateOnly.FromDateTime(p.Date),
        //        Credit = p.DocumentAmount,
        //        ContractId = p.ContractId,
        //        DocumentName = "Поступление товаров и услуг"
        //    });

        //    var receiptProcessing = (await gettingData.ReceiptProcessingAsync()).Value.Where(x => DateOnly.FromDateTime(x.Date) >= date);
        //    var ReceiptProcessing = receiptProcessing.Select(p => new IncomeAndExpenses()
        //    {
        //        Date = DateOnly.FromDateTime(p.Date),
        //        Credit = p.DocumentAmount,
        //        ContractId = p.ContractId,
        //        DocumentName = "Поступление из переработки"
        //    });

        //    var paymentsPlusreceiptGoodsServices = Payments.Concat(ReceiptGoodsServices);
        //    var plusReceiptProcessing = paymentsPlusreceiptGoodsServices.Concat(ReceiptProcessing);

        //    var selling = (await gettingData.SaleGoodsServicesAsync()).Value
        //        .Where(x => DateOnly.FromDateTime(x.Date) >= date)
        //                        .Select(y => new IncomeAndExpenses
        //                        {
        //                            Date = DateOnly.FromDateTime(y.Date),
        //                            Debit = y.DocumentAmount,
        //                            ContractId = y.ContractId,
        //                            DocumentName = "Реализация товаров и услуг"
        //                        });

        //    var plusSelling = plusReceiptProcessing.Concat(selling);
        //    // ---------------------------------------------------------------------------------------------------------------

        //    var debtAdjustment = (await gettingData.DebtAdjustmentAsync()).Value.ToList();
        //    // Убираем из Корректировки долга проводки по одному договору в одном документе Корректировка долга
        //    foreach (var item in debtAdjustment)
        //    {
        //        if (item.AccountsPayable.Length > 0 && item.AccountsReceivable.Length > 0
        //            && item.AccountsPayable.First().ContractId == item.AccountsReceivable.First().ContractId)
        //        {
        //            item.DeletionMark = true;
        //        }
        //        if (item.AccountsPayable.Length > 0 && item.AccountsPayable.First().ContractId == item.AccountsPayable.First().CorContractId)
        //        {
        //            item.DeletionMark = true;
        //        }
        //        if (item.AccountsReceivable.Length > 0 && item.AccountsReceivable.First().ContractId == item.AccountsReceivable.First().CorContractId)
        //        {
        //            item.DeletionMark = true;
        //        }
        //    }
        //    debtAdjustment.RemoveAll(x => x.DeletionMark);
        //    _exportingReportsToExcel.Browse(debtAdjustment); // сравнить

        //    var Payable = debtAdjustment.SelectMany(x => x.AccountsPayable, (p, c) =>
        //    new { p.Ref_Key, c.ContractId, c.CorContractId, c.Sum, p.Date, p })
        //        .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

        //    var Receivable = debtAdjustment.SelectMany(x => x.AccountsReceivable, (p, c) =>
        //    new { p.Ref_Key, c.ContractId, c.CorContractId, c.Sum, p.Date, p })
        //        .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

        //    var payableIncomeAndExpenses = Payable.Select(y => new IncomeAndExpenses
        //    {
        //        Date = DateOnly.FromDateTime(y.Date),
        //        Debit = y.Sum,
        //        ContractId = y.ContractId,
        //        DocumentName = "Корректировка долга"
        //    });

        //    var receivableIncomeAndExpenses = Receivable.Select(y => new IncomeAndExpenses
        //    {
        //        Date = DateOnly.FromDateTime(y.Date),
        //        Credit = y.Sum,
        //        ContractId = y.ContractId,
        //        DocumentName = "Корректировка долга"
        //    });

        //    var plusPayable = plusSelling.Concat(payableIncomeAndExpenses);
        //    var plusReceivable = plusPayable.Concat(receivableIncomeAndExpenses);

        //    var ReceivableDoubleEntry = debtAdjustment.Where(x => x.AccountsReceivable.Length == 0).SelectMany(x => x.AccountsPayable, (p, c) =>
        //    new { p.Ref_Key, CounterpartyAgreementId = c.CorContractId, c.Sum, p.Date, p })
        //        .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

        //    var PayableDoubleEntry = debtAdjustment.Where(x => x.AccountsPayable.Length == 0).SelectMany(x => x.AccountsReceivable, (p, c) =>
        //    new { p.Ref_Key, CounterpartyAgreementId = c.CorContractId, c.Sum, p.Date, p })
        //        .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

        //    var PlusReceivableDoubleEntry = ReceivableDoubleEntry.Select(y => new IncomeAndExpenses
        //    {
        //        Date = DateOnly.FromDateTime(y.Date),
        //        Credit = y.Sum,
        //        ContractId = y.CounterpartyAgreementId,
        //        DocumentName = "Корректировка долга"
        //    });

        //    var PlusPayableDoubleEntry = PayableDoubleEntry.Select(y => new IncomeAndExpenses
        //    {
        //        Date = DateOnly.FromDateTime(y.Date),
        //        Debit = y.Sum,
        //        ContractId = y.CounterpartyAgreementId,
        //        DocumentName = "Корректировка долга"
        //    });

        //    var plusReceivableDoubleEntry = plusReceivable.Concat(PlusReceivableDoubleEntry);
        //    var plusPayableDoubleEntry = plusReceivableDoubleEntry.Concat(PlusPayableDoubleEntry);

        //    _exportingReportsToExcel.Browse(plusPayableDoubleEntry); // сравнить

        //    // ---------------------------------------------------------------------------------------------------------------

        //    var receiptToCurrentAccount = (await IncomePaymentsAsync(organization))
        //        .Where(x => x.Date >= date)
        //                        .Select(y => new IncomeAndExpenses
        //                        {
        //                            Date = y.Date,
        //                            Credit = y.PaymentAmount,
        //                            ContractId = y.ContractId,
        //                            TypeOperation = y.TypeOperation,
        //                            DocumentName = "Поступление на расчетный счет"
        //                        });

        //    var plusreceiptToCurrentAccount = plusPayableDoubleEntry.Concat(receiptToCurrentAccount);

        //    var operations = gettingData.GetOperations();
        //    var operationDebit = operations.Where(x => x.Date >= date)
        //            .Select(y => new IncomeAndExpenses
        //            {
        //                Date = y.Date,
        //                Debit = y.Sum,
        //                ContractId = y.ContractDebit,
        //                DocumentName = "Операция"
        //            });

        //    var plusOperationDebit = plusreceiptToCurrentAccount.Concat(operationDebit);

        //    var operationCredit = operations.Where(x => x.Date >= date)
        //            .Select(y => new IncomeAndExpenses
        //            {
        //                Date = y.Date,
        //                Credit = y.Sum,
        //                ContractId = y.ContractCredit,
        //                DocumentName = "Операция"
        //            });

        //    var plusOperationCredit = plusOperationDebit.Concat(operationCredit);

        //    var implementationConstructionWorks = (await gettingData.ImplementationConstructionWorksAsync()).Value
        //            ?.Where(x => DateOnly.FromDateTime(x.Date) >= date)
        //            ?.Select(y => new IncomeAndExpenses
        //            {
        //                Date = DateOnly.FromDateTime(y.Date),
        //                Debit = y.DocumentAmount,
        //                ContractId = y.ContractId,
        //                DocumentName = "Реализация строительных работ и услуг"
        //            });

        //    var plusImplementationConstructionWorks = implementationConstructionWorks != null ? plusOperationCredit.Concat(implementationConstructionWorks)
        //                                                                                      : plusOperationCredit;
        //    //var contract = new List<Contracts>();
        //    //if (costOrIncome == "Затраты")
        //    //    contract = gettingData.GetContracts().Where(x => x.ContractorOrSupplier != "Покупатель").ToList();
        //    //else if (costOrIncome == "Доходы")
        //    //    contract = gettingData.GetContracts().Where(x => x.ContractorOrSupplier == "Покупатель").ToList();
        //    //else
        //        var contract = gettingData.GetContracts().ToList();
        //    _exportingReportsToExcel.Browse(contract); // сравнить

        //    var plusContract = from p in plusImplementationConstructionWorks
        //                       join c in contract
        //                       on p.ContractId equals c.ContractId into tmp
        //                       from subC in tmp.DefaultIfEmpty()
        //                       select new { p, subC = subC ?? new Contracts() };

        //    var incomeAndExpenses = plusContract.Select(x => new IncomeAndExpenses
        //    {
        //        ContractId = x.subC.ContractId,
        //        DocumentName = x.p.DocumentName,
        //        Date = x.p.Date,
        //        CostItem = x.subC.CostItem,
        //        TypeOperation = x.p.TypeOperation,
        //        Debit = x.p.Debit,
        //        Credit = x.p.Credit,
        //        AreaOfActivity = x.subC.AreaOfActivity,
        //        Contractor = x.subC.Contractor,
        //        Number = x.subC.Number
        //    });

        //    return incomeAndExpenses.OrderBy(x => x.Date).ToList();
        //}

        //public async Task<IEnumerable<Contracts>> MovementUnderContractsAsync(Organizations organization) // Движение по договорам
        //{
        //    IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

        //    var incomeAndExpenses = await IncomeAndExpensesAsync(organization, new DateOnly(2023, 1, 1));
        //    var contracts = incomeAndExpenses.GroupBy(x => x.ContractId).Select(y => new Contracts
        //    {
        //        ContractId = y.Key,
        //        Sum = y.Sum(z => z.Payment + z.Receipt)
        //    });

        //    return contracts;
        //}

        public async Task<List<CashFlow>> CashFlowAsync(Organizations organization, DateOnly startDate, DateOnly endDate) // ДДС
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = (await IncomeAndExpensesAsync(organization, gettingData.StartDate))
            //var incomeAndExpenses = (await IncomeAndExpensesAsync(organization))
                .Where(w => (w.DocumentName == "Списание с расчетного счета" || w.DocumentName == "Поступление на расчетный счет")).ToList();
            var literAndCostItemInAreaOfActivity = gettingData.GetLiterAndCostItemInAreaOfActivity();

            var incomeAndExpensesNotEmpty = incomeAndExpenses.Where(x => !string.IsNullOrEmpty(x.AreaOfActivity));
            var incomeAndExpensesEmpty = incomeAndExpenses.Where(x => string.IsNullOrEmpty(x.AreaOfActivity));
            var incomeAndExpensesEmptyPlusAreaOfActivity = from income in incomeAndExpensesEmpty
                                                           join areaOfActivity in literAndCostItemInAreaOfActivity
                                                           on income.Liter + income.CostItem equals areaOfActivity.Liter + areaOfActivity.CostItems
                                                           into tmp
                                                           from subareaOfActivity in tmp.DefaultIfEmpty()
                                                           select new IncomeAndExpenses
                                                           {
                                                               Date = income.Date,
                                                               DocumentName = income.DocumentName,
                                                               Credit = income.Credit,
                                                               Debit = income.Debit,
                                                               TypeOperation = income.TypeOperation,
                                                               AreaOfActivity = subareaOfActivity != null ? subareaOfActivity.AreaOfActivity : income.TypeOperation,
                                                               Liter = income.Liter,
                                                               CostItem = income.CostItem,
                                                               ContractId = income.ContractId,
                                                               Contractor = income.Contractor,
                                                               Number = income.Number
                                                           };

            var result = incomeAndExpensesNotEmpty.Concat(incomeAndExpensesEmptyPlusAreaOfActivity).ToList();
            _exportingReportsToExcel.Browse(result); // сравнить

            // -------------------------------------------------------

            var startCashFlow = result.Where(z => z.Date < startDate)
                                                 .GroupBy(x => x.AreaOfActivity)
                                                 .Select(y => new CashFlow
                                                 {
                                                     AreaOfActivity = y.Key,
                                                     Receipt = y.Sum(z => z.Credit),
                                                     Payment = y.Sum(z => z.Debit),
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
                                                Receipt = y.Sum(z => z.Credit),
                                                Payment = y.Sum(z => z.Debit),
                                            })
                                            .Where(z => z.AreaOfActivity != "ПереводСДругогоСчета"
                                                     && z.AreaOfActivity != "ПереводНаДругойСчет")
                                            .OrderBy(or => or.AreaOfActivity)
                                            .ToList();

            cashFlow[0].Organization = organization.ToString();
            cashFlow[0].StartDate = startDate;
            cashFlow[0].EndDate = endDate;
            cashFlow[0].StartBalance = startBalance;

            return cashFlow;
        }

        //public async Task<List<IncomeAndExpenses>> NoAreaOfActivityAsync(Organizations organization, DateOnly startDate, DateOnly endDate) // ДДС
        //{
        //    IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

        //    //var incomeAndExpenses = (await IncomeAndExpensesAsync(organization, new DateOnly(2026, 1, 1)))
        //    var incomeAndExpenses = (await IncomeAndExpensesAsync(organization))
        //        .Where(w => (w.DocumentName == "Списание с расчетного счета" || w.DocumentName == "Поступление на расчетный счет")).ToList();
        //    var literAndCostItemInAreaOfActivity = gettingData.GetLiterAndCostItemInAreaOfActivity();

        //    var incomeAndExpensesNotEmpty = incomeAndExpenses.Where(x => !string.IsNullOrEmpty(x.AreaOfActivity));
        //    var incomeAndExpensesEmpty = incomeAndExpenses.Where(x => string.IsNullOrEmpty(x.AreaOfActivity));
        //    var incomeAndExpensesEmptyPlusAreaOfActivity = from income in incomeAndExpensesEmpty
        //                                                   join areaOfActivity in literAndCostItemInAreaOfActivity
        //                                                   on income.Liter + income.CostItem equals areaOfActivity.Liter + areaOfActivity.CostItems
        //                                                   into tmp
        //                                                   from subareaOfActivity in tmp.DefaultIfEmpty()
        //                                                   select new IncomeAndExpenses
        //                                                   {
        //                                                       Date = income.Date,
        //                                                       Credit = income.Credit,
        //                                                       Debit = income.Debit,
        //                                                       TypeOperation = income.TypeOperation,
        //                                                       AreaOfActivity = subareaOfActivity != null ? subareaOfActivity.AreaOfActivity : income.TypeOperation,
        //                                                       Liter = income.Liter,
        //                                                       CostItem = income.CostItem,
        //                                                       DocumentName = income.DocumentName,
        //                                                       //Contractor = income.Contractor,
        //                                                       //Number = income.Number,
        //                                                       ContractId = income.ContractId
        //                                                   };

        //    var result = incomeAndExpensesNotEmpty.Concat(incomeAndExpensesEmptyPlusAreaOfActivity).ToList();
        //    return result;
        //}

        public async Task<IEnumerable<Domain.Cost>> CurrentDebtAsync(Organizations organization) // Текущая задолженность
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var cost = await CostAsync(organization);
            foreach (var item in cost)
            {
                if (item.ConstructionObject.Contains("Смородина", StringComparison.OrdinalIgnoreCase))
                {
                    item.ResidentialComplex = "Смородина";
                    item.Number = item.Contractor + "   " + item.Number;
                    if (item.ContractorOrSupplier == "Подрядчик")
                    {
                        if (item.ContractClosed == "Закрыт" || item.ContractClosed == "Расторгнут")
                            item.CurrentDebt = item.Receipt - item.Receipt * item.GeneralContracting - item.Payment;
                        else
                            item.CurrentDebt = item.Receipt - item.Receipt * (item.GeneralContracting + item.WarrantyLien) - item.Payment;
                    }
                }

                if (item.ConstructionObject.Contains("Кипарис", StringComparison.OrdinalIgnoreCase))
                {
                    item.ResidentialComplex = "Кипарис";
                    item.Number = item.Contractor + "   " + item.Number;
                    if (item.ContractorOrSupplier == "Подрядчик")
                    {
                        if (item.ContractClosed == "Закрыт" || item.ContractClosed == "Расторгнут")
                            item.CurrentDebt = item.Receipt - item.Receipt * item.GeneralContracting - item.Payment;
                        else
                            item.CurrentDebt = item.Receipt - item.Receipt * (item.GeneralContracting + item.WarrantyLien) - item.Payment;
                    }
                }
            }
            return cost.Where(x => !string.IsNullOrEmpty(x.ResidentialComplex))
                       .OrderBy(y => y.ResidentialComplex)
                       .ThenBy(t => t.ConstructionObject)
                       .ThenBy(z => z.ContractorOrSupplier)
                       .ThenBy(o => o.CostItem);
        }

        public async Task<IEnumerable<ActOfCompletionValue>> ActOfCompletionAsync(Organizations organization) // Акты об окончании СМР
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var actOfCompletion = (await gettingData.ActOfCompletionAsync()).Value;

            return actOfCompletion;
        }
    }
}