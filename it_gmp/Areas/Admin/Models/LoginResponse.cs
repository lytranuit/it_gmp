using System.ComponentModel.DataAnnotations;
namespace it.Areas.Admin.Models
{
	public class LoginResponse
	{
		[Key]
		public bool authed { get; set; }

		public string? error { get; set; }
		public Dictionary<string, string>? parameter { get; set; }

		public string? session { get; set; }
		public string? user { get; set; }
		public string? token { get; set; }
	}
}
