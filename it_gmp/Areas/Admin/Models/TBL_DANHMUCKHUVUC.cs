using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it.Areas.Admin.Models
{

    [Table("TBL_DANHMUCKHUVUC")]
    public class TBL_DANHMUCKHUVUC
    {
        [Key]
        public string makhuvuc { get; set; }
        [Column("tenkhuvuc_VN")]
        public string? tenkhuvuc { get; set; }
        [Column("tenkhuvuc")]
        public string? tenkhuvuc_en { get; set; }

        [Column("SOP_noti")]
        public string? email_SOP { get; set; }

        [NotMapped]
        public List<string>? list_email_SOP
        {
            get
            {
                if (email_SOP != null)
                {

                    return email_SOP.Split(",").ToList();
                }
                return null;
            }
            set
            {
                email_SOP = value != null && value.Count() > 0 ? string.Join(",", value) : null;
            }
        }
        [NotMapped]
        public List<string>? list_email_SOP_id { get; set; }

    }
}
