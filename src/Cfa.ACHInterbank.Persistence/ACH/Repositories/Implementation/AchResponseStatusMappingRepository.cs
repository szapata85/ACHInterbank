using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchResponseStatusMappingRepository : IAchResponseStatusMappingRepository
{
    private readonly AchDbContext _context;

    public AchResponseStatusMappingRepository(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AchResponseStatusMappingModel>> FindCandidatesAsync(string codigoCamaraCompensacion, TipoRespuestaAch tipoRespuesta, string codigoEstadoExterno, CancellationToken cancellationToken = default)
    {
        var camara = Normalize(codigoCamaraCompensacion);
        var estado = Normalize(codigoEstadoExterno);

        var query = _context.AchResponseStatusMappings
            .AsNoTracking()
            .Where(x => x.CodigoCamaraCompensacion == camara)
            .Where(x => x.TipoRespuesta == tipoRespuesta)
            .Where(x => x.CodigoEstadoExterno == estado);

        return await query
            .Select(MapToModel())
            .ToListAsync(cancellationToken);
    }

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static System.Linq.Expressions.Expression<Func<AchResponseStatusMapping, AchResponseStatusMappingModel>> MapToModel()
        => x => new AchResponseStatusMappingModel
        {
            Id = x.Id,
            CodigoCamaraCompensacion = x.CodigoCamaraCompensacion,
            TipoRespuesta = x.TipoRespuesta,
            CodigoEstadoExterno = x.CodigoEstadoExterno,
            CodigoCausalExterna = x.CodigoCausalExterna,
            IdEstadoInterno = x.IdEstadoInterno,
            IdEstadoServicioExterno = x.IdEstadoServicioExterno,
            EstadoInternoNombre = x.EstadoInternoNombre,
            CausalNormalizada = x.CausalNormalizada,
            DescripcionCausalNormalizada = x.DescripcionCausalNormalizada,
            RequiereCausal = x.RequiereCausal,
            PermiteNotificacion = x.PermiteNotificacion,
            Activo = x.Activo,
            FechaInicioVigencia = x.FechaInicioVigencia,
            FechaFinVigencia = x.FechaFinVigencia
        };
}
