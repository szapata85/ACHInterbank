using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaService
{
    Task SaveHeaderAsync(NachaHeader header);
}

