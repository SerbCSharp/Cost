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
        public async Task<IActionResult> ReconciliationStatementAsync([Required] Organizations organization)
        {
            //  добавить string
            var reconciliationStatement = await _generatingReports.ReconciliationStatementAsync("");
            _exportingReportsToExcel.ReconciliationStatement(reconciliationStatement);
            return NoContent();
        }

        /// <summary>Отчет о стоимости строительства</summary>
        /// <response>Записывает информацию в Cost.xlsx</response>
        [HttpGet("Cost")]
        public async Task<IActionResult> CostAsync([Required] Organizations organization)
        {
            var cost = await _generatingReports.CostAsync();
            _exportingReportsToExcel.Cost(cost);
            return NoContent();
        }

        /// <summary>Отсутствующие у нас договора</summary>
        /// <response>Записывает информацию в WeDoNotHaveTheseContracts.xlsx</response>
        [HttpGet("WeDoNotHaveTheseContracts")]
        public async Task<IActionResult> WeDoNotHaveTheseContractsAsync([Required] Organizations organization)
        {
            var noContracts = await _generatingReports.WeDoNotHaveTheseContractsAsync();
            _exportingReportsToExcel.WeDoNotHaveTheseContracts(noContracts);
            return NoContent();
        }

        /// <summary>Генподрядные, НДС за период</summary>
        /// <response>Записывает информацию в IncomeAndExpenses.xlsx</response>
        [HttpGet("IncomeAndExpenses")]
        public async Task<IActionResult> IncomeAndExpensesAsync([Required] Organizations organization)
        {
            // добавить date
            var incomeAndExpenses = await _generatingReports.IncomeAndExpensesAsync();
            _exportingReportsToExcel.IncomeAndExpenses(incomeAndExpenses);
            return NoContent();
        }
    }
}
