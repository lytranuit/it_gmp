using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it.Areas.Admin.Models
{

    [Table("Token")]
    public class TokenModel
    {
        [Key]
        public int id { get; set; }
        public string email { get; set; }
        public string token { get; set; }

        public DateTime? vaild_to { get; set; }
        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }


    }
}
