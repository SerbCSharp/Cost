using Cost.Domain;
using Cost.Infrastructure.Repositories;
using Cost.Infrastructure.Repositories.Models.AdditionalInformation;
using Cost.Infrastructure.Repositories.Models.ContractsCounterparties;
using Cost.Infrastructure.Repositories.Models.InvoiceReceived;
using Microsoft.AspNetCore.Mvc;

namespace Cost.Application
{
    public class GeneratingReports
    {
        private readonly IGettingDataFactory _gettingDataFactory;

        public GeneratingReports(IGettingDataFactory gettingDataFactory)
        {
            _gettingDataFactory = gettingDataFactory;
        }

        public async Task<List<ContractsCounterpartiesValue>> ContractsFrom1CAsync(int attributeAgreement, string typeAgreement, string purchaser = "") // Договора из 1С
        {
            IGettingData gettingData = _gettingDataFactory.Create("");
            var contractsCounterpartiesValue = (await gettingData.ContractsCounterpartiesAsync()).Value
                .Where(x => x.TypeAgreement == typeAgreement).ToList();

            List<AdditionalInformationValue> contractorOrSupplier = null;
            List<ContractsCounterpartiesValue> contractsCounterparties = null;

            var additionalInformation = await _iGettingData.AdditionalInformationAsync();
            switch (attributeAgreement)
            {
                case 1: // Подрядчики
                    contractorOrSupplier = additionalInformation.Value.Where(x => x.IndicatorType.Contains("ДоговорыКонтрагентов", StringComparison.OrdinalIgnoreCase)
                        && x.IndicatorValue == "1").ToList();
                    contractsCounterparties = contractsCounterpartiesValue.Join(contractorOrSupplier, p => p.CounterpartyAgreementId,
                        c => c.Indicator, (p, c) => p).ToList();
                    break;
                case 2: // Поставщики материалов
                    contractorOrSupplier = additionalInformation.Value.Where(x => x.IndicatorType.Contains("ДоговорыКонтрагентов", StringComparison.OrdinalIgnoreCase)
                        && x.IndicatorValue == "2").ToList();
                    contractsCounterparties = contractsCounterpartiesValue.Join(contractorOrSupplier, p => p.CounterpartyAgreementId,
                        c => c.Indicator, (p, c) => p).ToList();
                    break;
                default:
                    contractsCounterparties = contractsCounterpartiesValue.ToList();
                    break;
            }

            var nomenclatureGroups = await _iGettingData.NomenclatureGroupsAsync();
            var constructionProjects = await _iGettingData.ConstructionProjectsAsync();
            var typesCalculations = await _iGettingData.TypesCalculationsAsync();
            var costItems = await _iGettingData.CostItemsAsync();

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
                                                join nomenclatureGroup in nomenclatureGroups.Value
                                                       on c1.NomenclatureGroupsId equals nomenclatureGroup.Ref_Key into tmp
                                                from subNomenclatureGroup in tmp.DefaultIfEmpty()
                                                select new { c1, subNomenclatureGroup?.ConstructionObjectId };

            var contractPlusNomenclatureGroupPlusConstruction = from c2 in contractPlusNomenclatureGroup
                                                                join construction in constructionProjects.Value
                                                                       on c2.ConstructionObjectId equals construction.Ref_Key into tmp
                                                                from subConstruction in tmp.DefaultIfEmpty()
                                                                select new { c2, subConstruction?.Description };

            var contractPlusNomenclatureGroupPlusConstructionPlusTypesCalculation = from c3 in contractPlusNomenclatureGroupPlusConstruction
                                                                                    join calculation in typesCalculations.Value
                                                                                           on c3.c2.c1.TypeCalculationId equals calculation.Ref_Key into tmp
                                                                                    from subCalculation in tmp.DefaultIfEmpty()
                                                                                    select new { c3, subCalculation?.Description };
            // Поставщики + договора
            var counterparties = await _iGettingData.CounterpartiesAsync();
            var contractorPlusContract = counterparties.Value.Join(contractPlusNomenclatureGroupPlusConstructionPlusTypesCalculation, p1 => p1.Ref_Key, c1 => c1.c3.c2.c1.ContractorId,
                (p5, c5) => new { p5, c5 }).Where(x => x.p5.Description != purchaser).ToList();


            var contracts = from c4 in contractorPlusContract
                            join cost in costItems.Value
                                   on c4.c5.c3.c2.c1.CostItemsId equals cost.Ref_Key into tmp
                            from subCost in tmp.DefaultIfEmpty()
                            select new ContractsCounterpartiesValue
                            {
                                AgreementSigned = c4.c5.c3.c2.c1.AgreementSigned,
                                AmountIncludesNDS = c4.c5.c3.c2.c1.AmountIncludesNDS,
                                AmountSecurityRetention = c4.c5.c3.c2.c1.AmountSecurityRetention,
                                Comment = c4.c5.c3.c2.c1.Comment,
                                ConstructionProjects = c4.c5.c3.Description,
                                ContractClosed = c4.c5.c3.c2.c1.ContractClosed,
                                ContractorId = c4.c5.c3.c2.c1.ContractorId,
                                CostItemsId = c4.c5.c3.c2.c1.CostItemsId,
                                CostItems = subCost?.Description,
                                CounterpartyAgreementId = c4.c5.c3.c2.c1.CounterpartyAgreementId,
                                Date = c4.c5.c3.c2.c1.Date,
                                NomenclatureGroupsId = c4.c5.c3.c2.c1.NomenclatureGroupsId,
                                Number = c4.c5.c3.c2.c1.Number,
                                Name = c4.c5.c3.c2.c1.Name,
                                OrganizationId = c4.c5.c3.c2.c1.OrganizationId,
                                RateNDS = c4.c5.c3.c2.c1.RateNDS,
                                Sum = c4.c5.c3.c2.c1.Sum,
                                SumNDS = c4.c5.c3.c2.c1.SumNDS,
                                TypeCalculation = c4.c5.Description,
                                Contractor = c4.p5.Description,
                                TypeAgreement = c4.c5.c3.c2.c1.TypeAgreement
                            };

            return contracts.ToList();
        }

        public async Task<IEnumerable<Contracts>> WeDoNotHaveTheseContractsAsync() // Отсутствующие у нас договора
        {
            var contractsCounterpartiesValue = (await _iGettingData.ContractsCounterpartiesAsync()).Value
                .Where(x => x.TypeAgreement == "СПоставщиком" && x.Date?.Year > 2024 && x.DeletionMark == false);

            // Поставщики + договора
            var counterparties = await _iGettingData.CounterpartiesAsync();
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

            var contractsFromExcel = _iGettingData.GetContracts();

            return contractsFrom1C.Except(contractsFromExcel);
        }

        public async Task<List<ReconciliationStatement>> ReconciliationStatementAsync(string contractName) // Акт сверки
        {
            var contract = (await ContractsFrom1CAsync(0, "СПоставщиком", "СЗ ЛЕГИС ООО")).FirstOrDefault(x => x.Name == contractName);
            var payments = (await _iGettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false
                && x.CounterpartyAgreementId == contract.CounterpartyAgreementId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Debit = y.DocumentAmount,
                                    DocumentName = "Списание с расчетного счета"
                                });
            var receiptGoodsServices = (await _iGettingData.ReceiptGoodsServicesAsync()).Value
                .Where(x => x.Posted == true && x.ContractId == contract.CounterpartyAgreementId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление товаров и услуг"
                                });

            var receiptProcessing = (await _iGettingData.ReceiptProcessingAsync()).Value
                .Where(x => x.Posted == true && x.ContractId == contract.CounterpartyAgreementId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление из переработки"
                                });

            var paymentsPlusreceiptGoodsServices = payments.Concat(receiptGoodsServices);
            var plusReceiptProcessing = paymentsPlusreceiptGoodsServices.Concat(receiptProcessing);

            // ---------------------------------------------------------------------------------------------------------------

            var selling = (await _iGettingData.SellingAsync()).Value
                .Where(x => x.Posted == true && x.CounterpartyAgreementId == contract.CounterpartyAgreementId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Debit = y.DocumentAmount,
                                    DocumentName = "Реализация"
                                });

            var plusSelling = plusReceiptProcessing.Concat(selling);

            var debtAdjustment = (await _iGettingData.DebtAdjustmentAsync()).Value.Where(x => x.Posted == true).ToList();
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
                .Where(y => y.CounterpartyAgreementId == contract.CounterpartyAgreementId).ToList();

            var Receivable = debtAdjustment.SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, c.CounterpartyAgreementId, c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.CounterpartyAgreementId == contract.CounterpartyAgreementId).ToList();

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
                .Where(y => y.CounterpartyAgreementId == contract.CounterpartyAgreementId).ToList();

            var PayableDoubleEntry = debtAdjustment.Where(x => x.AccountsPayable.Length == 0).SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum, p.Date, p })
                .Where(y => y.CounterpartyAgreementId == contract.CounterpartyAgreementId).ToList();

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

            var receiptToCurrentAccount = (await _iGettingData.ReceiptToCurrentAccountAsync()).Value
                .Where(x => x.Posted == true && x.CounterpartyAgreementId == contract.CounterpartyAgreementId)
                                .Select(y => new ReconciliationStatement
                                {
                                    Date = y.Date,
                                    Credit = y.DocumentAmount,
                                    DocumentName = "Поступление на расчетный счет"
                                });

            var plusreceiptToCurrentAccount = plusPayableDoubleEntry.Concat(receiptToCurrentAccount);

            // ---------------------------------------------------------------------------------------------------------------

            var operations = _iGettingData.GetOperations();
            var operationDebit = operations.Where(x => x.ContractDebit == contract.CounterpartyAgreementId)
                    .Select(y => new ReconciliationStatement
                    {
                        Date = y.Date,
                        Debit = y.Sum,
                        DocumentName = "Операция"
                    });

            var plusOperationDebit = plusreceiptToCurrentAccount.Concat(operationDebit);

            var operationCredit = operations.Where(x => x.ContractCredit == contract.CounterpartyAgreementId)
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

        public async Task<List<Domain.Cost>> CostAsync() // Стоимость строительства объектов
        {
            var contracts = _iGettingData.GetContracts();

            var payments = (await _iGettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false);

            var groupPayments = payments.GroupBy(x => x.CounterpartyAgreementId)
                .Select(y => new { ContractId = y.Key, SumPayment = y.Sum(z => z.DocumentAmount) }).ToList();

            var receiptGoodsServices = (await _iGettingData.ReceiptGoodsServicesAsync()).Value.Where(x => x.Posted == true);
            var receiptProcessing = (await _iGettingData.ReceiptProcessingAsync()).Value.Where(x => x.Posted == true);
            var receipts = receiptGoodsServices.Concat(receiptProcessing).ToList();
            var groupReceipts = receipts.GroupBy(x => x.ContractId).Select(y => new { ContractId = y.Key, SumReceipt = y.Sum(z => z.DocumentAmount) }).ToList();

            var contractsPlusPayments = from con1 in contracts
                                        join pay in groupPayments
                                        on con1.ContractId equals pay.ContractId into tmp
                                        from subPay in tmp.DefaultIfEmpty()
                                        select new { con1, subPay };

            var contractsPlusPaymentsPlusReceipts = from con2 in contractsPlusPayments
                                                    join rec in groupReceipts
                                                    on con2.con1.ContractId equals rec.ContractId into tmp
                                                    from subRec in tmp.DefaultIfEmpty()
                                                    select new { con2, sumReceipt = subRec?.SumReceipt };

            // ---------------------------------------------------------------------------------------------------------------

            var selling = (await _iGettingData.SellingAsync()).Value.Where(x => x.Posted == true);
            var sellingGrouped = selling.GroupBy(y => y.CounterpartyAgreementId)
                .Select(x => new { CounterpartyAgreementId = x.Key, DocumentAmount = x.Sum(y => y.DocumentAmount) });

            var PlusSelling = from c1 in contractsPlusPaymentsPlusReceipts
                              join Selling in sellingGrouped
                              on c1.con2.con1.ContractId equals Selling.CounterpartyAgreementId into tmp
                              from subSelling in tmp.DefaultIfEmpty()
                              select new { c1, Selling = subSelling?.DocumentAmount ?? 0 };

            var debtAdjustment = (await _iGettingData.DebtAdjustmentAsync()).Value.Where(x => x.Posted == true).ToList();
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

            var Payable = debtAdjustment.SelectMany(x => x.AccountsPayable, (p, c) => new { p.Ref_Key, c.CounterpartyAgreementId, c.Sum }).ToList();
            var Receivable = debtAdjustment.SelectMany(x => x.AccountsReceivable, (p, c) => new { p.Ref_Key, c.CounterpartyAgreementId, c.Sum }).ToList();


            var ReceivableDoubleEntry = debtAdjustment.Where(x => x.AccountsReceivable.Length == 0).SelectMany(x => x.AccountsPayable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum }).ToList();

            var PayableDoubleEntry = debtAdjustment.Where(x => x.AccountsPayable.Length == 0).SelectMany(x => x.AccountsReceivable, (p, c) =>
            new { p.Ref_Key, CounterpartyAgreementId = c.CorCounterpartyAgreementId, c.Sum }).ToList();

            var ReceivableDE = Receivable.Concat(ReceivableDoubleEntry);
            var PayableDE = Payable.Concat(PayableDoubleEntry);

            var payableGrouped = PayableDE.GroupBy(y => y.CounterpartyAgreementId)
                .Select(x => new { CounterpartyAgreementId = x.Key, Sum = x.Sum(y => y.Sum) });
            var receivableGrouped = ReceivableDE.GroupBy(y => y.CounterpartyAgreementId)
                .Select(x => new { CounterpartyAgreementId = x.Key, Sum = x.Sum(y => y.Sum) });

            var PlusPayable = from c2 in PlusSelling
                              join payable in payableGrouped
                              on c2.c1.con2.con1.ContractId equals payable.CounterpartyAgreementId into tmp
                              from subPayable in tmp.DefaultIfEmpty()
                              select new { c2, sumPayable = subPayable?.Sum ?? 0 };

            var PlusReceivable = from c3 in PlusPayable
                                 join receivable in receivableGrouped
                                 on c3.c2.c1.con2.con1.ContractId equals receivable.CounterpartyAgreementId into tmp
                                 from subReceivable in tmp.DefaultIfEmpty()
                                 select new { c3, sumReceivable = subReceivable?.Sum ?? 0 };

            var receiptToCurrentAccount = (await _iGettingData.ReceiptToCurrentAccountAsync()).Value.Where(x => x.Posted == true);
            var receiptToCurrentAccountGrouped = receiptToCurrentAccount.GroupBy(y => y.CounterpartyAgreementId)
                .Select(x => new { CounterpartyAgreementId = x.Key, DocumentAmount = x.Sum(y => y.DocumentAmount) });

            var PlusReceiptToCurrentAccount = from c4 in PlusReceivable
                                              join receiptTo in receiptToCurrentAccountGrouped
                                              on c4.c3.c2.c1.con2.con1.ContractId equals receiptTo.CounterpartyAgreementId into tmp
                                              from subReceiptTo in tmp.DefaultIfEmpty()
                                              select new { c4, ReceiptToCurrentAccount = subReceiptTo?.DocumentAmount ?? 0 };

            // ---------------------------------------------------------------------------------------------------------------

            var facility = _iGettingData.GetFacility();

            var facilityGrouped = facility.GroupBy(y => y.ObjectNameIn1C).Select(x => new { ObjectNameIn1C = x.Key, x.FirstOrDefault().TotalArea });
            var PlusFacility = from c5 in PlusReceiptToCurrentAccount
                               join area in facilityGrouped
                               on c5.c4.c3.c2.c1.con2.con1.ConstructionObject equals area.ObjectNameIn1C into tmp
                               from subArea in tmp.DefaultIfEmpty()
                               select new { c5, TotalArea = subArea?.TotalArea ?? 0 };

            var operations = _iGettingData.GetOperations();
            var operationsDebitGrouped = operations.GroupBy(y => y.ContractDebit).Select(x => new { ContractDebit = x.Key, Sum = x.Sum(y => y.Sum) });
            var PlusOperationDebit = from c6 in PlusFacility
                                     join operD in operationsDebitGrouped
                                     on c6.c5.c4.c3.c2.c1.con2.con1.ContractId equals operD.ContractDebit into tmp
                                     from suboperD in tmp.DefaultIfEmpty()
                                     select new { c6, SumDebit = suboperD?.Sum ?? 0 };

            var operationsCreditGrouped = operations.GroupBy(y => y.ContractCredit).Select(x => new { ContractCredit = x.Key, Sum = x.Sum(y => y.Sum) });
            var PlusOperationCredit = from c7 in PlusOperationDebit
                                      join operC in operationsCreditGrouped
                                      on c7.c6.c5.c4.c3.c2.c1.con2.con1.ContractId equals operC.ContractCredit into tmp
                                      from suboperC in tmp.DefaultIfEmpty()
                                      select new { c7, SumCredit = suboperC?.Sum ?? 0 };

            var invoiceReceived = await InvoiceReceivedAsync();
            var invoiceReceivedGrouped = invoiceReceived.GroupBy(y => y.CounterpartyAgreementId).Select(x => new
            {
                CounterpartyAgreementId = x.Key,
                DocumentAmount = x.Sum(y => y.DocumentAmount),
                DocumentNDSAmount = x.Sum(y => y.DocumentNDSAmount)
            });
            var PlusInvoiceReceived = from c8 in PlusOperationCredit
                                      join invoice in invoiceReceivedGrouped
                                      on c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.ContractId equals invoice.CounterpartyAgreementId into tmp
                                      from subInvoice in tmp.DefaultIfEmpty()
                                      select new { c8, DocumentAmount = subInvoice?.DocumentAmount ?? 0, DocumentNDSAmount = subInvoice?.DocumentNDSAmount ?? 0 };

            var cost = PlusInvoiceReceived.Select(x => new Domain.Cost
            {
                ContractId = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.ContractId,
                Contractor = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.Contractor,
                Number = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.Number,
                NumberAA = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.NumberAA,
                Date = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.Date,
                Sum = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.Sum,
                ConstructionObject = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.ConstructionObject,
                CostItem = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.CostItem,
                Receipt = x.c8.c7.c6.c5.c4.c3.c2.c1.sumReceipt ?? 0,
                Payment = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.subPay?.SumPayment ?? 0,
                Selling = x.c8.c7.c6.c5.c4.c3.c2.Selling,
                Payable = x.c8.c7.c6.c5.c4.c3.sumPayable,
                Receivable = x.c8.c7.c6.c5.c4.sumReceivable,
                ReceiptToCurrentAccount = x.c8.c7.c6.c5.ReceiptToCurrentAccount,
                AmountIncludesNDS = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.AmountIncludesNDS,
                ContractClosed = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.ContractClosed,
                ContractorOrSupplier = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.ContractorOrSupplier,
                GeneralContracting = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.GeneralContracting,
                RateNDS = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.RateNDS,
                Name = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.Name,
                WarrantyLien = x.c8.c7.c6.c5.c4.c3.c2.c1.con2.con1.WarrantyLien,
                TotalArea = x.c8.c7.c6.TotalArea,
                SumDebit = x.c8.c7.SumDebit,
                SumCredit = x.c8.SumCredit,
                DocumentAmount = x.DocumentAmount,
                DocumentNDSAmount = x.DocumentNDSAmount
            }).ToList();

            cost.ForEach(item =>
            {
                item.Receipt = item.Receipt + item.Receivable + item.ReceiptToCurrentAccount + item.SumCredit;
                item.Payment = item.Payment + item.Payable + item.Selling + item.SumDebit;
            });

            var result = cost.Where(y => y.NumberAA != "Гарантийное удержание").GroupBy(x => x.Contractor + x.Number).Select(y => new Domain.Cost
            {
                Contractor = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Contractor,
                Number = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Number,
                Date = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).Date,
                Sum = y.Sum(z => z.Sum),
                ConstructionObject = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).ConstructionObject,
                CostItem = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).CostItem,
                Receipt = y.Sum(z => z.Receipt),
                Payment = y.Sum(z => z.Payment),
                AmountIncludesNDS = y.FirstOrDefault(z => string.IsNullOrEmpty(z.NumberAA)).AmountIncludesNDS,
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
                    if (item.ContractorOrSupplier == "Подрядчик")
                    {
                        if (item.ContractClosed == "Закрыт" || item.ContractClosed == "Расторгнут" || item.Receipt > item.Sum)
                        {
                            item.ConstructionCost = item.Receipt;
                        }
                        else
                        {
                            item.ConstructionCost = item.Sum ?? 0;
                        }
                    }
                    else
                    {
                        item.ConstructionCost = item.Payment;
                    }
                }
            });

            return result;
        }

        public async Task<List<InvoiceReceivedValue>> InvoiceReceivedAsync()
        {
            var invoiceReceived = (await _iGettingData.InvoiceReceivedAsync()).Value.Where(x => x.DeletionMark == false && x.Posted == true);
            var noContractCode = invoiceReceived.Where(x => x.CounterpartyAgreementId == "00000000-0000-0000-0000-000000000000");

            var payments = (await _iGettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false);
            var PlusPayments = from c1 in noContractCode
                               join payment in payments
                               on c1.DocumentId equals payment.PaymentId into tmp
                               from subPayment in tmp.DefaultIfEmpty()
                               select new { c1, subPayment?.CounterpartyAgreementId };

            var PlusNoContractCode = from c2 in invoiceReceived
                                     join plusPayment in PlusPayments
                                     on c2.InvoiceReceivedId equals plusPayment.c1.InvoiceReceivedId into tmp
                                     from subPlusPayment in tmp.DefaultIfEmpty()
                                     select new { c2, subPlusPayment?.CounterpartyAgreementId };

            return PlusNoContractCode.Select(x => new InvoiceReceivedValue
            {
                InvoiceReceivedId = x.c2.InvoiceReceivedId,
                Number = x.c2.Number,
                DocumentType = x.c2.DocumentType,
                Date = x.c2.Date,
                TypeInvoice = x.c2.TypeInvoice,
                CounterpartyAgreementId = x.c2.CounterpartyAgreementId == "00000000-0000-0000-0000-000000000000" ? x.CounterpartyAgreementId : x.c2.CounterpartyAgreementId,
                DocumentAmount = x.c2.DocumentAmount + x.c2.AmountPlus - x.c2.AmountMinus,
                DocumentNDSAmount = x.c2.DocumentNDSAmount + x.c2.AmountNDSPlus - x.c2.AmountNDSMinus
            }).ToList();
        }

        public async Task<List<IncomeAndExpenses>> IncomeAndExpensesAsync()
        {
            var date = new DateTime(2021, 10, 1);

            var invoiceReceived = (await _iGettingData.InvoiceReceivedAsync()).Value.Where(x => x.DeletionMark == false && x.Posted == true);

            var paymentsNDS = invoiceReceived.Where(x => x.DocumentType == "StandardODATA.Document_СписаниеСРасчетногоСчета");
            var payments = (await _iGettingData.PaymentsAsync()).Value.Where(x => x.Posted == true && x.DeletionMark == false && x.Date >= date);
            var Payments = from p in payments
                           join c in paymentsNDS
                               on p.PaymentId equals c.DocumentId into tmp
                               from subC in tmp.DefaultIfEmpty()
                               select new IncomeAndExpenses()
                               { 
                                   Date = p.Date, 
                                   Payment = p.DocumentAmount,
                                   ContractId = p.CounterpartyAgreementId,
                                   DocumentAmount = subC?.DocumentAmount,
                                   DocumentNDSAmount = subC?.DocumentNDSAmount,
                                   DocumentName = "Списание с расчетного счета"
                               };

            var receiptGoodsServicesNDS = invoiceReceived.Where(x => x.DocumentType == "StandardODATA.Document_ПоступлениеТоваровУслуг");
            var receiptGoodsServices = (await _iGettingData.ReceiptGoodsServicesAsync()).Value.Where(x => x.Posted == true && x.Date >= date);
            var ReceiptGoodsServices = from p in receiptGoodsServices
                                       join c in receiptGoodsServicesNDS
                                       on p.ReceiptId equals c.DocumentId into tmp
                                       from subC in tmp.DefaultIfEmpty()
                                       select new IncomeAndExpenses()
                                       {
                                           Date = p.Date,
                                           Receipt = p.DocumentAmount,
                                           ContractId = p.ContractId,
                                           DocumentAmount = subC?.DocumentAmount,
                                           DocumentNDSAmount = subC?.DocumentNDSAmount,
                                           DocumentName = "Поступление товаров и услуг"
                                       };

            var receiptProcessingNDS = invoiceReceived.Where(x => x.DocumentType == "StandardODATA.Document_ПоступлениеИзПереработки");
            var receiptProcessing = (await _iGettingData.ReceiptProcessingAsync()).Value.Where(x => x.Posted == true && x.Date >= date);
            var ReceiptProcessing = from p in receiptProcessing
                                    join c in receiptProcessingNDS
                                       on p.ReceiptId equals c.DocumentId into tmp
                                       from subC in tmp.DefaultIfEmpty()
                                       select new IncomeAndExpenses()
                                       {
                                           Date = p.Date,
                                           Receipt = p.DocumentAmount,
                                           ContractId = p.ContractId,
                                           DocumentAmount = subC?.DocumentAmount,
                                           DocumentNDSAmount = subC?.DocumentNDSAmount,
                                           DocumentName = "Поступление из переработки"
                                       };

            var paymentsPlusreceiptGoodsServices = Payments.Concat(ReceiptGoodsServices);
            var plusReceiptProcessing = paymentsPlusreceiptGoodsServices.Concat(ReceiptProcessing);

            // ---------------------------------------------------------------------------------------------------------------

            var selling = (await _iGettingData.SellingAsync()).Value
                .Where(x => x.Posted == true && x.Date >= date)
                                .Select(y => new IncomeAndExpenses
                                {
                                    Date = y.Date,
                                    Payment = y.DocumentAmount,
                                    ContractId = y.CounterpartyAgreementId,
                                    DocumentName = "Реализация"
                                });

            var plusSelling = plusReceiptProcessing.Concat(selling);

            var debtAdjustment = (await _iGettingData.DebtAdjustmentAsync()).Value.Where(x => x.Posted == true).ToList();
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

            var receiptToCurrentAccount = (await _iGettingData.ReceiptToCurrentAccountAsync()).Value
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

            var operations = _iGettingData.GetOperations();
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
            var contract = _iGettingData.GetContracts();


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
                Contractor = x.subC.Contractor,
                Number = x.subC.Number,
                RateNDS = x.subC.RateNDS,
                GeneralContracting = x.subC.GeneralContracting
            });

            return incomeAndExpenses.Where(y => !string.IsNullOrEmpty(y.ContractId)).OrderBy(x => x.Date).ToList();
        }
    }
}