using Cost.Domain;
using Cost.Infrastructure.Repositories.Models.ActOfCompletion;
using Cost.Presentation.DTO.Request;
using Cost.Presentation.DTO.Response;
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

            //var serb = multiplePayments.Where(z => z.Date >= new DateOnly(2026, 4, 1) && z.Date <= new DateOnly(2026, 4, 10))
            //    .GroupBy(x => x.PaymentId).Select(y => new { y.Key, Count = y.Count() }).ToList();
            //_exportingReportsToExcel.Browse(serb);

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

            var additionalInformation = (await gettingData.AdditionalInformationAsync()).Value ?? [];
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
                //TypeOfActivity = x.subvExpensePaymentsFromExcel?.TypeOfActivity,
                //AreaOfActivity = x.subvExpensePaymentsFromExcel?.AreaOfActivity,
                //ContractIdIncome = x.subvExpensePaymentsFromExcel?.ContractIdIncome,
                PaymentPurpose = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentPurpose,
                TypeOperation = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.TypeOperation,
                CommentFromPaymentInvoice = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.subvSupplierPaymentInvoice?.Comment,
                PaymentDetailsId = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentDetailsId
            }).OrderByDescending(x => x.Date);

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
            var plusBuyerPaymentInvoice = from vAllPayments in allPayments
                                          join vbuyerPaymentInvoice in buyerPaymentInvoice
                                          on vAllPayments.PaymentDetailsId equals vbuyerPaymentInvoice.BuyerPaymentInvoiceId into leftJoin
                                          from subvbuyerPaymentInvoice in leftJoin.DefaultIfEmpty()
                                          select new { vAllPayments, subvbuyerPaymentInvoice?.Comment };

            //var incomePaymentsFromExcel = gettingData.IncomePaymentsFromExcel();
            //var plusIncomePaymentsFromExcel = from vPlusBuyerPaymentInvoice in plusBuyerPaymentInvoice
            //                                  join vIncomePaymentsFromExcel in incomePaymentsFromExcel
            //                                   on vPlusBuyerPaymentInvoice.vAllPayments.PaymentId equals vIncomePaymentsFromExcel.PaymentId into leftJoin
            //                                  from subvIncomePaymentsFromExcel in leftJoin.DefaultIfEmpty()
            //                                  select new { vPlusBuyerPaymentInvoice, subvIncomePaymentsFromExcel };

            var result = plusBuyerPaymentInvoice.Select(x => new Payment
            {
                PaymentId = x.vAllPayments.PaymentId,
                Date = x.vAllPayments.Date,
                PaymentAmount = x.vAllPayments.PaymentAmount,
                ContractId = x.vAllPayments.ContractId,
                PaymentPurpose = x.vAllPayments.PaymentPurpose,
                TypeOperation = x.vAllPayments.TypeOperation,
                CommentFromPaymentInvoice = x.Comment,
                PaymentDetailsId = x.vAllPayments.PaymentDetailsId,
                //TypeOfActivity = x.subvIncomePaymentsFromExcel?.TypeOfActivity,
                //AreaOfActivity = x.subvIncomePaymentsFromExcel?.AreaOfActivity,
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
                    ContractId = y.ContractId,
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
                    //TypeOfActivity = x.TypeOfActivity,
                    //AreaOfActivity = x.AreaOfActivity,
                    //ContractIdIncome = x.ContractIdIncome,
                    PaymentId = x.PaymentId,
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
                    //TypeOfActivity = x.TypeOfActivity,
                    //AreaOfActivity = x.AreaOfActivity,
                    PaymentId = x.PaymentId,
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
                ?.Select(y => new IncomeAndExpenses
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
                    PaymentPurpose = x.PaymentPurpose,
                    //TypeOfActivity = x.TypeOfActivity,
                    //AreaOfActivity = x.AreaOfActivity,
                    //ContractIdIncome = x.ContractIdIncome,
                    PaymentId = x.PaymentId
                });

            return incomeAndExpenses.OrderBy(x => x.Date);
        }

        public async Task<IEnumerable<CashFlow>> CashFlowSourceAsync(Organizations organization, DateOnly startDate, DateOnly endDate) // ДДС Source
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = (await IncomeAndExpensesAsync(organization)).Where(w => w.Date >= gettingData.StartDate
                && (w.DocumentName == "Списание с расчетного счета" || w.DocumentName == "Поступление на расчетный счет"));



            var addAreaOfActivity = AddAreaOfActivity(organization, incomeAndExpenses);



            var contracts = gettingData.GetContracts();
            var result = from vIncomeAndExpenses in addAreaOfActivity
                         join vContracts in contracts
                         on vIncomeAndExpenses.ContractId equals vContracts.ContractId into leftJoin
                         from subvContracts in leftJoin.DefaultIfEmpty()
                         select new CashFlow
                         {
                             Date = vIncomeAndExpenses.Date,
                             Debit = vIncomeAndExpenses.Credit,
                             Credit = vIncomeAndExpenses.Debit,
                             TypeOperation = vIncomeAndExpenses.TypeOperation,
                             TypeOfActivity = string.IsNullOrEmpty(vIncomeAndExpenses.TypeOfActivity) ? subvContracts?.TypeOfActivity : vIncomeAndExpenses.TypeOfActivity,
                             AreaOfActivity = AreaOfActivity(vIncomeAndExpenses.AreaOfActivity, subvContracts?.AreaOfActivity, vIncomeAndExpenses.TypeOperation),
                             Liter = vIncomeAndExpenses.Liter,
                             CostItem = vIncomeAndExpenses.CostItem,
                             Contractor = subvContracts?.Contractor,
                             Number = subvContracts?.Number,
                             RateNDS = subvContracts?.RateNDS2026 ?? 0,
                             PaymentPurpose = vIncomeAndExpenses.PaymentPurpose,
                             PaymentId = vIncomeAndExpenses.PaymentId
                         };

            _exportingReportsToExcel.Browse(result.Where(z => z.Date >= startDate && z.Date <= endDate)); // Source

            return result;
        }

        public decimal StartBalance(IEnumerable<CashFlow> cashFlow, Organizations organization, DateOnly startDate)
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            var startBalance = gettingData.StartBalance;

            var startCashFlow = cashFlow.Where(z => z.Date < startDate)
                                        .GroupBy(x => new { x.TypeOfActivity, x.AreaOfActivity })
                                        .Select(y => new CashFlow
                                        {
                                            TypeOfActivity = y.Key.TypeOfActivity,
                                            AreaOfActivity = y.Key.AreaOfActivity,
                                            Debit = y.Sum(z => z.Debit),
                                            Credit = y.Sum(z => z.Credit),
                                        });

            foreach (var item in startCashFlow)
            {
                startBalance = startBalance + item.Debit - item.Credit;
            }

            return startBalance;
        }

        public IEnumerable<CashFlow> CashFlow(List<CashFlow> cashFlow, Organizations organization, DateOnly startDate, DateOnly endDate) // ДДС
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            // -------------------------------------------------
            //var indirectCosts = gettingData.GetIndirectCosts().Where(x => x.Date >= startDate && x.Date <= endDate).ToList();
            //var serb = indirectCosts.Where(z => z.Date >= new DateOnly(2026, 4, 1) && z.Date <= new DateOnly(2026, 4, 10))
            //    .GroupBy(x => x.PaymentId).Select(y => new { y.Key, Count = y.Count() }).ToList();
            //_exportingReportsToExcel.Browse(indirectCosts);

            //var plusIndirectCosts = from vCashFlow in cashFlow
            //                        join vIndirectCosts in indirectCosts
            //                        on vCashFlow.PaymentId equals vIndirectCosts.PaymentId into leftJoin
            //                        from subvIndirectCosts in leftJoin.DefaultIfEmpty()
            //                        select new { vCashFlow, subvIndirectCosts };

            //var indirectCostsAdded = new List<CashFlow>();
            //foreach (var item in plusIndirectCosts)
            //{
            //    if (!string.IsNullOrEmpty(item.subvIndirectCosts?.PaymentId))
            //    {
            //        if (item.subvIndirectCosts.Ketov != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Производственная деятельность (включая косвенные расходы)",
            //                AreaOfActivity = "Субподряд (Кетов)",
            //                IndirectCosts = item.subvIndirectCosts.DirectOrIndirect ? 0 : item.vCashFlow.Payment * item.subvIndirectCosts.Ketov,
            //                Payment = item.subvIndirectCosts.DirectOrIndirect ? item.vCashFlow.Payment * item.subvIndirectCosts.Ketov : 0
            //            });
            //        if (item.subvIndirectCosts.Gontar != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Производственная деятельность (включая косвенные расходы)",
            //                AreaOfActivity = "Субподряд (Гонтарь)",
            //                IndirectCosts = item.subvIndirectCosts.DirectOrIndirect ? 0 : item.vCashFlow.Payment * item.subvIndirectCosts.Gontar,
            //                Payment = item.subvIndirectCosts.DirectOrIndirect ? item.vCashFlow.Payment * item.subvIndirectCosts.Gontar : 0
            //            });
            //        if (item.subvIndirectCosts.Endulsi != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Производственная деятельность (включая косвенные расходы)",
            //                AreaOfActivity = "Субподряд (Эндульси)",
            //                IndirectCosts = item.subvIndirectCosts.DirectOrIndirect ? 0 : item.vCashFlow.Payment * item.subvIndirectCosts.Endulsi,
            //                Payment = item.subvIndirectCosts.DirectOrIndirect ? item.vCashFlow.Payment * item.subvIndirectCosts.Endulsi : 0
            //            });
            //        if (item.subvIndirectCosts.TechnicalCustomer != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Производственная деятельность (включая косвенные расходы)",
            //                AreaOfActivity = "Технический заказчик",
            //                IndirectCosts = item.subvIndirectCosts.DirectOrIndirect ? 0 : item.vCashFlow.Payment * item.subvIndirectCosts.TechnicalCustomer,
            //                Payment = item.subvIndirectCosts.DirectOrIndirect ? item.vCashFlow.Payment * item.subvIndirectCosts.TechnicalCustomer : 0
            //            });
            //        if (item.subvIndirectCosts.TransportRental != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Производственная деятельность (включая косвенные расходы)",
            //                AreaOfActivity = "Аренда транспорта",
            //                IndirectCosts = item.subvIndirectCosts.DirectOrIndirect ? 0 : item.vCashFlow.Payment * item.subvIndirectCosts.TransportRental,
            //                Payment = item.subvIndirectCosts.DirectOrIndirect ? item.vCashFlow.Payment * item.subvIndirectCosts.TransportRental : 0
            //            });
            //        if (item.subvIndirectCosts.SalesDepartment != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Производственная деятельность (включая косвенные расходы)",
            //                AreaOfActivity = "Отдел продаж",
            //                IndirectCosts = item.subvIndirectCosts.DirectOrIndirect ? 0 : item.vCashFlow.Payment * item.subvIndirectCosts.SalesDepartment,
            //                Payment = item.subvIndirectCosts.DirectOrIndirect ? item.vCashFlow.Payment * item.subvIndirectCosts.SalesDepartment : 0
            //            });
            //        if (item.subvIndirectCosts.Rent != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Производственная деятельность (включая косвенные расходы)",
            //                AreaOfActivity = "Аренда",
            //                IndirectCosts = item.subvIndirectCosts.DirectOrIndirect ? 0 : item.vCashFlow.Payment * item.subvIndirectCosts.Rent,
            //                Payment = item.subvIndirectCosts.DirectOrIndirect ? item.vCashFlow.Payment * item.subvIndirectCosts.Rent : 0
            //            });
            //        if (item.subvIndirectCosts.Withdrawal != 0)
            //            indirectCostsAdded.Add(new CashFlow
            //            {
            //                Date = item.vCashFlow.Date,
            //                TypeOfActivity = "Финансовая деятельность",
            //                AreaOfActivity = "Отвлечение",
            //                Payment = item.vCashFlow.Payment * item.subvIndirectCosts.Withdrawal
            //            });
            //    }
            //}

            //var countRemove = cashFlow.RemoveAll(x => indirectCosts.Any(y => y.PaymentId == x.PaymentId));
            //var cashFlowAdded = cashFlow.Concat(indirectCostsAdded);
            // -------------------------------------------------

            var cashFlowGroup = cashFlow.Where(z => z.Date >= startDate && z.Date <= endDate)
                                 .GroupBy(x => new { x.TypeOfActivity, x.AreaOfActivity })
                                 .Select(y => new CashFlow
                                 {
                                     TypeOfActivity = y.Key.TypeOfActivity,
                                     AreaOfActivity = y.Key.AreaOfActivity,
                                     Debit = y.Sum(z => z.Debit),
                                     Credit = y.Sum(z => z.Credit),
                                     IndirectCosts = y.Sum(z => z.IndirectCosts)
                                 })
                                 .Where(z => z.AreaOfActivity != "ПереводСДругогоСчета" && z.AreaOfActivity != "ПереводНаДругойСчет");

            var groupTypeOfActivity = cashFlowGroup.GroupBy(y => y.TypeOfActivity)
                .Select(x => new { typeOfActivity = x.Key, sumTypeOfActivity = x.Sum(z => z.Debit) - x.Sum(z => z.Credit) - x.Sum(z => z.IndirectCosts) });
            
            var order = new List<CashFlowOrder>
            {
                new() { Name = "Производственная деятельность (включая косвенные расходы)", Order = 1 },
                new() { Name = "Инвестиционная деятельность", Order = 2 },
                new() { Name = "Финансовая деятельность", Order = 3 }
            };
            
            var orderTypeOfActivity = from vGroupTypeOfActivity in groupTypeOfActivity
                                      join vOrder in order
                                      on vGroupTypeOfActivity.typeOfActivity equals vOrder.Name into leftJoin
                                      from subvOrder in leftJoin.DefaultIfEmpty()
                                      select new { vGroupTypeOfActivity, subvOrder?.Order };

            var cashFlowFinal = from vCashFlow in cashFlowGroup
                                join vOrderTypeOfActivity in orderTypeOfActivity
                                on vCashFlow.TypeOfActivity equals vOrderTypeOfActivity.vGroupTypeOfActivity.typeOfActivity into leftJoin
                                from subvOrderTypeOfActivity in leftJoin.DefaultIfEmpty()
                                orderby subvOrderTypeOfActivity?.Order ?? 0
                                select new CashFlow
                                {
                                    AreaOfActivity = vCashFlow.AreaOfActivity,
                                    TypeOfActivity = vCashFlow.TypeOfActivity,
                                    Debit = vCashFlow.Debit,
                                    Credit = vCashFlow.Credit,
                                    IndirectCosts = vCashFlow.IndirectCosts,
                                    SumTypeOfActivity = subvOrderTypeOfActivity?.vGroupTypeOfActivity != null ? subvOrderTypeOfActivity.vGroupTypeOfActivity.sumTypeOfActivity : 0
                                };

            return cashFlowFinal;
        }

        public async Task<IEnumerable<Expense>> CurrentDebtAsync(Organizations organization) // Текущая задолженность
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var expense = (await ExpenseAsync(organization)).ToList();
            foreach (var item in expense)
            {
                if (item.Liter.Contains("Смородина", StringComparison.OrdinalIgnoreCase))
                {
                    item.ResidentialComplex = "Смородина";
                    item.Number = item.Contractor + "   " + item.Number;
                    if (item.ContractorOrSupplier == "Подрядчик")
                        item.CurrentDebt = item.Receipt - item.Payment;
                }

                if (item.Liter.Contains("Кипарис", StringComparison.OrdinalIgnoreCase))
                {
                    item.ResidentialComplex = "Кипарис";
                    item.Number = item.Contractor + "   " + item.Number;
                    if (item.ContractorOrSupplier == "Подрядчик")
                        item.CurrentDebt = item.Receipt - item.Payment;
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




            var addAreaOfActivity = AddAreaOfActivity(organization, incomeAndExpenses);





            var contracts = gettingData.GetContracts();
            var plusContracts = from vIncomeAndExpenses in addAreaOfActivity
                                join vContracts in contracts
                                on vIncomeAndExpenses.ContractId equals vContracts.ContractId into leftJoin
                                from subvContracts in leftJoin.DefaultIfEmpty()
                                select (vIncomeAndExpenses, subvContracts);

            var reconciliationStatement = string.IsNullOrEmpty(contractor) ? 
                plusContracts.Where(x => x.subvContracts?.Name == contractName) :
                plusContracts.Where(x => x.subvContracts?.Name == contractName && x.subvContracts?.Contractor == contractor);

            return reconciliationStatement
                  .Select(y => new ReconciliationStatement
                  {
                      ContractId = y.vIncomeAndExpenses.ContractId,
                      Credit = y.vIncomeAndExpenses.Credit,
                      Debit = y.vIncomeAndExpenses.Debit,
                      Contractor = y.subvContracts.Contractor,
                      Date = y.subvContracts.Date,
                      Sum = y.subvContracts.Sum,
                      Name = y.subvContracts.Name,
                      DocumentName = y.vIncomeAndExpenses.DocumentName
                  });
        }

        //public async Task<IEnumerable<Income>> IncomeAsync(Organizations organization) // Доходы от строительства объектов
        //{
        //    IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

        //    var incomeAndExpenses = await IncomeAndExpensesAsync(organization);

        //    var contracts = gettingData.GetContracts();
        //    var plusContracts = from vContracts in contracts
        //                        join vIncomeAndExpenses in incomeAndExpenses
        //                        on vContracts.ContractId equals vIncomeAndExpenses.ContractId into leftJoin
        //                        from subvIncomeAndExpenses in leftJoin.DefaultIfEmpty()
        //                        select (vContracts, subvIncomeAndExpenses);


        //    var buyersContracts = plusContracts.Where(x => x.vContracts?.ContractorOrSupplier == "Покупатель")
        //                      .GroupBy(y => y.vContracts.ContractId)
        //                      .Select(z => new Income
        //                      {
        //                          ContractId = z.Key,
        //                          Receipt = z.Sum(s => s.subvIncomeAndExpenses?.Debit ?? 0),
        //                          Payment = z.Sum(s => s.subvIncomeAndExpenses?.Credit ?? 0),
        //                          Contractor = z.FirstOrDefault().vContracts.Contractor,
        //                          Number = z.FirstOrDefault().vContracts.Number,
        //                          Liter = z.FirstOrDefault().vContracts.Liter,
        //                          Date = z.FirstOrDefault().vContracts.Date,
        //                          Sum = z.FirstOrDefault().vContracts.Sum,
        //                          Name = z.FirstOrDefault().vContracts.Name,
        //                          AmountUntil2026 = z.FirstOrDefault().vContracts.AmountUntil2026
        //                      });

        //    var income = buyersContracts.GroupBy(x => x.Contractor + x.Number)
        //                      .Select(y => new Income
        //                      {
        //                          Receipt = y.Sum(s => s.Receipt),
        //                          Payment = y.Sum(s => s.Payment),
        //                          ContractId = y?.FirstOrDefault().ContractId,
        //                          Contractor = y.FirstOrDefault().Contractor,
        //                          Number = y.FirstOrDefault().Number,
        //                          Date = y.FirstOrDefault().Date,
        //                          Sum = y.Sum(z => z.Sum),
        //                          Liter = y.FirstOrDefault()?.Liter,
        //                          Name = y.FirstOrDefault().Name,
        //                          AmountUntil2026 = y.Sum(s => s.AmountUntil2026)
        //                      }).ToList();

        //    income.ForEach(item =>
        //    {
        //        item.OutgoingNDS = item.AmountUntil2026 * 0.2M + (item.Receipt - item.AmountUntil2026) * 0.22M;
        //    });

        //    return income.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number);
        //}

        public async Task<IEnumerable<ActOfCompletionValue>> ActOfCompletionAsync(Organizations organization) // Акты об окончании СМР
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            return (await gettingData.ActOfCompletionAsync()).Value;
        }

        public async Task<IEnumerable<Expense>> ExpenseAsync(Organizations organization) // Стоимость строительства объектов
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization);

            var contracts = gettingData.GetContracts();
            var plusContracts = from vIncomeAndExpenses in incomeAndExpenses
                                join vContracts in contracts
                                on vIncomeAndExpenses.ContractId equals vContracts.ContractId into leftJoin
                                from subvContracts in leftJoin.DefaultIfEmpty()
                                select (vIncomeAndExpenses, subvContracts);

            //var browse = plusContracts.Select(z => new Expense
            //{
            //    ContractId = z.subvContracts?.ContractId,
            //    Receipt = z.vIncomeAndExpenses.Credit,
            //    Payment = z.vIncomeAndExpenses.Debit,
            //    Contractor = z.subvContracts?.Contractor,
            //    Number = z.subvContracts?.Number,
            //    Liter = z.subvContracts?.Liter,
            //    ContractClosed = z.subvContracts?.ContractClosed,
            //    ContractorOrSupplier = z.subvContracts?.ContractorOrSupplier,
            //    CostItem = z.subvContracts?.CostItem,
            //    Date = z.vIncomeAndExpenses.Date,
            //    Sum = z.subvContracts?.Sum,
            //    Name = z.subvContracts?.Name,
            //    NumberAA = z.vIncomeAndExpenses.TypeOperation
            //});
            //_exportingReportsToExcel.Browse(browse);

            var contractor = plusContracts.Where(x => x.subvContracts?.ContractorOrSupplier == "Подрядчик").GroupBy(y => y.subvContracts.ContractId).Select(z => new Expense
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

            // Могут быть договора по которым пока еще нет движения в IncomeAndExpenses
            var contractsContractors = contracts.Where(x => x.ContractorOrSupplier == "Подрядчик");
            var plusContractor = from vContractsContractors in contractsContractors
                                          join vContractor in contractor
                                          on vContractsContractors.ContractId equals vContractor.ContractId into leftJoin
                                          from subvContractore in leftJoin.DefaultIfEmpty()
                                          select new Expense
                                          {
                                              ContractId = vContractsContractors.ContractId,
                                              Receipt = subvContractore?.Receipt ?? 0,
                                              Payment = subvContractore?.Payment ?? 0,
                                              Contractor = vContractsContractors.Contractor,
                                              Number = vContractsContractors.Number,
                                              RateNDS = vContractsContractors.RateNDS,
                                              GeneralContracting = vContractsContractors.GeneralContracting,
                                              Liter = vContractsContractors.Liter,
                                              ContractClosed = vContractsContractors.ContractClosed,
                                              ContractorOrSupplier = vContractsContractors.ContractorOrSupplier,
                                              CostItem = vContractsContractors.CostItem,
                                              Date = vContractsContractors.Date,
                                              Sum = vContractsContractors.Sum,
                                              SecurityDeposit = vContractsContractors.SecurityDeposit,
                                              Name = vContractsContractors.Name,
                                              NumberAA = vContractsContractors.NumberAA,
                                              AmountUntil2026 = vContractsContractors.AmountUntil2026,
                                              RateNDS2026 = vContractsContractors.RateNDS2026
                                          };

            var builder = plusContractor.Where(y => y.NumberAA != "Гарантийное удержание").GroupBy(x => x.Contractor + x.Number).Select(y => new Expense
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

            var supplierAll = plusContracts.Where(x => x.subvContracts?.ContractorOrSupplier == "Поставщик")
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

            var supplier = supplierOld.Concat(supplierNew);
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
                                   NumberAA = vBuilderPlusSupplier.NumberAA,
                                   ConstructionCost = vBuilderPlusSupplier.ConstructionCost,
                                   TotalArea = subvTotalAreas?.TotalArea ?? 0,
                                   Year = vBuilderPlusSupplier.Year,
                                   ConstructionCostNDS = vBuilderPlusSupplier.ConstructionCostNDS,
                                   InputNDS = vBuilderPlusSupplier.InputNDS,
                                   Expenses = vBuilderPlusSupplier.Expenses,
                                   AmountUntil2026 = vBuilderPlusSupplier.AmountUntil2026,
                                   RateNDS2026 = vBuilderPlusSupplier.RateNDS2026,
                               };
            return plusFacility.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number);
        }

        public async Task<IEnumerable<IncomeAndExpenses>> AmountUntil2026Async(Organizations organization) // Выполнения до 2026 года
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization);
            return incomeAndExpenses
                .Where(w => w.Date.Year < 2026 && (w.DocumentName != "Списание с расчетного счета" && w.DocumentName != "Поступление на расчетный счет"))
                .GroupBy(x => x.ContractId)
                .Select(y => new IncomeAndExpenses
                {
                    ContractId = y.Key,
                    Credit = y.Sum(z => z.Credit),
                    Debit = y.Sum(z => z.Debit)
                });
        }

        public async Task<IEnumerable<HowMuchIsLeftToPayExtra>> HowMuchIsLeftToPayExtraAsync(Organizations organization) // Сколько осталось доплатить по счетам
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var supplierPaymentInvoice = (await gettingData.SupplierPaymentInvoiceAsync()).Value;
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

            var supplierPaymentInvoicePlusPayments = from vSupplierPaymentInvoice in supplierPaymentInvoice
                                                     join vAllPayments in allPayments
                                                     on vSupplierPaymentInvoice.SupplierPaymentInvoiceId equals vAllPayments.PaymentDetailsId into leftJoin
                                                     from subvAllPayments in leftJoin.DefaultIfEmpty()
                                                     select new { vSupplierPaymentInvoice, subvAllPayments };

            var counterparties = (await gettingData.CounterpartiesAsync()).Value;
            var plusCounterparties = from vSupplierPaymentInvoicePlusPayments in supplierPaymentInvoicePlusPayments
                                     join vCounterparties in counterparties
                                     on vSupplierPaymentInvoicePlusPayments.vSupplierPaymentInvoice.ContractorId equals vCounterparties.Ref_Key into leftJoin
                                     from subvCounterparties in leftJoin.DefaultIfEmpty()
                                     select new { vSupplierPaymentInvoicePlusPayments, subvCounterparties };

            var contractsCounterparties = (await gettingData.ContractsCounterpartiesAsync()).Value;
            var plusContractsCounterparties = from vPlusCounterparties in plusCounterparties
                                              join vContractsCounterparties in contractsCounterparties
                                              on vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.vSupplierPaymentInvoice.ContractId
                                              equals vContractsCounterparties.ContractId into leftJoin
                                              from subvContractsCounterparties in leftJoin.DefaultIfEmpty()
                                              select new { vPlusCounterparties, subvContractsCounterparties };

            return plusContractsCounterparties
                .Where(w => !string.IsNullOrEmpty(w.vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.vSupplierPaymentInvoice.SupplierPaymentInvoiceId))
                .GroupBy(x => x.vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.vSupplierPaymentInvoice.SupplierPaymentInvoiceId)
                .Select(y => new HowMuchIsLeftToPayExtra
                {
                    PaymentAmount = y.Sum(z => z.vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.subvAllPayments?.PaymentAmount ?? 0),
                    Contract = y.FirstOrDefault().subvContractsCounterparties?.Number,
                    Contractor = y.FirstOrDefault().vPlusCounterparties.subvCounterparties.Description,
                    Date = y.FirstOrDefault().vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.vSupplierPaymentInvoice.Date,
                    Number = y.FirstOrDefault().vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.vSupplierPaymentInvoice.Number,
                    SupplierPaymentInvoiceAmount = y.FirstOrDefault().vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.vSupplierPaymentInvoice.PaymentAmount,
                    PaymentId = y.FirstOrDefault().vPlusCounterparties.vSupplierPaymentInvoicePlusPayments.subvAllPayments?.PaymentId
                }).OrderBy(x => x.Date);
        }

        public IEnumerable<IncomeAndExpenses> AddAreaOfActivity(Organizations organization, IEnumerable<IncomeAndExpenses> incomeAndExpenses) // Добавляем направления
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var areaOfActivityPaymentsFromExcel = gettingData.GetAreaOfActivityPaymentsFromExcel();

            return incomeAndExpenses;
        }

        public IEnumerable<ShareInNDS> ShareInNDS(IEnumerable<CashFlow> cashFlow) // Доля в НДС по направлениям
        {
            var shareInNDS = cashFlow.Where(w => w.TypeOfActivity == "Производственная деятельность (включая косвенные расходы)")
                                 .GroupBy(x => x.AreaOfActivity)
                                 .Select(y => new ShareInNDS
                                 {
                                     AreaOfActivity = y.Key,
                                     OutputNDS = y.Sum(z => z.Receipt * z.RateNDS / (1 + z.RateNDS)),
                                     InputNDS = y.Sum(z => z.Payment * z.RateNDS / (1 + z.RateNDS))
                                 });

            var sum = shareInNDS.Sum(x => x.OutputNDS - x.InputNDS);

            var result = shareInNDS.Select(y => new ShareInNDS
            {
                AreaOfActivity = y.AreaOfActivity,
                OutputNDS = y.OutputNDS,
                InputNDS = y.InputNDS,
                NDSPayable = y.OutputNDS - y.InputNDS,
                Share = (y.OutputNDS - y.InputNDS) / sum * 100
            });

            return result;
        }

        public async Task<IEnumerable<ExpensesUnderIncomeContracts>> ExpensesUnderIncomeContractsAsync(Organizations organization, DateOnly startDate, DateOnly endDate) // Затраты по доходным договорам
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = (await IncomeAndExpensesAsync(organization)).Where(w => w.Date >= startDate && w.Date <= endDate);





            var addAreaOfActivity = AddAreaOfActivity(organization, incomeAndExpenses);





            var contracts = gettingData.GetContracts();
            var plusContracts = from vContracts in contracts
                                join vIncomeAndExpenses in addAreaOfActivity
                                on vContracts.ContractId equals vIncomeAndExpenses.ContractId into leftJoin
                                from subvIncomeAndExpenses in leftJoin.DefaultIfEmpty()
                                select (vContracts, subvIncomeAndExpenses);

            var buyersContracts = plusContracts.Where(x => !string.IsNullOrEmpty(x.vContracts?.ContractIdIncome))
                              .GroupBy(y => y.vContracts.ContractId)
                              .Select(z => new ExpensesUnderIncomeContracts
                              {
                                  ContractId = z.Key,
                                  Receipt = z.Sum(s => s.subvIncomeAndExpenses?.Debit ?? 0),
                                  Payment = z.Sum(s => s.subvIncomeAndExpenses?.Credit ?? 0),
                                  Contractor = z.FirstOrDefault().vContracts.Contractor,
                                  Number = z.FirstOrDefault().vContracts.Number,
                                  Liter = z.FirstOrDefault().vContracts.Liter,
                                  Date = z.FirstOrDefault().vContracts.Date,
                                  Sum = z.FirstOrDefault(f => string.IsNullOrEmpty(f.vContracts.NumberAA)).vContracts.Sum,
                                  TypeOfActivity = z.FirstOrDefault().vContracts.TypeOfActivity,
                                  AreaOfActivity = z.FirstOrDefault().vContracts.AreaOfActivity
                              });

            var income = buyersContracts.GroupBy(x => x.Contractor + x.Number)
                  .Select(y => new ExpensesUnderIncomeContracts
                  {
                      Receipt = y.Sum(s => s.Receipt),
                      Payment = y.Sum(s => s.Payment),
                      ContractId = y?.FirstOrDefault().ContractId,
                      Contractor = y.FirstOrDefault().Contractor,
                      Number = y.FirstOrDefault().Number,
                      Date = y.FirstOrDefault().Date,
                      Sum = y.Sum(z => z.Sum),
                      Liter = y.FirstOrDefault().Liter,
                      TypeOfActivity = y.FirstOrDefault().TypeOfActivity,
                      AreaOfActivity= y.FirstOrDefault().AreaOfActivity
                  });

            var expenses = addAreaOfActivity.Where(x => !string.IsNullOrEmpty(x.ContractIdIncome) && x.Date >= startDate && x.Date <= endDate)
                              .GroupBy(y => y.ContractIdIncome)
                              .Select(z => new ExpensesUnderIncomeContracts
                              {
                                  ContractId = z.Key,
                                  Expenses = z.Sum(s => s.Debit)
                              });

            var expensesUnderIncomeContracts = from vBuyersContracts in income
                                               join vExpenses in expenses
                                               on vBuyersContracts.ContractId equals vExpenses.ContractId into leftJoin
                                               from subvExpenses in leftJoin.DefaultIfEmpty()
                                               select new ExpensesUnderIncomeContracts
                                               {
                                                   ContractId = vBuyersContracts.ContractId,
                                                   Receipt = vBuyersContracts.Receipt,
                                                   Payment = vBuyersContracts.Payment,
                                                   Contractor = vBuyersContracts.Contractor,
                                                   Number = vBuyersContracts.Number,
                                                   Liter = vBuyersContracts.Liter,
                                                   Date = vBuyersContracts.Date,
                                                   Sum = vBuyersContracts.Sum,
                                                   TypeOfActivity = vBuyersContracts.TypeOfActivity,
                                                   AreaOfActivity = vBuyersContracts.AreaOfActivity,
                                                   Expenses = subvExpenses?.Expenses ?? 0
                                               };

            return expensesUnderIncomeContracts.OrderByDescending(x => x.Date);
        }
    }
}