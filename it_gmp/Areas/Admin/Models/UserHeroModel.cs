using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it.Areas.Admin.Models
{

    [Table("user_hero")]
    public class UserHeroModel
    {
        [Key]
        public string userid { get; set; }

        public string? email { get; set; }
        public string? lastname { get; set; }

        public string? firstname { get; set; }
    }
}
