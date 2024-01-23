using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace it.Areas.Admin.Models
{
    [Table("document_event")]
    public class DocumentEventModel
    {
        public int id { get; set; }

        [Required]
        [StringLength(255)]
        public string event_content { get; set; }


        public int document_id { get; set; }

        [JsonIgnore]
        [ForeignKey("document_id")]
        public virtual DocumentModel? document { get; set; }
        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }


    }
}
