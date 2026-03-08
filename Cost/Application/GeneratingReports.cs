using Cost.Domain;
using Cost.Infrastructure.Repositories.Models;
using Cost.Infrastructure.Repositories.Models.ActOfCompletion;
using Cost.Infrastructure.Repositories.Models.Payments;
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

            _exportingReportsToExcel.Browse(payments); // проверить попадание удаленных и не проведенных

            var multiplePayments = payments.Where(x => x.PaymentDetails.Length > 0)
                .SelectMany(y => y.PaymentDetails, (x, y) => new { payment = x, PaymentDetails = y })
                .Select(z => new Payment
                {
                    PaymentId = z.payment.PaymentId,
                    Date = DateOnly.FromDateTime(z.payment.Date),
                    PaymentDetailsId = z.PaymentDetails.Ref_Key,
                    ContractId = z.PaymentDetails.ContractId,
                    PaymentAmount = z.PaymentDetails.PaymentAmount,
                    PaymentPurpose = z.payment.PaymentPurpose,
                    TypeOperation = z.payment.TypeOperation
                });

            _exportingReportsToExcel.Browse(multiplePayments); // проверить

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

            _exportingReportsToExcel.Browse(singlePayment); // проверить
            var allPayments = multiplePayments.Concat(singlePayment);

            var supplierPaymentInvoice = (await gettingData.SupplierPaymentInvoiceAsync()).Value;

            _exportingReportsToExcel.Browse(supplierPaymentInvoice); // проверить

            var paymentsPlusSupplierPaymentInvoice = from vAllPayments in allPayments
                                                     join vSupplierPaymentInvoice in supplierPaymentInvoice
                                                     on vAllPayments.PaymentDetailsId equals vSupplierPaymentInvoice.SupplierPaymentInvoiceId into leftJoin
                                                     from subvSupplierPaymentInvoice in leftJoin.DefaultIfEmpty()
                                                     select new { vAllPayments, subvSupplierPaymentInvoice.Comment };

            _exportingReportsToExcel.Browse(paymentsPlusSupplierPaymentInvoice); // проверить

            var additionalInformation = (await gettingData.AdditionalInformationAsync()).Value;

            _exportingReportsToExcel.Browse(additionalInformation); // проверить

            var literId = additionalInformation.Where(x => x.ValueType.Contains("НоменклатурныеГруппы", StringComparison.OrdinalIgnoreCase));

            _exportingReportsToExcel.Browse(literId); // проверить

            var paymentsPlusLiterId = from vPaymentsPlusSupplierPaymentInvoice in paymentsPlusSupplierPaymentInvoice
                                      join vLiterId in literId
                                      on vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentDetailsId equals vLiterId.ADObject into leftJoin
                                      from subvLiterId in leftJoin.DefaultIfEmpty()
                                      select new { vPaymentsPlusSupplierPaymentInvoice, subvLiterId.ADValue };

            _exportingReportsToExcel.Browse(paymentsPlusLiterId); // проверить

            var costItemId = additionalInformation.Where(x => x.ValueType.Contains("СтатьиЗатрат", StringComparison.OrdinalIgnoreCase));

            _exportingReportsToExcel.Browse(costItemId); // проверить

            var paymentsPlusLiterIdPlusCostItemId = from vPaymentsPlusLiterId in paymentsPlusLiterId
                                                    join vCostItemId in costItemId
                                                    on vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentDetailsId equals vCostItemId.ADObject into leftJoin
                                                    from subvCostItemId in leftJoin.DefaultIfEmpty()
                                                    select new { vPaymentsPlusLiterId, subvCostItemId.ADValue };

            _exportingReportsToExcel.Browse(paymentsPlusLiterIdPlusCostItemId); // проверить

            var nomenclatureGroups = (await gettingData.NomenclatureGroupsAsync()).Value;

            _exportingReportsToExcel.Browse(nomenclatureGroups); // проверить

            var plusLiterName = from vPaymentsPlusLiterIdPlusCostItemId in paymentsPlusLiterIdPlusCostItemId
                                join vNomenclatureGroups in nomenclatureGroups
                                on vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.ADValue equals vNomenclatureGroups.Ref_Key into leftJoin
                                from subvNomenclatureGroups in leftJoin.DefaultIfEmpty()
                                select new { vPaymentsPlusLiterIdPlusCostItemId, subvNomenclatureGroups.Description };

            _exportingReportsToExcel.Browse(plusLiterName); // проверить

            var costItems = (await gettingData.CostItemsAsync()).Value;

            _exportingReportsToExcel.Browse(costItems); // проверить

            var plusCostItemName = from vPlusLiterName in plusLiterName
                                   join vCostItems in costItems
                                   on vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.ADValue equals vCostItems.Ref_Key into leftJoin
                                   from subvCostItems in leftJoin.DefaultIfEmpty()
                                   select new { vPlusLiterName, subvCostItems.Description };

            _exportingReportsToExcel.Browse(plusCostItemName); // проверить

            // Объекты и статьи затрат по старым оплатам
            var expensePaymentsFromExcel = gettingData.ExpensePaymentsFromExcel();

            _exportingReportsToExcel.Browse(expensePaymentsFromExcel); // проверить

            var plusExpensePaymentsFromExcel = from vPlusCostItemName in plusCostItemName
                                               join vExpensePaymentsFromExcel in expensePaymentsFromExcel
                                               on vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentId
                                               equals vExpensePaymentsFromExcel.PaymentId into leftJoin
                                               from subvExpensePaymentsFromExcel in leftJoin.DefaultIfEmpty()
                                               select new { vPlusCostItemName, subvExpensePaymentsFromExcel };

            _exportingReportsToExcel.Browse(plusExpensePaymentsFromExcel); // проверить

            var result = plusExpensePaymentsFromExcel.Select(x => new Payment
            {
                PaymentId = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentId,
                Date = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.Date,
                PaymentAmount = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentAmount,
                ContractId = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.ContractId,
                Liter = string.IsNullOrEmpty(x.subvExpensePaymentsFromExcel.Liter) ? x.vPlusCostItemName.vPlusLiterName.Description : x.subvExpensePaymentsFromExcel.Liter,
                CostItem = string.IsNullOrEmpty(x.subvExpensePaymentsFromExcel.CostItems) ? x.vPlusCostItemName.Description : x.subvExpensePaymentsFromExcel.CostItems,
                PaymentPurpose = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.PaymentPurpose,
                TypeOperation = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.vAllPayments.TypeOperation,
                CommentFromPaymentInvoice = x.vPlusCostItemName.vPlusLiterName.vPaymentsPlusLiterIdPlusCostItemId.vPaymentsPlusLiterId.vPaymentsPlusSupplierPaymentInvoice.Comment
            }).OrderBy(x => x.Date);

            _exportingReportsToExcel.Browse(result); // проверить

            return result;
        }

        public async Task<IEnumerable<Payment>> IncomePaymentsAsync(Organizations organization) // Доходные оплаты
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            var payments = (await gettingData.DepositToCurrentAccountAsync()).Value;

            _exportingReportsToExcel.Browse(payments); // проверить попадание удаленных и не проведенных

            var multiplePayments = payments.Where(x => x.PaymentDetails.Length > 0)
                .SelectMany(y => y.PaymentDetails, (x, y) => new { payment = x, PaymentDetails = y })
                .Select(z => new Payment
                {
                    PaymentId = z.payment.PaymentId,
                    Date = DateOnly.FromDateTime(z.payment.Date),
                    PaymentDetailsId = z.PaymentDetails.Ref_Key,
                    ContractId = z.PaymentDetails.ContractId,
                    PaymentAmount = z.PaymentDetails.PaymentAmount,
                    PaymentPurpose = z.payment.PaymentPurpose,
                    TypeOperation = z.payment.TypeOperation
                });

            _exportingReportsToExcel.Browse(multiplePayments); // проверить

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

            _exportingReportsToExcel.Browse(singlePayment); // проверить

            var allPayments = multiplePayments.Concat(singlePayment);

            var buyerPaymentInvoice = (await gettingData.BuyerPaymentInvoiceAsync()).Value;

            _exportingReportsToExcel.Browse(buyerPaymentInvoice); // проверить

            var paymentsPlusSupplierPaymentInvoice = from vAllPayments in allPayments
                                                     join vbuyerPaymentInvoice in buyerPaymentInvoice
                                                     on vAllPayments.PaymentDetailsId equals vbuyerPaymentInvoice.BuyerPaymentInvoiceId into leftJoin
                                                     from subvbuyerPaymentInvoice in leftJoin.DefaultIfEmpty()
                                                     select new { vAllPayments, subvbuyerPaymentInvoice.Comment };

            _exportingReportsToExcel.Browse(paymentsPlusSupplierPaymentInvoice); // проверить

            var result = paymentsPlusSupplierPaymentInvoice.Select(x => new Payment
            {
                PaymentId = x.vAllPayments.PaymentId,
                Date = x.vAllPayments.Date,
                PaymentAmount = x.vAllPayments.PaymentAmount,
                ContractId = x.vAllPayments.ContractId,
                PaymentPurpose = x.vAllPayments.PaymentPurpose,
                TypeOperation = x.vAllPayments.TypeOperation,
                CommentFromPaymentInvoice = x.Comment
            }).OrderBy(x => x.Date);

            _exportingReportsToExcel.Browse(result); // проверить

            return result;
        }












        public async Task<IEnumerable<Contracts>> WeDoNotHaveTheseContractsAsync(Organizations organization) // Отсутствующие у нас договора
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var contractsCounterparties = (await gettingData.ContractsCounterpartiesAsync());
            var contractsCounterpartiesValue = contractsCounterparties.Value
            .Where(x => x.DeletionMark == false && int.Parse(x.Code.Substring(x.Code.Length - 6)) > contractsCounterparties.CodeContract);

            // Поставщики + договора
            var counterparties = await gettingData.CounterpartiesAsync();
            var contractorPlusContract = counterparties.Value.Join(contractsCounterpartiesValue, p1 => p1.Ref_Key, c1 => c1.ContractorId,
                (p2, c2) => new { p2, c2 });

            var contractsFrom1C = contractorPlusContract.Select(x => new Contracts
            {
                ContractId = x.c2.CounterpartyAgreementId,
                Contractor = x.p2.Description,
                Number = x.c2.Number,
                Name = x.c2.Name,
                Date = DateOnly.FromDateTime(x.c2.Date ?? new DateTime()),
                Sum = x.c2.Sum,
                Code = x.c2.Code
            });

            var contractsFromExcel = gettingData.GetContracts();

            return contractsFrom1C.Except(contractsFromExcel);
        }

        public async Task<List<ReconciliationStatement>> ReconciliationStatementAsync(string contractName, Organizations organization, string contractor) // Акт сверки
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var contract = string.IsNullOrEmpty(contractor) ? gettingData.GetContracts().FirstOrDefault(x => x.Name == contractName) :
                                                              gettingData.GetContracts().FirstOrDefault(x => x.Contractor == contractor && x.Name == contractName);
            var payments = (await gettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false
                && x.CounterpartyAgreementId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = DateOnly.FromDateTime(y.Date),
                                    Debit = y.DocumentAmount,
                                    DocumentName = "Списание с расчетного счета"
                                });
            var receiptGoodsServices = (await gettingData.ReceiptGoodsServicesAsync()).Value
                .Where(x => x.Posted == true && x.ContractId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = DateOnly.FromDateTime(y.Date),
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление товаров и услуг"
                                });

            var receiptProcessing = (await gettingData.ReceiptProcessingAsync()).Value
                .Where(x => x.Posted == true && x.ContractId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = DateOnly.FromDateTime(y.Date),
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление из переработки"
                                });

            var paymentsPlusreceiptGoodsServices = payments.Concat(receiptGoodsServices);
            var plusReceiptProcessing = paymentsPlusreceiptGoodsServices.Concat(receiptProcessing);

            var selling = (await gettingData.SellingAsync()).Value
                .Where(x => x.Posted == true && x.CounterpartyAgreementId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = DateOnly.FromDateTime(y.Date),
                                    Debit = y.DocumentAmount,
                                    DocumentName = "Реализация товаров и услуг"
                                });

            var plusSelling = plusReceiptProcessing.Concat(selling);
            // ---------------------------------------------------------------------------------------------------------------

            var debtAdjustment = (await gettingData.DebtAdjustmentAsync()).Value.Where(x => x.Posted == true).ToList();
            // Убираем из Корректировки долга проводки по одному договору в одном документе Корректировка долга
            foreach (var item in debtAdjustment)
            {
                if (item.AccountsPayable.Length > 0 && item.AccountsReceivable.Length > 0
                    && item.AccountsPayable.First().CounterpartyAgreementId == item.AccountsReceivable.First().CounterpartyAgreementId)
                {
                    item.DeletionMark = true;
                }
                if (item.AccountsPayable.Length > 0 && item.AccountsPayable.First().CounterpartyAgreementId == item.AccountsPayable.First().CorCounterpartyAgreementId)
                {
                    item.DeletionMark = true;
                }
                if (item.AccountsReceivable.Length > 0 && item.AccountsReceivable.First().CounterpartyAgreementId == item.AccountsReceivable.First().CorCounterpartyAgreementId)
                {
                    item.DeletionMark = true;
                }
            }
            debtAdjustment.RemoveAll(x => x.DeletionMark);

            var Payable = debtAdjustment.SelectMany(x => x.AccountsPayable, (p, c) =>
            new { p.Ref_Key, c.CounterpartyAgreementId, c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.CounterpartyAgreementId == contract.ContractId).ToList();

            var Receivable = debtAdjustment.SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, c.CounterpartyAgreementId, c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.CounterpartyAgreementId == contract.ContractId).ToList();

            var payableReconciliationStatement = Payable.Select(y => new ReconciliationStatement
            {
                Date = DateOnly.FromDateTime(y.Date),
                Debit = y.Sum,
                DocumentName = "Корректировка долга"
            });

            var receivableReconciliationStatement = Receivable.Select(y => new ReconciliationStatement
            {
                Date = DateOnly.FromDateTime(y.Date),
                Credit = y.Sum,
                DocumentName = "Корректировка долга"
            });

            var plusPayable = plusSelling.Concat(payableReconciliationStatement);
            var plusReceivable = plusPayable.Concat(receivableReconciliationStatement);

            var ReceivableDoubleEntry = debtAdjustment.Where(x => x.AccountsReceivable.Length == 0).SelectMany(x => x.AccountsPayable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.CounterpartyAgreementId == contract.ContractId).ToList();

            var PayableDoubleEntry = debtAdjustment.Where(x => x.AccountsPayable.Length == 0).SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.CounterpartyAgreementId == contract.ContractId).ToList();

            var PlusReceivableDoubleEntry = ReceivableDoubleEntry.Select(y => new ReconciliationStatement
            {
                Date = DateOnly.FromDateTime(y.Date),
                Credit = y.Sum,
                DocumentName = "Корректировка долга"
            });

            var PlusPayableDoubleEntry = PayableDoubleEntry.Select(y => new ReconciliationStatement
            {
                Date = DateOnly.FromDateTime(y.Date),
                Debit = y.Sum,
                DocumentName = "Корректировка долга"
            });

            var plusReceivableDoubleEntry = plusReceivable.Concat(PlusReceivableDoubleEntry);
            var plusPayableDoubleEntry = plusReceivableDoubleEntry.Concat(PlusPayableDoubleEntry);
            // ---------------------------------------------------------------------------------------------------------------

            var receiptToCurrentAccount = (await gettingData.ReceiptToCurrentAccountAsync()).Value
                .Where(x => x.Posted == true && x.CounterpartyAgreementId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = DateOnly.FromDateTime(y.Date),
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление на расчетный счет"
                                });

            var plusreceiptToCurrentAccount = plusPayableDoubleEntry.Concat(receiptToCurrentAccount);

            var operations = gettingData.GetOperations();
            var operationDebit = operations.Where(x => x.ContractDebit == contract.ContractId)
                    .Select(y => new ReconciliationStatement
                    {
                        Date = y.Date,
                        Debit = y.Sum,
                        DocumentName = "Операция"
                    });

            var plusOperationDebit = plusreceiptToCurrentAccount.Concat(operationDebit);

            var operationCredit = operations.Where(x => x.ContractCredit == contract.ContractId)
                    .Select(y => new ReconciliationStatement
                    {
                        Date = y.Date,
                        Credit = y.Sum,
                        DocumentName = "Операция"
                    });

            var plusOperationCredit = plusOperationDebit.Concat(operationCredit);

            var implementationConstructionWorks = (await gettingData.ImplementationConstructionWorksAsync()).Value
                    ?.Where(x => x.Posted == true && x.ContractId == contract.ContractId)
                    ?.Select(y => new ReconciliationStatement
                    {
                        Date = DateOnly.FromDateTime(y.Date),
                        Debit = y.DocumentAmount,
                        DocumentName = "Реализация строительных работ и услуг"
                    });

            var plusImplementationConstructionWorks = implementationConstructionWorks != null ? plusOperationCredit.Concat(implementationConstructionWorks)
                                                                                  : plusOperationCredit;

            var reconciliationStatement = plusImplementationConstructionWorks.OrderBy(x => x.Date).ToList();
            reconciliationStatement.ForEach(item => { item.Contractor = contract.Contractor; item.Sum = contract.Sum; item.Name = contract.Name; });
            return reconciliationStatement;
        }

        public async Task<List<Domain.Cost>> CostAsync(Organizations organization) // Стоимость строительства объектов
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization, new DateOnly(), "Затраты");

            var contractor = incomeAndExpenses.Where(x => x.ContractorOrSupplier == "Подрядчик").GroupBy(y => y.ContractId).Select(z => new Domain.Cost
            {
                ContractId = z.Key,
                Receipt = z.Sum(s => s.Receipt),
                Payment = z.Sum(s => s.Payment),
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
                                              ConstructionObject = con.ConstructionObject,
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
                    Payment = z.Sum(s => s.Payment),
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
                    ConstructionCost = z.Sum(s => s.Payment),
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

        public async Task<List<Income>> IncomeAsync(Organizations organization) // Доходы от строительства объектов
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization, new DateOnly(), "Доходы");

            var contractor = incomeAndExpenses.GroupBy(y => y.ContractId).Select(z => new Income
            {
                ContractId = z.Key,
                Payment = z.Sum(s => s.Receipt),
                Receipt = z.Sum(s => s.Payment),
                Contractor = z.FirstOrDefault().Contractor,
                Number = z.FirstOrDefault().Number,
                ConstructionObject = z.FirstOrDefault().ConstructionObject,
                Date = z.FirstOrDefault().Date,
                Sum = z.FirstOrDefault().SumContract,
                Name = z.FirstOrDefault().Name
            });

            var contracts = gettingData.GetContracts().Where(x => x.ContractorOrSupplier == "Покупатель");
            var contractsPlusContractor = from con in contracts
                                          join income in contractor
                                          on con.ContractId equals income.ContractId into tmp
                                          from subIncome in tmp.DefaultIfEmpty()
                                          select new Income
                                          {
                                              ContractId = con.ContractId,
                                              Receipt = subIncome?.Receipt ?? 0,
                                              Payment = subIncome?.Payment ?? 0,
                                              Contractor = con.Contractor,
                                              Number = con.Number,
                                              ConstructionObject = con.ConstructionObject,
                                              Date = con.Date,
                                              Sum = con.Sum,
                                              Name = con.Name,
                                              NumberAA = con.NumberAA,
                                              AmountUntil2026 = con.AmountUntil2026,
                                          };

            var result = contractsPlusContractor.GroupBy(x => x.Contractor + x.Number).Select(y => new Income
            {
                ContractId = y?.FirstOrDefault().ContractId,
                Contractor = y.FirstOrDefault().Contractor,
                Number = y.FirstOrDefault().Number,
                Date = y.FirstOrDefault().Date,
                Sum = y.Sum(z => z.Sum),
                ConstructionObject = y.FirstOrDefault()?.ConstructionObject,
                Receipt = y.Sum(z => z.Receipt),
                Payment = y.Sum(z => z.Payment),
                Name = y.FirstOrDefault().Name,
                AmountUntil2026 = y.Sum(z => z.AmountUntil2026),
            }).ToList();

            result.ForEach(item =>
            {
                item.OutgoingNDS = item.AmountUntil2026 * 0.2M + (item.Receipt - item.AmountUntil2026) * 0.22M;
            });

            return result.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number).ToList();
        }

        public async Task<List<IncomeAndExpenses>> IncomeAndExpensesAsync(Organizations organization, DateOnly date, string costOrIncome = "") // Доходы и расходы по документам
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var payments = (await gettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false && DateOnly.FromDateTime(x.Date) >= date).ToList();

            var literAndCostItemInPayments = await PaymentsAsync(organization);
            var plusLiterAndCostItemInPayments = from p in payments
                                                 join c in literAndCostItemInPayments
                                                 on p.PaymentId equals c.PaymentId into tmp
                                                 from subC in tmp.DefaultIfEmpty()
                                                 select new PaymentsValue()
                                                 {
                                                     Date = p.Date,
                                                     DocumentAmount = subC.PaymentAmount,
                                                     CounterpartyAgreementId = subC.ContractId,
                                                     Liter = subC.Liter,
                                                     CostItems = subC.CostItems,
                                                     TypeOperation = subC.TypeOperation
                                                 };

            _exportingReportsToExcel.Browse(plusLiterAndCostItemInPayments); // проверить

            var serb = await ExpensePaymentsAsync(organization);

            _exportingReportsToExcel.Browse(serb); // сравнить



            var Payments = plusLiterAndCostItemInPayments.Select(p => new IncomeAndExpenses()
            {
                Date = DateOnly.FromDateTime(p.Date),
                Payment = p.DocumentAmount,
                ContractId = p.CounterpartyAgreementId,
                DocumentAmount = p.DocumentAmount,
                DocumentNDSAmount = p.PaymentNDSAmount,
                LiterPayment = p.Liter,
                CostItemPayment = p.CostItems,
                TypeOperation = p.TypeOperation,
                DocumentName = "Списание с расчетного счета"
            });

            var receiptGoodsServices = (await gettingData.ReceiptGoodsServicesAsync()).Value.Where(x => x.Posted == true && DateOnly.FromDateTime(x.Date) >= date);
            var ReceiptGoodsServices = receiptGoodsServices.Select(p => new IncomeAndExpenses()
                                       {
                                           Date = DateOnly.FromDateTime(p.Date),
                                           Receipt = p.DocumentAmount,
                                           ContractId = p.ContractId,
                                           DocumentAmount = p.DocumentAmount,
                                           DocumentName = "Поступление товаров и услуг"
                                       });

            var receiptProcessing = (await gettingData.ReceiptProcessingAsync()).Value.Where(x => x.Posted == true && DateOnly.FromDateTime(x.Date) >= date);
            var ReceiptProcessing = receiptProcessing.Select(p => new IncomeAndExpenses()
                                    {
                                        Date = DateOnly.FromDateTime(p.Date),
                                        Receipt = p.DocumentAmount,
                                        ContractId = p.ContractId,
                                        DocumentAmount = p.DocumentAmount,
                                        DocumentName = "Поступление из переработки"
                                    });

            var paymentsPlusreceiptGoodsServices = Payments.Concat(ReceiptGoodsServices);
            var plusReceiptProcessing = paymentsPlusreceiptGoodsServices.Concat(ReceiptProcessing);

            var selling = (await gettingData.SellingAsync()).Value
                .Where(x => x.Posted == true && DateOnly.FromDateTime(x.Date) >= date)
                                .Select(y => new IncomeAndExpenses
                                {
                                    Date = DateOnly.FromDateTime(y.Date),
                                    Payment = y.DocumentAmount,
                                    ContractId = y.CounterpartyAgreementId,
                                    DocumentName = "Реализация товаров и услуг"
                                });

            var plusSelling = plusReceiptProcessing.Concat(selling);
            // ---------------------------------------------------------------------------------------------------------------

            var debtAdjustment = (await gettingData.DebtAdjustmentAsync()).Value.Where(x => x.Posted == true).ToList();
            // Убираем из Корректировки долга проводки по одному договору в одном документе Корректировка долга
            foreach (var item in debtAdjustment)
            {
                if (item.AccountsPayable.Length > 0 && item.AccountsReceivable.Length > 0
                    && item.AccountsPayable.First().CounterpartyAgreementId == item.AccountsReceivable.First().CounterpartyAgreementId)
                {
                    item.DeletionMark = true;
                }
                if (item.AccountsPayable.Length > 0 && item.AccountsPayable.First().CounterpartyAgreementId == item.AccountsPayable.First().CorCounterpartyAgreementId)
                {
                    item.DeletionMark = true;
                }
                if (item.AccountsReceivable.Length > 0 && item.AccountsReceivable.First().CounterpartyAgreementId == item.AccountsReceivable.First().CorCounterpartyAgreementId)
                {
                    item.DeletionMark = true;
                }
            }
            debtAdjustment.RemoveAll(x => x.DeletionMark);

            var Payable = debtAdjustment.SelectMany(x => x.AccountsPayable, (p, c) =>
            new { p.Ref_Key, c.CounterpartyAgreementId, c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

            var Receivable = debtAdjustment.SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, c.CounterpartyAgreementId, c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

            var payableIncomeAndExpenses = Payable.Select(y => new IncomeAndExpenses
            {
                Date = DateOnly.FromDateTime(y.Date),
                Payment = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var receivableIncomeAndExpenses = Receivable.Select(y => new IncomeAndExpenses
            {
                Date = DateOnly.FromDateTime(y.Date),
                Receipt = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var plusPayable = plusSelling.Concat(payableIncomeAndExpenses);
            var plusReceivable = plusPayable.Concat(receivableIncomeAndExpenses);

            var ReceivableDoubleEntry = debtAdjustment.Where(x => x.AccountsReceivable.Length == 0).SelectMany(x => x.AccountsPayable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

            var PayableDoubleEntry = debtAdjustment.Where(x => x.AccountsPayable.Length == 0).SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => DateOnly.FromDateTime(y.Date) >= date).ToList();

            var PlusReceivableDoubleEntry = ReceivableDoubleEntry.Select(y => new IncomeAndExpenses
            {
                Date = DateOnly.FromDateTime(y.Date),
                Receipt = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var PlusPayableDoubleEntry = PayableDoubleEntry.Select(y => new IncomeAndExpenses
            {
                Date = DateOnly.FromDateTime(y.Date),
                Payment = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var plusReceivableDoubleEntry = plusReceivable.Concat(PlusReceivableDoubleEntry);
            var plusPayableDoubleEntry = plusReceivableDoubleEntry.Concat(PlusPayableDoubleEntry);
            // ---------------------------------------------------------------------------------------------------------------

            var receiptToCurrentAccount = (await gettingData.ReceiptToCurrentAccountAsync()).Value
                .Where(x => x.Posted == true && DateOnly.FromDateTime(x.Date) >= date)
                                .Select(y => new IncomeAndExpenses
                                {
                                    Date = DateOnly.FromDateTime(y.Date),
                                    Receipt = y.DocumentAmount,
                                    ContractId = y.CounterpartyAgreementId,
                                    TypeOperation = y.TypeOperation,
                                    DocumentName = "Поступление на расчетный счет"
                                });

            _exportingReportsToExcel.Browse(receiptToCurrentAccount); // проверить


            var serb1 = await IncomePaymentsAsync(organization);

            _exportingReportsToExcel.Browse(serb1); // сравнить


            var plusreceiptToCurrentAccount = plusPayableDoubleEntry.Concat(receiptToCurrentAccount);

            var operations = gettingData.GetOperations();
            var operationDebit = operations.Where(x => x.Date >= date)
                    .Select(y => new IncomeAndExpenses
                    {
                        Date = y.Date,
                        Payment = y.Sum,
                        ContractId = y.ContractDebit,
                        DocumentName = "Операция"
                    });

            var plusOperationDebit = plusreceiptToCurrentAccount.Concat(operationDebit);

            var operationCredit = operations.Where(x => x.Date >= date)
                    .Select(y => new IncomeAndExpenses
                    {
                        Date = y.Date,
                        Receipt = y.Sum,
                        ContractId = y.ContractCredit,
                        DocumentName = "Операция"
                    });

            var plusOperationCredit = plusOperationDebit.Concat(operationCredit);

            var implementationConstructionWorks = (await gettingData.ImplementationConstructionWorksAsync()).Value
                    ?.Where(x => x.Posted == true && DateOnly.FromDateTime(x.Date) >= date)
                    ?.Select(y => new IncomeAndExpenses
                    {
                        Date = DateOnly.FromDateTime(y.Date),
                        Payment = y.DocumentAmount,
                        ContractId = y.ContractId,
                        DocumentName = "Реализация строительных работ и услуг"
                    });

            var plusImplementationConstructionWorks = implementationConstructionWorks != null ? plusOperationCredit.Concat(implementationConstructionWorks)
                                                                                              : plusOperationCredit;
            var contract = new List<Contracts>();
            if (costOrIncome == "Затраты")
                contract = gettingData.GetContracts().Where(x => x.ContractorOrSupplier != "Покупатель").ToList();
            else if (costOrIncome == "Доходы")
                contract = gettingData.GetContracts().Where(x => x.ContractorOrSupplier == "Покупатель").ToList();
            else
                contract = gettingData.GetContracts();

            var plusContract = from p in plusImplementationConstructionWorks
                               join c in contract
                               on p.ContractId equals c.ContractId into tmp
                               from subC in tmp.DefaultIfEmpty()
                               select new { p, subC = subC ?? new Contracts() };

            var incomeAndExpenses = plusContract.Select(x => new IncomeAndExpenses
            {
                ContractId = x.subC.ContractId,
                DocumentName = x.p.DocumentName,
                Receipt = x.p.Receipt,
                Payment = x.p.Payment,
                Date = x.p.Date,
                DocumentAmount = x.p.DocumentAmount,
                DocumentNDSAmount = x.p.DocumentNDSAmount,
                InvoiceReceivedNDS = x.p.InvoiceReceivedNDS,
                Contractor = x.subC.Contractor,
                Number = x.subC.Number,
                RateNDS = x.subC.RateNDS,
                GeneralContracting = x.subC.GeneralContracting,
                ConstructionObject = x.subC.ConstructionObject,
                ContractClosed = x.subC.ContractClosed,
                ContractorOrSupplier = x.subC.ContractorOrSupplier,
                CostItem = x.subC.CostItem,
                DateContract = x.subC.Date,
                SumContract = x.subC.Sum,
                WarrantyLien = x.subC.WarrantyLien,
                LiterPayment = x.p.LiterPayment,
                CostItemPayment = x.p.CostItemPayment,
                Name = x.subC.Name,
                RateNDS2026 = x.subC.RateNDS2026,
                AreaOfActivity = x.subC.AreaOfActivity,
                TypeOperation = x.p.TypeOperation
            });

            return incomeAndExpenses.OrderBy(x => x.Date).ToList();
        }

        //public async Task<List<ContractsCounterpartiesValue>> ContractsFrom1CAsync(Organizations organization) // Договора из 1С
        //{
        //    IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
        //    var contractsCounterpartiesValue = (await gettingData.ContractsCounterpartiesAsync()).Value;

        //    List<ContractsCounterpartiesValue> contractsCounterparties = null;

        //    var additionalInformation = await gettingData.AdditionalInformationAsync();

        //    contractsCounterparties = contractsCounterpartiesValue.ToList();

        //    var nomenclatureGroups = (await gettingData.NomenclatureGroupsAsync()).Value;
        //    var typesCalculations = await gettingData.TypesCalculationsAsync();
        //    var costItems = await gettingData.CostItemsAsync();

        //    foreach (var contractsCounterpartie in contractsCounterparties)
        //    {
        //        foreach (var AdditionalDetail in contractsCounterpartie.AdditionalDetails)
        //        {
        //            if (AdditionalDetail.ValueType.Contains("НоменклатурныеГруппы"))
        //            {
        //                contractsCounterpartie.NomenclatureGroupsId = AdditionalDetail.Value;
        //            }
        //            if (AdditionalDetail.ValueType.Contains("СтатьиЗатрат"))
        //            {
        //                contractsCounterpartie.CostItemsId = AdditionalDetail.Value;
        //            }
        //        }
        //    }

        //    var contractsGrouped = contractsCounterparties.ToList();

        //    var contractPlusNomenclatureGroup = from c1 in contractsGrouped
        //                                        join nomenclatureGroup in nomenclatureGroups
        //                                               on c1.NomenclatureGroupsId equals nomenclatureGroup.Ref_Key into tmp
        //                                        from subNomenclatureGroup in tmp.DefaultIfEmpty()
        //                                        select new { c1, subNomenclatureGroup?.Description };

        //    var contractPlusNomenclatureGroupPlusConstructionPlusTypesCalculation = from c3 in contractPlusNomenclatureGroup
        //                                                                            join calculation in typesCalculations.Value
        //                                                                                   on c3.c1.TypeCalculationId equals calculation.Ref_Key into tmp
        //                                                                            from subCalculation in tmp.DefaultIfEmpty()
        //                                                                            select new { c3, subCalculation?.Description };
        //    // Поставщики + договора
        //    var counterparties = await gettingData.CounterpartiesAsync();
        //    var contractorPlusContract = counterparties.Value.Join(contractPlusNomenclatureGroupPlusConstructionPlusTypesCalculation, p1 => p1.Ref_Key, c1 => c1.c3.c1.ContractorId,
        //        (p5, c5) => new { p5, c5 }).ToList();


        //    var contracts = from c4 in contractorPlusContract
        //                    join cost in costItems.Value
        //                           on c4.c5.c3.c1.CostItemsId equals cost.Ref_Key into tmp
        //                    from subCost in tmp.DefaultIfEmpty()
        //                    select new ContractsCounterpartiesValue
        //                    {
        //                        ConstructionProjects = c4.c5.c3.Description,
        //                        ContractClosed = c4.c5.c3.c1.ContractClosed,
        //                        ContractorId = c4.c5.c3.c1.ContractorId,
        //                        CostItemsId = c4.c5.c3.c1.CostItemsId,
        //                        CostItems = subCost?.Description,
        //                        CounterpartyAgreementId = c4.c5.c3.c1.CounterpartyAgreementId,
        //                        Date = c4.c5.c3.c1.Date,
        //                        NomenclatureGroupsId = c4.c5.c3.c1.NomenclatureGroupsId,
        //                        Number = c4.c5.c3.c1.Number,
        //                        Name = c4.c5.c3.c1.Name,
        //                        OrganizationId = c4.c5.c3.c1.OrganizationId,
        //                        RateNDS = c4.c5.c3.c1.RateNDS,
        //                        Sum = c4.c5.c3.c1.Sum,
        //                        SumNDS = c4.c5.c3.c1.SumNDS,
        //                        TypeCalculation = c4.c5.Description,
        //                        Contractor = c4.p5.Description,
        //                        TypeAgreement = c4.c5.c3.c1.TypeAgreement
        //                    };

        //    return contracts.ToList();
        //}

        public async Task<List<LiterAndCostItemInPayments>> PaymentsAsync(Organizations organization) // Оплаты
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var payments = (await gettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false);
            var billPayment = await gettingData.BillPaymentAsync();
            var additionalInformation = await gettingData.AdditionalInformationAsync();
            var nomenclatureGroups = (await gettingData.NomenclatureGroupsAsync()).Value;
            var costItems = await gettingData.CostItemsAsync();

            var paymentMany = payments.Where(x => x.PaymentDecryption.Length > 0)
                .SelectMany(y => y.PaymentDecryption, (x, y) => new { payment = x, paymentDecryption = y })
                .Select(z => new PaymentsValue
                {
                    PaymentId = z.payment.PaymentId,
                    Date = z.payment.Date,
                    PaymentDecryptionId = z.paymentDecryption.Ref_Key,
                    CounterpartyAgreementId = z.paymentDecryption.CounterpartyAgreementId,
                    DocumentAmount = z.paymentDecryption.PaymentAmount,
                    PaymentNDSAmount = z.paymentDecryption.PaymentNDSAmount,
                    PaymentPurpose = z.payment.PaymentPurpose,
                    Number = z.payment.Number,
                    TypeOperation = z.payment.TypeOperation
                }).ToList();

            var paymentNoMany = payments.Where(x => x.PaymentDecryption.Length == 0).ToList();
            var concat = paymentMany.Concat(paymentNoMany).ToList();

            var billPaymentMany = billPayment.Value.Select(x => new { x, x.RecordSet.FirstOrDefault().InvoiceForPaymentId });
            var paymentsPlusCashFlowArticlesPlusBillPayment = from payMany in concat
                                                              join bill in billPaymentMany
                                                              on payMany.PaymentDecryptionId equals bill.x.Recorder into tmp
                                                              from subBill in tmp.DefaultIfEmpty()
                                                              select new { payMany, subBill?.InvoiceForPaymentId };

            var ConstructionObjectIds = additionalInformation.Value.Where(x => x.ValueType.Contains("НоменклатурныеГруппы", StringComparison.OrdinalIgnoreCase));
            var paymentsPlusCashFlowArticlesPlusBillPaymentPlusConstructionObject = from payBill in paymentsPlusCashFlowArticlesPlusBillPayment
                                                                                    join cons in ConstructionObjectIds
                                                                                    on payBill.InvoiceForPaymentId equals cons.ADObject into tmp
                                                                                    from subCons in tmp.DefaultIfEmpty()
                                                                                    select new { payBill, subCons?.ADValue };

            var CostItemIds = additionalInformation.Value.Where(x => x.ValueType.Contains("СтатьиЗатрат", StringComparison.OrdinalIgnoreCase));
            var paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItem = from payCons in paymentsPlusCashFlowArticlesPlusBillPaymentPlusConstructionObject
                                                                          join cost in CostItemIds
                                                                          on payCons.payBill.InvoiceForPaymentId equals cost.ADObject into tmp
                                                                          from subCost in tmp.DefaultIfEmpty()
                                                                          select new { payCons, subCost?.ADValue };

            var paymentsPlusCashFlowArticlesPlusBillPaymentPlusConstructionObjectName = from payObjectName in paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItem
                                                                                        join objectName in nomenclatureGroups
                                                                                        on payObjectName.payCons.ADValue equals objectName.Ref_Key into tmp
                                                                                        from subObjectName in tmp.DefaultIfEmpty()
                                                                                        select new { payObjectName, subObjectName?.Description };

            var paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItemName = from payCostName in paymentsPlusCashFlowArticlesPlusBillPaymentPlusConstructionObjectName
                                                                              join costName in costItems.Value
                                                                              on payCostName.payObjectName.ADValue equals costName.Ref_Key into tmp
                                                                              from subCostName in tmp.DefaultIfEmpty()
                                                                              select new { payCostName, subCostName?.Description };

            var contracts = gettingData.GetContracts();

            // Оплата + поставщики + договора
            var contractorPlusContractPlusPayment = from payment in paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItemName
                                                    join contract in contracts
                                                    on payment.payCostName.payObjectName.payCons.payBill.payMany.CounterpartyAgreementId
                                                    equals contract.ContractId into tmp
                                                    from subcontract in tmp.DefaultIfEmpty()
                                                    select new { payment, subcontract };

            var literAndCostItemInPayments = gettingData.GetLiterAndCostItemInPayments();

            // Оплата + поставщики + договора + объекты и статьи затрат по старым оплатам
            var paymentCosts = from payment in contractorPlusContractPlusPayment
                               join cost in literAndCostItemInPayments
                               on payment.payment.payCostName.payObjectName.payCons.payBill.payMany.PaymentId
                               equals cost.PaymentId into tmp
                               from subcost in tmp.DefaultIfEmpty()
                               select new { payment, subcost };

            var result = paymentCosts.Select(z => new LiterAndCostItemInPayments
            {
                Liter = string.IsNullOrEmpty(z.subcost?.Liter) ? z.payment.payment.payCostName.Description : z.subcost?.Liter,
                CostItems = string.IsNullOrEmpty(z.subcost?.CostItems) ? z.payment.payment.Description : z.subcost?.CostItems,
                PaymentId = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.PaymentId,
                PaymentAmount = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.DocumentAmount,
                //PaymentNDSAmount = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.PaymentNDSAmount,
                PurposePayment = string.IsNullOrEmpty(z.subcost?.PurposePayment)
                    ? z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.PaymentPurpose : z.subcost?.PurposePayment,
                Date = DateOnly.FromDateTime(z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.Date),
                //Number = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.Number,
                Contractor = z.payment.subcontract?.Contractor,
                //LiterInAgreement = z.payment.subcontract?.ConstructionObject,
                //CostItemsInAgreement = z.payment.subcontract?.CostItem,
                ContractorOrSupplier = z.payment.subcontract?.ContractorOrSupplier,
                ContractId = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.CounterpartyAgreementId,
                ContractNumber = z.payment.subcontract?.Number,
                TypeOperation = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.TypeOperation
            }).OrderBy(x => x.Date).ToList();

            var paymentsGrouped = result.GroupBy(y => y.ContractId).Select(x => new LiterAndCostItemInPayments { ContractId = x.Key, PaymentAmount = x.Sum(z => z.PaymentAmount) })
                .ToList();

            return result;
        }

        public async Task<IEnumerable<Contracts>> MovementUnderContractsAsync(Organizations organization) // Движение по договорам
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization, new DateOnly(2023, 1, 1));
            var contracts = incomeAndExpenses.GroupBy(x => x.ContractId).Select(y => new Contracts
            {
                ContractId = y.Key,
                Sum = y.Sum(z => z.Payment + z.Receipt)
            });

            return contracts;
        }

        public async Task<List<CashFlow>> CashFlowAsync(Organizations organization, DateOnly startDate, DateOnly endDate) // ДДС
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = (await IncomeAndExpensesAsync(organization, gettingData.StartDate))
                .Where(w => (w.DocumentName == "Списание с расчетного счета" || w.DocumentName == "Поступление на расчетный счет")).ToList();
            var literAndCostItemInAreaOfActivity = gettingData.GetLiterAndCostItemInAreaOfActivity();

            var incomeAndExpensesNotEmpty = incomeAndExpenses.Where(x => !string.IsNullOrEmpty(x.AreaOfActivity));
            var incomeAndExpensesEmpty = incomeAndExpenses.Where(x => string.IsNullOrEmpty(x.AreaOfActivity));
            var incomeAndExpensesEmptyPlusAreaOfActivity = from income in incomeAndExpensesEmpty
                                                           join areaOfActivity in literAndCostItemInAreaOfActivity
                                                           on income.LiterPayment + income.CostItemPayment equals areaOfActivity.Liter + areaOfActivity.CostItems
                                                           into tmp
                                                           from subareaOfActivity in tmp.DefaultIfEmpty()
                                                           select new IncomeAndExpenses
                                                           {
                                                               Date = income.Date,
                                                               Receipt = income.Receipt,
                                                               Payment = income.Payment,
                                                               TypeOperation = income.TypeOperation,
                                                               AreaOfActivity = subareaOfActivity != null ? subareaOfActivity.AreaOfActivity : income.TypeOperation,
                                                               LiterPayment = income.LiterPayment,
                                                               CostItemPayment = income.CostItemPayment                                                                
                                                           };

            var result  = incomeAndExpensesNotEmpty.Concat(incomeAndExpensesEmptyPlusAreaOfActivity).ToList();

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
                                            .OrderBy(or => or.AreaOfActivity)
                                            .ToList();

            cashFlow[0].Organization = organization.ToString();
            cashFlow[0].StartDate = startDate;
            cashFlow[0].EndDate = endDate;
            cashFlow[0].StartBalance = startBalance;

            return cashFlow;
        }

        public async Task<List<IncomeAndExpenses>> NoAreaOfActivityAsync(Organizations organization, DateOnly startDate, DateOnly endDate) // ДДС
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = (await IncomeAndExpensesAsync(organization, new DateOnly(2026, 1, 1)))
                .Where(w => (w.DocumentName == "Списание с расчетного счета" || w.DocumentName == "Поступление на расчетный счет")).ToList();
            var literAndCostItemInAreaOfActivity = gettingData.GetLiterAndCostItemInAreaOfActivity();

            var incomeAndExpensesNotEmpty = incomeAndExpenses.Where(x => !string.IsNullOrEmpty(x.AreaOfActivity));
            var incomeAndExpensesEmpty = incomeAndExpenses.Where(x => string.IsNullOrEmpty(x.AreaOfActivity));
            var incomeAndExpensesEmptyPlusAreaOfActivity = from income in incomeAndExpensesEmpty
                                                           join areaOfActivity in literAndCostItemInAreaOfActivity
                                                           on income.LiterPayment + income.CostItemPayment equals areaOfActivity.Liter + areaOfActivity.CostItems
                                                           into tmp
                                                           from subareaOfActivity in tmp.DefaultIfEmpty()
                                                           select new IncomeAndExpenses
                                                           {
                                                               Date = income.Date,
                                                               Receipt = income.Receipt,
                                                               Payment = income.Payment,
                                                               TypeOperation = income.TypeOperation,
                                                               AreaOfActivity = subareaOfActivity != null ? subareaOfActivity.AreaOfActivity : income.TypeOperation,
                                                               LiterPayment = income.LiterPayment,
                                                               CostItemPayment = income.CostItemPayment,
                                                               DocumentName = income.DocumentName,
                                                               Contractor = income.Contractor,
                                                               Number = income.Number,
                                                               ContractId = income.ContractId
                                                           };

            var result = incomeAndExpensesNotEmpty.Concat(incomeAndExpensesEmptyPlusAreaOfActivity).ToList();
            return result;
        }

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

            var actOfCompletion = (await gettingData.ActOfCompletionAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false);

            return actOfCompletion;
        }
    }
}