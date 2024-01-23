using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("representative")]
    public class RepresentativeModel
    {
        public int id { get; set; }

        [Required]
        public string user_id { get; set; }

        [ForeignKey("user_id")]
        public virtual UserModel? user { get; set; }
        public string representative_id { get; set; }

        [ForeignKey("representative_id")]
        public virtual UserModel? representative { get; set; }

        public DateTime date_from { get; set; }
        public DateTime date_to { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }


    }
}
