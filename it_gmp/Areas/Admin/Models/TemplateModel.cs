using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("template")]
    public class TemplateModel
    {
        public int id { get; set; }

        public string name { get; set; }
        public string? code { get; set; }

        public int? type_id { get; set; }
        [ForeignKey("type_id")]
        public virtual DocumentTypeModel? type { get; set; }
        public string? file_url { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }


    }
}
