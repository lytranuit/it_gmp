
using CertificateManager;
using CertificateManager.Models;
using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace it.Areas.Admin.Controllers
{

    public class MemberController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private RoleManager<IdentityRole> RoleManager;
        [BindProperty]
        public InputModel? Input { get; set; }
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string? OldPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }
        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }
        public MemberController(ItContext context, UserManager<UserModel> UserMgr, RoleManager<IdentityRole> RoleMgr) : base(context)
        {
            UserManager = UserMgr;
            RoleManager = RoleMgr;
        }
        // GET: MemberController
        public async Task<IActionResult> Index()
        {

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            UserModel User = await UserManager.FindByIdAsync(user_id);
            return View(User);
        }

        // POST: MemberController/Edit
        [HttpPost]
        public async Task<IActionResult> Index(UserModel User)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string id = UserManager.GetUserId(currentUser); // Get user id:
            UserModel User_old = await UserManager.FindByIdAsync(id);
            User_old.FullName = User.FullName;
            User_old.image_sign = User.image_sign;
            User_old.image_url = User.image_url;
            User_old.position = User.position;
            IdentityResult result = await UserManager.UpdateAsync(User_old);
            if (result.Succeeded)
            {
                /// Audittrail
                var audit = new AuditTrailsModel();
                audit.UserId = User_old.Id;
                audit.Type = AuditType.Update.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {User_old.FullName} cập nhật thông tin tài khoản của mình";
                _context.Add(audit);
                _context.SaveChanges();


                StatusMessage = "Cập nhật thành công!";
            }
            else
            {
                ErrorMessage = "Cập nhập thất bại";
            }
            return RedirectToAction(nameof(Index));
        }



        // GET: MemberController
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: MemberController/Edit
        [HttpPost]
        public async Task<IActionResult> ChangePassword(InputModel Input)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string id = UserManager.GetUserId(currentUser); // Get user id:

            var user = await UserManager.GetUserAsync(currentUser);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await UserManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                ErrorMessage = "";
                foreach (var error in changePasswordResult.Errors)
                {
                    ErrorMessage += error.Description + "<br>";
                }
                return RedirectToAction(nameof(ChangePassword));
            }

            user.expiry_date = DateTime.Now + TimeSpan.FromDays(180);
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            await UserManager.UpdateAsync(user);

            /// Audittrail
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.ChangePassword.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã đổi mật khẩu";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            StatusMessage = "Mật khẩu đã được thay đổi";
            return RedirectToAction(nameof(ChangePassword));
        }

        // POST: MemberController/CreateSignature
        [HttpPost]
        public IActionResult CreateSignature()
        {

            var base64 = Request.Form["base_image"].FirstOrDefault();
            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string id = UserManager.GetUserId(currentUser); // Get user id:
            byte[] bytes = Convert.FromBase64String(base64.Split(',')[1]);
            using (FileStream stream = new FileStream("private\\upload\\" + id + "\\" + timeStamp + ".png", FileMode.Create))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
            return Json(new { success = 1, image = "/private/upload/" + id + "/" + timeStamp + ".png" });
        }

        // POST: UserController/Create
        [HttpPost]
        public async Task<IActionResult> RenewCertificate(string user_id)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser); // Get user id:
            if (user_id != null && user_id != "")
            {
                user = await UserManager.FindByIdAsync(user_id);
            }

            // Generate private-public key pair
            var serviceProvider = new ServiceCollection()
                  .AddCertificateManager()
                  .BuildServiceProvider();

            string passwordPublic = "!PMP_it123456";
            var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();
            var path = @"private\rootca\localhost_root.pfx";
            //return Ok(path);
            X509Certificate2 rootCaL1 = new X509Certificate2(path, passwordPublic);
            var serverL3 = createClientServerAuthCerts.NewClientChainedCertificate(
                new DistinguishedName { CommonName = user.FullName + "<" + user.Email + ">", OrganisationUnit = user.position },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                "localhost", rootCaL1);
            var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();
            var serverCertL3InPfxBtyes = importExportCertificate.ExportChainedCertificatePfx(passwordPublic, serverL3, rootCaL1);
            System.IO.File.WriteAllBytes("private/pfx/" + user.Id + ".pfx", serverCertL3InPfxBtyes);

            return Ok(1);


        }
    }
}
