using Cfa.ACHInterbank.Domain.Entities.User;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchHeader
{
    public int BatchID { get; set; }
    public string ServiceClassCode { get; set; }
    public string CompanyName { get; set; }
    public string DiscretionaryData { get; set; }
    public string CompanyId { get; set; }
    public string StandardEntryClassCode { get; set; }
    public string CompanyEntryDescription { get; set; }
    public string DescriptiveDate { get; set; }
    public string EffectiveEntryDate { get; set; }
    public string CompensationDate { get; set; }
    public string OriginUserStatusCode { get; set; }
    public string OriginParticipantEntityCode { get; set; }
    public int BatchNumber { get; set; }
    public int NachaID { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
}
