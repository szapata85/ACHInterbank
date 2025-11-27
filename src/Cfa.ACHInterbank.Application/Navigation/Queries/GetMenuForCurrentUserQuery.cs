using MediatR;

namespace Cfa.ACHInterbank.Application.Navigation.Queries;

public record GetMenuForCurrentUserQuery() : IRequest<IList<MenuItemDto>>;
