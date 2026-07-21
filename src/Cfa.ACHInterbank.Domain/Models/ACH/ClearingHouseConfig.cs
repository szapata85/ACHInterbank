using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouseConfig
{
 
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string? HolidayStrategy { get; set; } // e.g., "Colombian", "US", etc.
    public string TimeZoneId { get; set; } = "America/Bogota";
    public bool RequiresNachaProfile { get; set; }
    public int? NachaProfileId { get; set; }
    public CfgProfile? NachaProfile { get; set; }

    
    public virtual ICollection<ClearingHouse>? ClearingHouses { get; set; }
}

