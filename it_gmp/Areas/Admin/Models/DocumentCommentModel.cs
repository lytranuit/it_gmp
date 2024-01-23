using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace it.Areas.Admin.Models
{
    [Table("document_comment")]
    public class DocumentCommentModel
    {
        public int id { get; set; }
        public int document_id { get; set; }
        public string? comment { get; set; }


        [JsonIgnore]
        [ForeignKey("document_id")]
        public virtual DocumentModel? document { get; set; }


        public string user_id { get; set; }

        [ForeignKey("user_id")]
        public virtual UserModel? user { get; set; }
        public virtual List<DocumentCommentFileModel>? files { get; set; }

        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
        [NotMapped]

        public bool is_read { get; set; } = false;


    }
}
