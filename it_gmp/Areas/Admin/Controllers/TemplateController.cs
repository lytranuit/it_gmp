using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace it.Areas.Admin.Controllers
{
    public class TemplateController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "Template";
        public TemplateController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/Template
        public IActionResult Index()
        {
            return View();
        }



        [Authorize(Roles = "Administrator")]
        // GET: Admin/Template/Create
        public IActionResult Create()
        {
            ViewData["type"] = _context.DocumentTypeModel.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();
            return View();
        }

        // POST: Admin/Template/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(TemplateModel TemplateModel)
        {
            if (ModelState.IsValid)
            {
                var files = Request.Form.Files;
                var list_file = new List<IFormFile>();
                var list_attachment = new List<IFormFile>();
                foreach (IFormFile f in files)
                {
                    if (f.Name == "file")
                    {
                        list_file.Add(f);
                    }
                }
                if (list_file == null || list_file.Count == 0)
                {
                    return Ok("Require a File");
                }
                var file = list_file[0];
                var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                string name = file.FileName;
                string ext = Path.GetExtension(name);
                string mimeType = file.ContentType;

                var newName = timeStamp + "-" + name;
                var pathroot = "private\\template\\";
                bool exists = System.IO.Directory.Exists(pathroot);

                if (!exists)
                    System.IO.Directory.CreateDirectory(pathroot);
                string filePath = pathroot + newName;
                string url = "/" + pathroot.Replace("\\", "/") + newName;
                using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileSrteam);
                }
                TemplateModel.file_url = url;
                TemplateModel.created_at = DateTime.Now;
                _context.Add(TemplateModel);
                _context.SaveChanges();

                /// Audittrail
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                var user = await UserManager.GetUserAsync(currentUser);
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Create.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã thêm 1 biểu mẫu mới.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Ok(ModelState);
        }


        [Authorize(Roles = "Administrator")]
        // GET: Admin/Template/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.TemplateModel == null)
            {
                return NotFound();
            }

            var TemplateModel = _context.TemplateModel
                .Where(d => d.id == id).FirstOrDefault();
            if (TemplateModel == null)
            {
                return NotFound();
            }
            ViewData["type"] = _context.DocumentTypeModel.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();
            return View(TemplateModel);
        }

        // POST: Admin/Template/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, TemplateModel TemplateModel, List<string> users_follow)
        {

            if (id != TemplateModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var TemplateModel_old = await _context.TemplateModel.FindAsync(id);
                    TemplateModel_old.updated_at = DateTime.Now;
                    TemplateModel_old.name = TemplateModel.name;
                    TemplateModel_old.type_id = TemplateModel.type_id;

                    var files = Request.Form.Files;
                    var list_file = new List<IFormFile>();
                    var list_attachment = new List<IFormFile>();
                    foreach (IFormFile f in files)
                    {
                        if (f.Name == "file")
                        {
                            list_file.Add(f);
                        }
                    }
                    if (list_file != null && list_file.Count > 0)
                    {
                        var file = list_file[0];
                        var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                        string name = file.FileName;
                        string ext = Path.GetExtension(name);
                        string mimeType = file.ContentType;

                        var newName = timeStamp + "-" + name;
                        var pathroot = "private\\template\\" + TemplateModel_old.id + "\\";
                        bool exists = System.IO.Directory.Exists(pathroot);

                        if (!exists)
                            System.IO.Directory.CreateDirectory(pathroot);
                        string filePath = pathroot + newName;
                        string url = "/" + pathroot.Replace("\\", "/") + newName;
                        using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileSrteam);
                        }
                        TemplateModel_old.file_url = url;
                    }

                    _context.Update(TemplateModel_old);
                    _context.SaveChanges();

                    /// Audittrail
                    System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                    var user = await UserManager.GetUserAsync(currentUser);
                    var audit = new AuditTrailsModel();
                    audit.UserId = user.Id;
                    audit.Type = AuditType.Update.ToString();
                    audit.DateTime = DateTime.Now;
                    audit.description = $"Tài khoản {user.FullName} đã sửa một biểu mẫu.";
                    _context.Add(audit);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View(TemplateModel);
        }


        [Authorize(Roles = "Administrator")]
        // GET: Admin/Template/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.TemplateModel == null)
            {
                return Problem("Entity set 'ItContext.TemplateModel'  is null.");
            }
            var TemplateModel = await _context.TemplateModel.FindAsync(id);
            if (TemplateModel != null)
            {
                TemplateModel.deleted_at = DateTime.Now;
                _context.TemplateModel.Update(TemplateModel);
            }

            _context.SaveChanges();
            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Delete.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã xóa một biểu mẫu.";
            _context.Add(audit);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Up(int id)
        {
            if (_context.TemplateModel == null)
            {
                return Problem("Entity set 'ItContext.TemplateModel'  is null.");
            }
            var TemplateModel = await _context.TemplateModel.FindAsync(id);
            if (TemplateModel != null)
            {
                TemplateModel.created_at = DateTime.Now;
                _context.TemplateModel.Update(TemplateModel);
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<JsonResult> Table()
        {

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var is_admin = await UserManager.IsInRoleAsync(user, "Administrator");
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var customerData = (from tempcustomer in _context.TemplateModel select tempcustomer);
            int recordsTotal = customerData.Count();
            customerData = customerData.Where(m => m.deleted_at == null);
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.name.Contains(searchValue));
            }
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.created_at).Skip(skip).Take(pageSize).Include(d => d.type).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var type_name = record.type != null ? record.type.name : "";
                var file = record.file_url != null ? "<a href='" + record.file_url + "' download class='btn btn-secondary btn-xs'><i class='fas fa-file-excel mr-2'></i>Download</a>" : "";
                var id = record.id.ToString();
                var action = "<div class='btn-group'>" + file + "</div>";
                if (is_admin)
                {
                    id = "<a href='/admin/" + _type + "/edit/" + record.id + "'><i class='fas fa-pencil-alt mr-2'></i> " + record.id + "</a>";
                    action = "<div class='btn-group'>" + file + "<a href='/admin/" + _type + "/up/" + record.id + "' class='btn btn-primary btn-sm' title='Up?' data-type='confirm'>"
                        + "<i class='fas fa-arrow-alt-circle-up'></i>"
                        + "</i>"
                        + "</a><a href='/admin/" + _type + "/delete/" + record.id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>";
                }
                var data1 = new
                {
                    action = action,
                    id = id,
                    name = record.name,
                    code = record.code,
                    type = type_name,
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
    }
}
