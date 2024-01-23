// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using it.Areas.Admin.Models;
using it.Data;
using it.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace it.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<UserModel> _userManager;
        protected readonly ItContext _context;
        private readonly ViewRender _view;

        public ForgotPasswordModel(UserManager<UserModel> userManager, ItContext context, ViewRender view)
        {
            _userManager = userManager;
            _context = context;
            _view = view;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordFailed");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                var body = _view.Render("Emails/ResetPassword", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/Identity/Account/ResetPassword?email=" + Input.Email + "&code=" + code });
                var email = new EmailModel
                {
                    email_to = Input.Email,
                    subject = "[Đổi mật khẩu] Xác nhận thay đổi mật khẩu",
                    body = body,
                    email_type = "resetpassword",
                    status = 1
                };
                _context.Add(email);
                await _context.SaveChangesAsync();

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
