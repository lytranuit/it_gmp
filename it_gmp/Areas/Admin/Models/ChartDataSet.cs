using System.ComponentModel.DataAnnotations;
namespace it.Areas.Admin.Models
{
    public class ChartDataSet
    {
        [Key]
        public string label { get; set; }

        public List<string> backgroundColor { get; set; }
        public List<int?> data { get; set; }

    }
}
