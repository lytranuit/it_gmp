using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace it.Areas.Admin.Models
{
    [Table("document_signature")]
    public class DocumentSignatureModel
    {
        public int id { get; set; }
        public int document_id { get; set; }
        public double? position_x { get; set; }
        public double? position_y { get; set; }
        public string? url { get; set; }
        public string? reason { get; set; }
        public int? page { get; set; }
        public int? stt { get; set; }
        public int? status { get; set; } = 1;

        public string? image_sign { get; set; }

        public double? position_image_x { get; set; }
        public double? position_image_y { get; set; }
        public double? image_size_width { get; set; }
        public double? image_size_height { get; set; }

        [JsonIgnore]
        [ForeignKey("document_id")]
        public DocumentModel? document { get; set; }


        public string user_id { get; set; }

        [ForeignKey("user_id")]
        public virtual UserModel? user { get; set; }
        public string? user_sign { get; set; }

        [ForeignKey("user_sign")]
        public virtual UserModel? user_s { get; set; }

        public DateTime? date { get; set; }

        [NotMapped]
        public virtual int? num_sign { get; set; }

    }
}
