using Cost.Application;
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
        public async Task<IActionResult> ReconciliationStatementAsync([Required] Organizations Organization, [Required] string ContractName)
        {
            //  добавить string
            var reconciliationStatement = await _generatingReports.ReconciliationStatementAsync(ContractName, Organization);
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

        /// <summary>Генподрядные, НДС за период</summary>
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
            var contractsFrom1C = await _generatingReports.ContractsFrom1CAsync("СПоставщиком", Organization);
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
    }
}
