using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace it.TagHelpers
{
    public class CountUnreadSigndocTagHelper : TagHelper
    {
        private readonly ItContext _context;
        private IActionContextAccessor actionAccessor;
        private UserManager<UserModel> UserManager;
        public CountUnreadSigndocTagHelper(ItContext context, UserManager<UserModel> UserMgr, IActionContextAccessor ActionAccessor)
        {
            _context = context;
            UserManager = UserMgr;
            actionAccessor = ActionAccessor;
        }
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var user = actionAccessor.ActionContext.HttpContext.User;
            var user_id = UserManager.GetUserId(user);

            List<int> documents_unread = _context.DocumentUserUnreadModel.Where(d => d.user_id == user_id).Select(d => d.document_id).Distinct().ToList();
            var document_signed = _context.DocumentSignatureModel.Where(d => d.status == 2 && (d.user_sign == user_id || d.user_id == user_id)).Select(d => d.document_id).ToList();
            var count = _context.DocumentModel.Where(d => d.deleted_at == null && document_signed.Contains(d.id) && documents_unread.Contains(d.id)).Count();
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