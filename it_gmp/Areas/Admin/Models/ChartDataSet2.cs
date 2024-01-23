using System.ComponentModel.DataAnnotations;
namespace it.Areas.Admin.Models
{
    public class ChartDataSet2
    {
        [Key]
        public string label { get; set; }

        public string backgroundColor { get; set; }
        public List<int?> data { get; set; }

    }
}
