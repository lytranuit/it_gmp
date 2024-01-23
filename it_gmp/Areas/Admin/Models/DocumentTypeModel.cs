using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("document_type")]
    public class DocumentTypeModel
    {
        public int id { get; set; }

        [Required]
        [StringLength(255)]
        public string name { get; set; }

        public bool? is_self_check { get; set; }
        public bool? is_manager_create { get; set; }
        public string? color { get; set; }
        public int? stt { get; set; }
        public string? symbol { get; set; }
        public int? group_id { get; set; }

        [ForeignKey("group_id")]
        public virtual DocumentTypeGroupModel? group { get; set; }

        public virtual TemplateModel? template { get; set; }
        public virtual List<DocumentTypeFollowModel>? users_follow { get; set; }
        public virtual List<DocumentTypeReceiveModel>? users_receive { get; set; }
        public virtual List<UserDocumentTypeModel>? users_manager { get; set; }
        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }


    }
}
