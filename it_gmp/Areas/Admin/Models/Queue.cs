using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace it.Areas.Admin.Models
{
    [Table("queue")]
    public class QueueModel
    {
        [Key]
        public int id { get; set; }
        public string? created_by { get; set; }
        public int? status_id { get; set; } = 1;
        public string? json { get; set; }
        public string? type { get; set; }
        public DateTime? created_at { get; set; }
        [NotMapped]
        public virtual QueueValue? valueQ
        {
            get
            {
                //Console.WriteLine(settings);
                return JsonSerializer.Deserialize<QueueValue>(string.IsNullOrEmpty(json) ? "{}" : json);
            }
            set
            {
                json = JsonSerializer.Serialize(value, new JsonSerializerOptions()
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                });
            }
        }

    }
    public class QueueValue
    {
        public int? esign_id { get; set; }
    }
}
