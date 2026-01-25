using Cfa.ACHInterbank.Application.Navigation.Dtos;

namespace Cfa.ACHInterbank.Application.Navigation.Interfaces;

public interface IMenuItemsService
{
    Task<IEnumerable<MenuItemAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<MenuItemAdminDto> CreateAsync(SaveMenuItemRequest request, CancellationToken ct = default);
    Task<MenuItemAdminDto?> UpdateAsync(int id, SaveMenuItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
