// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace it.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<UserModel> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        private UserManager<UserModel> UserManager;
        protected readonly ItContext _context;
        private readonly IConfiguration _configuration;
        public LogoutModel(SignInManager<UserModel> signInManager, ILogger<LogoutModel> logger, ItContext context, UserManager<UserModel> UserMgr, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            UserManager = UserMgr;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser); // Get user
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Logout.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã đăng xuất";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            await _signInManager.SignOutAsync();
            ////Remove Cookie
            Response.Cookies.Delete(_configuration["JWT:NameCookieAuth"], new CookieOptions()
            {
                Domain = _configuration["JWT:Domain"]
            });
            ///
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
