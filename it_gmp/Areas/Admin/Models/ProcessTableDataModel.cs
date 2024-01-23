using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("process_table_data")]
    public class ProcessTableDataModel
    {
        public int id { get; set; }
        public int process_table_id { get; set; }

        [ForeignKey("process_table_id")]
        public virtual ProcessTableModel ProcessTable { get; set; }
        public int process_step_id { get; set; }
        public int step_number { get; set; }
        [Column("data")]
        internal string? _data { get; set; }
        [NotMapped]
        public JObject E_data
        {
            get
            {
                return JsonConvert.DeserializeObject<JObject>(string.IsNullOrEmpty(_data) ? "{}" : _data);
            }
            set
            {
                _data = value.ToString();
            }
        }
        public bool is_finish { get; set; }


    }
}
