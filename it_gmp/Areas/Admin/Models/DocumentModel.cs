using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
namespace it.Areas.Admin.Models
{
    [Table("document")]
    public class DocumentModel
    {
        public int id { get; set; }

        public string? code { get; set; }

        [Required]
        public string name_vi { get; set; }

        public string? name_en { get; set; }
        public string? description_vi { get; set; }
        public string? description_en { get; set; }

        public int? priority { get; set; }
        public int? status_id { get; set; }
        public string? reason { get; set; }
        public virtual List<DocumentFileModel>? files { get; set; }
        public virtual List<DocumentAttachmentModel>? attachments { get; set; }

        public virtual List<DocumentRelatedModel>? related { get; set; }

        public virtual List<DocumentUserReceiveModel>? users_receive { get; set; }
        public virtual List<DocumentUserFollowModel>? users_follow { get; set; }
        public virtual List<DocumentSignatureModel>? users_signature { get; set; }
        public virtual List<DocumentCommentModel>? comments { get; set; }
        public virtual List<DocumentEventModel>? events { get; set; }





        public int type_id { get; set; }

        [ForeignKey("type_id")]
        public virtual DocumentTypeModel? type { get; set; }
        public int? position_id { get; set; }

        public int? signature_type_id { get; set; }

        public string? user_id { get; set; }

        [ForeignKey("user_id")]
        public virtual UserModel? user { get; set; }

        public string? user_next_signature_id { get; set; }

        [ForeignKey("user_next_signature_id")]
        public virtual UserModel? user_next_signature { get; set; }

        public string? user_next_representative_id { get; set; }

        [ForeignKey("user_next_representative_id")]
        public virtual UserModel? user_next_representative { get; set; }

        public string? keyword { get; set; }
        public string? location { get; set; }

        public bool? is_sign_multiple { get; set; }
        public bool? is_sign_parellel { get; set; } = false;


        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? date_finish { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? date_effect { get; set; }
        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? date_review { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? date_expire { get; set; }


        public DateTime? time_signature_previous { get; set; }



        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }


    }

    enum DocumentStatus
    {
        [Display(Name = "Nháp")]
        Draft = 1,
        [Display(Name = "Trình ký")]
        Release = 2,
        [Display(Name = "Đã hủy")]
        Cancle = 3,
        [Display(Name = "Đã ký xong")]
        Success = 4,
        [Display(Name = "Đã xử lý")]
        Processed = 5,
        [Display(Name = "Hiện hành")]
        Current = 6,
        [Display(Name = "Superseded")]
        Superseded = 7,
        [Display(Name = "Obsoleted")]
        Obsoleted = 8,
    }

}
