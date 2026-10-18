using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Deterministic physical blocking/padding primitives for NACHA files.
/// No chamber-specific business or regulatory decisions are owned here.
/// </summary>
internal static class NachaFileBlocking
{
    internal static NachaFileBlockLayout Calculate(int physicalRecordCountBeforePadding, int blockSize)
    {
        if (blockSize <= 0)
        {
            throw new NachaGenerationException("NACHA_BLOCK_SIZE_INVALID", $"El blockSize NACHA-M debe ser mayor a cero. Valor recibido: {blockSize}.");
        }

        var blockCount = (int)Math.Ceiling(physicalRecordCountBeforePadding / (decimal)blockSize);
        var physicalRecordCountAfterPadding = blockCount * blockSize;
        return new NachaFileBlockLayout(
            blockCount,
            physicalRecordCountAfterPadding - physicalRecordCountBeforePadding,
            physicalRecordCountAfterPadding);
    }

    internal static int ResolveConfiguredBlockSize(CfgLayoutVariant record1Layout)
    {
        var field = record1Layout.Fields.FirstOrDefault(x =>
            x.IsEnabled && string.Equals(x.FieldCode, "BLOCKINGFACTOR", StringComparison.OrdinalIgnoreCase));
        var configuredValue = field?.SourceDefinition?.ConstantValue;
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return 10;
        }

        if (!int.TryParse(configuredValue, NumberStyles.None, CultureInfo.InvariantCulture, out var blockSize) || blockSize <= 0)
        {
            throw new NachaGenerationException("NACHA_BLOCK_SIZE_INVALID", $"El blockSize configurado es inválido: {configuredValue}.");
        }

        return blockSize;
    }

    internal static string BuildPaddingRecord(int recordLength)
        => new string('9', recordLength);
}

internal readonly record struct NachaFileBlockLayout(
    int BlockCount,
    int PaddingRecordCount,
    int PhysicalRecordCountAfterPadding);
