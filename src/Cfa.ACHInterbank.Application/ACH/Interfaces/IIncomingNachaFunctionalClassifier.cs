using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaFunctionalClassifier
{
    IncomingNachaClassificationResult Classify(EntryDetail entry, AddendaRecord? addenda);
}
