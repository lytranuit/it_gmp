using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections;

namespace it.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class DocumentTypeGroupController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "DocumentTypeGroup";
        public DocumentTypeGroupController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/DocumentTypeGroup
        public IActionResult Index()
        {
            return View();
        }


        // GET: Admin/DocumentTypeGroup/Create
        public IActionResult Create()
        {
            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "< " + a.Email + " >"
            }).ToList();
            return View();
        }

        // POST: Admin/DocumentTypeGroup/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(DocumentTypeGroupModel DocumentTypeGroupModel)
        {
            if (ModelState.IsValid)
            {
                DocumentTypeGroupModel.created_at = DateTime.Now;
                _context.Add(DocumentTypeGroupModel);
                _context.SaveChanges();

                /// Audittrail
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                var user = await UserManager.GetUserAsync(currentUser);
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Create.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã tạo một nhóm hồ sơ mới.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Ok(ModelState);
        }

        // GET: Admin/DocumentTypeGroup/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.DocumentTypeGroupModel == null)
            {
                return NotFound();
            }

            var DocumentTypeGroupModel = _context.DocumentTypeGroupModel
                .Where(d => d.id == id).FirstOrDefault();
            if (DocumentTypeGroupModel == null)
            {
                return NotFound();
            }
            return View(DocumentTypeGroupModel);
        }

        // POST: Admin/DocumentTypeGroup/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, DocumentTypeGroupModel DocumentTypeGroupModel)
        {

            if (id != DocumentTypeGroupModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var DocumentTypeGroupModel_old = await _context.DocumentTypeGroupModel.FindAsync(id);
                DocumentTypeGroupModel_old.updated_at = DateTime.Now;
                DocumentTypeGroupModel_old.name = DocumentTypeGroupModel.name;
                DocumentTypeGroupModel_old.stt = DocumentTypeGroupModel.stt;

                _context.Update(DocumentTypeGroupModel_old);
                _context.SaveChanges();

                /// Audittrail
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                var user = await UserManager.GetUserAsync(currentUser);
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Update.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã chỉnh sửa nhóm hồ sơ.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(DocumentTypeGroupModel);
        }


        // GET: Admin/DocumentTypeGroup/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.DocumentTypeGroupModel == null)
            {
                return Problem("Entity set 'ItContext.DocumentTypeGroupModel'  is null.");
            }
            var DocumentTypeGroupModel = await _context.DocumentTypeGroupModel.FindAsync(id);
            if (DocumentTypeGroupModel != null)
            {
                DocumentTypeGroupModel.deleted_at = DateTime.Now;
                _context.DocumentTypeGroupModel.Update(DocumentTypeGroupModel);
            }
            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Delete.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã xóa nhóm hồ sơ.";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<JsonResult> Table()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var customerData = (from tempcustomer in _context.DocumentTypeGroupModel select tempcustomer);
            int recordsTotal = customerData.Count();
            customerData = customerData.Where(m => m.deleted_at == null);
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.name.Contains(searchValue));
            }
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.created_at).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var data1 = new
                {
                    action = "<div class='btn-group'><a href='/admin/" + _type + "/delete/" + record.id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>",
                    id = "<a href='/admin/" + _type + "/edit/" + record.id + "'><i class='fas fa-pencil-alt mr-2'></i> " + record.id + "</a>",
                    name = record.name,
                    stt = record.stt,
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }


    }
}
