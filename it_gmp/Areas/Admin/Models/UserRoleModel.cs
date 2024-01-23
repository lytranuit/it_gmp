using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace it.Areas.Admin.Models
{

	[Table("AspNetUserRoles")]
	public class UserRoleModel
	{
		[Key, Column(Order = 0)]
		public string RoleId { get; set; }
		[Key, Column(Order = 1)]
		public string UserId { get; set; }

	}
}
