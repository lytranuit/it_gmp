using it.Areas.Admin.Models;
using it.Data;
using it.Services;
using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Signatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;

namespace it.Areas.Admin.Controllers
{
    public class DocumentController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private readonly ViewRender _view;
        IConfiguration _configuration;

        private string _type = "Document";

        [TempData]
        public string StatusMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        public DocumentController(ItContext context, UserManager<UserModel> UserMgr, ViewRender view, IConfiguration configuration) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
            _view = view;
            _configuration = configuration;
        }

        // GET: Admin/Document
        public async Task<IActionResult> Index()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            ViewData["keyword"] = _context.DocumentUserKeywordModel.Where(d => d.user_id == user_id).OrderByDescending(d => d.keyword).ToList();

            ViewData["type"] = _context.DocumentTypeModel.Where(d => d.deleted_at == null).OrderByDescending(d => d.created_at).ToList();
            return View();
        }
        // GET: Admin/Document/Receive
        public async Task<IActionResult> Receive()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            ViewData["keyword"] = _context.DocumentUserKeywordModel.Where(d => d.user_id == user_id).OrderByDescending(d => d.keyword).ToList();
            ViewData["type"] = _context.DocumentTypeModel.Where(d => d.deleted_at == null).OrderByDescending(d => d.created_at).ToList();
            return View();
        }


        // GET: Admin/Document/wait
        public async Task<IActionResult> wait()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            ViewData["keyword"] = _context.DocumentUserKeywordModel.Where(d => d.user_id == user_id).OrderByDescending(d => d.keyword).ToList();
            ViewData["type"] = _context.DocumentTypeModel.Where(d => d.deleted_at == null).OrderByDescending(d => d.created_at).OrderByDescending(d => d.created_at).ToList();
            return View();
        }

        // GET: Admin/Document/signdoc
        public async Task<IActionResult> signdoc()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            ViewData["keyword"] = _context.DocumentUserKeywordModel.Where(d => d.user_id == user_id).OrderByDescending(d => d.keyword).ToList();
            ViewData["type"] = _context.DocumentTypeModel.Where(d => d.deleted_at == null).OrderByDescending(d => d.created_at).OrderByDescending(d => d.created_at).ToList();
            return View();
        }
        // GET: Admin/Document/signdoc
        public async Task<IActionResult> types(int id)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            ViewData["keyword"] = _context.DocumentUserKeywordModel.Where(d => d.user_id == user_id).OrderByDescending(d => d.keyword).ToList();
            ViewData["type"] = _context.DocumentTypeModel.Where(d => d.deleted_at == null).OrderByDescending(d => d.created_at).OrderByDescending(d => d.created_at).ToList();
            ViewBag.id = id;
            return View();
        }
        // GET: Admin/Document/merge
        public async Task<IActionResult> merge()
        {
            return View();
        }
        // GET: Admin/Document/Create
        public IActionResult Create(string? related, int? type_id)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            ViewData["user_id"] = user_id;
            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "< " + a.Email + " >"
            }).ToList();
            var types = new List<SelectListItem>();
            //var optionGroup = new SelectListGroup() { Name = group.Key };
            var groups = _context.DocumentTypeGroupModel.Where(a => a.deleted_at == null).Include(d => d.types.Where(t => t.deleted_at == null)).OrderBy(d => d.stt).OrderByDescending(d => d.created_at).ToList();
            foreach (var group in groups)
            {
                // Create a SelectListGroup
                var optionGroup = new SelectListGroup() { Name = group.name };
                // Add SelectListItem's
                foreach (var type in group.types)
                {
                    types.Add(new SelectListItem()
                    {
                        Value = type.id.ToString(),
                        Text = type.name,
                        Group = optionGroup
                    });
                }
            }
            types.Insert(0, new SelectListItem()
            {
                Value = "",
                Text = "Chọn loại hồ sơ"
            });
            ViewData["types"] = types;
            ViewData["documents"] = _context.DocumentModel.Where(a => a.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.code + " - " + a.name_vi
            }).ToList();
            List<string> related_arr = new List<string>();
            if (related != null)
            {
                related_arr = related.Split(",").ToList();
                related_arr = _context.DocumentModel.Where(a => related_arr.Contains(a.code)).Select(a => a.id.ToString()).ToList();
            }
            ViewData["related"] = related_arr;
            ViewData["related_doc"] = new DocumentModel();
            if (related_arr.Count > 0)
            {
                var id = Int32.Parse(related_arr[0]);
                ViewData["related_doc"] = _context.DocumentModel.Where(d => d.id == id).Include(d => d.users_signature).FirstOrDefault();
            }
            ViewData["type_id"] = null;
            if (type_id > 0)
            {
                ViewData["type_id"] = type_id;
                ViewData["type"] = _context.DocumentTypeModel.Where(d => d.id == type_id).FirstOrDefault();
            }

            return View();
        }

        // POST: Admin/Document/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(DocumentModel DocumentModel, List<string>? keyword, List<string> users_signature, List<string> users_receive, List<int> documents_related)
        {
            var files = Request.Form.Files;
            var list_file = new List<IFormFile>();
            var list_attachment = new List<IFormFile>();
            foreach (var file in files)
            {
                if (file.Name == "file")
                {
                    list_file.Add(file);
                }
                else if (file.Name == "attachments")
                {
                    list_attachment.Add(file);
                }
            }
            if (list_file == null || list_file.Count == 0)
            {
                return Ok("Require a PDF File");
            }
            if (ModelState.IsValid)
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                var user = await UserManager.GetUserAsync(currentUser);
                DocumentModel.user_id = user_id;
                DocumentModel.created_at = DateTime.Now;
                DocumentModel.status_id = 2;
                DocumentModel.time_signature_previous = DateTime.Now;
                var count_type = _context.DocumentModel.Where(d => d.type_id == DocumentModel.type_id).Count();
                var type = _context.DocumentTypeModel.Where(d => d.id == DocumentModel.type_id).FirstOrDefault();

                DocumentModel.code = type.symbol + "00" + (count_type + 1);
                //if (users_signature != null && users_signature.Count > 0)
                //{
                //    DocumentModel.user_next_signature_id = users_signature[0];
                //}
                _context.Add(DocumentModel);
                _context.SaveChanges();
                //DocumentModel.code = DocumentModel.id.ToString();
                ////ADD FILE
                var file = list_file[0];
                var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                string name = file.FileName;
                string ext = Path.GetExtension(name);
                string mimeType = file.ContentType;

                var newName = timeStamp + "-" + name;
                var pathroot = "private\\documents\\" + DocumentModel.id + "\\";
                bool exists = System.IO.Directory.Exists(pathroot);

                if (!exists)
                    System.IO.Directory.CreateDirectory(pathroot);



                newName = newName.Replace("+", "_");
                newName = newName.Replace("%", "_");
                string filePath = pathroot + newName;
                string url = "/" + pathroot.Replace("\\", "/") + newName;
                DocumentFileModel DocumentFileModel = new DocumentFileModel
                {
                    document_id = DocumentModel.id,
                    ext = ext,
                    url = url,
                    name = name,
                    mimeType = mimeType,
                    created_at = DateTime.Now
                };

                using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileSrteam);
                }
                _context.Add(DocumentFileModel);

                //ADD attachments
                foreach (var attachment in list_attachment)
                {
                    name = attachment.FileName;
                    ext = Path.GetExtension(name);
                    mimeType = attachment.ContentType;

                    newName = timeStamp + "-" + name;

                    newName = newName.Replace("+", "_");
                    newName = newName.Replace("%", "_");
                    filePath = pathroot + newName;
                    url = "/" + pathroot.Replace("\\", "/") + newName;
                    DocumentAttachmentModel DocumentAttachmentModel = new DocumentAttachmentModel
                    {
                        document_id = DocumentModel.id,
                        ext = ext,
                        url = url,
                        name = name,
                        mimeType = mimeType,
                        created_at = DateTime.Now
                    };

                    using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(fileSrteam);
                    }
                    _context.Add(DocumentAttachmentModel);
                }
                _context.SaveChanges();

                //users_signature

                var users_representative = new List<string>();
                if (users_signature != null && users_signature.Count > 0)
                {
                    for (int k = 0; k < users_signature.Count; ++k)
                    {
                        string key = users_signature[k];

                        DocumentSignatureModel DocumentSignatureModel = new DocumentSignatureModel() { document_id = DocumentModel.id, user_id = key, stt = k };

                        var RepresentativeModel = _context.RepresentativeModel
                        .Where(d => d.deleted_at == null && d.user_id == key && DateTime.Now.Date >= d.date_from.Date && DateTime.Now.Date <= d.date_to.Date)
                        .FirstOrDefault();
                        if (RepresentativeModel != null)
                        {
                            DocumentSignatureModel.representative_id = RepresentativeModel.representative_id;
                            users_representative.Add(DocumentSignatureModel.representative_id);
                        }
                        _context.Add(DocumentSignatureModel);
                    }
                    _context.SaveChanges();
                }
                //user follow
                //if (users_follow != null && users_follow.Count > 0)
                //{
                //	foreach (string key in users_follow)
                //	{
                //		DocumentUserFollowModel DocumentUserFollowModel = new DocumentUserFollowModel() { document_id = DocumentModel.id, user_id = key };
                //		_context.Add(DocumentUserFollowModel);
                //	}

                //	_context.SaveChanges();
                //}
                //user receive
                if (users_receive != null && users_receive.Count > 0)
                {
                    foreach (string key in users_receive)
                    {
                        DocumentUserReceiveModel DocumentUserReceiveModel = new DocumentUserReceiveModel() { document_id = DocumentModel.id, user_id = key };
                        _context.Add(DocumentUserReceiveModel);
                    }

                    _context.SaveChanges();
                }
                //realated
                if (documents_related != null && documents_related.Count > 0)
                {
                    foreach (int key in documents_related)
                    {
                        DocumentRelatedModel DocumentRelatedModel = new DocumentRelatedModel() { document_id = DocumentModel.id, document_related_id = key };
                        _context.Add(DocumentRelatedModel);
                    }

                    _context.SaveChanges();
                }

                //CREATE KEYWORD
                if (keyword != null && keyword.Count > 0)
                {
                    foreach (string key in keyword)
                    {
                        DocumentUserKeywordModel DocumentUserKeywordModel = new DocumentUserKeywordModel() { document_id = DocumentModel.id, user_id = user_id, keyword = key };
                        _context.Add(DocumentUserKeywordModel);
                    }

                    _context.SaveChanges();
                }

                ///UPDATE USER SIGN NEXT
                if (DocumentModel.is_sign_parellel == true)
                {
                    var users_signature_list = _context.DocumentSignatureModel.Where(u => u.status == 1 && u.document_id == DocumentModel.id && u.user_id != DocumentModel.user_id).Include(d => d.user).ToList();
                    //SEND MAIL
                    if (users_signature_list.Count() > 0)
                    {
                        var mail_list = users_signature_list.Select(u => u.user.Email).ToArray();
                        var mail_string = string.Join(",", mail_list);
                        string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                        var body = _view.Render("Emails/WaitSignDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel.id });
                        var attach = new List<string>()
                        {
                            DocumentFileModel.url
                        };
                        var email = new EmailModel
                        {
                            email_to = mail_string,
                            subject = "[Đang chờ ký] " + DocumentModel.name_vi,
                            body = body,
                            email_type = "wait_sign_document",
                            status = 1,
                            data_attachments = attach
                        };
                        _context.Add(email);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    var user_signature = _context.DocumentSignatureModel.OrderBy(u => u.stt).Where(u => u.status == 1 && u.document_id == DocumentModel.id).Include(d => d.user).Include(d => d.representative).FirstOrDefault();
                    string? user_signature_id = null;
                    string? user_representative_id = null;

                    if (user_signature != null)
                    {
                        user_signature_id = user_signature.user_id;
                        user_representative_id = user_signature.representative_id;
                    }
                    else
                    {
                        DocumentModel.deleted_at = DateTime.Now; // Ko ai ký nữa thì xóa
                    }
                    if ((DocumentModel.user_next_signature_id != user_signature_id || DocumentModel.user_next_representative_id != user_representative_id) && DocumentModel.status_id == 2)
                    {
                        DocumentModel.user_next_signature_id = user_signature_id;
                        DocumentModel.user_next_representative_id = user_representative_id;
                        _context.Update(DocumentModel);
                        _context.SaveChanges();
                        //SEND MAIL
                        if (user_signature != null)
                        {
                            var mail_string = user_signature.representative != null ? user_signature.representative.Email : user_signature.user.Email;
                            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                            var body = _view.Render("Emails/WaitSignDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel.id });
                            var attach = new List<string>()
                        {
                            DocumentFileModel.url
                        };
                            var email = new EmailModel
                            {
                                email_to = mail_string,
                                subject = "[Đang chờ ký] " + DocumentModel.name_vi,
                                body = body,
                                email_type = "wait_sign_document",
                                status = 1,
                                data_attachments = attach
                            };
                            _context.Add(email);
                            await _context.SaveChangesAsync();
                        }

                    }


                }
                /////create event
                DocumentEventModel DocumentEventModel = new DocumentEventModel
                {
                    document_id = DocumentModel.id,
                    event_content = "<b>" + user.FullName + "</b> tạo hồ sơ mới",
                    created_at = DateTime.Now,
                };
                _context.Add(DocumentEventModel);



                List<string> users_related = new List<string>();
                //users_related.AddRange(users_follow);
                users_related.Add(DocumentModel.user_id);
                users_related.AddRange(users_signature);
                users_related.AddRange(users_representative);
                users_related.AddRange(users_receive);
                users_related = users_related.Distinct().ToList();
                var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
                users_related.Remove(itemToRemove);
                var items = new List<DocumentUserUnreadModel>();
                foreach (string u in users_related)
                {
                    items.Add(new DocumentUserUnreadModel
                    {
                        user_id = u,
                        document_id = DocumentModel.id,
                        time = DateTime.Now,
                    });
                }
                _context.AddRange(items);
                await _context.SaveChangesAsync();
                //SEND MAIL
                //var mail_list = _context.UserModel.Where(u => users_related.Contains(u.Id)).Select(u => u.Email).ToArray();
                //if (mail_list.Length > 0)
                //{
                //    var mail_string = string.Join(",", mail_list);
                //    string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                //    var body = _view.Render("Emails/CreateDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel.id });
                //    var email = new EmailModel
                //    {
                //        email_to = mail_string,
                //        subject = "[Hồ sơ mới] " + DocumentModel.name_vi,
                //        body = body,
                //        email_type = "new_document",
                //        status = 1
                //    };
                //    _context.Add(email);
                //    await _context.SaveChangesAsync();
                //}
                //SendMail(mail_string, "Pymepharco - Hồ sơ mới", body);

                /// Audittrail
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Create.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã tạo một hồ sơ mới.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(SuggestSign), new { id = DocumentModel.id });
            }

            string messages = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
            return Ok(messages);
        }

        // GET: Admin/Document/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id
            var user_current = await UserManager.GetUserAsync(currentUser); // Get user id:
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            var is_admin = await UserManager.IsInRoleAsync(user_current, "Administrator");
            ViewData["user_id"] = user_id;
            ViewData["is_admin"] = Convert.ToInt32(is_admin);
            if (id == null || _context.DocumentModel == null)
            {
                return NotFound();
            }
            if (checkPermission("edit", id, is_admin, is_manager) != 0)
            {
                return NotFound();
            }

            var DocumentModel = await _context.DocumentModel
                .Where(d => d.id == id)
                .Include(d => d.type)
                .Include(d => d.files)
                .Include(d => d.attachments)
                .Include(d => d.related)
                //.Include(d => d.users_follow)
                .Include(d => d.users_receive)
                .Include(d => d.users_signature.OrderBy(u => u.stt))
                .ThenInclude(u => u.user)
                .Include(d => d.users_signature)
                .ThenInclude(u => u.representative)
                .FirstAsync();

            //return Ok(DocumentModel.users_signature);
            if (DocumentModel == null)
            {
                return NotFound();
            }


            ///USER
            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "< " + a.Email + " >"
            }).ToList();
            //types
            var types = new List<SelectListItem>();
            //var optionGroup = new SelectListGroup() { Name = group.Key };
            var groups = _context.DocumentTypeGroupModel.Where(a => a.deleted_at == null).Include(d => d.types).OrderBy(d => d.stt).OrderByDescending(d => d.created_at).ToList();
            foreach (var group in groups)
            {
                // Create a SelectListGroup
                var optionGroup = new SelectListGroup() { Name = group.name };
                // Add SelectListItem's
                foreach (var type in group.types)
                {
                    types.Add(new SelectListItem()
                    {
                        Value = type.id.ToString(),
                        Text = type.name,
                        Group = optionGroup
                    });
                }
            }
            types.Insert(0, new SelectListItem()
            {
                Value = "",
                Text = "Chọn loại hồ sơ"
            });
            ViewData["types"] = types;

            ViewData["documents"] = _context.DocumentModel.Where(a => a.deleted_at == null && a.id != id).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.code + " - " + a.name_vi
            }).ToList();

            DocumentModel.users_signature = DocumentModel.users_signature.OrderBy(u => u.stt).ToList();
            var keyword = _context.DocumentUserKeywordModel.Where(d => d.user_id == user_id && d.document_id == id).Select(d => d.keyword).ToList();
            ViewData["keyword"] = keyword;
            ViewData["type"] = _context.DocumentTypeModel.Where(d => d.id == DocumentModel.type_id).FirstOrDefault();
            var is_manager_document = false;
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Contains(DocumentModel.type_id) == true)
                {
                    is_manager_document = true;
                }
            }
            if (is_admin)
            {
                is_manager_document = true;
            }
            ViewData["is_manager_document"] = is_manager_document;
            //types
            //ViewData["users"] = _context.DocumentTypeModel.Where(a => a.deleted_at == null).Select(a => new SelectListItem()
            //{
            //    Value = a.id.ToString(),
            //    Text = a.name
            //}).ToList();
            return View(DocumentModel);
        }

        // POST: Admin/Document/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, DocumentModel DocumentModel, List<string> users_receive, Dictionary<string, DocumentSignatureModel> dic_users_signature, List<int> documents_related, List<int> delete_attach, List<string>? keyword)
        {
            var files = Request.Form.Files;
            var list_attachment = new List<IFormFile>();
            foreach (var file in files)
            {
                if (file.Name == "attachments")
                {
                    list_attachment.Add(file);
                }
            }
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user = await UserManager.GetUserAsync(currentUser);

            var is_manager = await UserManager.IsInRoleAsync(user, "Manager Esign");
            var is_admin = await UserManager.IsInRoleAsync(user, "Administrator");
            //return Ok(dic_users_signature);
            if (id != DocumentModel.id)
            {
                return NotFound();
            }
            if (checkPermission("edit", DocumentModel.id, is_admin, is_manager) != 0)
            {
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                var DocumentModel_old = _context.DocumentModel.Where(d => d.id == id).Include(d => d.files).Include(d => d.attachments).FirstOrDefault();
                if (DocumentModel_old == null)
                    return NotFound();
                DocumentModel_old.updated_at = DateTime.Now;
                //return Ok(DocumentModel);
                foreach (string key in HttpContext.Request.Form.Keys)
                {
                    var prop = DocumentModel_old.GetType().GetProperty(key);
                    var prop_new = DocumentModel.GetType().GetProperty(key);
                    //if (key == "keyword")
                    //{
                    //    var type1 = "";
                    //}
                    if (prop != null)
                    {
                        string temp = Request.Form[key].FirstOrDefault();
                        var value = prop.GetValue(DocumentModel_old, null);
                        var value_new = prop.GetValue(DocumentModel, null);
                        if (value == null && value_new == null)
                            continue;

                        var type = value != null ? value.GetType() : value_new.GetType();


                        if (type == typeof(int))
                        {
                            int val = Int32.Parse(temp);
                            prop.SetValue(DocumentModel_old, val);
                        }
                        else if (type == typeof(Boolean))
                        {
                            Boolean val = Boolean.Parse(temp);
                            prop.SetValue(DocumentModel_old, val);
                        }
                        else if (type == typeof(string))
                        {
                            prop.SetValue(DocumentModel_old, temp);
                        }
                        else if (type == typeof(decimal))
                        {
                            decimal val = decimal.Parse(temp);
                            prop.SetValue(DocumentModel_old, temp);
                        }
                        else if (type == typeof(DateTime))
                        {
                            if (string.IsNullOrEmpty(temp))
                            {
                                prop.SetValue(DocumentModel_old, null);
                            }
                            else
                            {
                                DateTime.TryParse(temp, out DateTime val);
                                prop.SetValue(DocumentModel_old, val);
                            }
                        }
                    }
                }
                //return Ok(DocumentModel_old);
                _context.Update(DocumentModel_old);
                _context.SaveChanges();

                //Files
                /////XÓA và edit lại
                //var files_old = _context.DocumentFileModel.Where(d => d.document_id == id).ToList();
                //files_old.ForEach(a => a.document_id = 0);
                //await _context.SaveChangesAsync();

                //if (files != null && files.Count > 0)
                //{
                //    //var items = new List<DocumentFileModel>();
                //    foreach (int key in files)
                //    {
                //        DocumentFileModel DocumentFileModel = _context.DocumentFileModel.Find(key);
                //        DocumentFileModel.document_id = DocumentModel.id;
                //        _context.Update(DocumentFileModel);
                //    }
                //    await _context.SaveChangesAsync();
                //}
                //ADD attachments
                foreach (var attachment in list_attachment)
                {
                    string name = attachment.FileName;
                    string ext = Path.GetExtension(name);
                    string mimeType = attachment.ContentType;

                    var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                    var pathroot = "private\\documents\\" + DocumentModel_old.id + "\\";
                    string newName = timeStamp + "-" + name;

                    newName = newName.Replace("+", "_");
                    newName = newName.Replace("%", "_");
                    string filePath = pathroot + newName;
                    string url = "/" + pathroot.Replace("\\", "/") + newName;
                    DocumentAttachmentModel DocumentAttachmentModel = new DocumentAttachmentModel
                    {
                        document_id = DocumentModel_old.id,
                        ext = ext,
                        url = url,
                        name = name,
                        mimeType = mimeType,
                        created_at = DateTime.Now
                    };

                    using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(fileSrteam);
                    }
                    _context.Add(DocumentAttachmentModel);
                }
                _context.SaveChanges();
                ///DELETE Attachment
                if (delete_attach != null && delete_attach.Count > 0)
                {
                    var attach_delete = _context.DocumentAttachmentModel.Where(d => delete_attach.Contains(d.id)).ToList();
                    _context.RemoveRange(attach_delete);
                    _context.SaveChanges();
                }

                //user_receive
                /////XÓA và edit lại
                var users_receive_old = _context.DocumentUserReceiveModel.Where(d => d.document_id == id).ToList();
                _context.RemoveRange(users_receive_old);
                await _context.SaveChangesAsync();
                if (users_receive != null && users_receive.Count > 0)
                {
                    foreach (string key in users_receive)
                    {
                        DocumentUserReceiveModel DocumentUserReceiveModel = new DocumentUserReceiveModel() { document_id = id, user_id = key };
                        _context.Add(DocumentUserReceiveModel);
                    }

                    await _context.SaveChangesAsync();
                }
                //document_related
                /////XÓA và edit lại
                var documents_related_old = _context.DocumentRelatedModel.Where(d => d.document_id == id).ToList();
                _context.RemoveRange(documents_related_old);
                await _context.SaveChangesAsync();
                if (documents_related != null && documents_related.Count > 0)
                {
                    foreach (int key in documents_related)
                    {
                        DocumentRelatedModel DocumentRelatedModel = new DocumentRelatedModel() { document_id = id, document_related_id = key };
                        _context.Add(DocumentRelatedModel);
                    }

                    await _context.SaveChangesAsync();
                }

                //users signature
                var users_representative = new List<string>();
                var users_signature_old = _context.DocumentSignatureModel.Where(d => d.document_id == id).Select(a => a.id).ToList();

                //if (users_signature.Contains("name_vi"))
                //{
                //    users_signature = new List<int>();
                //}
                List<DocumentSignatureModel> users_signature_val = dic_users_signature.Values.ToList();


                List<int> users_signature = users_signature_val.Select(d => d.id).ToList();
                var users_signature_val_user = users_signature_val.Select(d => d.user_id).ToList();
                IEnumerable<int> list_delete = users_signature_old.Except(users_signature).Where(d => d > 0);


                if (list_delete != null)
                {
                    foreach (int key in list_delete)
                    {
                        DocumentSignatureModel DocumentSignatureModel = _context.DocumentSignatureModel.Where(d => d.id == key).First();
                        _context.Remove(DocumentSignatureModel);
                    }

                    await _context.SaveChangesAsync();
                }

                if (users_signature_val.Count() > 0)
                {
                    for (int key = 0; key < users_signature_val.Count; ++key)
                    {
                        var sign = users_signature_val[key];
                        sign.document_id = id;
                        if (sign.id > 0)
                        {
                            DocumentSignatureModel DocumentSignatureModel = _context.DocumentSignatureModel.Where(d => d.id == sign.id).First();
                            DocumentSignatureModel.stt = key;
                            _context.Update(DocumentSignatureModel);
                        }
                        else
                        {
                            _context.Add(sign);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                if (keyword != null)
                {
                    var users_keyword_old = _context.DocumentUserKeywordModel.Where(d => d.document_id == id && d.user_id == user_id).Select(a => a.keyword).ToList();
                    var list_delete1 = users_keyword_old.Except(keyword);
                    var list_add1 = keyword.Except(users_keyword_old);
                    if (list_add1 != null)
                    {
                        foreach (string key in list_add1)
                        {
                            DocumentUserKeywordModel DocumentUserKeywordModel = new DocumentUserKeywordModel() { document_id = id, user_id = user_id, keyword = key };
                            _context.Add(DocumentUserKeywordModel);
                        }
                    }
                    if (list_delete1 != null)
                    {
                        foreach (string key in list_delete1)
                        {
                            var DocumentUserKeywordModel = _context.DocumentUserKeywordModel.Where(d => d.document_id == id && d.user_id == user_id && d.keyword == key).First();
                            _context.Remove(DocumentUserKeywordModel);
                        }
                    }
                    _context.SaveChanges();
                }
                else
                {
                    var users_keyword_old = _context.DocumentUserKeywordModel.Where(d => d.document_id == id && d.user_id == user_id).ToList();
                    _context.RemoveRange(users_keyword_old);
                    _context.SaveChanges();
                }
                ///UPDATE USER SIGN NEXT
                if (DocumentModel_old.is_sign_parellel == true)
                {
                    var users_signature_list = _context.DocumentSignatureModel.Where(u => u.status == 1 && u.document_id == DocumentModel_old.id && u.user_id != DocumentModel_old.user_id).Include(d => d.user).ToList();
                    //SEND MAIL
                    if (users_signature_list.Count() > 0)
                    {
                        var mail_list = users_signature_list.Select(u => u.user.Email).ToArray();
                        var mail_string = string.Join(",", mail_list);
                        string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                        var body = _view.Render("Emails/WaitSignDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel_old.id });
                        var file_sign = _context.DocumentFileModel.Where(d => d.document_id == DocumentModel_old.id).OrderBy(d => d.id).LastOrDefault();
                        var attach = new List<string>();
                        if (file_sign != null)
                        {
                            attach.Add(file_sign.url);
                        };
                        var email = new EmailModel
                        {
                            email_to = mail_string,
                            subject = "[Đang chờ ký] " + DocumentModel_old.name_vi,
                            body = body,
                            email_type = "wait_sign_document",
                            status = 1,
                            data_attachments = attach
                        };
                        _context.Add(email);
                        DocumentModel_old.status_id = 2; // Co nguoi ky thi trinh ky tiep
                    }
                    else
                    {
                        if (DocumentModel_old.status_id == 2)
                        {
                            DocumentModel_old.status_id = 4; // Ko ai ký nữa thì hoàn thành
                            DocumentModel_old.date_finish = DateTime.Now;
                        }
                    }
                    _context.Update(DocumentModel_old);
                    await _context.SaveChangesAsync();
                }
                else
                {


                    var user_signature = _context.DocumentSignatureModel.OrderBy(u => u.stt).Where(u => u.status == 1 && u.document_id == id).Include(d => d.user).Include(d => d.representative).FirstOrDefault();
                    string? user_signature_id = null;
                    string? user_representative_id = null;

                    if (user_signature != null)
                    {
                        user_signature_id = user_signature.user_id;
                        user_representative_id = user_signature.representative_id;
                        DocumentModel_old.status_id = 2; // Co nguoi ky thi trinh ky tiep
                    }
                    else
                    {
                        if (DocumentModel_old.status_id == 2)
                        {
                            DocumentModel_old.status_id = 4; // Ko ai ký nữa thì hoàn thành
                            DocumentModel_old.date_finish = DateTime.Now;
                        }
                    }
                    if ((DocumentModel_old.user_next_signature_id != user_signature_id || DocumentModel_old.user_next_representative_id != user_representative_id) && DocumentModel_old.status_id == 2)
                    {
                        DocumentModel_old.user_next_signature_id = user_signature_id;
                        DocumentModel_old.user_next_representative_id = user_representative_id;
                        _context.Update(DocumentModel_old);

                        //SEND MAIL
                        if (user_signature != null)
                        {
                            var file_sign = _context.DocumentFileModel.Where(d => d.document_id == DocumentModel_old.id).OrderBy(d => d.id).LastOrDefault();
                            var attach = new List<string>();
                            if (file_sign != null)
                            {
                                attach.Add(file_sign.url);
                            };
                            var mail_string = user_signature.representative != null ? user_signature.representative.Email : user_signature.user.Email;
                            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                            var body = _view.Render("Emails/WaitSignDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel_old.id });
                            var email = new EmailModel
                            {
                                email_to = mail_string,
                                subject = "[Đang chờ ký] " + DocumentModel_old.name_vi,
                                body = body,
                                email_type = "wait_sign_document",
                                status = 1,
                                data_attachments = attach
                            };
                            _context.Add(email);
                        }
                        await _context.SaveChangesAsync();
                    }

                }

                /////Nêu Hiện hành thì gửi mail && Superseded document related
                if (DocumentModel_old.status_id == (int)DocumentStatus.Current)
                {
                    var document_related_id_list = _context.DocumentRelatedModel.Where(d => d.document_id == DocumentModel_old.id).Select(d => d.document_related_id).ToList();

                    var document_related = _context.DocumentModel.Where(d => document_related_id_list.Contains(d.id) && d.status_id == (int)DocumentStatus.Current).ToList();
                    document_related.ForEach(a => a.status_id = (int)DocumentStatus.Obsoleted);
                    _context.UpdateRange(document_related);
                    await _context.SaveChangesAsync();



                    var user_receive = _context.DocumentUserReceiveModel.Where(u => u.document_id == DocumentModel_old.id).Include(d => d.user).Select(d => d.user.Email).ToList();
                    user_receive = user_receive.Distinct().ToList();
                    ////Only for tranning.
                    var file_current = DocumentModel_old.files.OrderBy(f => f.created_at).LastOrDefault();
                    if (user_receive.Count() > 0)
                    {
                        var mail_string_new = string.Join(",", user_receive.ToArray());
                        string Domain_2 = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                        var body_new = _view.Render("Emails/CurrentDocument", new { link_logo = Domain_2 + "/images/clientlogo_astahealthcare.com_f1800.png", title = DocumentModel_old.name_vi, date_effect = DocumentModel_old.date_effect, note = DocumentModel_old.description_vi });

                        //string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                        var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                        var pathroot = "wwwroot\\temp";
                        bool exists = System.IO.Directory.Exists(pathroot);

                        if (!exists)
                            System.IO.Directory.CreateDirectory(pathroot);

                        string filePath = "/wwwroot/temp/" + timeStamp + ".pdf";

                        // Create a FileStream to write the PDF file
                        var dest = new PdfWriter("." + filePath);
                        var reader = new PdfReader(file_current.url.Replace("/private/", _configuration["Source:Path_Private"] + "\\").Replace("/", "\\"));
                        PdfDocument pdfDoc = new PdfDocument(reader, dest);
                        iText.Layout.Document doc = new iText.Layout.Document(pdfDoc);
                        pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new ForTranningEventHandler(doc));
                        doc.Close();

                        /////Attachment thêm vào mail
                        var attachments_file = DocumentModel_old.attachments.Select(f => f.url).ToList();

                        var attachments = new List<string>();
                        attachments.Add(filePath);
                        attachments.AddRange(attachments_file);
                        var email_new = new EmailModel
                        {
                            email_to = mail_string_new,
                            subject = "Thông báo hiệu lực " + DocumentModel_old.name_vi,
                            body = body_new,
                            email_type = "curent_document",
                            status = 1,
                            data_attachments = attachments
                        };
                        _context.Add(email_new);
                    }

                    /////Copy qua thư viện
                    try
                    {

                        var path_files = DocumentModel_old.location;
                        string _userName = _configuration["FileServer:User"];
                        string _password = _configuration["FileServer:Pass"];
                        using (new NetworkConnection.NetworkConnection(path_files, new NetworkCredential(_userName, _password)))
                        {
                            if (System.IO.Directory.Exists(path_files))
                            {
                                string fileToCopy = file_current.url.Replace("/private/", _configuration["Source:Path_Private"] + "\\").Replace("/", "\\");
                                System.IO.File.Copy(fileToCopy, path_files + "\\" + DocumentModel_old.name_vi + ".pdf", true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                /////create event
                DocumentEventModel DocumentEventModel = new DocumentEventModel
                {
                    document_id = DocumentModel.id,
                    event_content = "<b>" + user.FullName + "</b> cập nhật hồ sơ mới",
                    created_at = DateTime.Now,
                };
                _context.Add(DocumentEventModel);


                ///Create unread
                List<string> users_related = new List<string>();
                //users_related.AddRange(users_follow);
                var users_signature_string = users_signature_val.Select(d => d.user_id).ToList();
                users_related.Add(DocumentModel_old.user_id);
                users_related.AddRange(users_signature_string);
                users_related.AddRange(users_receive);
                users_related = users_related.Distinct().ToList();
                var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
                users_related.Remove(itemToRemove);
                var items = new List<DocumentUserUnreadModel>();
                foreach (string u in users_related)
                {
                    items.Add(new DocumentUserUnreadModel
                    {
                        user_id = u,
                        document_id = DocumentModel.id,
                        time = DateTime.Now,
                    });
                }
                _context.AddRange(items);
                //await _context.SaveChangesAsync();
                StatusMessage = "Cập nhật thành công!";
                /// Audittrail
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Update.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã chỉnh sửa hồ sơ.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Edit));
            }
            return Ok();
        }
        [HttpPost]
        public async Task<JsonResult> saverecieve(int id, List<string> users_receive)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user = await UserManager.GetUserAsync(currentUser);
            var DocumentModel_old = await _context.DocumentModel.FindAsync(id);


            //user_receive
            /////XÓA và edit lại
            ///var users_signature_old = _context.DocumentSignatureModel.Where(d => d.document_id == id).Select(a => a.user_id).ToList();


            var users_receive_old = _context.DocumentUserReceiveModel.Where(d => d.document_id == id).Select(a => a.user_id).ToList();
            IEnumerable<string> list_delete = users_receive_old.Except(users_receive);
            IEnumerable<string> list_add = users_receive.Except(users_receive_old);
            //_context.RemoveRange(users_receive_old);
            //_context.SaveChanges();
            if (list_add != null)
            {
                foreach (string key in list_add)
                {
                    DocumentUserReceiveModel DocumentUserReceiveModel = new DocumentUserReceiveModel() { document_id = id, user_id = key };
                    _context.Add(DocumentUserReceiveModel);
                }
            }
            if (list_delete != null)
            {
                foreach (string key in list_delete)
                {
                    var DocumentUserReceiveModel = _context.DocumentUserReceiveModel.Where(d => d.document_id == id && d.user_id == key).First();
                    _context.Remove(DocumentUserReceiveModel);
                }
            }
            _context.SaveChanges();


            /////create event
            DocumentEventModel DocumentEventModel = new DocumentEventModel
            {
                document_id = DocumentModel_old.id,
                event_content = "<b>" + user.FullName + "</b> cập nhật người nhận",
                created_at = DateTime.Now,
            };
            _context.Add(DocumentEventModel);


            ///Create unread
            List<string> users_related = new List<string>();
            users_related.AddRange(users_receive);
            users_related = users_related.Distinct().ToList();
            var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
            users_related.Remove(itemToRemove);
            var items = new List<DocumentUserUnreadModel>();
            foreach (string u in users_related)
            {
                items.Add(new DocumentUserUnreadModel
                {
                    user_id = u,
                    document_id = DocumentModel_old.id,
                    time = DateTime.Now,
                });
            }
            _context.AddRange(items);
            //await _context.SaveChangesAsync();

            /// Audittrail
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Update.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã cập nhật người nhận.";
            _context.Add(audit);
            await _context.SaveChangesAsync();
            return Json(new { success = 1 });
        }
        [HttpPost]
        public async Task<JsonResult> savekeyword(int document_id, List<string> keyword)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user = await UserManager.GetUserAsync(currentUser);




            var users_keyword_old = _context.DocumentUserKeywordModel.Where(d => d.document_id == document_id && d.user_id == user_id).Select(a => a.keyword).ToList();
            IEnumerable<string> list_delete = users_keyword_old.Except(keyword);
            IEnumerable<string> list_add = keyword.Except(users_keyword_old);
            if (list_add != null)
            {
                foreach (string key in list_add)
                {
                    DocumentUserKeywordModel DocumentUserKeywordModel = new DocumentUserKeywordModel() { document_id = document_id, user_id = user_id, keyword = key };
                    _context.Add(DocumentUserKeywordModel);
                }
            }
            if (list_delete != null)
            {
                foreach (string key in list_delete)
                {
                    var DocumentUserKeywordModel = _context.DocumentUserKeywordModel.Where(d => d.document_id == document_id && d.user_id == user_id && d.keyword == key).First();
                    _context.Remove(DocumentUserKeywordModel);
                }
            }
            _context.SaveChanges();
            return Json(new { success = 1 });
        }
        public async Task<JsonResult> processedDocument(int id)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user = await UserManager.GetUserAsync(currentUser);
            var DocumentModel_old = await _context.DocumentModel.FindAsync(id);
            DocumentModel_old.status_id = (int)DocumentStatus.Processed;
            _context.Update(DocumentModel_old);
            _context.SaveChanges();
            return Json(new { success = 1 });
        }



        // GET: Admin/Document/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.DocumentModel == null)
            {
                return Problem("Entity set 'ItContext.DocumentModel'  is null.");
            }
            if (checkPermission("delete", id) != 0)
            {
                return NotFound();
            }

            var DocumentModel = await _context.DocumentModel.FindAsync(id);
            if (DocumentModel != null)
            {
                DocumentModel.deleted_at = DateTime.Now;
                _context.DocumentModel.Update(DocumentModel);
            }

            _context.SaveChanges();

            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Delete.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã xóa hồ sơ.";
            _context.Add(audit);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<JsonResult> Table()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var search_type = Request.Form["search_type"].FirstOrDefault();
            var search_priority = Int32.Parse(Request.Form["search_priority"].FirstOrDefault("0"));
            var search_amount_type = Int32.Parse(Request.Form["search_amount_type"].FirstOrDefault("0"));
            var search_status = Int32.Parse(Request.Form["search_status"].FirstOrDefault("0"));
            var search_branch = Int32.Parse(Request.Form["search_branch"].FirstOrDefault("0"));
            var search_type_option = Int32.Parse(Request.Form["search_type_option"].FirstOrDefault("0"));
            var search_keyword_option = Request.Form["search_keyword_option"].FirstOrDefault("");
            var filter_type = Int32.Parse(Request.Form["filter_type"].FirstOrDefault("0"));
            var filter_cancle = Boolean.Parse(Request.Form["filter_cancle"].FirstOrDefault("false"));
            var search_date_range = Request.Form["search_date"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var customerData = (from tempcustomer in _context.DocumentModel select tempcustomer);
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser); // Get user id:
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            var is_admin = await UserManager.IsInRoleAsync(user_current, "Administrator");
            List<int> documents_unread = _context.DocumentUserUnreadModel.Where(d => d.user_id == user_id).Select(d => d.document_id).Distinct().ToList();

            ////
            if (type == "receive")
            {
                var document_receive = _context.DocumentUserReceiveModel.Where(d => d.user_id == user_id).Select(d => d.document_id).ToList();
                customerData = customerData.Where(d => document_receive.Contains(d.id));
            }
            else if (type == "wait")
            {
                var data2 = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id == 2 && d.is_sign_parellel == true).Join(_context.DocumentSignatureModel, d => d.id, ds => ds.document_id, (d, ds) => new
                {
                    document = d,
                    status = ds.status,
                    user_id = ds.user_id,
                }).Distinct().Where(d => d.status == 1 && d.user_id == user_id).Select(d => d.document.id).ToList();
                Console.WriteLine(data2.ToString());
                customerData = customerData.Where(d => (d.user_next_signature_id == user_id && d.status_id == 2 && d.is_sign_parellel == false) || data2.Contains(d.id));
            }
            else if (type == "send")
            {
                customerData = customerData.Where(d => d.user_id == user_id);
            }
            else if (type == "follow")
            {
                var document_follow = _context.DocumentUserFollowModel.Where(d => d.user_id == user_id).Select(d => d.document_id).ToList();
                customerData = customerData.Where(d => document_follow.Contains(d.id));
            }
            else if (type == "signdoc")
            {
                var document_sign = _context.DocumentSignatureModel.Where(d => d.status == 2 && (d.user_sign == user_id || d.user_id == user_id)).Select(d => d.document_id).ToList();
                customerData = customerData.Where(d => document_sign.Contains(d.id));
            }
            else if (type == "types" && filter_type > 0)
            {
                customerData = customerData.Where(d => d.type_id == filter_type);
            }
            customerData = customerData.Where(m => m.deleted_at == null);
            int recordsTotal = customerData.Count();

            if (filter_cancle == true && search_type != "status")
            {
                customerData = customerData.Where(m => m.status_id != 3);
            }

            ////
            if (search_type == null && !string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.code.Contains(searchValue) || m.name_vi.Contains(searchValue));
            }
            else if (search_type == null && string.IsNullOrEmpty(searchValue))
            {

            }
            else if (search_type == "status" && search_status != 0)
            {

                customerData = customerData.Where(m => m.status_id == search_status);
            }
            else if (search_type == "priority" && search_priority != 0)
            {
                customerData = customerData.Where(m => m.priority == search_priority);
            }
            else if (search_type == "type" && search_type_option != 0)
            {
                customerData = customerData.Where(m => m.type_id == search_type_option);
            }
            else if (search_type == "keyword" && search_keyword_option != "")
            {
                var document_list = _context.DocumentUserKeywordModel.Where(d => d.keyword == search_keyword_option && d.user_id == user_id).Select(d => d.document_id).ToList();
                customerData = customerData.Where(m => document_list.Contains(m.id));
            }
            else if (search_type == "code" && !string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.code.Contains(searchValue));
            }
            else if (search_type == "name_vi" && !string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.name_vi.Contains(searchValue));
            }
            else if (search_type == "created_by" && !string.IsNullOrEmpty(searchValue))
            {
                var user_list = _context.UserModel.Where(d => d.FullName.Contains(searchValue)).Select(d => d.Id).ToList();
                customerData = customerData.Where(m => user_list.Contains(m.user_id));
            }
            else if (search_type == "created_at" && !string.IsNullOrEmpty(search_date_range))
            {
                var explode = search_date_range.Split(" - ");
                if (explode.Length > 1)
                {
                    DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);
                    DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);

                    customerData = customerData.Where(m => m.created_at != null && m.created_at.Value.Date >= start_date.Date && m.created_at.Value.Date <= end_date.Date);
                }
            }
            else if (search_type == "date_finish" && !string.IsNullOrEmpty(search_date_range))
            {
                var explode = search_date_range.Split(" - ");
                if (explode.Length > 1)
                {
                    DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);
                    DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);

                    customerData = customerData.Where(m => m.date_finish != null && m.date_finish.Value.Date >= start_date.Date && m.date_finish.Value.Date <= end_date.Date);
                }
            }
            else if (search_type == "unread")
            {
                customerData = customerData.Where(d => documents_unread.Contains(d.id));
            }
            /////
            customerData = customerData
                .Include(d => d.type)
                //.Include(d => d.users_follow)
                .Include(d => d.users_signature.OrderBy(u => u.stt))
                .ThenInclude(u => u.user)
                .Include(d => d.user);
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var users_signature = record.users_signature.OrderBy(u => u.stt).ToList();
                //var users_follow = record.users_follow.Select(d => d.user_id).ToList();
                var users_follow = new List<string>();
                users_follow.Add(record.user_id);
                var group_id = record.type != null ? record.type.group_id : 0;
                if (is_admin)
                {
                    users_follow.Add(user_id);
                }
                var html_user_signature = "<div class='img-group'>";
                var html_status = "";
                var count_sign = 0;
                bool has_us = false;
                bool is_sign = false;
                bool is_cancle = false;
                var status_id = record.status_id;
                var us = new DocumentSignatureModel();
                foreach (var user_signature in users_signature)
                {
                    if (user_signature.user == null)
                    {
                        continue;
                    }
                    if (user_signature.status == 2)
                    {
                        count_sign++;
                        is_sign = true;
                    }
                    if (!has_us && user_signature.status == 1)
                    {
                        us = user_signature;
                        has_us = true;
                    }
                    var user = user_signature.user;
                    html_user_signature += "<div class='user-avatar user-avatar-group' title='" + user.FullName + "'><img class='rounded-circle' width='30px' src='" + user.image_url + "' /></div>";
                }
                bool is_success = false;
                if (count_sign == users_signature.Count && count_sign > 0)
                {
                    is_success = true;
                }

                if (status_id == (int)DocumentStatus.Draft)
                {
                    html_status = "<button class='btn btn-info btn-sm'>Tạo mới</button>";
                }
                else if (status_id == (int)DocumentStatus.Cancle)
                {
                    html_status = "<button class='btn btn-danger btn-sm text-white'>Đã hủy</button>";
                }
                else if (status_id == (int)DocumentStatus.Processed)
                {
                    html_status = "<button class='btn btn-secondary btn-sm text-white'>Đã xử lý</button>";
                }
                else if (status_id == (int)DocumentStatus.Current)
                {
                    html_status = "<button class='btn btn-secondary btn-sm text-white'>Hiện hành</button>";
                }
                else if (status_id == (int)DocumentStatus.Obsoleted)
                {
                    html_status = "<button class='btn btn-danger btn-sm text-white'>Obsoleted</button>";
                }
                else if (status_id == (int)DocumentStatus.Superseded)
                {
                    html_status = "<button class='btn btn-danger btn-sm text-white'>Superseded</button>";
                }
                else if (status_id == (int)DocumentStatus.Success || is_success)
                {
                    html_status = "<button class='btn btn-success btn-sm text-white'>Đã ký xong</button>";
                }
                else if (!is_sign)
                {
                    html_status = "<button class='btn btn-warning btn-sm'>Trình ký (Chưa ký) </button>";
                }
                else
                {
                    html_status = "<button class='btn btn-primary btn-sm' title='Chờ " + us.user.FullName + " ký'>Đã ký " + count_sign + "/" + users_signature.Count + "<br></button>";
                }

                html_user_signature += "</div>";
                var html_priority = "";
                switch (record.priority)
                {
                    case 1:
                        html_priority = "<i class='far fa-star'></i>";
                        break;
                    case 2:
                        html_priority = "<i class='fas fa-star'></i>";
                        break;
                    case 3:
                        html_priority = "<i class='fas fa-star text-warning'></i>";
                        break;
                    case 4:
                        html_priority = "<i class='fas fa-star text-danger'></i>";
                        break;
                    default:
                        html_priority = "<i class='fas fa-star'></i>";
                        break;
                }
                ////
                DocumentUserReadModel user_read = _context.DocumentUserReadModel.Where(d => d.user_id == user_id && d.document_id == record.id).FirstOrDefault();
                DateTime? time_read = new DateTime(2022, 01, 01);
                if (user_read != null)
                {
                    time_read = user_read.time_read;
                }
                int comments_unread = _context.DocumentCommentModel.Where(d => d.document_id == record.id && d.created_at > time_read).Count();
                var class_name = "";
                var span_unread = "";
                if (documents_unread.Contains(record.id))
                {
                    class_name = "font-weight-bold";
                }
                if (comments_unread > 0)
                {
                    span_unread = "<span class='badge badge-danger ml-2'>" + comments_unread + " bình luận chưa đọc</span>";
                }
                var user_create = record.user != null ? record.user.FullName : "";
                var date_create = record.created_at != null ? record.created_at.Value.ToString("HH:mm dd/MM/yyyy") : "";
                var type_name = record.type != null ? record.type.name : "";
                var type_color = record.type != null ? record.type.color : "";
                var keyword = _context.DocumentUserKeywordModel.Where(d => d.document_id == record.id && d.user_id == user_id).Select(d => d.keyword).ToList();
                var html_keyword = "";
                if (keyword.Count > 0)
                {
                    html_keyword += "<div class=''>";
                    foreach (var item in keyword)
                    {
                        html_keyword += "<span class='badge badge-success px-3 mr-2'>" + item + "</span>";
                    }
                    html_keyword += "<div>";
                }
                var html_code = "";
                if (record.code != null)
                {
                    html_code = "[" + record.code + "]";
                }
                var html_name_body = "<div><a class='text-dark " + class_name + "'href='/admin/" + _type + "/details/" + record.id + "'>" + html_code + " " + record.name_vi + span_unread + "</a></div>";
                html_name_body += "<div><span class='small'>Tạo bởi <i>" + user_create + "</i> lúc <i>" + date_create + "</i></span><span class='badge text-white px-3 ml-2' style='background: " + type_color + "'>" + type_name + "</span></div>";


                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>" + html_keyword;
                var action = "";
                if (users_follow.Contains(user_id))
                {
                    action += "<a class='mr-2' href='/admin/document/create?related=" + record.code + "'href='#' title='Tạo mới và đính kèm hồ sơ này!'>"
                        + "<i class='fas fa-plus text-success font-16'>"
                        + "</i>"
                        + "</a>";
                }
                if (record.status_id == (int)DocumentStatus.Release && (user_id == record.user_next_signature_id || user_id == record.user_next_representative_id))
                {
                    action += "<a class='mr-2' href='/admin/document/sign/" + record.id + "'href='#' title='Ký tên'>"
                        + "<i class='fas fa-edit text-success font-16'>"
                        + "</i>"
                        + "</a>";
                }
                if (users_follow.Contains(user_id) && (record.status_id != (int)DocumentStatus.Cancle && record.status_id != (int)DocumentStatus.Obsoleted && record.status_id != (int)DocumentStatus.Superseded))
                {
                    action += "<a href='/admin/" + _type + "/edit/" + record.id + "'class='mr-2' title='Sửa hồ sơ?'>"
                        + "<i class='fas fa-cog text-info font-16'>"
                        + "</i>"
                        + "</a>";
                }
                if (record.status_id == (int)DocumentStatus.Release && (user_id == record.user_id || users_follow.Contains(user_id)) && type != "wait")
                {
                    action += "<a class='huy' data-id='" + record.id + "'href='#' data-toggle='modal' data-animation='bounce' data-target='.modal-reason' title='Hủy hồ sơ'>"
                        + "<i class='fas fa-trash-alt text-danger font-16'>"
                        + "</i>"
                        + "</a>";
                }

                var data1 = new
                {
                    action = action,
                    name = html_name,
                    user_signature = html_user_signature,
                    status = html_status,
                    priority = html_priority
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }

        public async Task<JsonResult> GetUser(string id)
        {
            UserModel User = await UserManager.FindByIdAsync(id);
            var RepresentativeModel = _context.RepresentativeModel
                .Where(d => d.deleted_at == null && d.user_id == id && DateTime.Now.Date >= d.date_from.Date && DateTime.Now.Date <= d.date_to.Date)
                .Include(d => d.representative)
                .FirstOrDefault();
            string? representative_id = null;
            UserModel? representative = null;
            if (RepresentativeModel != null)
            {
                representative = RepresentativeModel.representative;
                representative_id = RepresentativeModel.representative_id;
            }
            var is_sign = true;
            if (User.image_sign == "/private/images/tick.png")
            {
                is_sign = false;
            }
            return Json(new { Id = User.Id, representative = representative, representative_id = representative_id, position = User.position, FullName = User.FullName, Email = User.Email, image_url = User.image_url, image_sign = User.image_sign, is_sign = is_sign });
        }
        public async Task<JsonResult> Get(int id)
        {
            var DocumentModel = _context.DocumentModel
                 .Where(d => d.id == id)
                 .Include(d => d.files)
                 .Include(d => d.attachments)
                 .Include(d => d.related)
                 .Include(d => d.users_follow)
                 .Include(d => d.users_receive)
                 .Include(d => d.users_signature.OrderBy(u => u.stt))
                 .ThenInclude(u => u.user)
                 .Include(d => d.users_signature)
                 .ThenInclude(u => u.representative)
                 .FirstOrDefault();

            return Json(new { data = DocumentModel });
        }

        [HttpPost]
        public async Task<JsonResult> fileupload()
        {
            var files = Request.Form.Files;
            //return Json(files);
            if (files != null && files.Count > 0)
            {
                var items = new List<DocumentFileModel>();
                foreach (var file in files)
                {
                    var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    string name = file.FileName;
                    string ext = Path.GetExtension(name);
                    string mimeType = file.ContentType;

                    //var fileName = Path.GetFileName(name);
                    var newName = timeStamp + " - " + name;
                    newName = newName.Replace("+", "_");
                    newName = newName.Replace("%", "_");
                    var filePath = "private\\documents\\" + newName;
                    string url = "/private/documents/" + newName;
                    items.Add(new DocumentFileModel
                    {
                        ext = ext,
                        url = url,
                        name = name,
                        mimeType = mimeType,
                        created_at = DateTime.Now
                    });

                    using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileSrteam);
                    }
                }
                _context.AddRange(items);
                _context.SaveChanges();
                return Json(new { success = 1, items = items });
            }
            return Json(new { message = "Lỗi" });
        }

        // GET: Admin/Document/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id == null || _context.DocumentModel == null)
            {
                return NotFound();
            }
            if (checkPermission("details", id) != 0)
            {
                return RedirectToAction(nameof(Index));
            }
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string current_user_id = UserManager.GetUserId(currentUser); // Get user id:

            var user_current = await UserManager.GetUserAsync(currentUser); // Get user id:
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            var is_admin = await UserManager.IsInRoleAsync(user_current, "Administrator");

            //Thêm Read và xóa Unread
            DocumentUserReadModel user_read = _context.DocumentUserReadModel.Where(d => d.user_id == current_user_id && d.document_id == id).FirstOrDefault();
            DateTime? time_read = null;
            if (user_read != null)
            {
                time_read = user_read.time_read;
                user_read.time_read = DateTime.Now;
                _context.Update(user_read);
            }
            else
            {
                user_read = new DocumentUserReadModel
                {
                    user_id = current_user_id,
                    document_id = id,
                    time_read = DateTime.Now
                };

                _context.Add(user_read);
            }

            var unread_delete = _context.DocumentUserUnreadModel.Where(d => d.document_id == id && d.user_id == current_user_id).ToList();
            _context.RemoveRange(unread_delete);

            await _context.SaveChangesAsync();


            ////
            ///
            var DocumentModel = _context.DocumentModel
                .Where(d => d.id == id)
                .Include(d => d.type)
                .Include(d => d.user)
                .Include(d => d.files)
                .Include(d => d.attachments)
                .Include(d => d.related)
                .Include(d => d.events)
                .Include(d => d.users_follow)
                .Include(d => d.users_receive)
                .ThenInclude(u => u.user)
                .Include(d => d.comments.Where(d => d.deleted_at == null).OrderByDescending(u => u.id).Take(11))
                .ThenInclude(u => u.user)
                .Include(d => d.comments)
                .ThenInclude(u => u.files)
                .Include(d => d.users_signature.OrderBy(u => u.stt))
                .ThenInclude(u => u.user)
                .Include(d => d.users_signature)
                .ThenInclude(u => u.representative)
                .FirstOrDefault();
            //return Ok(DocumentModel);
            foreach (var comment in DocumentModel.comments)
            {
                if (comment.user_id == current_user_id)
                {
                    comment.is_read = true;
                    continue;
                }
                if (time_read != null && comment.created_at <= time_read)
                    comment.is_read = true;
            }
            ViewBag.users_receive = DocumentModel.users_receive.Select(d => d.user).ToList();
            DocumentModel.users_signature = DocumentModel.users_signature.OrderBy(u => u.stt).ToList();
            var document_related = DocumentModel.related.Select(a => a.document_related = _context.DocumentModel.Find(a.document_related_id)).ToList();
            bool is_sign = false;
            if (DocumentModel.is_sign_parellel == true)
            {
                var user_signature = DocumentModel.users_signature.OrderBy(u => u.stt).Where(u => u.status == 1 && u.user_id == current_user_id).FirstOrDefault();
                if (DocumentModel.status_id == 2 && user_signature != null)
                {
                    is_sign = true;
                    ViewBag.user_signature = user_signature;
                }
            }
            else
            {
                var user_signature = DocumentModel.users_signature.OrderBy(u => u.stt).Where(u => u.status == 1).FirstOrDefault();

                ViewBag.current_user_id = current_user_id;
                //return Ok(users_signture.Count);
                if (DocumentModel.status_id == 2 && user_signature != null && (user_signature.user_id == current_user_id))
                {
                    is_sign = true;
                    ViewBag.user_signature = user_signature;
                }

            }
            var keyword = _context.DocumentUserKeywordModel.Where(d => d.user_id == current_user_id && d.document_id == id).Select(d => d.keyword).ToList();
            ViewBag.keyword = keyword;
            ViewBag.is_sign = is_sign;
            ViewBag.document_related = document_related;
            ViewBag.no_edit = checkPermission("edit", id, is_admin, is_manager);
            ViewBag.no_suggestsign = checkPermission("suggestsign", id);
            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "< " + a.Email + " >"
            }).ToList();
            return View(DocumentModel);
        }

        // GET: Admin/Document/Sign/5
        public async Task<IActionResult> Sign(int? id)
        {
            if (id == null || _context.DocumentModel == null)
            {
                return NotFound();
            }
            if (checkPermission("sign", id) != 0)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }
            var DocumentModel = await _context.DocumentModel
                .Where(d => d.id == id)
                .Include(d => d.files)
                .Include(d => d.users_signature.Where(u => u.status == 1).OrderBy(u => u.stt))
                .ThenInclude(u => u.user)
                .Include(d => d.users_signature)
                .ThenInclude(u => u.representative)
                .FirstAsync();
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string current_user_id = UserManager.GetUserId(currentUser); // Get user id:
                                                                         //return Ok(users_signture.Count);

            var user_signature = DocumentModel.users_signature.Where(u => u.status == 1 && u.user_id == current_user_id).OrderBy(u => u.stt).FirstOrDefault();
            ViewBag.current_user_id = current_user_id;
            ViewBag.user_signature = user_signature;
            return View(DocumentModel);
        }
        // GET: Admin/Document/Sign/5
        public async Task<IActionResult> SignCustom(int? id)
        {
            if (id == null || _context.DocumentModel == null)
            {
                return NotFound();
            }
            //if (checkPermission("sign", id) != 0)
            //{
            //	return RedirectToAction(nameof(Details), new { id = id });
            //}
            var DocumentModel = await _context.DocumentModel
                .Where(d => d.id == id)
                .Include(d => d.users_signature)
                .Include(d => d.files)
                .FirstAsync();


            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string current_user_id = UserManager.GetUserId(currentUser); // Get user id:	
            var current_user = await UserManager.GetUserAsync(currentUser);

            ViewBag.current_user_id = current_user_id;
            ViewBag.current_user = current_user;
            return View(DocumentModel);
        }
        // GET: Admin/Document/Sign/5
        public async Task<IActionResult> SuggestSign(int? id)
        {
            if (id == null || _context.DocumentModel == null)
            {
                return NotFound();
            }
            if (checkPermission("suggestsign", id) != 0)
            {
                return NotFound();
            }
            var DocumentModel = await _context.DocumentModel
                .Where(d => d.id == id)
                .Include(d => d.files)
                .Include(d => d.users_signature.Where(u => u.status == 1).OrderBy(u => u.stt))
                .ThenInclude(u => u.user)
                .FirstAsync();
            var users_signature = DocumentModel.users_signature.Where(u => u.status == 1).OrderBy(u => u.stt).ToList();
            if (users_signature == null || users_signature.Count == 0)
            {
                return Ok("Không có người ký!");
            }
            ViewBag.users_signature = users_signature;
            if (DocumentModel == null)
            {
                return NotFound();
            }
            return View(DocumentModel);
        }
        [HttpPost]
        public async Task<IActionResult> SaveSuggest(List<DocumentSignatureModel> list_sign)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user = await UserManager.GetUserAsync(currentUser); // Get user id:
            int document_id = 0;
            if (list_sign.Count > 0)
            {

                ////check
                var reader = new PdfReader("." + list_sign[0].url);
                var doc = new PdfDocument(reader);
                foreach (DocumentSignatureModel sign in list_sign)
                {
                    PdfPage page = doc.GetPage((int)sign.page);
                    int rotation = page.GetRotation();
                    if (rotation != 0)
                    {
                        return Ok(new { message = "Không thể ký vào trang đã xoay(Rotate)", failed = 1 });
                    }


                    DocumentSignatureModel sign_old = await _context.DocumentSignatureModel.Where(d => d.id == sign.id).FirstAsync();
                    document_id = sign_old.document_id;
                    ///Save DB
                    /// Cap nhat user_sign
                    sign_old.position_x = sign.position_x;
                    sign_old.position_y = sign.position_y;
                    sign_old.position_image_x = sign.position_image_x;
                    sign_old.position_image_y = sign.position_image_y;
                    sign_old.image_size_width = sign.image_size_width;
                    sign_old.image_size_height = sign.image_size_height;
                    sign_old.page = sign.page;

                    _context.Update(sign_old);
                }
                _context.SaveChanges();
                //create event
                DocumentEventModel DocumentEventModel = new DocumentEventModel
                {
                    document_id = document_id,
                    event_content = "<b>" + user.FullName + "</b> đã gợi ý vị trí ký",
                    created_at = DateTime.Now,
                };
                _context.Add(DocumentEventModel);

                var DocumentModel = _context.DocumentModel
                  .Where(d => d.id == document_id)
                  .Include(d => d.users_follow)
                  .Include(d => d.users_signature)
                  .Include(d => d.users_receive)
                  .FirstOrDefault();

                var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
                var users_signature = DocumentModel.users_signature.Select(a => a.user_id).ToList();
                var users_representative = DocumentModel.users_signature.Where(a => a.representative_id != null).Select(a => a.representative_id).ToList();
                var users_receive = DocumentModel.users_receive.Select(a => a.user_id).ToList();
                List<string> users_related = new List<string>();
                users_related.AddRange(users_follow);
                users_related.AddRange(users_signature);
                users_related.AddRange(users_representative);
                users_related.AddRange(users_receive);
                users_related = users_related.Distinct().ToList();
                var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
                users_related.Remove(itemToRemove);
                var items = new List<DocumentUserUnreadModel>();
                foreach (string u in users_related)
                {
                    items.Add(new DocumentUserUnreadModel
                    {
                        user_id = u,
                        document_id = DocumentModel.id,
                        time = DateTime.Now,
                    });
                }
                _context.AddRange(items);
                //await _context.SaveChangesAsync();

                /// Audittrail
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Update.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã gợi ý vị trí ký.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
            }


            return Ok(new { message = "Thành công" });
        }
        [HttpPost]
        public async Task<IActionResult> Save(DocumentSignatureModel sign)
        {
            //return Ok(sign);
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:

            DocumentSignatureModel sign_old = await _context.DocumentSignatureModel.Where(d => d.id == sign.id).FirstAsync();

            if (checkPermission("sign", sign_old.document_id) != 0)
            {
                return RedirectToAction(nameof(Details), new { id = sign_old.document_id });
            }
            UserModel user = await UserManager.FindByIdAsync(user_id);
            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            string fileName = Path.GetFileNameWithoutExtension(sign.url);
            string forlder = Path.GetDirectoryName(sign.url);
            string ext = Path.GetExtension(user.image_sign);
            string save = forlder.Substring(1) + "\\" + timeStamp + "(" + user.FullName + " Signed).pdf";
            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value + "/";
            //Draw the image
            var file_image = "." + user.image_sign;
            //PdfImage pdfImage = PdfImage.FromFile("." + user.image_sign);
            ImageData da = ImageDataFactory.Create(file_image);
            int image_size_width = (int)Math.Round((float)sign.image_size_width);
            int image_size_height = (int)Math.Round((float)sign.image_size_height);
            if (ext.ToLower() == ".png")
            {
                using (System.Drawing.Image src = System.Drawing.Image.FromFile("." + user.image_sign))
                using (Bitmap dst = new Bitmap(image_size_width, image_size_height))
                using (Graphics g = Graphics.FromImage(dst))
                {
                    //g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, 0, 0, dst.Width, dst.Height);
                    file_image = "wwwroot\\temp\\" + timeStamp + ".png";
                    dst.Save(file_image, ImageFormat.Png);
                    da = ImageDataFactory.CreatePng(new Uri(Domain + "/temp/" + timeStamp + ".png"));
                }
                //pdfImage = PdfImage.FromFile("wwwroot\\temp\\" + timeStamp + "png");
            }
            else
            {
                //System.Drawing.Image image = System.Drawing.Image.FromFile("." + user.image_sign);
                //image = FixedSize(image, image_size_width, image_size_height);
                //pdfImage = PdfImage.FromImage(image);
            }
            // os = new FileStream(dest, FileMode.Create, FileAccess.Write);

            //Activate MultiSignatures
            //To disable Multi signatures uncomment this line : every new signature will invalidate older ones !
            //stamper = PdfStamper.CreateSignature(reader, os, '\0');
            var dest = new PdfWriter(save);
            var reader = new PdfReader("." + sign.url);

            PdfSignerNoObjectStream signer = new PdfSignerNoObjectStream(reader, dest, new StampingProperties().UseAppendMode());
            // Creating the appearance
            FontProgram fontProgram = FontProgramFactory.CreateFont("wwwroot/assets/fonts/vuArial.ttf");
            PdfFont font = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);
            var width = (int)sign.image_size_width;
            var heigth = (int)sign.image_size_height;

            if (user_id != "67688e5b-575d-4d12-8370-25e57105a24d")
            {
                if (width < 180)
                    width = 180;
                heigth += 40;
                if (sign.reason != null)
                {
                    heigth += 30;
                }
            }

            PdfDocument doc = signer.GetDocument();
            PdfPage page = doc.GetPage((int)sign.page);
            int rotation = page.GetRotation();
            if (rotation != 0)
            {
                return Ok(new { message = "Không thể ký vào trang đã xoay(Rotate)!", failed = 1 });
            }
            iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle((int)sign.position_x, (int)sign.position_y, width, heigth);

            PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                .SetReuseAppearance(false)
                .SetPageRect(rect)
                .SetPageNumber((int)sign.page);


            if (sign.reason != null)
            {
                appearance = appearance.SetReason(sign.reason);
            }
            PdfFormXObject layer2 = appearance.GetLayer2();
            //layer2.
            PdfCanvas canvas = new PdfCanvas(layer2, doc);
            //PdfPage page = doc.GetPage(appearance.GetPageNumber());
            //int rotation = page.GetRotation();
            //if (rotation == 90)
            //	canvas.ConcatMatrix(0, 1, -1, 0, rect.GetWidth(), 0);
            //else if (rotation == 180)
            //	canvas.ConcatMatrix(-1, 0, 0, -1, rect.GetWidth(), rect.GetHeight());
            //else if (rotation == 270)
            //	canvas.ConcatMatrix(0, -1, 1, 0, 0, rect.GetHeight());


            int p_y = 0;
            if (user_id != "67688e5b-575d-4d12-8370-25e57105a24d")
            {
                p_y += 40;
                var position = user.position != null && user.position != "" ? " (" + user.position + ")" : "";
                var text = user.FullName + position + "\n" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                if (sign.reason != null)
                {
                    text += "\nÝ kiến: " + sign.reason;
                    p_y += 30;
                }
                iText.Kernel.Geom.Rectangle signatureRect = new iText.Kernel.Geom.Rectangle(0, 0, 180, p_y);
                Canvas signLayoutCanvas = new Canvas(canvas, signatureRect);
                Paragraph paragraph = new Paragraph(text).SetFont(font).SetMargin(0).SetMultipliedLeading(1.2f).SetFontSize(10);
                Div div = new Div();
                div.SetHeight(signatureRect.GetHeight());
                div.SetWidth(signatureRect.GetWidth());

                div.SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.TOP);
                div.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
                div.Add(paragraph);
                signLayoutCanvas.Add(div);
            }
            iText.Kernel.Geom.Rectangle dataRect = new iText.Kernel.Geom.Rectangle(0, p_y, (float)sign.image_size_width, rect.GetHeight() - p_y);
            Canvas dataLayoutCanvas = new Canvas(canvas, dataRect);
            iText.Layout.Element.Image image = new iText.Layout.Element.Image(da);
            image.SetAutoScale(true);
            Div dataDiv = new Div();
            dataDiv.SetHeight(dataRect.GetHeight());
            dataDiv.SetWidth(dataRect.GetWidth());
            dataDiv.SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE);
            dataDiv.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
            dataDiv.Add(image);
            dataLayoutCanvas.Add(dataDiv);




            var field = timeStamp.ToString() + "-GMP-" + sign_old.document_id;

            signer.SetFieldName(field);
            // Creating the signature
            string KEYSTORE = "private/pfx/" + user_id + ".pfx";
            char[] PASSWORD = "!PMP_it123456".ToCharArray();

            Pkcs12Store pk12 = new Pkcs12Store(new FileStream(KEYSTORE,
            FileMode.Open, FileAccess.Read), PASSWORD);
            string alias = null;
            foreach (object a in pk12.Aliases)
            {
                alias = ((string)a);
                if (pk12.IsKeyEntry(alias))
                {
                    break;
                }
            }
            ICipherParameters pk = pk12.GetKey(alias).Key;
            X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
            X509Certificate[] chain = new X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }
            IExternalSignature pks = new PrivateKeySignature(pk, DigestAlgorithms.SHA256);

            signer.SignDetached(pks, chain, null, null, null, 0,
                    PdfSignerNoObjectStream.CryptoStandard.CMS);

            dest.Close();
            ///Save DB
            /// Cap nhat user_sign
            sign_old.date = DateTime.Now;
            sign_old.status = 2;
            sign_old.position_x = sign.position_x;
            sign_old.position_y = sign.position_y;
            sign_old.position_image_x = sign.position_image_x;
            sign_old.position_image_y = sign.position_image_y;
            sign_old.image_size_width = sign.image_size_width;
            sign_old.image_size_height = sign.image_size_height;
            sign_old.url = sign.url;
            sign_old.user_sign = sign.user_sign;
            sign_old.reason = sign.reason;
            sign_old.page = sign.page;
            _context.Update(sign_old);

            _context.SaveChanges();


            /// Add document file
            var item = new DocumentFileModel
            {
                ext = ".pdf",
                url = forlder.Replace("\\", "/") + "/" + timeStamp + "(" + user.FullName + " Signed).pdf",
                name = timeStamp + "(" + user.FullName + " Signed).pdf",
                mimeType = "application/pdf",
                created_at = DateTime.Now,
                document_id = sign_old.document_id
            };

            _context.Add(item);
            _context.SaveChanges();

            var DocumentModel_old = await _context.DocumentModel.FindAsync(sign_old.document_id);
            ///Đóng mộc
            //if (DocumentModel_old.type_id == 50)
            //{
            //    PdfSignerNoObjectStream signer1 = new PdfSignerNoObjectStream(new PdfReader(save), new FileStream(forlder.Substring(1) + "\\" + fileName + "(" + user.FullName + " Signed_moc).pdf", FileMode.Create), new StampingProperties().UseAppendMode());

            //    // Creating the appearance
            //    var width1 = 60;
            //    var heigth1 = 60;
            //    iText.Kernel.Geom.Rectangle rect1 = new iText.Kernel.Geom.Rectangle((int)sign.position_x - 15, (int)sign.position_y + p_y, width1, heigth1);
            //    PdfSignatureAppearance appearance1 = signer1.GetSignatureAppearance()
            //        .SetReuseAppearance(false)
            //        .SetPageRect(rect1)
            //        .SetPageNumber((int)sign.page);

            //    PdfFormXObject layer21 = appearance1.GetLayer2();
            //    PdfDocument doc1 = signer1.GetDocument();
            //    PdfCanvas canvas1 = new PdfCanvas(layer21, doc1);



            //    ImageData da1 = ImageDataFactory.Create("./private/images/dau_moc.png");
            //    iText.Kernel.Geom.Rectangle dataRect1 = new iText.Kernel.Geom.Rectangle(0, 0, width1, heigth1);
            //    Canvas dataLayoutCanvas1 = new Canvas(canvas1, dataRect1);
            //    iText.Layout.Element.Image image1 = new iText.Layout.Element.Image(da1);
            //    image1.SetAutoScale(true);
            //    Div dataDiv1 = new Div();
            //    dataDiv1.SetHeight(dataRect1.GetHeight());
            //    dataDiv1.SetWidth(dataRect1.GetWidth());
            //    dataDiv1.SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE);
            //    dataDiv1.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
            //    dataDiv1.Add(image1);
            //    dataLayoutCanvas1.Add(dataDiv1);

            //    signer1.SetFieldName("dau_moc");
            //    // Creating the signature
            //    string KEYSTORE1 = "private/pfx/dau_moc.pfx";
            //    char[] PASSWORD1 = "!PMP_it123456".ToCharArray();

            //    Pkcs12Store pk121 = new Pkcs12Store(new FileStream(KEYSTORE1,
            //    FileMode.Open, FileAccess.Read), PASSWORD1);
            //    string alias1 = null;
            //    foreach (object a in pk121.Aliases)
            //    {
            //        alias1 = ((string)a);
            //        if (pk121.IsKeyEntry(alias1))
            //        {
            //            break;
            //        }
            //    }
            //    ICipherParameters pk1 = pk121.GetKey(alias1).Key;
            //    X509CertificateEntry[] ce1 = pk121.GetCertificateChain(alias1);
            //    X509Certificate[] chain1 = new X509Certificate[ce1.Length];
            //    for (int k = 0; k < ce1.Length; ++k)
            //    {
            //        chain1[k] = ce1[k].Certificate;
            //    }
            //    IExternalSignature pks1 = new PrivateKeySignature(pk1, DigestAlgorithms.SHA256);

            //    signer1.SignDetached(pks1, chain1, null, null, null, 0,
            //            PdfSignerNoObjectStream.CryptoStandard.CMS);

            //    /// Add document file
            //    var item1 = new DocumentFileModel
            //    {
            //        ext = ".pdf",
            //        url = forlder.Replace("\\", "/") + "/" + fileName + "(" + user.FullName + " Signed_moc).pdf",
            //        name = fileName + "(" + user.FullName + " Signed_moc).pdf",
            //        mimeType = "application/pdf",
            //        created_at = DateTime.Now,
            //        document_id = sign_old.document_id
            //    };

            //    _context.Add(item1);
            //    _context.SaveChanges();
            //}
            ///UPDATE NEXT SIGN
            DocumentModel_old.time_signature_previous = DateTime.Now;

            if (DocumentModel_old.is_sign_parellel == true)
            {
                var users_signature_list = _context.DocumentSignatureModel.Where(u => u.status == 1 && u.document_id == DocumentModel_old.id && u.user_id != DocumentModel_old.user_id).Include(d => d.user).ToList();
                //SEND MAIL
                if (users_signature_list.Count() > 0)
                {
                    DocumentModel_old.status_id = 2; // Co nguoi ky thi trinh ky tiep
                }
                else
                {
                    if (DocumentModel_old.status_id == 2)
                    {
                        DocumentModel_old.status_id = 4; // Ko ai ký nữa thì hoàn thành
                        DocumentModel_old.date_finish = DateTime.Now;
                        var user_create = await UserManager.FindByIdAsync(DocumentModel_old.user_id);
                        var user_receive = _context.DocumentUserReceiveModel.Where(u => u.document_id == DocumentModel_old.id).Include(d => d.user).Select(d => d.user.Email).ToList();
                        user_receive.Add(user_create.Email);
                        user_receive = user_receive.Distinct().ToList();
                        var mail_string = string.Join(",", user_receive.ToArray());
                        string Domain_1 = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                        var body = _view.Render("Emails/FinishDocument", new { link_logo = Domain_1 + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain_1 + "/admin/document/details/" + DocumentModel_old.id });
                        var attach = new List<string>()
                        {
                            item.url
                        };
                        var email = new EmailModel
                        {
                            email_to = mail_string,
                            subject = "[Hoàn thành] " + DocumentModel_old.name_vi,
                            body = body,
                            email_type = "finish_document",
                            status = 1,
                            data_attachments = attach
                        };

                        _context.Add(email);
                    }
                }
                _context.Update(DocumentModel_old);
                await _context.SaveChangesAsync();
            }
            else
            {
                var user_signature = _context.DocumentSignatureModel.OrderBy(u => u.stt).Where(u => u.status == 1 && u.document_id == sign_old.document_id).Include(d => d.user).Include(d => d.representative).FirstOrDefault();
                string? user_signature_id = null;
                string? user_presentative_id = null;
                if (user_signature != null)
                {
                    user_signature_id = user_signature.user_id;
                    user_presentative_id = user_signature.representative_id;
                }
                if (DocumentModel_old.user_next_signature_id != user_signature_id || DocumentModel_old.user_next_representative_id != user_presentative_id)
                {
                    DocumentModel_old.user_next_signature_id = user_signature_id;
                    DocumentModel_old.user_next_representative_id = user_presentative_id;
                    if (user_signature_id == null)
                    {
                        if (DocumentModel_old.status_id == 2)
                        {
                            DocumentModel_old.status_id = 4;
                            DocumentModel_old.date_finish = DateTime.Now;
                        }

                        var user_create = await UserManager.FindByIdAsync(DocumentModel_old.user_id);
                        var user_receive = _context.DocumentUserReceiveModel.Where(u => u.document_id == DocumentModel_old.id).Include(d => d.user).Select(d => d.user.Email).ToList();
                        user_receive.Add(user_create.Email);
                        user_receive = user_receive.Distinct().ToList();
                        var mail_string = string.Join(",", user_receive.ToArray());
                        string Domain_1 = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                        var body = _view.Render("Emails/FinishDocument", new { link_logo = Domain_1 + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain_1 + "/admin/document/details/" + DocumentModel_old.id });
                        var attach = new List<string>()
                        {
                            item.url
                        };
                        var email = new EmailModel
                        {
                            email_to = mail_string,
                            subject = "[Hoàn thành] " + DocumentModel_old.name_vi,
                            body = body,
                            email_type = "finish_document",
                            status = 1,
                            data_attachments = attach
                        };

                        _context.Add(email);

                    }   //SEND MAIL
                    else if (user_signature != null)
                    {
                        var mail_string = user_signature.representative != null ? user_signature.representative.Email : user_signature.user.Email;
                        string Domain_1 = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                        var body = _view.Render("Emails/WaitSignDocument", new { link_logo = Domain_1 + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain_1 + "/admin/document/details/" + DocumentModel_old.id });
                        var attach = new List<string>()
                        {
                            item.url
                        };
                        var email = new EmailModel
                        {
                            email_to = mail_string,
                            subject = "[Đang chờ ký] " + DocumentModel_old.name_vi,
                            body = body,
                            email_type = "wait_sign_document",
                            status = 1,
                            data_attachments = attach
                        };
                        _context.Add(email);
                    }
                    _context.Update(DocumentModel_old);
                    _context.SaveChanges();
                }
            }


            //create event
            DocumentEventModel DocumentEventModel = new DocumentEventModel
            {
                document_id = sign_old.document_id,
                event_content = "<b>" + user.FullName + "</b> ký vào hồ sơ",
                created_at = DateTime.Now,
            };
            _context.Add(DocumentEventModel);
            //Create unread
            var DocumentModel = _context.DocumentModel
              .Where(d => d.id == sign_old.document_id)
              .Include(d => d.users_follow)
              .Include(d => d.users_signature)
              .Include(d => d.users_receive)
              .FirstOrDefault();

            var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
            var users_signature = DocumentModel.users_signature.Select(a => a.user_id).ToList();
            var users_representative = DocumentModel.users_signature.Where(a => a.representative_id != null).Select(a => a.representative_id).ToList();
            var users_receive = DocumentModel.users_receive.Select(a => a.user_id).ToList();
            List<string> users_related = new List<string>();
            users_related.AddRange(users_follow);
            users_related.AddRange(users_signature);
            users_related.AddRange(users_representative);
            users_related.AddRange(users_receive);
            users_related = users_related.Distinct().ToList();
            var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
            users_related.Remove(itemToRemove);
            var items = new List<DocumentUserUnreadModel>();
            foreach (string u in users_related)
            {
                items.Add(new DocumentUserUnreadModel
                {
                    user_id = u,
                    document_id = DocumentModel.id,
                    time = DateTime.Now,
                });
            }
            _context.AddRange(items);
            //await _context.SaveChangesAsync();


            /// Audittrail
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Update.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã ký hồ sơ.";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thành công" });
        }
        [HttpPost]
        public async Task<IActionResult> SaveSignCustom(DocumentSignatureModel sign)
        {
            //return Ok(sign);
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            UserModel user = await UserManager.FindByIdAsync(user_id);
            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            //string fileName = Path.GetFileNameWithoutExtension(sign.url);
            string folder = Path.GetDirectoryName(sign.url);
            folder = folder.Replace("\\private", _configuration["Source:Path_Private"]);
            string ext = Path.GetExtension(sign.image_sign);
            string save = folder + "\\" + timeStamp + ".pdf";
            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value + "/";

            // os = new FileStream(dest, FileMode.Create, FileAccess.Write);

            //Activate MultiSignatures
            //To disable Multi signatures uncomment this line : every new signature will invalidate older ones !
            //stamper = PdfStamper.CreateSignature(reader, os, '\0');
            var dest = new PdfWriter(save);
            var reader = new PdfReader(sign.url.Replace("/private/", _configuration["Source:Path_Private"] + "\\").Replace("/", "\\"));

            PdfSignerNoObjectStream signer = new PdfSignerNoObjectStream(reader, dest, new StampingProperties().UseAppendMode());
            // Creating the appearance
            FontProgram fontProgram = FontProgramFactory.CreateFont("wwwroot/assets/fonts/vuArial.ttf");
            PdfFont font = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);
            var width = (int)sign.image_size_width;
            var heigth = (int)sign.image_size_height;
            if (width < 180)
                width = 180;
            if (sign.reason != null)
            {
                heigth += 30;
            }

            iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle((int)sign.position_x, (int)sign.position_y, width, heigth);
            PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                .SetReuseAppearance(false)
                .SetPageRect(rect)
                .SetPageNumber((int)sign.page);


            if (sign.reason != null)
            {
                appearance = appearance.SetReason(sign.reason);
            }
            PdfFormXObject layer2 = appearance.GetLayer2();
            PdfDocument doc = signer.GetDocument();
            PdfCanvas canvas = new PdfCanvas(layer2, doc);

            int p_y = 0;
            var text = "";
            if (sign.reason != null)
            {
                text += sign.reason;
                p_y += 30;
            }
            iText.Kernel.Geom.Rectangle signatureRect = new iText.Kernel.Geom.Rectangle(0, 0, 180, p_y);
            Canvas signLayoutCanvas = new Canvas(canvas, signatureRect);
            Paragraph paragraph = new Paragraph(text).SetFont(font).SetMargin(0).SetMultipliedLeading(1.2f).SetFontSize(10);
            Div div = new Div();
            div.SetHeight(signatureRect.GetHeight());
            div.SetWidth(signatureRect.GetWidth());
            div.SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.TOP);
            div.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
            div.Add(paragraph);
            signLayoutCanvas.Add(div);



            if (sign.image_sign != null)
            {

                var file_image = sign.image_sign.Replace("/private/", _configuration["Source:Path_Private"] + "\\").Replace("/", "\\");
                ImageData da = ImageDataFactory.Create(file_image);
                int image_size_width = (int)Math.Round((float)sign.image_size_width);
                int image_size_height = (int)Math.Round((float)sign.image_size_height);

                if (ext.ToLower() == ".png")
                {
                    using (System.Drawing.Image src = System.Drawing.Image.FromFile(file_image))
                    using (Bitmap dst = new Bitmap(image_size_width, image_size_height))
                    using (Graphics g = Graphics.FromImage(dst))
                    {
                        //g.Clear(Color.White);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, 0, 0, dst.Width, dst.Height);
                        MemoryStream ms = new MemoryStream();
                        dst.Save(ms, ImageFormat.Png);


                        da = ImageDataFactory.CreatePng(ms.ToArray());
                    }
                }

                iText.Kernel.Geom.Rectangle dataRect = new iText.Kernel.Geom.Rectangle(0, p_y, (float)sign.image_size_width, rect.GetHeight() - p_y);
                Canvas dataLayoutCanvas = new Canvas(canvas, dataRect);
                iText.Layout.Element.Image image = new iText.Layout.Element.Image(da);
                image.SetAutoScale(true);
                Div dataDiv = new Div();
                dataDiv.SetHeight(dataRect.GetHeight());
                dataDiv.SetWidth(dataRect.GetWidth());
                dataDiv.SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE);
                dataDiv.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
                dataDiv.Add(image);
                dataLayoutCanvas.Add(dataDiv);
            }



            var field = timeStamp.ToString() + "-GMP-" + sign.document_id;

            signer.SetFieldName(field);
            // Creating the signature
            string KEYSTORE = _configuration["Source:Path_Private"] + "\\pfx\\" + user_id + ".pfx";
            char[] PASSWORD = "!PMP_it123456".ToCharArray();

            Pkcs12Store pk12 = new Pkcs12Store(new FileStream(KEYSTORE,
            FileMode.Open, FileAccess.Read), PASSWORD);
            string alias = null;
            foreach (object a in pk12.Aliases)
            {
                alias = ((string)a);
                if (pk12.IsKeyEntry(alias))
                {
                    break;
                }
            }
            ICipherParameters pk = pk12.GetKey(alias).Key;
            X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
            X509Certificate[] chain = new X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }
            IExternalSignature pks = new PrivateKeySignature(pk, DigestAlgorithms.SHA256);

            signer.SignDetached(pks, chain, null, null, null, 0,
                    PdfSignerNoObjectStream.CryptoStandard.CMS);

            dest.Close();
            ///Save DB
            /// Cap nhat user_sign
            sign.date = DateTime.Now;
            sign.status = 2;
            _context.Add(sign);
            _context.SaveChanges();


            /// Add document file
            var item = new DocumentFileModel
            {
                ext = ".pdf",
                url = "/private/documents/" + sign.document_id + "/" + timeStamp + ".pdf",
                name = timeStamp + ".pdf",
                mimeType = "application/pdf",
                created_at = DateTime.Now,
                document_id = sign.document_id
            };

            _context.Add(item);
            _context.SaveChanges();



            //create event
            DocumentEventModel DocumentEventModel = new DocumentEventModel
            {
                document_id = sign.document_id,
                event_content = "<b>" + user.FullName + "</b> ký vào hồ sơ",
                created_at = DateTime.Now,
            };
            _context.Add(DocumentEventModel);


            /// Audittrail
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Update.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã ký hồ sơ.";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thành công" });
        }
        static System.Drawing.Image FixedSize(System.Drawing.Image imgPhoto, int Width, int Height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)Width / (float)sourceWidth);
            nPercentH = ((float)Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((Width -
                              (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((Height -
                              (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(Width, Height,
                              System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                             imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(System.Drawing.Color.White);
            grPhoto.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }

        // GET: Admin/Document/Cancle
        [HttpPost]
        public async Task<IActionResult> Cancle(int id, string reason)
        {
            if (_context.DocumentSignatureModel == null)
            {
                return Problem("Entity set 'ItContext.DocumentModel'  is null.");
            }

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            UserModel user = await UserManager.FindByIdAsync(user_id);

            var DocumentSignatureModel = await _context.DocumentSignatureModel.FindAsync(id);
            if (checkPermission("cancle_signature", DocumentSignatureModel.document_id) != 0)
            {
                return NotFound();
            }
            if (DocumentSignatureModel == null)
            {
                return NotFound();
            }
            DocumentSignatureModel.status = 3;
            DocumentSignatureModel.reason = reason;
            _context.DocumentSignatureModel.Update(DocumentSignatureModel);
            _context.SaveChanges();


            var DocumentModel = _context.DocumentModel
              .Where(d => d.id == DocumentSignatureModel.document_id)
              .Include(d => d.users_follow)
              .Include(d => d.users_signature)
              .Include(d => d.users_receive)
              .FirstOrDefault();
            ////SEND MAIL 

            var user_create = await UserManager.FindByIdAsync(DocumentModel.user_id);
            var users_signature_signed = _context.DocumentSignatureModel.Where(u => u.document_id == DocumentModel.id && u.status == 2).Include(d => d.user_s).Select(d => d.user_s.Email).ToList();
            users_signature_signed.Add(user_create.Email);
            users_signature_signed = users_signature_signed.Distinct().ToList();
            var mail_string = string.Join(",", users_signature_signed.ToArray());
            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
            var body = _view.Render("Emails/CancleSignature", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel.id, reason = reason });
            var email = new EmailModel
            {
                email_to = mail_string,
                subject = "[Không ký] " + DocumentModel.name_vi,
                body = body,
                email_type = "cancle_signature",
                status = 1
            };
            _context.Add(email);
            await _context.SaveChangesAsync();

            DocumentModel.status_id = 3;
            _context.Update(DocumentModel);
            _context.SaveChanges();
            ///UPDATE NEXT SIGN
            //var user_signature = _context.DocumentSignatureModel.OrderBy(u => u.stt).Where(u => u.status == 1 && u.document_id == DocumentSignatureModel.document_id).Include(d => d.user).Include(d => d.representative).FirstOrDefault();
            //string? user_signature_id = null;
            //string? user_representative_id = null;

            //if (user_signature != null)
            //{
            //	user_signature_id = user_signature.user_id;
            //	user_representative_id = user_signature.representative_id;
            //}
            //if (DocumentModel.user_next_signature_id != user_signature_id || DocumentModel.user_next_representative_id != user_representative_id)
            //{
            //	DocumentModel.user_next_signature_id = user_signature_id;
            //	DocumentModel.user_next_representative_id = user_representative_id;
            //	if (user_signature_id == null)
            //	{
            //		DocumentModel.status_id = 3;
            //		//await StartSign(DocumentModel_old.id);
            //	}//SEND MAIL
            //	else if (user_signature != null)
            //	{
            //		mail_string = user_signature.representative != null ? user_signature.representative.Email : user_signature.user.Email;
            //		body = _view.Render("Emails/WaitSignDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel.id });
            //		email = new EmailModel
            //		{
            //			email_to = mail_string,
            //			subject = "[Đang chờ ký] " + DocumentModel.name_vi,
            //			body = body,
            //			email_type = "wait_sign_document",
            //			status = 1
            //		};
            //		_context.Add(email);
            //		await _context.SaveChangesAsync();
            //	}
            //	_context.Update(DocumentModel);
            //	_context.SaveChanges();
            //}


            //create event
            DocumentEventModel DocumentEventModel = new DocumentEventModel
            {
                document_id = DocumentModel.id,
                event_content = "<b>" + user.FullName + "</b> không ký vào hồ sơ",
                created_at = DateTime.Now,
            };
            _context.Add(DocumentEventModel);

            //Create unread 
            var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
            var users_signature = DocumentModel.users_signature.Select(a => a.user_id).ToList();
            var users_representative = DocumentModel.users_signature.Where(a => a.representative_id != null).Select(a => a.representative_id).ToList();
            var users_receive = DocumentModel.users_receive.Select(a => a.user_id).ToList();
            List<string> users_related = new List<string>();
            users_related.AddRange(users_follow);
            users_related.AddRange(users_signature);
            users_related.AddRange(users_representative);
            users_related.AddRange(users_receive);
            users_related = users_related.Distinct().ToList();
            var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
            users_related.Remove(itemToRemove);
            var items = new List<DocumentUserUnreadModel>();
            foreach (string u in users_related)
            {
                items.Add(new DocumentUserUnreadModel
                {
                    user_id = u,
                    document_id = DocumentModel.id,
                    time = DateTime.Now,
                });
            }
            _context.AddRange(items);
            await _context.SaveChangesAsync();


            /// Audittrail
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Update.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} không ký hồ sơ.";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // GET: Admin/Document/Cancle
        [HttpPost]
        public async Task<IActionResult> cancledocument(int id, string reason)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            UserModel user = await UserManager.FindByIdAsync(user_id);
            var is_manager = await UserManager.IsInRoleAsync(user, "Manager Esign");
            var is_admin = await UserManager.IsInRoleAsync(user, "Administrator");
            if (checkPermission("cancledocument", id, is_admin, is_manager) != 0)
            {
                return NotFound();
            }

            var DocumentModel = _context.DocumentModel
              .Where(d => d.id == id)
              .Include(d => d.users_follow)
              .Include(d => d.users_signature)
              .Include(d => d.users_receive)
              .FirstOrDefault();
            DocumentModel.status_id = 3;
            DocumentModel.date_finish = DateTime.Now;
            DocumentModel.reason = reason;
            _context.DocumentModel.Update(DocumentModel);
            _context.SaveChanges();


            ////SEND MAIL 
            var user_create = await UserManager.FindByIdAsync(DocumentModel.user_id);
            var users_signature_signed = _context.DocumentSignatureModel.Where(u => u.document_id == DocumentModel.id && u.status == 2).Include(d => d.user_s).Select(d => d.user_s.Email).ToList();
            users_signature_signed.Add(user_create.Email);
            users_signature_signed = users_signature_signed.Distinct().ToList();
            var mail_string = string.Join(",", users_signature_signed.ToArray());
            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
            var body = _view.Render("Emails/CancleDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel.id, reason = reason });
            var email = new EmailModel
            {
                email_to = mail_string,
                subject = "[Hủy] " + DocumentModel.name_vi,
                body = body,
                email_type = "cancle_document",
                status = 1
            };
            _context.Add(email);

            //create event
            DocumentEventModel DocumentEventModel = new DocumentEventModel
            {
                document_id = DocumentModel.id,
                event_content = "<b>" + user.FullName + "</b> đã hủy hồ sơ này<div><span>Lý do: " + reason + "</span></div>",
                created_at = DateTime.Now,
            };
            _context.Add(DocumentEventModel);

            var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
            var users_signature = DocumentModel.users_signature.Select(a => a.user_id).ToList();
            var users_representative = DocumentModel.users_signature.Where(a => a.representative_id != null).Select(a => a.representative_id).ToList();
            var users_receive = DocumentModel.users_receive.Select(a => a.user_id).ToList();
            List<string> users_related = new List<string>();
            users_related.AddRange(users_follow);
            users_related.AddRange(users_signature);
            users_related.AddRange(users_representative);
            users_related.AddRange(users_receive);
            users_related = users_related.Distinct().ToList();
            var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
            users_related.Remove(itemToRemove);
            var items = new List<DocumentUserUnreadModel>();
            foreach (string u in users_related)
            {
                items.Add(new DocumentUserUnreadModel
                {
                    user_id = u,
                    document_id = DocumentModel.id,
                    time = DateTime.Now,
                });
            }
            _context.AddRange(items);
            //await _context.SaveChangesAsync();

            /// Audittrail
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Update.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã hủy hồ sơ.";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Document/addComment
        [HttpPost]
        public async Task<IActionResult> AddComment(DocumentCommentModel DocumentCommentModel)
        {
            if (_context.DocumentCommentModel == null)
            {
                return Problem("Entity set 'ItContext.DocumentCommentModel'  is null.");
            }
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user = await UserManager.GetUserAsync(currentUser); // Get user id:
            DocumentCommentModel.user_id = user_id;
            DocumentCommentModel.created_at = DateTime.Now;
            _context.Add(DocumentCommentModel);
            _context.SaveChanges();
            var files = Request.Form.Files;

            var items_comment = new List<DocumentCommentFileModel>();
            if (files != null && files.Count > 0)
            {

                foreach (var file in files)
                {
                    var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    string name = file.FileName;
                    string ext = Path.GetExtension(name);
                    string mimeType = file.ContentType;

                    //var fileName = Path.GetFileName(name);
                    var newName = timeStamp + " - " + name;

                    newName = newName.Replace("+", "_");
                    newName = newName.Replace("%", "_");
                    var filePath = "private\\documents\\" + DocumentCommentModel.document_id + "\\" + newName;
                    string url = "/private/documents/" + DocumentCommentModel.document_id + "/" + newName;
                    items_comment.Add(new DocumentCommentFileModel
                    {
                        ext = ext,
                        url = url,
                        name = name,
                        mimeType = mimeType,
                        document_comment_id = DocumentCommentModel.id,
                        created_at = DateTime.Now
                    });

                    using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileSrteam);
                    }
                }
                _context.AddRange(items_comment);
                _context.SaveChanges();
            }
            ////
            ///
            DocumentUserReadModel user_read = _context.DocumentUserReadModel.Where(d => d.document_id == DocumentCommentModel.document_id && d.user_id == DocumentCommentModel.user_id).FirstOrDefault();
            if (user_read == null)
            {
                user_read = new DocumentUserReadModel
                {
                    document_id = DocumentCommentModel.document_id,
                    user_id = DocumentCommentModel.user_id,
                    time_read = DateTime.Now,
                };
                _context.Add(user_read);
            }
            else
            {
                user_read.time_read = DateTime.Now;
                _context.Update(user_read);
            }

            ///create unread

            var DocumentModel = _context.DocumentModel
                        .Where(d => d.id == DocumentCommentModel.document_id)
                        .Include(d => d.users_follow)
                        .Include(d => d.users_signature)
                        .Include(d => d.users_receive)
                        .FirstOrDefault();

            var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
            var users_signature = DocumentModel.users_signature.Select(a => a.user_id).ToList();
            var users_representative = DocumentModel.users_signature.Where(a => a.representative_id != null).Select(a => a.representative_id).ToList();
            var users_receive = DocumentModel.users_receive.Select(a => a.user_id).ToList();
            List<string> users_related = new List<string>();
            users_related.AddRange(users_follow);
            users_related.AddRange(users_signature);
            users_related.AddRange(users_representative);
            users_related.AddRange(users_receive);
            users_related = users_related.Distinct().ToList();
            var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
            users_related.Remove(itemToRemove);
            var items = new List<DocumentUserUnreadModel>();
            foreach (string u in users_related)
            {
                items.Add(new DocumentUserUnreadModel
                {
                    user_id = u,
                    document_id = DocumentModel.id,
                    time = DateTime.Now,
                });
            }
            _context.AddRange(items);
            //SEND MAIL
            if (users_related != null)
            {
                var users_related_obj = _context.UserModel.Where(d => users_related.Contains(d.Id)).Select(d => d.Email).ToList();
                var mail_string = string.Join(",", users_related_obj.ToArray());
                string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                var attach = items_comment.Select(d => d.url).ToList();
                var text = DocumentCommentModel.comment;
                if (attach.Count() > 0 && DocumentCommentModel.comment == null)
                {
                    text = $"{user.FullName} gửi đính kèm";
                }
                var body = _view.Render("Emails/NewComment",
                    new
                    {
                        link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png",
                        link = Domain + "/admin/document/details/" + DocumentModel.id,
                        text = text,
                        name = user.FullName
                    });


                var email = new EmailModel
                {
                    email_to = mail_string,
                    subject = "[Tin nhắn mới] " + DocumentModel.name_vi,
                    body = body,
                    email_type = "new_comment_document",
                    status = 1,
                    data_attachments = attach

                };
                _context.Add(email);
            }
            //await _context.SaveChangesAsync();

            /// Audittrail
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Update.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã thêm bình luận.";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            DocumentCommentModel.user = await UserManager.GetUserAsync(currentUser);
            DocumentCommentModel.is_read = true;

            return Json(new
            {
                success = 1,
                comment = DocumentCommentModel
            });
        }


        public async Task<IActionResult> MoreComment(int document_id, int from_id, int limit)
        {
            List<DocumentCommentModel> comments = _context.DocumentCommentModel
                .Where(d => d.document_id == document_id && d.id < from_id && d.deleted_at == null)
                .OrderByDescending(d => d.id)
                .Take(limit).Include(d => d.files).Include(d => d.user).ToList();
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string current_user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_read = _context.DocumentUserReadModel.Where(d => d.user_id == current_user_id && d.document_id == document_id).FirstOrDefault();
            DateTime? time_read = null;
            if (user_read != null)
                time_read = user_read.time_read;

            foreach (var comment in comments)
            {
                if (comment.user_id == current_user_id)
                {
                    comment.is_read = true;
                    continue;
                }
                if (time_read != null && comment.created_at <= time_read)
                    comment.is_read = true;
            }
            return Json(new { success = 1, comments = comments });
        }
        [HttpPost]
        public async Task<JsonResult> GetType(int id)
        {
            DocumentTypeModel type = _context.DocumentTypeModel.Where(d => d.id == id).Include(d => d.users_follow).Include(d => d.users_receive).FirstOrDefault();
            return Json(new { success = 1, item = type });
        }
        private int checkPermission(string permission, int? document_id, bool is_admin = false, bool is_manager = false)
        {
            if (permission == "edit")
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                ///CHỈ user tạo và user follow
                var DocumentModel = _context.DocumentModel
                            .Where(d => d.id == document_id)
                            .Include(d => d.users_follow)
                            .FirstOrDefault();
                if (DocumentModel == null)
                {
                    return 1;
                }
                var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
                if (is_admin)
                {
                    users_follow.Add(user_id);
                }
                List<string> users_related = new List<string>();
                users_related.AddRange(users_follow);
                users_related.Add(DocumentModel.user_id);
                users_related = users_related.Distinct().ToList();
                if (!users_related.Contains(user_id)) /// CHECK QUYỀN
				{
                    return 1;
                }
            }
            else if (permission == "delete")
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                ///CHỈ user tạo 
                var DocumentModel = _context.DocumentModel
                            .Where(d => d.id == document_id)
                            .FirstOrDefault();
                if (DocumentModel == null)
                {
                    return 1;
                }
                List<string> users_related = new List<string>();
                users_related.Add(DocumentModel.user_id);
                users_related = users_related.Distinct().ToList();
                if (!users_related.Contains(user_id)) /// CHECK QUYỀN
				{
                    return 1;
                }

            }
            else if (permission == "details")
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                ///CHỈ user tạo và user follow
                var DocumentModel = _context.DocumentModel
                            .Where(d => d.id == document_id)
                            .Include(d => d.users_follow)
                            .Include(d => d.users_signature)
                            .Include(d => d.users_receive)
                            .FirstOrDefault();
                if (DocumentModel == null)
                {
                    return 1;
                }
                List<string> users_related = new List<string>();
                var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
                var users_signature = DocumentModel.users_signature.Select(a => a.user_id).ToList();
                var users_representative = DocumentModel.users_signature.Where(a => a.representative_id != null).Select(a => a.representative_id).ToList();
                var users_receive = DocumentModel.users_receive.Select(a => a.user_id).ToList();
                if (is_admin)
                {
                    users_follow.Add(user_id);
                }
                users_related.AddRange(users_follow);
                users_related.AddRange(users_signature);
                users_related.AddRange(users_receive);
                users_related.AddRange(users_representative);
                users_related.Add(DocumentModel.user_id);
                users_related = users_related.Distinct().ToList();
                //if (!users_related.Contains(user_id)) /// CHECK QUYỀN
                //{
                //	return 1;
                //}

            }
            else if (permission == "suggestsign")
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                ///CHỈ user tạo và user follow
                var DocumentModel = _context.DocumentModel
                            .Where(d => d.id == document_id)
                            .Include(d => d.users_follow)
                            .FirstOrDefault();
                if (DocumentModel == null)
                {
                    return 1;
                }
                var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
                List<string> users_related = new List<string>();
                if (is_admin)
                {
                    users_follow.Add(user_id);
                }
                users_related.AddRange(users_follow);
                users_related.Add(DocumentModel.user_id);
                users_related = users_related.Distinct().ToList();
                if (!users_related.Contains(user_id)) /// CHECK QUYỀN
				{
                    return 1;
                }


            }
            else if (permission == "sign")
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                ///CHỈ user tạo và user follow
                var DocumentModel = _context.DocumentModel
                        .Where(d => d.id == document_id)
                        .FirstOrDefault();
                if (DocumentModel == null)
                {
                    return 1;
                }
                if (DocumentModel.status_id != 2)
                {
                    return 1;
                }
                bool is_sign = false;
                if (DocumentModel.is_sign_parellel == true)
                {
                    var user_signature = _context.DocumentSignatureModel.Where(u => u.status == 1 && u.user_id == user_id && u.document_id == document_id).OrderBy(u => u.stt).FirstOrDefault();
                    if (user_signature != null)
                    {
                        is_sign = true;
                    }
                }
                else
                {
                    var user_signature = _context.DocumentSignatureModel.Where(u => u.status == 1 && u.document_id == document_id).OrderBy(u => u.stt).FirstOrDefault();

                    //return Ok(users_signture.Count);
                    if (user_signature != null && (user_signature.user_id == user_id))
                    {
                        is_sign = true;
                    }

                }
                if (is_sign == false)
                {
                    return 1;
                }
            }
            else if (permission == "cancle_signature")
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                var DocumentModel = _context.DocumentModel
                        .Where(d => d.id == document_id)
                        .FirstOrDefault();
                if (DocumentModel == null)
                {
                    return 1;
                }
                if (DocumentModel.status_id != 2)
                {
                    return 1;
                }
                bool is_sign = false;
                if (DocumentModel.is_sign_parellel == true)
                {
                    var user_signature = _context.DocumentSignatureModel.Where(u => u.status == 1 && u.user_id == user_id && u.document_id == document_id).OrderBy(u => u.stt).FirstOrDefault();
                    if (user_signature != null)
                    {
                        is_sign = true;
                    }
                }
                else
                {
                    var user_signature = _context.DocumentSignatureModel.Where(u => u.status == 1 && u.document_id == document_id).OrderBy(u => u.stt).FirstOrDefault();

                    //return Ok(users_signture.Count);
                    if (user_signature != null && (user_signature.user_id == user_id))
                    {
                        is_sign = true;
                    }

                }
                if (is_sign == false)
                {
                    return 1;
                }
            }
            else if (permission == "cancledocument")
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                var DocumentModel = _context.DocumentModel
                            .Where(d => d.id == document_id)
                            .Include(d => d.users_follow)
                            .FirstOrDefault();

                if (DocumentModel == null)
                {
                    return 1;
                }
                var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
                if (is_admin)
                {
                    users_follow.Add(user_id);
                }
                List<string> users_related = new List<string>();
                users_related.AddRange(users_follow);
                users_related.Add(DocumentModel.user_id);
                users_related = users_related.Distinct().ToList();
                if (!users_related.Contains(user_id)) /// CHECK QUYỀN
				{
                    return 1;
                }

            }
            return 0;
        }

        [Authorize(Roles = "Administrator")]
        public async Task<JsonResult> rollback(int id)
        {
            DocumentModel DocumentModel = _context.DocumentModel.Where(d => d.id == id)
                .Include(d => d.users_signature)
                .ThenInclude(d => d.user)
                .Include(d => d.users_signature)
                .ThenInclude(d => d.representative)
                .Include(d => d.users_follow)
                .Include(d => d.users_receive)
                .Include(d => d.files)
                .FirstOrDefault();
            DocumentModel.status_id = 2;
            var user_signature = DocumentModel.users_signature.Where(d => d.status == 2 || d.status == 3).OrderBy(d => d.stt).LastOrDefault();
            var count = DocumentModel.users_signature.Where(d => d.status == 2 || d.status == 3).Count();
            user_signature.status = 1;
            user_signature.date = null;
            user_signature.reason = null;
            DocumentModel.user_next_signature_id = user_signature.user_id;
            DocumentModel.user_next_representative_id = user_signature.representative_id;

            //SEND MAIL
            var mail_string = user_signature.representative != null ? user_signature.representative.Email : user_signature.user.Email;
            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
            var body = _view.Render("Emails/WaitSignDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/details/" + DocumentModel.id });
            var email = new EmailModel
            {
                email_to = mail_string,
                subject = "[Đang chờ ký] " + DocumentModel.name_vi,
                body = body,
                email_type = "wait_sign_document",
                status = 1,

            };
            _context.Add(email);
            //create event
            DocumentEventModel DocumentEventModel = new DocumentEventModel
            {
                document_id = DocumentModel.id,
                event_content = "<b>Administrator</b> đã khôi phục tình trạng ký tên",
                created_at = DateTime.Now,
            };
            _context.Add(DocumentEventModel);
            //Create unread 
            var users_follow = DocumentModel.users_follow.Select(a => a.user_id).ToList();
            var users_signature = DocumentModel.users_signature.Select(a => a.user_id).ToList();
            var users_representative = DocumentModel.users_signature.Where(a => a.representative_id != null).Select(a => a.representative_id).ToList();
            var users_receive = DocumentModel.users_receive.Select(a => a.user_id).ToList();
            List<string> users_related = new List<string>();
            users_related.AddRange(users_follow);
            users_related.AddRange(users_signature);
            users_related.AddRange(users_representative);
            users_related.AddRange(users_receive);
            users_related = users_related.Distinct().ToList();
            //var itemToRemove = users_related.SingleOrDefault(r => r == user_id);
            //users_related.Remove(itemToRemove);
            var items = new List<DocumentUserUnreadModel>();
            foreach (string u in users_related)
            {
                items.Add(new DocumentUserUnreadModel
                {
                    user_id = u,
                    document_id = DocumentModel.id,
                    time = DateTime.Now,
                });
            }
            _context.AddRange(items);
            ///XÓA FILE
            var files = DocumentModel.files.ToList();
            if (files.Count > count)
            {
                var file = files.OrderBy(d => d.created_at).LastOrDefault();
                _context.Remove(file);
            }
            _context.Update(user_signature);
            _context.Update(DocumentModel);
            _context.SaveChanges();
            return Json(new { success = 1 });
        }

        [HttpPost]
        public async Task<JsonResult> mergePdf(List<List<int>>? list_page)
        {
            var pdfList = new List<byte[]>();
            var files = Request.Form.Files;
            //return Json(list_page);
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    using (var ms1 = new MemoryStream())
                    {
                        file.CopyTo(ms1);
                        var fileBytes = ms1.ToArray();
                        pdfList.Add(fileBytes);
                    }

                }
            }
            var ms = Combine(pdfList, list_page);

            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var link = "/private/check/" + timeStamp + ".pdf";
            System.IO.File.WriteAllBytes("." + link, ms);
            return Json(new { success = true, link = link });
        }
        public byte[] Combine(List<byte[]> pdfs, List<List<int>>? list_page)
        {
            using (var writerMemoryStream = new MemoryStream())
            {
                using (var writer = new PdfWriter(writerMemoryStream))
                {
                    using (var mergedDocument = new PdfDocument(writer))
                    {
                        var merger = new PdfMerger(mergedDocument);

                        for (int key = 0; key < pdfs.Count(); key++)
                        {
                            var pdfBytes = pdfs[key];
                            using (var copyFromMemoryStream = new MemoryStream(pdfBytes))
                            {
                                using (var reader = new PdfReader(copyFromMemoryStream))
                                {
                                    using (var copyFromDocument = new PdfDocument(reader))
                                    {
                                        var pages = list_page != null && list_page.Count() > 0 ? list_page[key] : new List<int>();
                                        if (list_page == null || list_page.Count() == 0)
                                        {
                                            merger.Merge(copyFromDocument, 1, copyFromDocument.GetNumberOfPages());
                                        }
                                        else
                                        {
                                            merger.Merge(copyFromDocument, pages);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return writerMemoryStream.ToArray();
            }
        }
        public async Task<IActionResult> viewSupersededEsign(int id)
        {
            var file_esign = _context.DocumentFileModel.Where(d => d.id == id).FirstOrDefault();
            if (file_esign == null)
                return Ok();


            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var pathroot = "wwwroot\\temp";
            bool exists = System.IO.Directory.Exists(pathroot);

            if (!exists)
                System.IO.Directory.CreateDirectory(pathroot);

            string filePath = "wwwroot\\temp\\" + timeStamp + ".pdf";

            // Create a FileStream to write the PDF file
            var dest = new PdfWriter(filePath);
            var reader = new PdfReader(file_esign.url.Replace("/private/", _configuration["Source:Path_Private"] + "\\").Replace("/", "\\"));
            PdfDocument pdfDoc = new PdfDocument(reader, dest);
            iText.Layout.Document doc = new iText.Layout.Document(pdfDoc);
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new SupersededEventHandler(doc, Domain));




            // Create a paragraph with the text you want to add
            //iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph("Hello, World!");

            //// Apply a blur effect to the text
            //paragraph.SetStrokeColor(ColorConstants.GRAY);
            //paragraph.SetOpacity(0.2f);

            //doc.Add(paragraph);
            doc.Close();
            //return Ok();

            var myfile = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(myfile, "application/pdf");

        }
        public async Task<IActionResult> viewObsoletedEsign(int id)
        {
            var file_esign = _context.DocumentFileModel.Where(d => d.id == id).FirstOrDefault();
            if (file_esign == null)
                return Ok();

            //System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            //var user = await UserManager.GetUserAsync(currentUser);
            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var pathroot = "wwwroot\\temp";
            bool exists = System.IO.Directory.Exists(pathroot);

            if (!exists)
                System.IO.Directory.CreateDirectory(pathroot);

            string filePath = "wwwroot\\temp\\" + timeStamp + ".pdf";

            // Create a FileStream to write the PDF file
            var dest = new PdfWriter(filePath);
            var reader = new PdfReader(file_esign.url.Replace("/private/", _configuration["Source:Path_Private"] + "\\").Replace("/", "\\"));
            PdfDocument pdfDoc = new PdfDocument(reader, dest);
            iText.Layout.Document doc = new iText.Layout.Document(pdfDoc);
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new ObsoletedEventHandler(doc, Domain));




            // Create a paragraph with the text you want to add
            //iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph("Hello, World!");

            //// Apply a blur effect to the text
            //paragraph.SetStrokeColor(ColorConstants.GRAY);
            //paragraph.SetOpacity(0.2f);

            //doc.Add(paragraph);
            doc.Close();
            //return Ok();

            var myfile = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(myfile, "application/pdf");

        }
        public async Task<IActionResult> viewPrintEsign(int id)
        {
            var file_esign = _context.DocumentFileModel.Where(d => d.id == id).FirstOrDefault();
            if (file_esign == null)
                return Ok();

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var user_name = user.FullName;
            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var pathroot = "wwwroot\\temp";
            bool exists = System.IO.Directory.Exists(pathroot);

            if (!exists)
                System.IO.Directory.CreateDirectory(pathroot);

            string filePath = "wwwroot\\temp\\" + timeStamp + ".pdf";

            // Create a FileStream to write the PDF file
            var dest = new PdfWriter(filePath);
            var reader = new PdfReader(file_esign.url.Replace("/private/", _configuration["Source:Path_Private"] + "\\").Replace("/", "\\"));
            PdfDocument pdfDoc = new PdfDocument(reader, dest);
            iText.Layout.Document doc = new iText.Layout.Document(pdfDoc);
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TextFooterEventHandler(doc, user_name));




            // Create a paragraph with the text you want to add
            //iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph("Hello, World!");

            //// Apply a blur effect to the text
            //paragraph.SetStrokeColor(ColorConstants.GRAY);
            //paragraph.SetOpacity(0.2f);

            //doc.Add(paragraph);
            doc.Close();
            //return Ok();

            var myfile = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(myfile, "application/pdf");

        }
    }
    // Tạo lớp PdfPageEventHelper để tạo footer tùy chỉnh
    public class TextFooterEventHandler : IEventHandler
    {
        protected iText.Layout.Document doc;
        protected string _username;

        public TextFooterEventHandler(iText.Layout.Document doc, string username)
        {
            this.doc = doc;
            _username = username;
        }

        public void HandleEvent(Event currentEvent)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
            iText.Kernel.Geom.Rectangle pageSize = docEvent.GetPage().GetPageSize();
            PdfFont fontProgram = null;
            try
            {

                fontProgram = PdfFontFactory.CreateFont("./wwwroot/assets/fonts/vuArial.ttf");
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);
            iText.Layout.Element.Table table = new iText.Layout.Element.Table(3);
            var current = DateTime.Now;
            table.SetWidth(UnitValue.CreatePercentValue(100));
            table.AddCell(new Cell().Add(new Paragraph("User")));
            table.AddCell(new Cell().Add(new Paragraph("Printing date")));
            table.AddCell(new Cell().Add(new Paragraph("Remark")));

            table.AddCell(new Cell().Add(new Paragraph(_username)));
            table.AddCell(new Cell().Add(new Paragraph(current.ToString("dd-MM-yyyy HH:mm:ss"))));
            table.AddCell(new Cell().Add(new Paragraph("Document is only vaild on the day of printing")));
            table.SetFont(fontProgram);
            table.SetFontSize(8);
            table.SetFixedPosition(doc.GetLeftMargin(), doc.GetBottomMargin(), pageSize.GetWidth() - doc.GetLeftMargin() - doc.GetRightMargin());

            // Chèn bảng vào footer
            canvas.Add(table);
            canvas.Close();
        }
    }
    public class SupersededEventHandler : IEventHandler
    {
        protected iText.Layout.Document doc;
        protected string _domain;

        public SupersededEventHandler(iText.Layout.Document doc, string Domain)
        {
            this.doc = doc;
            this._domain = Domain;
        }

        public void HandleEvent(Event currentEvent)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
            iText.Kernel.Geom.Rectangle pageSize = docEvent.GetPage().GetPageSize();

            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);


            var da = ImageDataFactory.CreatePng(new Uri(_domain + "/images/Superseded.png"));
            iText.Layout.Element.Image image = new iText.Layout.Element.Image(da);

            image.SetAutoScale(true);
            image.ScaleAbsolute(175, 35);
            image.SetFixedPosition(pageSize.GetWidth() - 175, pageSize.GetHeight() - 35, pageSize.GetWidth() - doc.GetLeftMargin() - doc.GetRightMargin());

            // Chèn bảng vào footer
            canvas.Add(image);
            canvas.Close();
        }
    }
    public class ObsoletedEventHandler : IEventHandler
    {
        protected iText.Layout.Document doc;
        protected string _domain;

        public ObsoletedEventHandler(iText.Layout.Document doc, string Domain)
        {
            this.doc = doc;
            this._domain = Domain;
        }

        public void HandleEvent(Event currentEvent)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
            iText.Kernel.Geom.Rectangle pageSize = docEvent.GetPage().GetPageSize();

            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);

            var da = ImageDataFactory.CreatePng(new Uri(_domain + "/images/Obsoleted.png"));
            iText.Layout.Element.Image image = new iText.Layout.Element.Image(da);

            image.SetAutoScale(true);
            image.ScaleAbsolute(175, 35);
            image.SetFixedPosition(pageSize.GetWidth() - 175, pageSize.GetHeight() - 35, pageSize.GetWidth() - doc.GetLeftMargin() - doc.GetRightMargin());
            // Chèn bảng vào footer
            canvas.Add(image);
            canvas.Close();
        }
    }

    public class ForTranningEventHandler : IEventHandler
    {
        protected iText.Layout.Document doc;

        public ForTranningEventHandler(iText.Layout.Document doc)
        {
            this.doc = doc;
        }

        public void HandleEvent(Event currentEvent)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
            iText.Kernel.Geom.Rectangle pageSize = docEvent.GetPage().GetPageSize();

            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);

            // Create a paragraph with the text you want to add
            iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph("ONLY FOR TRAINING");

            // Apply a blur effect to the text
            paragraph.SetStrokeColor(ColorConstants.GRAY);
            paragraph.SetOpacity(0.2f);
            paragraph.SetRotationAngle(-45);
            paragraph.SetFontSize(30);
            paragraph.SetWidth(UnitValue.CreatePercentValue(100));
            paragraph.SetFixedPosition(pageSize.GetWidth() / 2, pageSize.GetHeight() / 2, pageSize.GetWidth() - doc.GetLeftMargin() - doc.GetRightMargin());

            canvas.Add(paragraph);
            canvas.Close();
        }
    }
    public class PdfSignerNoObjectStream : PdfSigner
    {
        public PdfSignerNoObjectStream(PdfReader reader, Stream outputStream, StampingProperties properties) : base(reader, outputStream, properties)
        {
        }

        protected override PdfDocument InitDocument(PdfReader reader, PdfWriter writer, StampingProperties properties)
        {
            try
            {
                return base.InitDocument(reader, writer, properties);
            }
            finally
            {
                if (reader.HasHybridXref())
                {
                    FieldInfo propertiesField = typeof(PdfWriter).GetField("properties", BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    WriterProperties writerProperties = (WriterProperties)propertiesField.GetValue(writer);
                    writerProperties.SetFullCompressionMode(false);
                }
            }
        }
    }
}

