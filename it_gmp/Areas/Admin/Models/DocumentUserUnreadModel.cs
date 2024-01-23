using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("document_user_unread")]
    public class DocumentUserUnreadModel
    {
        public int id { get; set; }

        [Required]
        public int document_id { get; set; }


        [Required]
        public string user_id { get; set; }

        [Required]
        public DateTime time { get; set; }

    }
}
