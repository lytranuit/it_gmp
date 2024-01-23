using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("emails")]
    public class EmailModel
    {
        public int id { get; set; }

        public int status { get; set; }
        public string subject { get; set; }
        public string email_to { get; set; }
        public string email_type { get; set; }
        public string body { get; set; }

        public DateTime? date { get; set; }
        public string? attachments { get; set; }
        [NotMapped]
        public virtual List<string> data_attachments
        {
            get
            {
                //Console.WriteLine(settings);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(string.IsNullOrEmpty(attachments) ? "[]" : attachments);
            }
            set
            {
                attachments = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            }
        }
        public string? error { get; set; }
    }
}
