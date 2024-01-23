using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace it.Areas.Admin.Models
{
    [Table("document_related")]
    public class DocumentRelatedModel
    {
        public int id { get; set; }
        public int document_id { get; set; }
        public int document_related_id { get; set; }

        [JsonIgnore]
        [ForeignKey("document_id")]
        public virtual DocumentModel? document { get; set; }
        [NotMapped]
        public virtual DocumentModel? document_related { get; set; }
    }
}
