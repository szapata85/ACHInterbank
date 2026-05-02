namespace Cfa.ACHInterbank.Application.Security;

public static class FineGrainedPermissions
{
    public const string CanGenerateNacha = "CanGenerateNacha";
    public const string CanGenerateEncryptedNacha = "CanGenerateEncryptedNacha";
    public const string CanManualEncryptEnvelope = "CanManualEncryptEnvelope";
    public const string CanManualDecryptEnvelope = "CanManualDecryptEnvelope";
    public const string CanDownloadPlainNacha = "CanDownloadPlainNacha";
    public const string CanDownloadEnvelope = "CanDownloadEnvelope";
    public const string CanViewNachaSecurityAudit = "CanViewNachaSecurityAudit";
    public const string CanManageCertificates = "CanManageCertificates";
    public const string CanRunInteroperabilityHarness = "CanRunInteroperabilityHarness";
    public const string CanViewPaymentRailCapabilityRegistry = "CanViewPaymentRailCapabilityRegistry";

    public static class Transactions
    {
        public const string Read = "Transactions.Read";
        public const string Create = "Transactions.Create";
        public const string BulkSubmit = "Transactions.BulkSubmit";
        public const string PolicyPreview = "Transactions.PolicyPreview";
    }

    public static class Nacha
    {
        public const string Read = "Nacha.Read";
        public const string Upload = "Nacha.Upload";
        public const string Export = "Nacha.Export";
        public const string Generate = "Nacha.Generate";
        public const string Configure = "Nacha.Configure";
        public const string PublishConfig = "Nacha.PublishConfig";
        public const string ArchiveConfig = "Nacha.ArchiveConfig";
    }

    public static class NachaSecurity
    {
        public const string Read = "NachaSecurity.Read";
        public const string GenerateEncrypted = "NachaSecurity.GenerateEncrypted";
        public const string ManualEncrypt = "NachaSecurity.ManualEncrypt";
        public const string ManualDecrypt = "NachaSecurity.ManualDecrypt";
        public const string AuthorizeDownload = "NachaSecurity.AuthorizeDownload";
    }

    public static class DigitalEnvelope
    {
        public const string Encrypt = "DigitalEnvelope.Encrypt";
        public const string Decrypt = "DigitalEnvelope.Decrypt";
        public const string Test = "DigitalEnvelope.Test";
    }

    public static class Certificates
    {
        public const string Read = "Certificates.Read";
        public const string UploadPublic = "Certificates.UploadPublic";
        public const string RegisterPrivate = "Certificates.RegisterPrivate";
        public const string Activate = "Certificates.Activate";
        public const string Revoke = "Certificates.Revoke";
        public const string Validate = "Certificates.Validate";
        public const string Audit = "Certificates.Audit";
    }

    public static class Reports
    {
        public const string Read = "Reports.Read";
        public const string Export = "Reports.Export";
    }

    public static class Cenit
    {
        public const string ReadQueues = "Cenit.ReadQueues";
        public const string ReadNetPositions = "Cenit.ReadNetPositions";
        public const string ReadOptimization = "Cenit.ReadOptimization";
        public const string ReadTraceability = "Cenit.ReadTraceability";
    }

    public static class CommandCenter
    {
        public const string Read = "CommandCenter.Read";
        public const string Retry = "CommandCenter.Retry";
        public const string Unblock = "CommandCenter.Unblock";
        public const string Requeue = "CommandCenter.Requeue";
        public const string MarkFailedFinal = "CommandCenter.MarkFailedFinal";
    }

    public static class BulkIngestion
    {
        public const string Upload = "BulkIngestion.Upload";
        public const string Read = "BulkIngestion.Read";
        public const string Retry = "BulkIngestion.Retry";
        public const string Cancel = "BulkIngestion.Cancel";
    }

    public static class Traceability
    {
        public const string Read = "Traceability.Read";
        public const string CertifySol02 = "Traceability.CertifySol02";
    }

    public static class Returns
    {
        public const string Read = "Returns.Read";
        public const string GenerateFile = "Returns.GenerateFile";
    }

    public static class Users
    {
        public const string Read = "Users.Read";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Deactivate = "Users.Deactivate";
        public const string AssignRoles = "Users.AssignRoles";
        public const string ManageBranding = "Users.ManageBranding";
        public const string ManagePasswordRules = "Users.ManagePasswordRules";
        public const string ManageLockout = "Users.ManageLockout";
    }

    public static class Roles
    {
        public const string Read = "Roles.Read";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
    }

    public static class Permissions
    {
        public const string Read = "Permissions.Read";
        public const string Assign = "Permissions.Assign";
    }

    public static class Config
    {
        public const string Read = "Config.Read";
        public const string Manage = "Config.Manage";
    }

    public static class Integrations
    {
        public const string Read = "Integrations.Read";
        public const string ManageMappings = "Integrations.ManageMappings";
        public const string Validate = "Integrations.Validate";
        public const string Publish = "Integrations.Publish";
        public const string Compare = "Integrations.Compare";
    }

    public static class RegulatoryCatalogs
    {
        public const string Read = "RegulatoryCatalogs.Read";
    }

    public static class Maintenance
    {
        public const string Seed = "Maintenance.Seed";
        public const string RunAdminTask = "Maintenance.RunAdminTask";
    }

    public static IReadOnlyList<string> AllPermissions { get; } =
    [
        Transactions.Read, Transactions.Create, Transactions.BulkSubmit, Transactions.PolicyPreview,
        Nacha.Read, Nacha.Upload, Nacha.Export, Nacha.Generate, Nacha.Configure, Nacha.PublishConfig, Nacha.ArchiveConfig,
        NachaSecurity.Read, NachaSecurity.GenerateEncrypted, NachaSecurity.ManualEncrypt, NachaSecurity.ManualDecrypt, NachaSecurity.AuthorizeDownload,
        DigitalEnvelope.Encrypt, DigitalEnvelope.Decrypt, DigitalEnvelope.Test,
        Certificates.Read, Certificates.UploadPublic, Certificates.RegisterPrivate, Certificates.Activate, Certificates.Revoke, Certificates.Validate, Certificates.Audit,
        Reports.Read, Reports.Export,
        Cenit.ReadQueues, Cenit.ReadNetPositions, Cenit.ReadOptimization, Cenit.ReadTraceability,
        CommandCenter.Read, CommandCenter.Retry, CommandCenter.Unblock, CommandCenter.Requeue, CommandCenter.MarkFailedFinal,
        BulkIngestion.Upload, BulkIngestion.Read, BulkIngestion.Retry, BulkIngestion.Cancel,
        Traceability.Read, Traceability.CertifySol02,
        Returns.Read, Returns.GenerateFile,
        Users.Read, Users.Create, Users.Update, Users.Deactivate, Users.AssignRoles, Users.ManageBranding, Users.ManagePasswordRules, Users.ManageLockout,
        Roles.Read, Roles.Create, Roles.Update, Roles.Delete,
        Permissions.Read, Permissions.Assign,
        Config.Read, Config.Manage,
        Integrations.Read, Integrations.ManageMappings, Integrations.Validate, Integrations.Publish, Integrations.Compare,
        RegulatoryCatalogs.Read,
        Maintenance.Seed, Maintenance.RunAdminTask
    ];
}
