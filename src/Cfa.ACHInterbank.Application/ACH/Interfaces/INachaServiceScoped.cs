using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaServiceScoped
{
    Task SaveHeaderAsync(NachaHeader header);
}

