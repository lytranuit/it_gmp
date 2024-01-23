using it.Areas.Admin.Models;
using it.Data;
using it.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace it.Controllers
{

	public class ApiController : Controller
	{
		protected readonly ItContext _context;

		private readonly LoginMailPyme _LoginMailPyme;

		private readonly IConfiguration _configuration;
		private SignInManager<UserModel> _signInManager;
		public ApiController(ItContext context, LoginMailPyme LoginMailPyme, SignInManager<UserModel> signInManager, IConfiguration configuration)
		{
			_context = context;
			_LoginMailPyme = LoginMailPyme;
			_signInManager = signInManager;
			_configuration = configuration;
		}

		public IActionResult Index()
		{
			return Redirect("/Admin");
		}
		[HttpPost]
		public async Task<JsonResult> login(string email, string password)
		{
			LoginResponse responseJson = await _LoginMailPyme.login(email, password);
			if (responseJson.authed)
			{
				var authClaims = new List<Claim>
						{
							new Claim("Email",email),
							new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
						};
				var token = GetToken(authClaims);
				var token_string = new JwtSecurityTokenHandler().WriteToken(token);
				var TokenModel = new TokenModel()
				{
					token = token_string,
					email = email,
					created_at = DateTime.Now,
					vaild_to = token.ValidTo
				};
				_context.Add(TokenModel);
				_context.SaveChanges();

				Response.Cookies.Append(
					_configuration["JWT:NameCookieAuth"],
					token_string,
					new CookieOptions()
					{
						Domain = _configuration["JWT:Domain"],
						Expires = DateTime.Now.AddHours(Int64.Parse(_configuration["JWT:Expire"]))
					}
				);
				responseJson.token = token_string;
			}
			return Json(responseJson);
		}
		public async Task<JsonResult> TokenInfo(string token)
		{
			var find = _context.TokenModel.Where(d => d.deleted_at == null && d.token == token && d.vaild_to > DateTime.Now).FirstOrDefault();
			if (find != null)
			{
				var token_string = find.token;
				return Json(new { success = true, email = find.email, vaild_to = find.vaild_to.Value.ToString("yyyy-MM-dd HH:mm:ss") });
			}
			else
			{
				return Json(new { success = false });
			}

		}
		private JwtSecurityToken GetToken(List<Claim> authClaims)
		{
			var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

			var token = new JwtSecurityToken(
				issuer: _configuration["JWT:ValidIssuer"],
				audience: _configuration["JWT:ValidAudience"],
				expires: DateTime.Now.AddHours(Int64.Parse(_configuration["JWT:Expire"])),
				claims: authClaims,
				signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
				);

			return token;
		}

		public async Task<JsonResult> UserInfo(string email)
		{
			var User = _context.UserModel.Where(d => d.Email == email).FirstOrDefault();
			if (User != null)
			{
				var is_sign = true;
				if (User.image_sign == "/private/images/tick.png")
				{
					is_sign = false;
				}
				return Json(new { Id = User.Id, position = User.position, FullName = User.FullName, Email = User.Email, image_url = User.image_url, image_sign = User.image_sign, is_sign = is_sign });
			}
			else
			{
				return Json(new { });
			}

		}

		[HttpPost]
		public async Task<JsonResult> CheckLogin(string email, string password)
		{
			LoginResponse responseJson = await _LoginMailPyme.login(email, password);
			if (responseJson.authed == false)
			{
				var result = await _signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: true);
				if (result.Succeeded)
				{
					responseJson.authed = true;
					var parameter = new Dictionary<string, string>();
					parameter.Add("user", "AstaCorp");
					parameter.Add("password", "!@#Asta_it@9504");
					responseJson.parameter = parameter;
				}
			}
			else
			{
				var parameter = new Dictionary<string, string>();
				parameter.Add("user", "AstaCorp");
				parameter.Add("password", "!@#Asta_it@9504");
				responseJson.parameter = parameter;
			}
			return Json(responseJson);
		}
	}
}
