namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Contrato interno tipado para Proc_Contrapartidas.
/// Cada propiedad representa un campo funcional de negocio enviado al legado SOAP.
/// </summary>
public sealed record ProcContrapartidasRequestContract
{
    /// <summary>Identificador de cámara compensadora (ClearingHouse.Id).</summary>
    public required int ClearingHouseId { get; init; }

    /// <summary>Código de cámara (ClearingHouse.Code).</summary>
    public required string ClearingHouseCode { get; init; }

    /// <summary>Identificador del ciclo ACH operativo.</summary>
    public required string CycleId { get; init; }

    /// <summary>Nombre funcional del ciclo ACH.</summary>
    public required string CycleName { get; init; }

    /// <summary>Fecha de proceso del ciclo.</summary>
    public required DateTime ProcessingDate { get; init; }

    /// <summary>Hora de inicio del ciclo.</summary>
    public required TimeSpan StartTime { get; init; }

    /// <summary>Hora de cierre del ciclo.</summary>
    public required TimeSpan EndTime { get; init; }

    /// <summary>Hora de corte del ciclo.</summary>
    public required TimeSpan CutoffTime { get; init; }

    /// <summary>Fecha/hora de ejecución del despacho.</summary>
    public required DateTime ExecutionDateTime { get; init; }

    /// <summary>Transacciones incluidas en el envío.</summary>
    public required IReadOnlyCollection<ProcContrapartidasTransactionContract> Transactions { get; init; }
}

public sealed record ProcContrapartidasTransactionContract
{
    public required int TransactionId { get; init; }
    public required int AchBatchId { get; init; }
    public required string AchCycleId { get; init; }
    public required decimal Amount { get; init; }
    public required string Type { get; init; }
    public required string TransactionCode { get; init; }
    public required string TraceNumber { get; init; }
    public required string Reference { get; init; }
    public required string OriginatingDfi { get; init; }
    public required string ReceivingDfi { get; init; }
    public required string CompanyIdentification { get; init; }
    public required DateTime EffectiveEntryDate { get; init; }
    public required int SourceInstitutionId { get; init; }
    public required int DestinationInstitutionId { get; init; }
    public required IReadOnlyCollection<ProcContrapartidasAddendaContract> Addendas { get; init; }
}

public sealed record ProcContrapartidasAddendaContract
{
    public required int SequenceNumber { get; init; }
    public required string AddendaType { get; init; }
    public required string BusinessType { get; init; }
    public string Information { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string CollectorId { get; init; } = string.Empty;
    public string ReceiverCustomerCode { get; init; } = string.Empty;
    public string ServiceDescription { get; init; } = string.Empty;
    public string ReturnReasonCode { get; init; } = string.Empty;
    public string OriginalTraceNumber { get; init; } = string.Empty;
    public string NewTraceNumber { get; init; } = string.Empty;
}
