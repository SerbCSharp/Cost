using Cost.Domain;
using Cost.Infrastructure.Repositories.Models;
using Cost.Infrastructure.Repositories.Models.ContractsCounterparties;
using Cost.Infrastructure.Repositories.Models.OperationsTmp;
using Cost.Infrastructure.Repositories.Models.Payments;
using Cost.Presentation.DTO.Request;

namespace Cost.Application
{
    public class GeneratingReports
    {
        private readonly IGettingDataFactory _gettingDataFactory;

        public GeneratingReports(IGettingDataFactory gettingDataFactory)
        {
            _gettingDataFactory = gettingDataFactory;
        }

        public async Task<IEnumerable<Contracts>> WeDoNotHaveTheseContractsAsync(Organizations organization) // Отсутствующие у нас договора
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            var contractsCounterpartiesValue = (await gettingData.ContractsCounterpartiesAsync()).Value
                .Where(x => x.TypeAgreement == "СПоставщиком" && x.Date?.Year > 2024 && x.DeletionMark == false);

            // Поставщики + договора
            var counterparties = await gettingData.CounterpartiesAsync();
            var contractorPlusContract = counterparties.Value.Join(contractsCounterpartiesValue, p1 => p1.Ref_Key, c1 => c1.ContractorId,
                (p2, c2) => new { p2, c2 }).Where(x => x.p2.Description != "СЗ ЛЕГИС ООО");

            var contractsFrom1C = contractorPlusContract.Select(x => new Contracts
            {
                ContractId = x.c2.CounterpartyAgreementId,
                Contractor = x.p2.Description,
                Number = x.c2.Number,
                Name = x.c2.Name,
                Date = x.c2.Date,
                Sum = x.c2.Sum
            });

            var contractsFromExcel = gettingData.GetContracts();

            return contractsFrom1C.Except(contractsFromExcel);
        }

        public async Task<List<ReconciliationStatement>> ReconciliationStatementAsync(string contractName, Organizations organization) // Акт сверки
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            var contract = gettingData.GetContracts().FirstOrDefault(x => x.Name == contractName);
            var payments = (await gettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false
                && x.CounterpartyAgreementId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Debit = y.DocumentAmount,
                                    DocumentName = "Списание с расчетного счета"
                                });
            var receiptGoodsServices = (await gettingData.ReceiptGoodsServicesAsync()).Value
                .Where(x => x.Posted == true && x.ContractId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление товаров и услуг"
                                });

            var receiptProcessing = (await gettingData.ReceiptProcessingAsync()).Value
                .Where(x => x.Posted == true && x.ContractId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление из переработки"
                                });

            var paymentsPlusreceiptGoodsServices = payments.Concat(receiptGoodsServices);
            var plusReceiptProcessing = paymentsPlusreceiptGoodsServices.Concat(receiptProcessing);

            // ---------------------------------------------------------------------------------------------------------------

            var selling = (await gettingData.SellingAsync()).Value
                .Where(x => x.Posted == true && x.CounterpartyAgreementId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Debit = y.DocumentAmount,
                                    DocumentName = "Реализация"
                                });

            var plusSelling = plusReceiptProcessing.Concat(selling);

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
                Date = y.Date,
                Debit = y.Sum,
                DocumentName = "Корректировка долга"
            });

            var receivableReconciliationStatement = Receivable.Select(y => new ReconciliationStatement
            {
                Date = y.Date,
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
                Date = y.Date,
                Credit = y.Sum,
                DocumentName = "Корректировка долга"
            });

            var PlusPayableDoubleEntry = PayableDoubleEntry.Select(y => new ReconciliationStatement
            {
                Date = y.Date,
                Debit = y.Sum,
                DocumentName = "Корректировка долга"
            });

            var plusReceivableDoubleEntry = plusReceivable.Concat(PlusReceivableDoubleEntry);
            var plusPayableDoubleEntry = plusReceivableDoubleEntry.Concat(PlusPayableDoubleEntry);

            var receiptToCurrentAccount = (await gettingData.ReceiptToCurrentAccountAsync()).Value
                .Where(x => x.Posted == true && x.CounterpartyAgreementId == contract.ContractId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление на расчетный счет"
                                });

            var plusreceiptToCurrentAccount = plusPayableDoubleEntry.Concat(receiptToCurrentAccount);

            // ---------------------------------------------------------------------------------------------------------------

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

            var reconciliationStatement = plusOperationCredit.OrderBy(x => x.Date).ToList();
            reconciliationStatement.ForEach(item => { item.Contractor = contract.Contractor; item.Sum = contract.Sum; item.Name = contract.Name; });
            return reconciliationStatement;
        }

        public async Task<List<Domain.Cost>> CostAsync(Organizations organization) // Стоимость строительства объектов
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var incomeAndExpenses = await IncomeAndExpensesAsync(organization, new DateTime());

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
                                              Receipt = subIncome?.Receipt,
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
                                              NumberAA = con.NumberAA
                                          };

            var result = contractsPlusContractor.Where(y => y.NumberAA != "Гарантийное удержание").GroupBy(x => x.Contractor + x.Number).Select(y => new Domain.Cost
            {
                ContractId = y?.FirstOrDefault(z => string.IsNullOrEmpty(z?.NumberAA))?.ContractId,
                Contractor = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Contractor,
                Number = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Number,
                Date = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Date,
                Sum = y.Sum(z => z.Sum),
                ConstructionObject = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ConstructionObject,
                CostItem = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).CostItem,
                Receipt = y.Sum(z => z.Receipt),
                Payment = y.Sum(z => z.Payment),
                ContractClosed = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ContractClosed,
                ContractorOrSupplier = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ContractorOrSupplier,
                GeneralContracting = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).GeneralContracting,
                RateNDS = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).RateNDS,
                Name = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Name,
                WarrantyLien = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).WarrantyLien,
                TotalArea = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).TotalArea
            }).ToList();

            result.ForEach(item =>
            {
                if (!string.IsNullOrEmpty(item.ConstructionObject))
                {
                    if (item.ContractClosed == "Закрыт" || item.ContractClosed == "Расторгнут" || item.Receipt > item.Sum)
                    {
                        item.ConstructionCost = item.Receipt ?? 0;
                    }
                    else
                    {
                        item.ConstructionCost = item.Sum ?? 0;
                    }
                }
            });

            var supplier = incomeAndExpenses.Where(x => x.ContractorOrSupplier == "Поставщик")
                .GroupBy(y => new { y.ContractId, y.LiterPayment, y.CostItemPayment }).Where(w => !string.IsNullOrEmpty(w.Key.LiterPayment)).Select(z => new Domain.Cost
                {
                    ContractId = z.Key.ContractId,
                    Receipt = null,
                    Payment = z.Sum(s => s.Payment),
                    Contractor = z.FirstOrDefault().Contractor,
                    Number = z.FirstOrDefault().Number,
                    RateNDS = z.FirstOrDefault().RateNDS,
                    GeneralContracting = z.FirstOrDefault().GeneralContracting,
                    ConstructionObject = z.Key.LiterPayment,
                    ContractClosed = z.FirstOrDefault().ContractClosed,
                    ContractorOrSupplier = z.FirstOrDefault().ContractorOrSupplier,
                    CostItem = z.Key.CostItemPayment,
                    Date = z.FirstOrDefault().Date,
                    Sum = null,
                    WarrantyLien = z.FirstOrDefault().WarrantyLien,
                    Name = z.FirstOrDefault().Name,
                    ConstructionCost = z.Sum(s => s.Payment)
                }).Where(w => w.Payment != 0).ToList();

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
                                   TotalArea = subArea?.TotalArea ?? 0
                               };

            return PlusFacility.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Contractor).ThenBy(z => z.Number).ToList();
        }

        public async Task<List<IncomeAndExpenses>> IncomeAndExpensesAsync(Organizations organization, DateTime date)
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var payments = (await gettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false && x.Date >= date);

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
                                                     PaymentNDSAmount = subC.PaymentNDSAmount,
                                                     Liter = subC.Liter,
                                                     CostItems = subC.CostItems
                                                 };

            var invoiceReceived = (await gettingData.InvoiceReceivedAsync()).Value.Where(x => x.DeletionMark == false && x.Posted == true);
            var paymentsNDS = invoiceReceived.Where(x => x.DocumentType == "StandardODATA.Document_СписаниеСРасчетногоСчета");
            var Payments = from p in plusLiterAndCostItemInPayments
                           join c in paymentsNDS
                               on p.PaymentId equals c.DocumentId into tmp
                               from subC in tmp.DefaultIfEmpty()
                               select new IncomeAndExpenses()
                               { 
                                   Date = p.Date, 
                                   Payment = p.DocumentAmount,
                                   ContractId = p.CounterpartyAgreementId,
                                   DocumentAmount = p.DocumentAmount,
                                   DocumentNDSAmount = p.PaymentNDSAmount,
                                   InvoiceReceivedNDS = subC?.DocumentNDSAmount,
                                   LiterPayment = p.Liter,
                                   CostItemPayment = p.CostItems,
                                   DocumentName = "Списание с расчетного счета"
                               };

            var receiptGoodsServicesNDS = invoiceReceived.Where(x => x.DocumentType == "StandardODATA.Document_ПоступлениеТоваровУслуг");
            var receiptGoodsServices = (await gettingData.ReceiptGoodsServicesAsync()).Value.Where(x => x.Posted == true && x.Date >= date);
            var ReceiptGoodsServices = from p in receiptGoodsServices
                                       join c in receiptGoodsServicesNDS
                                       on p.ReceiptId equals c.DocumentId into tmp
                                       from subC in tmp.DefaultIfEmpty()
                                       select new IncomeAndExpenses()
                                       {
                                           Date = p.Date,
                                           Receipt = p.DocumentAmount,
                                           ContractId = p.ContractId,
                                           DocumentAmount = p.DocumentAmount,
                                           InvoiceReceivedNDS = subC?.DocumentNDSAmount,
                                           DocumentName = "Поступление товаров и услуг"
                                       };

            var receiptProcessingNDS = invoiceReceived.Where(x => x.DocumentType == "StandardODATA.Document_ПоступлениеИзПереработки");
            var receiptProcessing = (await gettingData.ReceiptProcessingAsync()).Value.Where(x => x.Posted == true && x.Date >= date);
            var ReceiptProcessing = from p in receiptProcessing
                                    join c in receiptProcessingNDS
                                       on p.ReceiptId equals c.DocumentId into tmp
                                       from subC in tmp.DefaultIfEmpty()
                                       select new IncomeAndExpenses()
                                       {
                                           Date = p.Date,
                                           Receipt = p.DocumentAmount,
                                           ContractId = p.ContractId,
                                           DocumentAmount = p.DocumentAmount,
                                           InvoiceReceivedNDS = subC?.DocumentNDSAmount,
                                           DocumentName = "Поступление из переработки"
                                       };

            var paymentsPlusreceiptGoodsServices = Payments.Concat(ReceiptGoodsServices);
            var plusReceiptProcessing = paymentsPlusreceiptGoodsServices.Concat(ReceiptProcessing);

            // ---------------------------------------------------------------------------------------------------------------

            var selling = (await gettingData.SellingAsync()).Value
                .Where(x => x.Posted == true && x.Date >= date)
                                .Select(y => new IncomeAndExpenses
                                {
                                    Date = y.Date,
                                    Payment = y.DocumentAmount,
                                    ContractId = y.CounterpartyAgreementId,
                                    DocumentName = "Реализация"
                                });

            var plusSelling = plusReceiptProcessing.Concat(selling);

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
                .Where(y => y.Date >= date).ToList();

            var Receivable = debtAdjustment.SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, c.CounterpartyAgreementId, c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.Date >= date).ToList();

            var payableIncomeAndExpenses = Payable.Select(y => new IncomeAndExpenses
            {
                Date = y.Date,
                Payment = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var receivableIncomeAndExpenses = Receivable.Select(y => new IncomeAndExpenses
            {
                Date = y.Date,
                Receipt = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var plusPayable = plusSelling.Concat(payableIncomeAndExpenses);
            var plusReceivable = plusPayable.Concat(receivableIncomeAndExpenses);

            var ReceivableDoubleEntry = debtAdjustment.Where(x => x.AccountsReceivable.Length == 0).SelectMany(x => x.AccountsPayable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.Date >= date).ToList();

            var PayableDoubleEntry = debtAdjustment.Where(x => x.AccountsPayable.Length == 0).SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.Date >= date).ToList();

            var PlusReceivableDoubleEntry = ReceivableDoubleEntry.Select(y => new IncomeAndExpenses
            {
                Date = y.Date,
                Receipt = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var PlusPayableDoubleEntry = PayableDoubleEntry.Select(y => new IncomeAndExpenses
            {
                Date = y.Date,
                Payment = y.Sum,
                ContractId = y.CounterpartyAgreementId,
                DocumentName = "Корректировка долга"
            });

            var plusReceivableDoubleEntry = plusReceivable.Concat(PlusReceivableDoubleEntry);
            var plusPayableDoubleEntry = plusReceivableDoubleEntry.Concat(PlusPayableDoubleEntry);

            var receiptToCurrentAccount = (await gettingData.ReceiptToCurrentAccountAsync()).Value
                .Where(x => x.Posted == true && x.Date >= date)
                                .Select(y => new IncomeAndExpenses
                                {
                                    Date = y.Date,
                                    Receipt = y.DocumentAmount,
                                    ContractId = y.CounterpartyAgreementId,
                                    DocumentName = "Поступление на расчетный счет"
                                });

            var plusreceiptToCurrentAccount = plusPayableDoubleEntry.Concat(receiptToCurrentAccount);

            // ---------------------------------------------------------------------------------------------------------------

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

            var contract = gettingData.GetContracts();
            var plusContract = from p in plusOperationCredit
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
                Name = x.subC.Name
            });

            return incomeAndExpenses.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Date).ToList();
        }

        public async Task<List<ContractsCounterpartiesValue>> ContractsFrom1CAsync(string typeAgreement, Organizations organization) // Договора из 1С
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            var contractsCounterpartiesValue = (await gettingData.ContractsCounterpartiesAsync()).Value;
                //.Where(x => x.TypeAgreement == typeAgreement).ToList();

            List<ContractsCounterpartiesValue> contractsCounterparties = null;

            var additionalInformation = await gettingData.AdditionalInformationAsync();
            
            contractsCounterparties = contractsCounterpartiesValue.ToList();

            var nomenclatureGroups = (await gettingData.NomenclatureGroupsAsync()).Value.Where(x => x.DeletionMark == false);
            //var constructionProjects = await gettingData.ConstructionProjectsAsync();
            var typesCalculations = await gettingData.TypesCalculationsAsync();
            var costItems = await gettingData.CostItemsAsync();

            foreach (var contractsCounterpartie in contractsCounterparties)
            {
                foreach (var AdditionalDetail in contractsCounterpartie.AdditionalDetails)
                {
                    if (AdditionalDetail.ValueType.Contains("НоменклатурныеГруппы"))
                    {
                        contractsCounterpartie.NomenclatureGroupsId = AdditionalDetail.Value;
                    }
                    if (AdditionalDetail.ValueType.Contains("СтатьиЗатрат"))
                    {
                        contractsCounterpartie.CostItemsId = AdditionalDetail.Value;
                    }
                }
            }

            var contractsGrouped = contractsCounterparties.ToList();

            var contractPlusNomenclatureGroup = from c1 in contractsGrouped
                                                join nomenclatureGroup in nomenclatureGroups
                                                       on c1.NomenclatureGroupsId equals nomenclatureGroup.Ref_Key into tmp
                                                from subNomenclatureGroup in tmp.DefaultIfEmpty()
                                                select new { c1, subNomenclatureGroup?.ConstructionObjectId, subNomenclatureGroup?.Description };

            //var contractPlusNomenclatureGroupPlusConstruction = from c2 in contractPlusNomenclatureGroup
            //                                                    join construction in constructionProjects.Value
            //                                                           on c2.ConstructionObjectId equals construction.Ref_Key into tmp
            //                                                    from subConstruction in tmp.DefaultIfEmpty()
            //                                                    select new { c2, subConstruction?.Description };

            var contractPlusNomenclatureGroupPlusConstructionPlusTypesCalculation = from c3 in contractPlusNomenclatureGroup
                                                                                    join calculation in typesCalculations.Value
                                                                                           on c3.c1.TypeCalculationId equals calculation.Ref_Key into tmp
                                                                                    from subCalculation in tmp.DefaultIfEmpty()
                                                                                    select new { c3, subCalculation?.Description };
            // Поставщики + договора
            var counterparties = await gettingData.CounterpartiesAsync();
            var contractorPlusContract = counterparties.Value.Join(contractPlusNomenclatureGroupPlusConstructionPlusTypesCalculation, p1 => p1.Ref_Key, c1 => c1.c3.c1.ContractorId,
                (p5, c5) => new { p5, c5 }).ToList();


            var contracts = from c4 in contractorPlusContract
                            join cost in costItems.Value
                                   on c4.c5.c3.c1.CostItemsId equals cost.Ref_Key into tmp
                            from subCost in tmp.DefaultIfEmpty()
                            select new ContractsCounterpartiesValue
                            {
                                ConstructionProjects = c4.c5.c3.Description,
                                ContractClosed = c4.c5.c3.c1.ContractClosed,
                                ContractorId = c4.c5.c3.c1.ContractorId,
                                CostItemsId = c4.c5.c3.c1.CostItemsId,
                                CostItems = subCost?.Description,
                                CounterpartyAgreementId = c4.c5.c3.c1.CounterpartyAgreementId,
                                Date = c4.c5.c3.c1.Date,
                                NomenclatureGroupsId = c4.c5.c3.c1.NomenclatureGroupsId,
                                Number = c4.c5.c3.c1.Number,
                                Name = c4.c5.c3.c1.Name,
                                OrganizationId = c4.c5.c3.c1.OrganizationId,
                                RateNDS = c4.c5.c3.c1.RateNDS,
                                Sum = c4.c5.c3.c1.Sum,
                                SumNDS = c4.c5.c3.c1.SumNDS,
                                TypeCalculation = c4.c5.Description,
                                Contractor = c4.p5.Description,
                                TypeAgreement = c4.c5.c3.c1.TypeAgreement
                            };

            return contracts.ToList();
        }

        public async Task<List<OperationsTmpValue>> Operations(Organizations organization)
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());
            var operations = (await gettingData.OperationAsync()).Value.Where(x => !x.DeletionMark && x.FillingMethod == "Вручную")
                .OrderByDescending(y => y.Date).ToList();

            return operations;
        }

        public async Task<List<LiterAndCostItemInPayments>> PaymentsAsync(Organizations organization) // Оплаты
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            //var serb = await gettingData.TmpAsync();

            var payments = (await gettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false);
            var billPayment = await gettingData.BillPaymentAsync();
            var additionalInformation = await gettingData.AdditionalInformationAsync();
            var nomenclatureGroups = (await gettingData.NomenclatureGroupsAsync()).Value.Where(x => x.DeletionMark == false); ;
            var costItems = await gettingData.CostItemsAsync();
            //var constructionProjects = await gettingData.ConstructionProjectsAsync();

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
                    Number = z.payment.Number
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
                                                                                    on payBill.InvoiceForPaymentId equals cons.Indicator into tmp
                                                                                    from subCons in tmp.DefaultIfEmpty()
                                                                                    select new { payBill, subCons?.IndicatorValue };

            var CostItemIds = additionalInformation.Value.Where(x => x.ValueType.Contains("СтатьиЗатрат", StringComparison.OrdinalIgnoreCase));
            var paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItem = from payCons in paymentsPlusCashFlowArticlesPlusBillPaymentPlusConstructionObject
                                                                          join cost in CostItemIds
                                                                          on payCons.payBill.InvoiceForPaymentId equals cost.Indicator into tmp
                                                                          from subCost in tmp.DefaultIfEmpty()
                                                                          select new { payCons, subCost?.IndicatorValue };

            var paymentsPlusCashFlowArticlesPlusBillPaymentPlusConstructionObjectName = from payObjectName in paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItem
                                                                                        join objectName in nomenclatureGroups
                                                                                        on payObjectName.payCons.IndicatorValue equals objectName.Ref_Key into tmp
                                                                                        from subObjectName in tmp.DefaultIfEmpty()
                                                                                        select new { payObjectName, subObjectName?.ConstructionObjectId, subObjectName?.Description };

            var paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItemName = from payCostName in paymentsPlusCashFlowArticlesPlusBillPaymentPlusConstructionObjectName
                                                                              join costName in costItems.Value
                                                                              on payCostName.payObjectName.IndicatorValue equals costName.Ref_Key into tmp
                                                                              from subCostName in tmp.DefaultIfEmpty()
                                                                              select new { payCostName, subCostName?.Description };

            //var plusConstructionObjectName = from payObjectName in paymentsPlusCashFlowArticlesPlusBillPaymentPlusCostItemName
            //                                 join objectName in constructionProjects.Value
            //                                 on payObjectName.payCostName.ConstructionObjectId equals objectName.Ref_Key into tmp
            //                                 from subObjectName in tmp.DefaultIfEmpty()
            //                                 select new { subObjectName?.Description, payObjectName };

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
                PaymentNDSAmount = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.PaymentNDSAmount,
                PurposePayment = string.IsNullOrEmpty(z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.PaymentPurpose) 
                    ? z.subcost?.PurposePayment : z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.PaymentPurpose,
                Date = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.Date,
                Number = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.Number,
                Contractor = z.payment.subcontract?.Contractor,
                LiterInAgreement = z.payment.subcontract?.ConstructionObject,
                CostItemsInAgreement = z.payment.subcontract?.CostItem,
                ContractorOrSupplier = z.payment.subcontract?.ContractorOrSupplier,
                ContractId = z.payment.payment.payCostName.payObjectName.payCons.payBill.payMany.CounterpartyAgreementId,
                ContractNumber = z.payment.subcontract?.Number,
                //Nomenclature = z.payment.payment.payCostName.Description
            }).OrderBy(x => x.Date).ToList();

            var paymentsGrouped = result.GroupBy(y => y.Contractor).Select(x => new LiterAndCostItemInPayments { Contractor = x.Key, PaymentAmount = x.Sum(z => z.PaymentAmount) })
                .OrderByDescending(o => o.PaymentAmount).ToList();

            return paymentsGrouped;

        }




        public async Task<List<Nomenclature>> Nomenclature(Organizations organization) // Проверка заполнения номенклатурных групп
        {
            IGettingData gettingData = _gettingDataFactory.Create(organization.ToString());

            var nomenclatureGroups = (await gettingData.NomenclatureGroupsAsync()).Value.Where(x => x.DeletionMark == false); ;
            var constructionProjects = await gettingData.ConstructionProjectsAsync();

            var plusConstructionObjectName = from payObjectName in nomenclatureGroups
                                             join objectName in constructionProjects.Value
                                             on payObjectName.ConstructionObjectId equals objectName.Ref_Key into tmp
                                             from subObjectName in tmp.DefaultIfEmpty()
                                             select new { payObjectName, subObjectName?.Description };


            return plusConstructionObjectName.Select(z => new Nomenclature
            {
                ConstructionName = z.Description,
                Description = z.payObjectName.Description
            }).ToList();
        }




    }
}