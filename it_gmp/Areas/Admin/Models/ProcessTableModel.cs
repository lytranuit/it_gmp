using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("process_table")]
    public class ProcessTableModel
    {
        public int id { get; set; }
        public int process_id { get; set; }
        public int current_step { get; set; }
        public string title { get; set; }
        public string status { get; set; }


        public string? user_id { get; set; }

        [ForeignKey("user_id")]
        public virtual UserModel? user { get; set; }

        public virtual List<ProcessTableDataModel> Data { get; set; }
        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }


    }
}
