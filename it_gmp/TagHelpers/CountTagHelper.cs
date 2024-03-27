using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics;

namespace it_gmp.TagHelpers
{
    public class CountTagHelper : TagHelper
    {
        private readonly ItContext _context;
        private IActionContextAccessor actionAccessor;
        private UserManager<UserModel> UserManager;

        public CountTagHelper(ItContext context, UserManager<UserModel> UserMgr, IActionContextAccessor ActionAccessor)
        {
            _context = context;
            UserManager = UserMgr;
            actionAccessor = ActionAccessor; 
            var listener = _context.GetService<DiagnosticSource>();
            (listener as DiagnosticListener).SubscribeWithAdapter(new CommandInterceptor());
        }
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var user = actionAccessor.ActionContext.HttpContext.User;
            var uid = UserManager.GetUserId(user);
            var count1 = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id == 2 && d.user_next_signature_id == uid && d.is_sign_parellel == false).Count();
            var count2 = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id == 2 && d.is_sign_parellel == true).Join(_context.DocumentSignatureModel, d => d.id, ds => ds.document_id, (d, ds) => new
            {
                document_id = ds.document_id,
                status = ds.status,
                user_id = ds.user_id,
            }).Distinct().Where(d => d.status == 1 && d.user_id == uid).Count();
            //Console.WriteLine(count2);
            var count = count1 + count2;
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