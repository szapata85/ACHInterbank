using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ClearingHouseService : IClearingHouseService
{
    private const string PublishedStatus = "PUBLICADO";
    private const string DefaultTimeZone = "America/Bogota";
    private const string DefaultHolidayStrategy = "Colombian";
    private static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9_-]{1,19}$", RegexOptions.CultureInvariant);
    private readonly AchDbContext _context;
    private readonly IReadOnlyList<ClearingHousePaymentRailOptionDto> _paymentRailOptions;
    private readonly HashSet<string> _selectablePaymentRailCodes;

    public ClearingHouseService(AchDbContext context, IEnumerable<IPaymentRailOperationalStrategy> strategies)
    {
        _context = context;
        _paymentRailOptions = strategies
            .Where(x => x.IsAdministrativelySelectable
                        && !string.Equals(x.RailCode, PaymentRailCodes.Unknown, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.RailCode, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.DisplayName)
            .Select(x => new ClearingHousePaymentRailOptionDto
            {
                Code = x.RailCode.Trim().ToUpperInvariant(),
                Name = x.DisplayName
            })
            .ToArray();
        _selectablePaymentRailCodes = _paymentRailOptions
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<ClearingHouseDto>> GetAllAsync(CancellationToken ct = default)
        => await LoadDtosAsync(_context.ClearingHouses.AsNoTracking().OrderBy(x => x.Code).ThenBy(x => x.Id), ct);

    public async Task<IReadOnlyList<ClearingHouseDto>> GetOperationalAsync(CancellationToken ct = default)
    {
        var items = await LoadDtosAsync(
            _context.ClearingHouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ThenBy(x => x.Id),
            ct);
        return items.Where(x => x.IsReady).ToArray();
    }

    public async Task<ClearingHouseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var items = await LoadDtosAsync(_context.ClearingHouses.AsNoTracking().Where(x => x.Id == id), ct);
        return items.SingleOrDefault();
    }

    public async Task<PaginatedResult<ClearingHouseDto>> GetAsync(ClearingHouseAdminQuery request, CancellationToken ct = default)
    {
        var query = _context.ClearingHouses.AsNoTracking().AsQueryable();
        var search = request.Search?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Code.Contains(search) || x.Name.ToUpper().Contains(search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var total = await query.CountAsync(ct);
        query = query.OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);

        return new PaginatedResult<ClearingHouseDto>
        {
            Items = await LoadDtosAsync(query, ct),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<ClearingHouseDto> CreateAsync(CreateClearingHouseRequest request, CancellationToken ct = default)
    {
        var normalized = await ValidateAndNormalizeAsync(request, null, ct);
        if (await _context.ClearingHouses.AnyAsync(x => x.Code == normalized.Code, ct))
        {
            throw new ClearingHouseConflictException($"Ya existe una cámara compensadora con el código {normalized.Code}.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var fallbackConfig = await _context.ClearingHouseConfigs.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
            var ownsFallback = fallbackConfig is null;
            if (fallbackConfig is null)
            {
                fallbackConfig = new ClearingHouseConfig
                {
                    ClearingHouseId = 0,
                    HolidayStrategy = normalized.HolidayStrategy,
                    TimeZoneId = normalized.TimeZoneId,
                    PaymentRailCode = normalized.PaymentRailCode,
                    RequiresNachaProfile = normalized.RequiresNachaProfile,
                    NachaProfileId = normalized.NachaProfileId
                };
                _context.ClearingHouseConfigs.Add(fallbackConfig);
                await _context.SaveChangesAsync(ct);
            }

            var entity = new ClearingHouse
            {
                Code = normalized.Code,
                Name = normalized.Name,
                OriginCode = normalized.OriginCode,
                IsActive = false,
                ClearingHouseId = fallbackConfig.Id
            };
            _context.ClearingHouses.Add(entity);
            await _context.SaveChangesAsync(ct);

            ClearingHouseConfig ownConfig;
            if (ownsFallback)
            {
                ownConfig = fallbackConfig;
                ownConfig.ClearingHouseId = entity.Id;
            }
            else
            {
                ownConfig = new ClearingHouseConfig
                {
                    ClearingHouseId = entity.Id,
                    HolidayStrategy = normalized.HolidayStrategy,
                    TimeZoneId = normalized.TimeZoneId,
                    PaymentRailCode = normalized.PaymentRailCode,
                    RequiresNachaProfile = normalized.RequiresNachaProfile,
                    NachaProfileId = normalized.NachaProfileId
                };
                _context.ClearingHouseConfigs.Add(ownConfig);
                await _context.SaveChangesAsync(ct);
                entity.ClearingHouseId = ownConfig.Id;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return (await GetByIdAsync(entity.Id, ct))!;
            });
        }
        catch (DbUpdateException)
        {
            throw new ClearingHouseConflictException($"Ya existe una cámara compensadora con el código {normalized.Code}.");
        }
    }

    public async Task<ClearingHouseDto> UpdateAsync(int id, UpdateClearingHouseRequest request, CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouses
            .Include(x => x.ClearingHouseConfig)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ClearingHouseNotFoundException();

        if (request.ExpectedUpdatedAt.HasValue && entity.UpdatedAt != request.ExpectedUpdatedAt.Value)
        {
            throw new ClearingHouseConflictException("La cámara fue modificada por otro usuario. Recargue la información.");
        }

        var normalized = await ValidateAndNormalizeAsync(request, id, ct);
        if (!string.Equals(entity.Code, normalized.Code, StringComparison.Ordinal))
        {
            if (await HasOperationalRelationsAsync(id, ct))
            {
                throw new ClearingHouseConflictException("El código no puede cambiarse porque la cámara ya tiene relaciones operativas.");
            }

            entity.Code = normalized.Code;
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var config = await EnsureDedicatedConfigAsync(entity, ct);
            entity.Name = normalized.Name;
            entity.OriginCode = normalized.OriginCode;
            config.TimeZoneId = normalized.TimeZoneId;
            config.HolidayStrategy = normalized.HolidayStrategy;
            config.PaymentRailCode = normalized.PaymentRailCode;
            config.RequiresNachaProfile = normalized.RequiresNachaProfile;
            config.NachaProfileId = normalized.NachaProfileId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return (await GetByIdAsync(entity.Id, ct))!;
            });
        }
        catch (DbUpdateException)
        {
            throw new ClearingHouseConflictException($"Ya existe una cámara compensadora con el código {normalized.Code}.");
        }
    }

    public async Task<ClearingHouseDto> ChangeStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var entity = await _context.ClearingHouses.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ClearingHouseNotFoundException();

        if (entity.IsActive == isActive)
        {
            return (await GetByIdAsync(id, ct))!;
        }

        if (isActive)
        {
            var readiness = await GetReadinessAsync(id, ct);
            if (!readiness.IsReady)
            {
                throw new ClearingHouseValidationException(
                    "La cámara no puede activarse porque su configuración está incompleta.",
                    readiness.MissingRequirements);
            }
        }

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(ct);
        return (await GetByIdAsync(id, ct))!;
    }

    public async Task<ClearingHouseReadinessDto> GetReadinessAsync(int id, CancellationToken ct = default)
    {
        var dto = await GetByIdAsync(id, ct) ?? throw new ClearingHouseNotFoundException();
        return new ClearingHouseReadinessDto
        {
            IsReady = dto.IsReady,
            MissingRequirements = dto.MissingRequirements
        };
    }

    public IReadOnlyList<ClearingHousePaymentRailOptionDto> GetPaymentRailOptions()
        => _paymentRailOptions;

    public async Task<IReadOnlyList<ClearingHouseNachaProfileOptionDto>> GetNachaProfilesAsync(
        string? clearingHouseCode,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var normalizedCode = NormalizeCode(clearingHouseCode);
        var clearingHouseName = string.IsNullOrWhiteSpace(normalizedCode)
            ? null
            : await _context.ClearingHouses.AsNoTracking()
                .Where(x => x.Code == normalizedCode)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct);

        var profiles = await _context.CfgProfiles.AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.Status)
            .Where(x => x.Status.Code == PublishedStatus)
            .OrderBy(x => x.ProfileCode)
            .ToListAsync(ct);

        return profiles
            .Where(x => string.IsNullOrWhiteSpace(normalizedCode)
                || CodesCorrespond(normalizedCode, clearingHouseName, x.ClearingHouse.Code, x.ClearingHouse.Name))
            .Select(x => new ClearingHouseNachaProfileOptionDto
            {
                Id = x.Id,
                Code = x.ProfileCode,
                Name = x.NameEs,
                ClearingHouseCode = x.ClearingHouse.Code,
                IsPublished = true,
                IsCurrent = x.EffectiveFrom <= now && (!x.EffectiveTo.HasValue || x.EffectiveTo >= now)
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<ClearingHouseDto>> LoadDtosAsync(IQueryable<ClearingHouse> query, CancellationToken ct)
    {
        var houses = await query
            .Include(x => x.ClearingHouseConfig)
                .ThenInclude(x => x.NachaProfile)
                    .ThenInclude(x => x!.Status)
            .Include(x => x.ClearingHouseConfig)
                .ThenInclude(x => x.NachaProfile)
                    .ThenInclude(x => x!.ClearingHouse)
            .ToListAsync(ct);

        var ids = houses.Select(x => x.Id).ToArray();
        var today = DateTime.UtcNow.Date;
        var activeCycles = await _context.ClearingHouseCycleConfigs.AsNoTracking()
            .Where(x => ids.Contains(x.ClearingHouseId)
                        && x.IsActive
                        && x.EffectiveFrom.Date <= today
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= today))
            .GroupBy(x => x.ClearingHouseId)
            .Select(x => new { Id = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        return houses.Select(x => Map(x, activeCycles.GetValueOrDefault(x.Id), today)).ToArray();
    }

    private ClearingHouseDto Map(ClearingHouse entity, int activeCycleCount, DateTime today)
    {
        var missing = EvaluateReadiness(entity, activeCycleCount, today);
        return new ClearingHouseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            OriginCode = entity.OriginCode,
            IsActive = entity.IsActive,
            TimeZoneId = entity.ClearingHouseConfig?.TimeZoneId ?? string.Empty,
            HolidayStrategy = entity.ClearingHouseConfig?.HolidayStrategy,
            PaymentRailCode = entity.ClearingHouseConfig?.PaymentRailCode,
            RequiresNachaProfile = entity.ClearingHouseConfig?.RequiresNachaProfile ?? false,
            NachaProfileId = entity.ClearingHouseConfig?.NachaProfileId,
            NachaProfileCode = entity.ClearingHouseConfig?.NachaProfile?.ProfileCode,
            NachaProfileName = entity.ClearingHouseConfig?.NachaProfile?.NameEs,
            ActiveCycleCount = activeCycleCount,
            IsReady = missing.Count == 0,
            MissingRequirements = missing,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private List<string> EvaluateReadiness(ClearingHouse entity, int activeCycleCount, DateTime today)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(entity.Code)) missing.Add("Código funcional");
        if (string.IsNullOrWhiteSpace(entity.Name)) missing.Add("Nombre");
        if (string.IsNullOrWhiteSpace(entity.OriginCode)) missing.Add("Código de origen");
        if (entity.ClearingHouseConfig is null) missing.Add("Configuración principal");
        if (string.IsNullOrWhiteSpace(entity.ClearingHouseConfig?.TimeZoneId)) missing.Add("Zona horaria");
        if (string.IsNullOrWhiteSpace(entity.ClearingHouseConfig?.HolidayStrategy)) missing.Add("Estrategia de calendario");
        if (string.IsNullOrWhiteSpace(entity.ClearingHouseConfig?.PaymentRailCode)
            || !_selectablePaymentRailCodes.Contains(entity.ClearingHouseConfig.PaymentRailCode))
            missing.Add("Estrategia operativa registrada");
        if (activeCycleCount == 0) missing.Add("Al menos un ciclo activo y vigente");

        if (entity.ClearingHouseConfig?.RequiresNachaProfile == true)
        {
            var profile = entity.ClearingHouseConfig.NachaProfile;
            if (profile is null)
            {
                missing.Add("Perfil NACHA-M publicado");
            }
            else
            {
                if (!string.Equals(profile.Status.Code, PublishedStatus, StringComparison.OrdinalIgnoreCase)
                    || profile.EffectiveFrom.Date > today
                    || (profile.EffectiveTo.HasValue && profile.EffectiveTo.Value.Date < today))
                {
                    missing.Add("Perfil NACHA-M publicado y vigente");
                }

                if (!CodesCorrespond(entity.Code, entity.Name, profile.ClearingHouse.Code, profile.ClearingHouse.Name))
                {
                    missing.Add("Perfil NACHA-M correspondiente a la cámara");
                }
            }
        }

        return missing;
    }

    private async Task<NormalizedRequest> ValidateAndNormalizeAsync(
        CreateClearingHouseRequest request,
        int? currentId,
        CancellationToken ct)
    {
        var code = NormalizeCode(request.Code);
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(code)) errors.Add("El código funcional es obligatorio.");
        else if (!CodePattern.IsMatch(code)) errors.Add("El código debe tener entre 2 y 20 caracteres y usar solo letras, números, guion o guion bajo.");

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) errors.Add("El nombre es obligatorio.");
        else if (name.Length > 200) errors.Add("El nombre no puede superar 200 caracteres.");

        var originCode = request.OriginCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(originCode)) errors.Add("El código de origen es obligatorio.");
        else if (originCode.Length > 50) errors.Add("El código de origen no puede superar 50 caracteres.");

        var timeZoneId = request.TimeZoneId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(timeZoneId)) errors.Add("La zona horaria es obligatoria.");
        else
        {
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch (TimeZoneNotFoundException) { errors.Add("La zona horaria no es válida."); }
            catch (InvalidTimeZoneException) { errors.Add("La zona horaria no es válida."); }
        }

        var holidayStrategy = request.HolidayStrategy?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(holidayStrategy)) errors.Add("La estrategia de calendario es obligatoria.");

        var paymentRailCode = string.IsNullOrWhiteSpace(request.PaymentRailCode)
            ? null
            : request.PaymentRailCode.Trim().ToUpperInvariant();
        if (paymentRailCode is not null && !_selectablePaymentRailCodes.Contains(paymentRailCode))
            errors.Add("La estrategia operativa seleccionada no está registrada o no está permitida.");

        if (request.RequiresNachaProfile && !request.NachaProfileId.HasValue)
            errors.Add("Debe seleccionar un perfil NACHA-M cuando la cámara requiere procesamiento NACHA-M.");
        if (!request.RequiresNachaProfile && request.NachaProfileId.HasValue)
            errors.Add("No seleccione un perfil NACHA-M si el procesamiento NACHA-M no está habilitado.");

        if (request.NachaProfileId.HasValue)
        {
            var profile = await _context.CfgProfiles.AsNoTracking()
                .Include(x => x.ClearingHouse)
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Id == request.NachaProfileId.Value, ct);
            if (profile is null)
            {
                errors.Add("El perfil NACHA-M seleccionado no existe.");
            }
            else if (!string.Equals(profile.Status.Code, PublishedStatus, StringComparison.OrdinalIgnoreCase)
                     || !CodesCorrespond(code, name, profile.ClearingHouse.Code, profile.ClearingHouse.Name))
            {
                errors.Add("El perfil NACHA-M debe estar publicado y corresponder a la cámara.");
            }
        }

        if (!string.IsNullOrWhiteSpace(code)
            && await _context.ClearingHouses.AnyAsync(x => x.Code == code && x.Id != currentId, ct))
        {
            throw new ClearingHouseConflictException($"Ya existe una cámara compensadora con el código {code}.");
        }

        if (errors.Count > 0) throw new ClearingHouseValidationException("Revise los datos de la cámara compensadora.", errors);
        return new NormalizedRequest(code, name, originCode, timeZoneId, holidayStrategy, paymentRailCode, request.RequiresNachaProfile, request.NachaProfileId);
    }

    private async Task<ClearingHouseConfig> EnsureDedicatedConfigAsync(ClearingHouse entity, CancellationToken ct)
    {
        if (entity.ClearingHouseConfig.ClearingHouseId == entity.Id)
            return entity.ClearingHouseConfig;

        var existing = await _context.ClearingHouseConfigs.FirstOrDefaultAsync(x => x.ClearingHouseId == entity.Id, ct);
        if (existing is null)
        {
            existing = new ClearingHouseConfig
            {
                ClearingHouseId = entity.Id,
                HolidayStrategy = entity.ClearingHouseConfig.HolidayStrategy ?? DefaultHolidayStrategy,
                TimeZoneId = string.IsNullOrWhiteSpace(entity.ClearingHouseConfig.TimeZoneId) ? DefaultTimeZone : entity.ClearingHouseConfig.TimeZoneId,
                PaymentRailCode = entity.ClearingHouseConfig.PaymentRailCode,
                RequiresNachaProfile = entity.ClearingHouseConfig.RequiresNachaProfile,
                NachaProfileId = entity.ClearingHouseConfig.NachaProfileId
            };
            _context.ClearingHouseConfigs.Add(existing);
            await _context.SaveChangesAsync(ct);
        }

        entity.ClearingHouseId = existing.Id;
        await _context.SaveChangesAsync(ct);
        return existing;
    }

    private async Task<bool> HasOperationalRelationsAsync(int id, CancellationToken ct)
        => await _context.AchCycles.AnyAsync(x => x.ClearingHouseId == id, ct)
           || await _context.ClearingHouseCycleConfigs.AnyAsync(x => x.ClearingHouseId == id, ct)
           || await _context.ClearingHouseSpecialDates.AnyAsync(x => x.ClearingHouseId == id, ct)
           || await _context.InstitutionClearingHousePreferences.AnyAsync(x => x.ClearingHouseId == id, ct)
           || await _context.ClearingHouseTransactionRules.AnyAsync(x => x.ClearingHouseId == id, ct);

    private static string NormalizeCode(string? code) => code?.Trim().ToUpperInvariant() ?? string.Empty;

    private static bool CodesCorrespond(string code, string? name, string profileCode, string? profileName)
        => string.Equals(NormalizeCode(code), NormalizeCode(profileCode), StringComparison.Ordinal)
           || (!string.IsNullOrWhiteSpace(name)
               && !string.IsNullOrWhiteSpace(profileName)
               && string.Equals(name.Trim(), profileName.Trim(), StringComparison.OrdinalIgnoreCase));

    private sealed record NormalizedRequest(
        string Code,
        string Name,
        string OriginCode,
        string TimeZoneId,
        string HolidayStrategy,
        string? PaymentRailCode,
        bool RequiresNachaProfile,
        int? NachaProfileId);
}
