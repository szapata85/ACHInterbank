using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Services;

public sealed class ContractDrivenSoapBodyWriter : ISoapRequestBodyWriter
{
    private static readonly XNamespace Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    public string Write(ResolvedIntegrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateHeader(request);

        var contract = request.Contract;
        var contractById = contract.Parameters.ToDictionary(parameter => parameter.ParameterId);
        var resolvedById = request.Parameters.ToDictionary(parameter => parameter.ParameterId);

        foreach (var resolved in request.Parameters)
        {
            if (!contractById.TryGetValue(resolved.ParameterId, out var binding))
            {
                throw new InvalidOperationException(
                    $"RESOLVED_REQUEST_PARAMETER_NOT_IN_CONTRACT: {resolved.ParameterPath} no pertenece al contrato {contract.ContractVersionId}.");
            }

            ValidateResolvedMetadata(binding, resolved);
            if (binding.Direction != IntegrationParameterDirectionEnum.Input)
            {
                throw new InvalidOperationException(
                    $"RESOLVED_REQUEST_OUTPUT_PARAMETER: {binding.ParameterPath} no es un parámetro de entrada y no puede serializarse en el request SOAP.");
            }
        }

        XmlConvert.VerifyNCName(contract.OperationName);
        var targetNamespace = XNamespace.Get(contract.TargetNamespace);
        var operation = new XElement(targetNamespace + contract.OperationName);
        var requiresXsiNamespace = false;

        foreach (var binding in contract.Parameters
                     .OrderBy(parameter => parameter.SortOrder)
                     .ThenBy(parameter => parameter.ParameterPath, StringComparer.Ordinal))
        {
            if (binding.Direction != IntegrationParameterDirectionEnum.Input)
            {
                continue;
            }

            EnsureScalarCardinality(binding);

            if (!resolvedById.TryGetValue(binding.ParameterId, out var resolved))
            {
                if (binding.Required || binding.MinOccurs > 0)
                {
                    throw new InvalidOperationException(
                        $"RESOLVED_REQUEST_PARAMETER_MISSING: {binding.ParameterPath} es obligatorio en el contrato activo.");
                }

                continue;
            }

            if (!resolved.IsPresent)
            {
                if (binding.Required || binding.MinOccurs > 0)
                {
                    throw new InvalidOperationException(
                        $"RESOLVED_REQUEST_REQUIRED_OMITTED: {binding.ParameterPath} no puede omitirse según el contrato activo.");
                }

                continue;
            }

            XmlConvert.VerifyNCName(binding.XmlElementName);
            var element = new XElement(targetNamespace + binding.XmlElementName);

            if (resolved.Value is null)
            {
                if (binding.Nillable)
                {
                    element.Add(new XAttribute(XsiNamespace + "nil", XmlConvert.ToString(true)));
                    requiresXsiNamespace = true;
                }
                else if (binding.Required || binding.MinOccurs > 0 || !IsStringType(binding))
                {
                    throw new InvalidOperationException(
                        $"RESOLVED_REQUEST_NULL_NOT_ALLOWED: {binding.ParameterPath} no admite null según el contrato activo.");
                }
                // Optional + non-nillable string + explicit presence intentionally serializes as an empty element.
            }
            else
            {
                element.Value = FormatScalarValue(binding, resolved.Value);
            }

            operation.Add(element);
        }

        var envelope = new XElement(
            Soap11Namespace + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soapenv", Soap11Namespace.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "tem", targetNamespace.NamespaceName));

        if (requiresXsiNamespace)
        {
            envelope.Add(new XAttribute(XNamespace.Xmlns + "xsi", XsiNamespace.NamespaceName));
        }

        envelope.Add(new XElement(Soap11Namespace + "Body", operation));
        return new XDocument(envelope).ToString(SaveOptions.DisableFormatting);
    }

    private static void ValidateHeader(ResolvedIntegrationRequest request)
    {
        var contract = request.Contract;
        if (request.ContractVersionId != contract.ContractVersionId
            || request.ContractRevision != contract.ContractRevision
            || !string.Equals(request.MethodCode, contract.MethodCode, StringComparison.Ordinal)
            || !string.Equals(request.OperationName, contract.OperationName, StringComparison.Ordinal)
            || !string.Equals(request.TargetNamespace, contract.TargetNamespace, StringComparison.Ordinal)
            || !string.Equals(request.SoapAction, contract.SoapAction, StringComparison.Ordinal)
            || request.MappingSetId != contract.MappingSetId
            || request.MappingVersion != contract.MappingVersion
            || request.MappingRevision != contract.MappingRevision)
        {
            throw new InvalidOperationException(
                "RESOLVED_REQUEST_CONTRACT_HEADER_MISMATCH: la solicitud resuelta no corresponde al snapshot contractual asociado.");
        }
    }

    private static void ValidateResolvedMetadata(
        IntegrationExecutionParameterBinding binding,
        ResolvedIntegrationParameter resolved)
    {
        if (!string.Equals(binding.ParameterPath, resolved.ParameterPath, StringComparison.Ordinal)
            || !string.Equals(binding.XmlElementName, resolved.XmlElementName, StringComparison.Ordinal)
            || !string.Equals(binding.DataType, resolved.DataType, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(binding.XsdType, resolved.XsdType, StringComparison.OrdinalIgnoreCase)
            || binding.Nillable != resolved.Nillable
            || binding.MinOccurs != resolved.MinOccurs
            || binding.MaxOccurs != resolved.MaxOccurs
            || binding.Required != resolved.Required
            || binding.SortOrder != resolved.SortOrder
            || binding.Direction != resolved.Direction)
        {
            throw new InvalidOperationException(
                $"RESOLVED_REQUEST_CONTRACT_MISMATCH: {resolved.ParameterPath} no coincide con la metadata del contrato activo.");
        }
    }

    private static void EnsureScalarCardinality(IntegrationExecutionParameterBinding binding)
    {
        if (binding.MaxOccurs != 1)
        {
            throw new NotSupportedException(
                $"SOAP_SCALAR_CARDINALITY_NOT_SUPPORTED: {binding.ParameterPath} declara MaxOccurs={binding.MaxOccurs?.ToString(CultureInfo.InvariantCulture) ?? "unbounded"}.");
        }
    }

    private static string FormatScalarValue(IntegrationExecutionParameterBinding binding, object value)
    {
        var type = NormalizeDataType(binding.XsdType, binding.DataType);
        EnsureSupportedDataType(binding, type);

        if (value is string text)
        {
            return text;
        }

        try
        {
            return type switch
            {
                "string" => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
                "int" or "integer" or "int32" => XmlConvert.ToString(Convert.ToInt32(value, CultureInfo.InvariantCulture)),
                "long" or "int64" => XmlConvert.ToString(Convert.ToInt64(value, CultureInfo.InvariantCulture)),
                "short" or "int16" => XmlConvert.ToString(Convert.ToInt16(value, CultureInfo.InvariantCulture)),
                "uint" or "uint32" => XmlConvert.ToString(Convert.ToUInt32(value, CultureInfo.InvariantCulture)),
                "ulong" or "uint64" => XmlConvert.ToString(Convert.ToUInt64(value, CultureInfo.InvariantCulture)),
                "ushort" or "uint16" => XmlConvert.ToString(Convert.ToUInt16(value, CultureInfo.InvariantCulture)),
                "decimal" => XmlConvert.ToString(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
                "double" => XmlConvert.ToString(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
                "float" or "single" => XmlConvert.ToString(Convert.ToSingle(value, CultureInfo.InvariantCulture)),
                "bool" or "boolean" => XmlConvert.ToString(Convert.ToBoolean(value, CultureInfo.InvariantCulture)),
                "datetime" => FormatDateTime(value),
                "date" => FormatDate(value),
                "time" => FormatTime(value),
                "guid" or "uuid" => FormatGuid(value),
                _ => throw new InvalidOperationException("Tipo SOAP soportado sin formatter registrado.")
            };
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new InvalidOperationException(
                $"SOAP_VALUE_FORMAT_INVALID: {binding.ParameterPath} no puede representarse como {binding.XsdType}.",
                exception);
        }
    }


    private static bool IsStringType(IntegrationExecutionParameterBinding binding)
        => NormalizeDataType(binding.XsdType, binding.DataType) == "string";

    private static void EnsureSupportedDataType(IntegrationExecutionParameterBinding binding, string type)
    {
        if (type is not (
            "string" or
            "int" or "integer" or "int32" or
            "long" or "int64" or
            "short" or "int16" or
            "uint" or "uint32" or
            "ulong" or "uint64" or
            "ushort" or "uint16" or
            "decimal" or "double" or "float" or "single" or
            "bool" or "boolean" or
            "datetime" or "date" or "time" or
            "guid" or "uuid"))
        {
            throw new NotSupportedException(
                $"SOAP_DATA_TYPE_NOT_SUPPORTED: {binding.ParameterPath} declara el tipo {binding.XsdType}/{binding.DataType} no soportado por el writer contract-driven.");
        }
    }

    private static string NormalizeDataType(string xsdType, string dataType)
    {
        var candidate = string.IsNullOrWhiteSpace(xsdType) ? dataType : xsdType;
        var normalized = candidate.Trim();
        var prefixSeparator = normalized.IndexOf(':');
        if (prefixSeparator >= 0 && prefixSeparator < normalized.Length - 1 && !normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized[(prefixSeparator + 1)..];
        }

        return normalized.ToLowerInvariant();
    }

    private static string FormatDateTime(object value)
        => value switch
        {
            DateTime dateTime => XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.RoundtripKind),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
            _ => XmlConvert.ToString(
                Convert.ToDateTime(value, CultureInfo.InvariantCulture),
                XmlDateTimeSerializationMode.RoundtripKind)
        };

    private static string FormatDate(object value)
        => value switch
        {
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };

    private static string FormatTime(object value)
        => value switch
        {
            TimeSpan timeSpan => XmlConvert.ToString(timeSpan),
            DateTime dateTime => dateTime.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture),
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture)
        };

    private static string FormatGuid(object value)
        => value switch
        {
            Guid guid => guid.ToString("D"),
            _ => Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
                .ToString("D")
        };
}
