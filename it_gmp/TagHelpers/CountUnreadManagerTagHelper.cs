using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics;

namespace it_gmp.TagHelpers
{
    public class CountUnreadManagerTagHelper : TagHelper
    {
        private readonly ItContext _context;
        private IActionContextAccessor actionAccessor;
        private UserManager<UserModel> UserManager;
        public CountUnreadManagerTagHelper(ItContext context, UserManager<UserModel> UserMgr, IActionContextAccessor ActionAccessor)
        {
            _context = context;
            UserManager = UserMgr;
            actionAccessor = ActionAccessor;
            var listener = _context.GetService<DiagnosticSource>();
            (listener as DiagnosticListener).SubscribeWithAdapter(new CommandInterceptor());
        }
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var user = actionAccessor.ActionContext.HttpContext.User;
            var user_id = UserManager.GetUserId(user);
            var user_current = await UserManager.GetUserAsync(user); // Get user id:
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");


            List<int> documents_unread = _context.DocumentUserUnreadModel.Where(d => d.user_id == user_id).Select(d => d.document_id).Distinct().ToList();
            var document_receive = _context.DocumentUserReceiveModel.Where(d => d.user_id == user_id).Select(d => d.document_id).ToList();

            var customerData = _context.DocumentModel.Where(d => d.deleted_at == null && document_receive.Contains(d.id) && documents_unread.Contains(d.id));
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();

                customerData = customerData.Where(d => type_gmp.Contains(d.type_id));
            }
            var count = customerData.Count();
            if (count > 0)
            {
                output.TagName = "span";    // Replaces <email> with <a> tag

                output.Attributes.SetAttribute("class", "badge badge-danger float-right mr-2");
                if (count < 10)
                {
                    output.Content.SetContent(count.ToString());
                }
                else
                {
                    output.Content.SetContent("9+");
                }
            }
        }
    }
}