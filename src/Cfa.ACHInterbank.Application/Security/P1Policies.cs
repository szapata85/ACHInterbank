namespace Cfa.ACHInterbank.Application.Security;

public static class P1Policies
{
    public const string BulkIngestionRead = "P1.BulkIngestionRead";
    public const string BulkIngestionUpload = "P1.BulkIngestionUpload";
    public const string BulkIngestionRetry = "P1.BulkIngestionRetry";
    public const string BulkIngestionCancel = "P1.BulkIngestionCancel";
    public const string CommandCenterRead = "P1.CommandCenterRead";
    public const string CommandCenterRetry = "P1.CommandCenterRetry";
    public const string CommandCenterUnblock = "P1.CommandCenterUnblock";
    public const string CommandCenterRequeue = "P1.CommandCenterRequeue";
    public const string CommandCenterMarkFailedFinal = "P1.CommandCenterMarkFailedFinal";
    public const string NachaRead = "P1.NachaRead";
    public const string NachaUpload = "P1.NachaUpload";
    public const string NachaGenerate = "P1.NachaGenerate";
    public const string NachaExport = "P1.NachaExport";
}
