using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("process")]
    public class ProcessModel
    {
        public int id { get; set; }
        public string name { get; set; }

        public virtual List<ProcessStepModel> steps { get; set; }
        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }

    }
}
