using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace it.Areas.Admin.Models
{
	[Table("document_user_keyword")]
	public class DocumentUserKeywordModel
	{
		public int id { get; set; }
		public int document_id { get; set; }
		public string user_id { get; set; }
		public string? keyword { get; set; }

		[JsonIgnore]
		[ForeignKey("document_id")]
		public virtual DocumentModel? document { get; set; }
	}
}
