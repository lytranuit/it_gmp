using System.ComponentModel.DataAnnotations;
namespace it.Areas.Admin.Models
{
    public class Chart
    {
        [Key]
        public string time_type { get; set; }

        public string? title { get; set; }
        public int? num_finish { get; set; }
        public int? num_sign { get; set; }

        public int? num_cancle { get; set; }
    }
}
