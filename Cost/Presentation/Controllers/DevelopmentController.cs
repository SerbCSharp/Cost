using Cost.Application;
using Cost.Domain;
using Cost.Presentation.DTO.Request;
using Cost.Presentation.ReportsToExcel;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Cost.Presentation.Controllers
{
    [ApiController]
    public class DevelopmentController : ControllerBase
    {
        private readonly GeneratingReports _generatingReports;
        private readonly ExportingReportsToExcel _exportingReportsToExcel;

        public DevelopmentController(GeneratingReports generatingReports, ExportingReportsToExcel exportingReportsToExcel)
        {
            _generatingReports = generatingReports;
            _exportingReportsToExcel = exportingReportsToExcel;
        }

        /// <summary>Акт сверки</summary>
        /// <response>Записывает информацию в Transcript.xlsx</response>
        [HttpGet("ReconciliationStatement")]
        public async Task<IActionResult> ReconciliationStatementAsync([Required] Organizations Organization, [Required] string ContractName, string Contractor)
        {
            //  добавить string
            var reconciliationStatement = await _generatingReports.ReconciliationStatementAsync(ContractName, Organization, Contractor);
            _exportingReportsToExcel.ReconciliationStatement(reconciliationStatement);
            return NoContent();
        }

        /// <summary>Отчет о стоимости строительства</summary>
        /// <response>Записывает информацию в Cost.xlsx</response>
        [HttpGet("Cost")]
        public async Task<IActionResult> CostAsync([Required] Organizations Organization)
        {
            var cost = await _generatingReports.CostAsync(Organization);
            _exportingReportsToExcel.Cost(cost);
            return NoContent();
        }

        /// <summary>Отсутствующие у нас договора</summary>
        /// <response>Записывает информацию в WeDoNotHaveTheseContracts.xlsx</response>
        [HttpGet("WeDoNotHaveTheseContracts")]
        public async Task<IActionResult> WeDoNotHaveTheseContractsAsync([Required] Organizations Organization)
        {
            var noContracts = await _generatingReports.WeDoNotHaveTheseContractsAsync(Organization);
            _exportingReportsToExcel.WeDoNotHaveTheseContracts(noContracts);
            return NoContent();
        }

        /// <summary>Доходы и расходы</summary>
        /// <response>Записывает информацию в IncomeAndExpenses.xlsx</response>
        [HttpGet("IncomeAndExpenses")]
        public async Task<IActionResult> IncomeAndExpensesAsync([Required] Organizations Organization, DateTime date)
        {
            var incomeAndExpenses = await _generatingReports.IncomeAndExpensesAsync(Organization, date);
            _exportingReportsToExcel.IncomeAndExpenses(incomeAndExpenses);
            return NoContent();
        }

        /// <summary>Договора из 1С</summary>
        /// <response>Записывает информацию в Contracts.xlsx</response>
        [HttpGet("ContractsFrom1C")]
        public async Task<IActionResult> ContractsFrom1CAsync([Required] Organizations Organization)
        {
            var contractsFrom1C = await _generatingReports.ContractsFrom1CAsync(Organization);
            _exportingReportsToExcel.ContractsFrom1C(contractsFrom1C);
            return NoContent();
        }

        /// <summary>Операции из 1С</summary>
        /// <response>Записывает информацию в Operations.xlsx</response>
        [HttpGet("Operations")]
        public async Task<IActionResult> OperationsAsync([Required] Organizations Organization)
        {
            var operations = await _generatingReports.Operations(Organization);
            _exportingReportsToExcel.Operations(operations);
            return NoContent();
        }

        /// <summary>Оплаты</summary>
        /// <response>Записывает информацию в Payments.xlsx</response>
        [HttpGet("Payments")]
        public async Task<IActionResult> PaymentsAsync([Required] Organizations Organization)
        {
            var payments = await _generatingReports.PaymentsAsync(Organization);
            _exportingReportsToExcel.Payments(payments);
            return NoContent();

        }

        /// <summary>Отсутствующие у нас договора по которым есть оплаты</summary>
        /// <response>Записывает информацию в Payments.xlsx</response>
        [HttpGet("WeDoNotHaveThesePayments")]
        public async Task<IActionResult> WeDoNotHaveThesePaymentsAsync([Required] Organizations Organization)
        {
            var noPayments = await _generatingReports.WeDoNotHaveThesePaymentsAsync(Organization);
            _exportingReportsToExcel.Payments(noPayments.ToList());
            return NoContent();
        }

        /// <summary>Номенклатура</summary>
        /// <response>Записывает информацию в Nomenclature.xlsx</response>
        [HttpGet("Nomenclature")]
        public async Task<IActionResult> NomenclatureAsync([Required] Organizations Organization)
        {
            var noPayments = await _generatingReports.NomenclatureAsync(Organization);
            _exportingReportsToExcel.Nomenclature(noPayments);
            return NoContent();
        }

        /// <summary>Движение по договорам</summary>
        /// <response>Записывает информацию в WeDoNotHaveTheseContracts.xlsx</response>
        [HttpGet("MovementUnderContracts")]
        public async Task<IActionResult> MovementUnderContractsAsync([Required] Organizations Organization)
        {
            var contracts = await _generatingReports.MovementUnderContractsAsync(Organization);
            _exportingReportsToExcel.WeDoNotHaveTheseContracts(contracts);
            return NoContent();
        }

        /// <summary>Выполнения до 2026 года</summary>
        /// <response>Записывает информацию в IncomeAndExpenses.xlsx</response>
        [HttpGet("IncomeAndExpensesTmp")]
        public async Task<IActionResult> IncomeAndExpensesTmpAsync([Required] Organizations Organization, DateTime date)
        {
            var incomeAndExpenses = await _generatingReports.IncomeAndExpensesAsync(Organization, date);
            var result = incomeAndExpenses.Where(w => w.Date.Year != 2026).GroupBy(x => x.ContractId).Select(y => new IncomeAndExpenses
            {
                ContractId = y.Key,
                Receipt = y.Sum(z => z.Receipt),
                Payment = y.Sum(z => z.Payment)
            }).ToList();

            _exportingReportsToExcel.IncomeAndExpenses(result);
            return NoContent();
        }

        /// <summary>Отчет о доходах от строительства объектов</summary>
        /// <response>Записывает информацию в Cost.xlsx</response>
        [HttpGet("Income")]
        public async Task<IActionResult> IncomeAsync([Required] Organizations Organization)
        {
            var income = await _generatingReports.IncomeAsync(Organization);
            _exportingReportsToExcel.Income(income);
            return NoContent();
        }
    }
}
