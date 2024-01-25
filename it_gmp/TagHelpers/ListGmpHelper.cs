using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics;

namespace it.TagHelpers
{
    public class ListGmpHelper : TagHelper
    {
        private readonly ItContext _context;
        private IActionContextAccessor actionAccessor;
        private UserManager<UserModel> UserManager;

        public ListGmpHelper(ItContext context, UserManager<UserModel> UserMgr, IActionContextAccessor ActionAccessor)
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
            var DocumentTypeModel = _context.DocumentTypeModel.Where(d => d.deleted_at == null && d.group_id == 6).ToList();
            if (DocumentTypeModel.Count > 0)
            {
                output.TagName = "ul";    // Replaces <email> with <a> tag
                output.Attributes.SetAttribute("class", "nav-second-level mm-collapse");
                var html = "";
                foreach (var d in DocumentTypeModel)
                {
                    html += $@"
                        <li class='nav-item'>
					        <a class='nav-link' href='/admin/document/types/{d.id}'>
						        <i class='ti-control-record'></i>{d.name}
					        </a>
				        </li>";
                }
                output.Content.SetHtmlContent(html);
            }
        }
    }
}