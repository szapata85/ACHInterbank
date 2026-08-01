namespace Cfa.ACHInterbank.Domain.Models.ACH;

public static class IncomingNachaStageTransitions
{
    private static readonly IReadOnlyDictionary<IncomingNachaIngestionStage, IReadOnlySet<IncomingNachaIngestionStage>> Allowed
        = new Dictionary<IncomingNachaIngestionStage, IReadOnlySet<IncomingNachaIngestionStage>>
        {
            [IncomingNachaIngestionStage.Received] = Set(IncomingNachaIngestionStage.PreValidating, IncomingNachaIngestionStage.Decrypting, IncomingNachaIngestionStage.HeaderParsing, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.PreValidating] = Set(IncomingNachaIngestionStage.Decrypting, IncomingNachaIngestionStage.HeaderParsing, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.Decrypting] = Set(IncomingNachaIngestionStage.HeaderParsing, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.HeaderParsing] = Set(IncomingNachaIngestionStage.ValidatingHeader, IncomingNachaIngestionStage.ValidatingCycle, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.ValidatingHeader] = Set(IncomingNachaIngestionStage.ValidatingCycle, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.ValidatingCycle] = Set(IncomingNachaIngestionStage.Parsing, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.Parsing] = Set(IncomingNachaIngestionStage.ValidatingContent, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.ValidatingContent] = Set(IncomingNachaIngestionStage.Persisting, IncomingNachaIngestionStage.Rejected, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.Persisting] = Set(IncomingNachaIngestionStage.Persisted, IncomingNachaIngestionStage.Failed),
            [IncomingNachaIngestionStage.Persisted] = Set(),
            [IncomingNachaIngestionStage.Rejected] = Set(),
            [IncomingNachaIngestionStage.Failed] = Set()
        };

    public static void MoveTo(IncomingNachaFileIngestion ingestion, IncomingNachaIngestionStage target)
    {
        ArgumentNullException.ThrowIfNull(ingestion);
        if (ingestion.Stage == target) return;
        if (!Allowed.TryGetValue(ingestion.Stage, out var targets) || !targets.Contains(target))
            throw new InvalidOperationException($"INGESTION_STAGE_TRANSITION_INVALID:{ingestion.Stage}->{target}");
        ingestion.Stage = target;
    }

    public static bool CanMove(IncomingNachaIngestionStage source, IncomingNachaIngestionStage target)
        => source == target || (Allowed.TryGetValue(source, out var targets) && targets.Contains(target));

    private static IReadOnlySet<IncomingNachaIngestionStage> Set(params IncomingNachaIngestionStage[] values)
        => new HashSet<IncomingNachaIngestionStage>(values);
}
