using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace it.Areas.Admin.Models
{
	[Table("user_document_type")]
	public class UserDocumentTypeModel
	{
		public int id { get; set; }
		public int document_type_id { get; set; }
		public string user_id { get; set; }

		[ForeignKey("document_type_id")]
		public DocumentTypeModel? document_type { get; set; }

		[ForeignKey("user_id")]
		public UserModel? user { get; set; }
	}
}
