using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace it.Areas.Admin.Models
{
    [Table("document_comment_file")]
    public class DocumentCommentFileModel
    {
        public int id { get; set; }

        [StringLength(255)]
        public string name { get; set; }
        [StringLength(255)]
        public string url { get; set; }
        [StringLength(50)]
        public string ext { get; set; }
        [StringLength(255)]
        public string mimeType { get; set; }

        public int document_comment_id { get; set; }

        [JsonIgnore]
        [ForeignKey("document_comment_id")]
        public virtual DocumentCommentModel? comment { get; set; }

        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }




    }
}
