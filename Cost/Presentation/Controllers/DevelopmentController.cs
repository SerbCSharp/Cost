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

        /// <summary>Универсальный просмотрщик коллекций</summary>
        /// <response>Записывает информацию в Browse.xlsx</response>
        [HttpGet("Browse")]
        public async Task<IActionResult> BrowseAsync([Required] Organizations organization)
        {
            var browse = await _generatingReports.IncomeAndExpensesAsync(organization);
            _exportingReportsToExcel.Browse(browse);
            return NoContent();
        }

        /// <summary>Отсутствующие у нас договора</summary>
        /// <response>Записывает информацию в WeDoNotHaveTheseContracts.xlsx</response>
        [HttpGet("WeDoNotHaveTheseContracts")]
        public async Task<IActionResult> WeDoNotHaveTheseContractsAsync([Required] Organizations organization)
        {
            var noContracts = await _generatingReports.WeDoNotHaveTheseContractsAsync(organization);
            _exportingReportsToExcel.WeDoNotHaveTheseContracts(noContracts);
            return NoContent();
        }

        /// <summary>Расходные оплаты</summary>
        /// <response>Записывает информацию в Payments.xlsx</response>
        [HttpGet("Payments")]
        public async Task<IActionResult> PaymentsAsync([Required] Organizations organization)
        {
            var payments = await _generatingReports.PaymentsAsync(organization);
            _exportingReportsToExcel.Payments(payments);
            return NoContent();
        }

        /// <summary>Акт сверки</summary>
        /// <response>Записывает информацию в Transcript.xlsx</response>
        [HttpGet("ReconciliationStatement")]
        public async Task<IActionResult> ReconciliationStatementAsync([Required] Organizations organization, [Required] string contractName, string contractor)
        {
            var reconciliationStatement = await _generatingReports.ReconciliationStatementAsync(contractName, organization, contractor);
            _exportingReportsToExcel.ReconciliationStatement(reconciliationStatement);
            return NoContent();
        }

        /// <summary>Отчет о стоимости строительства</summary>
        /// <response>Записывает информацию в Cost.xlsx</response>
        [HttpGet("Expense")]
        public async Task<IActionResult> ExpenseAsync([Required] Organizations organization)
        {
            var expense = await _generatingReports.ExpenseAsync(organization);
            _exportingReportsToExcel.Expense(expense);
            return NoContent();
        }

        /// <summary>ДДС</summary>
        /// <response>Записывает информацию в CashFlow.xlsx</response>
        [HttpGet("CashFlow")]
        public async Task<IActionResult> CashFlowAsync([Required] Organizations organization, DateOnly startDate, DateOnly endDate)
        {
            startDate = startDate.Year == 1 ? new DateOnly(2026, 1, 1) : startDate;
            endDate = endDate.Year == 1 ? DateOnly.FromDateTime(DateTime.Now) : endDate;

            var cashFlowSource = await _generatingReports.CashFlowSourceAsync(organization, startDate, endDate);
            _exportingReportsToExcel.CashFlowSource(cashFlowSource.Where(z => z.Date >= startDate && z.Date <= endDate), organization.ToString());

            //var shareInNDS = _generatingReports.ShareInNDS(cashFlowSource.Where(z => z.Date >= startDate && z.Date <= endDate));
            //_exportingReportsToExcel.ShareInNDS(shareInNDS, organization.ToString(), startDate, endDate);

            var startBalance = _generatingReports.StartBalance(cashFlowSource, organization, startDate);
            var cashFlow = _generatingReports.CashFlow([.. cashFlowSource], organization, startDate, endDate);
            _exportingReportsToExcel.CashFlow(cashFlow, startBalance, organization.ToString(), startDate, endDate);
            return NoContent();
        }

        /// <summary>Текущая задолженность</summary>
        /// <response>Записывает информацию в CurrentDebt.xlsx</response>
        [HttpGet("CurrentDebt")]
        public async Task<IActionResult> CurrentDebtAsync([Required] Organizations organization)
        {
            var currentDebt = await _generatingReports.CurrentDebtAsync(organization);
            _exportingReportsToExcel.CurrentDebt(currentDebt);
            return NoContent();
        }

        /// <summary>Выполнения до 2026 года</summary>
        /// <response>Записывает информацию в Browse.xlsx</response>
        [HttpGet("AmountUntil2026")]
        public async Task<IActionResult> AmountUntil2026Async([Required] Organizations organization)
        {
            var browse = await _generatingReports.AmountUntil2026Async(organization);
            _exportingReportsToExcel.Browse(browse);
            return NoContent();
        }

        /// <summary>Сколько осталось доплатить по счетам</summary>
        /// <response>Записывает информацию в HowMuchIsLeftToPayExtra.xlsx</response>
        [HttpGet("HowMuchIsLeftToPayExtra")]
        public async Task<IActionResult> HowMuchIsLeftToPayExtraAsync([Required] Organizations organization)
        {
            var howMuchIsLeftToPayExtra = await _generatingReports.HowMuchIsLeftToPayExtraAsync(organization);
            _exportingReportsToExcel.HowMuchIsLeftToPayExtra(howMuchIsLeftToPayExtra, organization.ToString());
            return NoContent();
        }

        /// <summary>Затраты по доходным договорам</summary>
        /// <response>Записывает информацию в ExpensesUnderIncomeContracts.xlsx</response>
        [HttpGet("ExpensesUnderIncomeContracts")]
        public async Task<IActionResult> ExpensesUnderIncomeContractsAsync([Required] Organizations organization, DateOnly startDate, DateOnly endDate)
        {
            startDate = startDate.Year == 1 ? new DateOnly(2026, 1, 1) : startDate;
            endDate = endDate.Year == 1 ? DateOnly.FromDateTime(DateTime.Now) : endDate;
            var expensesUnderIncomeContracts = await _generatingReports.ExpensesUnderIncomeContractsAsync(organization, startDate, endDate);
            _exportingReportsToExcel.ExpensesUnderIncomeContracts(expensesUnderIncomeContracts);
            return NoContent();
        }

        /// <summary>Расшифровка затрат по доходным договорам</summary>
        /// <response>Записывает информацию в .xlsx</response>
        [HttpGet("BreakdownOfExpensesUnderRevenueContracts")]
        public async Task<IActionResult> BreakdownOfExpensesUnderRevenueContractsAsync([Required] Organizations organization, [Required] string contractName)
        {
            var breakdownOfExpensesUnderRevenueContracts = await _generatingReports.BreakdownOfExpensesUnderRevenueContractsAsync(contractName, organization);
            _exportingReportsToExcel.Browse(breakdownOfExpensesUnderRevenueContracts);
            return NoContent();
        }
    }
}
