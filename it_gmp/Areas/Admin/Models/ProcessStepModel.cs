using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("process_step")]
    public class ProcessStepModel
    {
        public int id { get; set; }
        public int process_id { get; set; }
        [ForeignKey("process_id")]

        public virtual ProcessModel process { get; set; }
        public int step_number { get; set; }
        public string title { get; set; }
        public string type { get; set; }


    }
}
