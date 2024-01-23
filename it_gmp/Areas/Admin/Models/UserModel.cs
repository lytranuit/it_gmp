using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace it.Areas.Admin.Models
{

	[Table("AspNetUsers")]
	public class UserModel : IdentityUser
	{
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string FullName { get; set; }
		public string? position { get; set; }
		public string? image_sign { get; set; }
		public string? image_url { get; set; }
		public DateTime? expiry_date { get; set; }
		public DateTime? last_login { get; set; }
		public bool? is_first_login { get; set; }

		public string? msnv { get; set; }
		public DateTime? created_at { get; set; }

		public DateTime? updated_at { get; set; }

		public DateTime? deleted_at { get; set; }

		public string? signature { get; set; }

		//[JsonIgnore]
		//public virtual List<DocumentSignatureModel>? users_signature { get; set; }

		public virtual List<UserDocumentTypeModel> DocumentTypes { get; set; }


	}
}
