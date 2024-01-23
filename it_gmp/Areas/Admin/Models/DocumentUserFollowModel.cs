using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace it.Areas.Admin.Models
{
    [Table("document_user_follow")]
    public class DocumentUserFollowModel
    {
        public int id { get; set; }
        public int document_id { get; set; }
        public string user_id { get; set; }

        [JsonIgnore]
        [ForeignKey("document_id")]
        public DocumentModel? document { get; set; }
    }
}
