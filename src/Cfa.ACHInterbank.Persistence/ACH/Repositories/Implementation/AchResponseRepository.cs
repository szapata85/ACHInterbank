using Cfa.ACHInterbank.Application.ACH.Responses.Queries.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchResponseRepository : IAchResponseRepository
{
    private readonly AchDbContext _context;

    public AchResponseRepository(AchDbContext context) => _context = context;

    public Task<AchResponse?> FindByIdempotencyHashAsync(string hashIdempotencia, CancellationToken cancellationToken = default)
        => _context.AchResponses.AsNoTracking().FirstOrDefaultAsync(x => x.HashIdempotencia == hashIdempotencia, cancellationToken);

    public async Task AddAsync(AchResponse response, CancellationToken cancellationToken = default)
        => await _context.AchResponses.AddAsync(response, cancellationToken);

    public Task UpdateAsync(AchResponse response, CancellationToken cancellationToken = default)
    {
        _context.AchResponses.Update(response);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<AchResponseListItemModel>> SearchAsync(AchResponseSearchQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);

        var data = _context.AchResponses.AsNoTracking().AsQueryable();
        if (query.FechaDesde.HasValue) data = data.Where(x => x.FechaRecepcion >= query.FechaDesde.Value);
        if (query.FechaHasta.HasValue) data = data.Where(x => x.FechaRecepcion <= query.FechaHasta.Value);
        if (!string.IsNullOrWhiteSpace(query.TipoRespuesta)) data = data.Where(x => x.TipoRespuesta.ToString() == query.TipoRespuesta.Trim());
        if (!string.IsNullOrWhiteSpace(query.IdTransaccion)) data = data.Where(x => x.IdTransaccion == query.IdTransaccion.Trim());
        if (!string.IsNullOrWhiteSpace(query.CodigoCamaraCompensacion)) data = data.Where(x => x.CodigoCamaraCompensacion == query.CodigoCamaraCompensacion.Trim());
        if (!string.IsNullOrWhiteSpace(query.CodigoEntidadOrigen)) data = data.Where(x => x.CodigoEntidadOrigen == query.CodigoEntidadOrigen.Trim());
        if (!string.IsNullOrWhiteSpace(query.CodigoEntidadDestino)) data = data.Where(x => x.CodigoEntidadDestino == query.CodigoEntidadDestino.Trim());
        if (!string.IsNullOrWhiteSpace(query.CodigoEstadoExterno)) data = data.Where(x => x.CodigoEstadoExterno == query.CodigoEstadoExterno.Trim());
        if (!string.IsNullOrWhiteSpace(query.EstadoProcesamiento)) data = data.Where(x => x.EstadoProcesamiento.ToString() == query.EstadoProcesamiento.Trim());
        if (!string.IsNullOrWhiteSpace(query.CorrelationId)) data = data.Where(x => x.CorrelationId == query.CorrelationId.Trim());

        var totalCount = await data.CountAsync(cancellationToken);
        var items = await data.OrderByDescending(x => x.FechaRecepcion)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AchResponseListItemModel(x.Id, x.TipoRespuesta.ToString(), x.IdTransaccion, x.CodigoCamaraCompensacion, x.CodigoEntidadOrigen, x.CodigoEntidadDestino, x.CodigoEstadoExterno, x.CodigoCausalExterna, x.EstadoInternoNombre, x.EstadoProcesamiento.ToString(), x.PermiteNotificacion, x.CorrelationId, x.FechaRecepcion, x.FechaCreacion))
            .ToListAsync(cancellationToken);

        return new PagedResult<AchResponseListItemModel>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<AchResponseDetailModel?> FindDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AchResponses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AchResponseDetailModel(
                x.Id, x.TipoRespuesta.ToString(), x.IdTransaccion, x.CodigoCamaraCompensacion, x.CodigoEntidadOrigen, x.CodigoEntidadDestino,
                x.CodigoEstadoExterno, x.CodigoCausalExterna, x.IdEstadoInterno, x.IdEstadoServicioExterno, x.EstadoInternoNombre,
                x.CausalNormalizada, x.DescripcionCausal, x.IdTransaccionServicioExterno, x.HashIdempotencia, x.EstadoProcesamiento.ToString(),
                x.MotivoNoHomologacion, x.PermiteNotificacion, x.CorrelationId, x.FechaRecepcion, x.FechaCreacion, x.FechaActualizacion,
                x.NotificationAttempts.OrderBy(a => a.NumeroIntento)
                    .Select(a => new AchResponseNotificationAttemptModel(a.Id, a.AchResponseId, a.NumeroIntento, a.EstadoNotificacion.ToString(), a.IdCanal, a.NombreCanal, a.IdTransaccion, a.IdEstado, a.Causal, a.IdTransaccionServicioExterno, a.DescripcionCausal, a.ExisteError, a.CodigoError, a.DescripcionError, a.ErrorTecnico, a.FechaCreacion, a.FechaEnvio))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
