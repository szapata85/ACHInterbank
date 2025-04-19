using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cfa.ACHInterbank.Domain.Entities.JwksService;

public class JwksService
{

    public class Keys
    {
        public List<Key>? keys { get; set; }
    }

    public class Key
    {
        public string? kty { get; set; }
        public string? kid { get; set; }
        public string? use { get; set; }
        public string? alg { get; set; }
        public string? n { get; set; }
        public string? e { get; set; }
        [JsonIgnore]
        [NotMapped]
        public DateTime? Expire { get; set; }
    }

    public class KeyMap
    {
        public string? kty { get; set; }
        public string? alg { get; set; }
        public string? use { get; set; }
        public string? kid { get; set; }
        public string? n { get; set; }
        public string? e { get; set; }
    }

}
