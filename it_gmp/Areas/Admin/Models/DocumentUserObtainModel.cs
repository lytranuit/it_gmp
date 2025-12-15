using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("document_user_obtain")]
    public class DocumentUserObtainModel
    {
        public int id { get; set; }
        public int document_id { get; set; }

        [ForeignKey("document_id")]
        public virtual DocumentModel? document { get; set; }
        public string user_id { get; set; }

        [ForeignKey("user_id")]
        public virtual UserModel? user { get; set; }
    }
}
