using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CenitNetPosition : AuditableEntity
{
    public long Id { get; set; }
    public long CenitNettingExecutionId { get; set; }
    public CenitNettingExecution CenitNettingExecution { get; set; } = null!;

    public int FinancialInstitutionId { get; set; }
    public FinancialInstitution FinancialInstitution { get; set; } = null!;

    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Saldo de liquidez provisto por una fuente externa (cuenta de liquidez real), si existe.
    /// </summary>
    public decimal? ExternalLiquidity { get; set; }
    /// <summary>
    /// Saldo de liquidez calculado por el motor interno para simulación.
    /// </summary>
    public decimal SimulatedLiquidity { get; set; }
    /// <summary>
    /// "External" cuando hay integración real, "Simulated" para motor interno.
    /// </summary>
    public string LiquiditySourceType { get; set; } = "Simulated";
    public decimal AvailableLiquidity { get; set; }
    public bool HasInsufficientFunds { get; set; }
}
