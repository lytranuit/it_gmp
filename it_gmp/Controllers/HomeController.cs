using it.Areas.Admin.Models;
using it.Data;
using it.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics;
using System.Net.Mail;
using System.Net.Mime;

namespace it.Controllers
{

    public class HomeController : Controller
    {
        private readonly SignInManager<UserModel> _signInManager;
        protected readonly ItContext _context;
        private readonly ViewRender _view;
        private readonly IConfiguration _configuration;
        private UserManager<UserModel> UserManager;


        public HomeController(ItContext context, ViewRender view, IConfiguration configuration, UserManager<UserModel> UserMgr, SignInManager<UserModel> signInManager)
        {
            _signInManager = signInManager;
            UserManager = UserMgr;
            _context = context;
            _view = view;
            _configuration = configuration;
            var listener = _context.GetService<DiagnosticSource>();
            (listener as DiagnosticListener).SubscribeWithAdapter(new CommandInterceptor());
        }

        public IActionResult Index()
        {
            return Redirect("/Admin");
        }

        public async Task<JsonResult> cronjob()
        {
            var emails = _context.EmailModel.Where(d => d.status == 1).Take(10).ToList();
            foreach (var email in emails)
            {
                var SuccesMail = SendMail(email.email_to, email.subject, email.body, email.data_attachments);
                if (SuccesMail.success == 1)
                {
                    email.status = 2;
                }
                else
                {
                    email.status = 3;
                    email.error = SuccesMail.ex.ToString();
                }
                email.date = DateTime.Now;
                _context.Update(email);
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        public async Task<JsonResult> cronjobremind()
        {
            //return Json(new { });
            var documents = _context.DocumentModel.Where(d => d.status_id == 2 && d.deleted_at == null && d.is_sign_parellel == false && d.user_next_signature_id != null && d.time_signature_previous != null && d.time_signature_previous < DateTime.Now.AddDays(-3));

            var group = documents.GroupBy(d => d.user_next_signature_id, (x, y) => new
            {
                num_sign = y.Count(),
                data = y.ToList(),
                user_sign = x
            }).ToList();



            var data2 = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id == 2 && d.is_sign_parellel == true).Join(_context.DocumentSignatureModel, d => d.id, ds => ds.document_id, (d, ds) => new
            {
                document = d,
                status = ds.status,
                user_id = ds.user_id,
            }).Distinct().Where(d => d.status == 1).GroupBy(d => d.user_id, (x, y) => new
            {
                num_sign = y.Count(),
                data = y.ToList(),
                user_sign = x
            }).ToList();
            var all = group.Concat(data2.Select(d => new
            {
                num_sign = d.num_sign,
                data = d.data.Select(d => d.document).ToList(),
                user_sign = d.user_sign
            })).ToList();


            foreach (var item in all)
            {

                var user = _context.UserModel.Where(d => d.Id == item.user_sign).FirstOrDefault();
                if (user == null)
                    continue;

                if (user.deleted_at != null || (user.LockoutEnd != null && user.LockoutEnd >= DateTime.Now))
                    continue;
                ///Xóa user nếu user 1 tháng chưa đăng nhập
                //var last_login = user.last_login != null ? user.last_login : user.created_at;
                //if (last_login < DateTime.Now.AddMonths(-1))
                //{
                //    user.LockoutEnd = DateTime.Now.AddDays(360);
                //    _context.Update(user);
                //    _context.SaveChanges();
                //    continue;
                //}
                foreach (var da in item.data)
                {
                    var type = _context.DocumentTypeModel.Where(d => d.id == da.type_id).FirstOrDefault();
                    da.type = type;
                }
                var mail_string = user.Email;
                string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                var body = _view.Render("Emails/RemindDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/wait", count = item.num_sign, data = item.data });
                var email = new EmailModel
                {
                    email_to = mail_string,
                    subject = "[Nhắc nhở] Các hồ sơ đang cần chữ ký của bạn",
                    body = body,
                    email_type = "remind_document",
                    status = 1
                };
                _context.Add(email);

            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, data = all });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<JsonResult> Resend(int id)
        {
            var email = _context.EmailModel.Where(d => d.id == id).FirstOrDefault();
            var SuccesMail = SendMail(email.email_to, email.subject, email.body, email.data_attachments);
            if (SuccesMail.success == 1)
            {
                email.status = 2;
            }
            else
            {
                email.status = 3;
                email.error = SuccesMail.ex.ToString();
            }
            email.date = DateTime.Now;
            _context.Update(email);
            await _context.SaveChangesAsync();
            return Json(new
            {
                success = SuccesMail.success
            });
        }
        private SuccesMail SendMail(string to, string subject, string body, List<string>? attachments = null)
        {
            try
            {

                string[] list_to = to.Split(",");

                MailMessage message = new MailMessage();
                message.From = new MailAddress(_configuration["Mail:User"], _configuration["Mail:Name"]);
                //message.From = new MailAddress("daolytran@pymepharco.com", "Pymepharco System");
                foreach (string str in list_to)
                {
                    message.To.Add(new MailAddress(str));
                }
                message.Subject = subject;
                message.Body = body;
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.SubjectEncoding = System.Text.Encoding.UTF8;
                message.IsBodyHtml = true;
                if (attachments != null)
                {
                    foreach (var attach in attachments)
                    {
                        if (!System.IO.File.Exists(attach))
                        {
                            continue;
                        }
                        // Create  the file attachment for this email message.
                        Attachment data = new Attachment("." + attach);
                        // Add time stamp information for the file.
                        ContentDisposition disposition = data.ContentDisposition;
                        disposition.CreationDate = System.IO.File.GetCreationTime(attach);
                        disposition.ModificationDate = System.IO.File.GetLastWriteTime(attach);
                        disposition.ReadDate = System.IO.File.GetLastAccessTime(attach);
                        // Add the file attachment to this email message.

                        message.Attachments.Add(data);
                    }
                }
                SmtpClient client = new SmtpClient(_configuration["Mail:SMTP"], 587);
                //SmtpClient client = new SmtpClient("mail.pymepharco.com", 993);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                //client.Credentials = new System.Net.NetworkCredential("pymepharco.mail@gmail.com", "xenrezrhmvueqmvw");
                client.Credentials = new System.Net.NetworkCredential(_configuration["Mail:User"], _configuration["Mail:Pass"]);
                client.Send(message);
            }
            catch (Exception ex)
            {
                return new SuccesMail { ex = ex, success = 0 };
            }
            return new SuccesMail { success = 1 };
        }
        public async Task<IActionResult> Logout()
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
            return Redirect("/");
        }

    }
    class SuccesMail
    {
        public int success { get; set; }
        public Exception ex { get; set; }
    }
}
