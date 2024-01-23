using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    public class ChartPie
    {
        [Key]
        public int id { get; set; }
        public int? num_finish { get; set; }
        public int? num_wait { get; set; }

        public int? num_cancle { get; set; }
        public int? num_current { get; set; }
        public int? num_superseded { get; set; }
        public int? num_obsoleted { get; set; }
    }
    public class ChartType
    {
        [Key]
        public int type_id { get; set; }
        public int? num { get; set; }

        [ForeignKey("type_id")]
        public DocumentTypeModel type { get; set; }
    }
    public class ChartTypeGroup
    {

        [Key]
        public int group_id { get; set; }
        public int? num { get; set; }

        [ForeignKey("group_id")]
        public DocumentTypeGroupModel group { get; set; }
    }
}
