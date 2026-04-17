using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IProcTransaccionesResponseParser
{
    ProcTransaccionesParsedResponse Parse(string responseXml);
}
