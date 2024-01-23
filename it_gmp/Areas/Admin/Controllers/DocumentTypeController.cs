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
    [Authorize(Roles = "Administrator")]
    public class DocumentTypeController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "DocumentType";
        public DocumentTypeController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/DocumentType
        public IActionResult Index()
        {
            return View();
        }


        // GET: Admin/DocumentType/Create
        public IActionResult Create()
        {
            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "< " + a.Email + " >"
            }).ToList();
            ViewData["groups"] = _context.DocumentTypeGroupModel.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();
            return View();
        }

        // POST: Admin/DocumentType/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(DocumentTypeModel documentTypeModel, List<string> users_receive, List<string> users_manager)
        {
            if (ModelState.IsValid)
            {
                documentTypeModel.created_at = DateTime.Now;
                documentTypeModel.is_manager_create = documentTypeModel.is_manager_create != null ? documentTypeModel.is_manager_create : false;
                _context.Add(documentTypeModel);
                _context.SaveChanges();
                //user receive
                if (users_receive != null && users_receive.Count > 0)
                {
                    foreach (string key in users_receive)
                    {
                        DocumentTypeReceiveModel DocumentTypeReceiveModel = new DocumentTypeReceiveModel() { type_id = documentTypeModel.id, user_id = key };
                        _context.Add(DocumentTypeReceiveModel);
                    }

                    _context.SaveChanges();
                }
                //user manager
                if (users_manager != null && users_manager.Count > 0)
                {
                    foreach (string key in users_manager)
                    {
                        UserDocumentTypeModel UserDocumentTypeModel = new UserDocumentTypeModel() { document_type_id = documentTypeModel.id, user_id = key };
                        _context.Add(UserDocumentTypeModel);
                    }

                    _context.SaveChanges();
                }
                /// Audittrail
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                var user = await UserManager.GetUserAsync(currentUser);
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Create.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã tạo một loại hồ sơ mới.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Ok(ModelState);
        }

        // GET: Admin/DocumentType/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.DocumentTypeModel == null)
            {
                return NotFound();
            }

            var documentTypeModel = _context.DocumentTypeModel
                .Where(d => d.id == id)
                .Include(d => d.users_receive)
                .Include(t => t.users_manager).FirstOrDefault();
            if (documentTypeModel == null)
            {
                return NotFound();
            }

            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "< " + a.Email + " >"
            }).ToList();
            ViewData["groups"] = _context.DocumentTypeGroupModel.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();
            return View(documentTypeModel);
        }

        // POST: Admin/DocumentType/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, DocumentTypeModel documentTypeModel, List<string> users_receive, List<string> users_manager)
        {

            if (id != documentTypeModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var documentTypeModel_old = await _context.DocumentTypeModel.FindAsync(id);
                documentTypeModel_old.updated_at = DateTime.Now;
                documentTypeModel_old.name = documentTypeModel.name;
                documentTypeModel_old.is_self_check = true;
                documentTypeModel_old.is_manager_create = documentTypeModel.is_manager_create != null ? documentTypeModel.is_manager_create : false;
                documentTypeModel_old.color = documentTypeModel.color;
                documentTypeModel_old.symbol = documentTypeModel.symbol;
                documentTypeModel_old.stt = documentTypeModel.stt;
                documentTypeModel_old.group_id = documentTypeModel.group_id;

                _context.Update(documentTypeModel_old);
                _context.SaveChanges();
                //user_receive
                /////XÓA và edit lại
                var users_receive_old = _context.DocumentTypeReceiveModel.Where(d => d.type_id == id).ToList();
                _context.RemoveRange(users_receive_old);
                _context.SaveChanges();
                if (users_receive != null && users_receive.Count > 0)
                {
                    foreach (string key in users_receive)
                    {
                        DocumentTypeReceiveModel DocumentTypeReceiveModel = new DocumentTypeReceiveModel() { type_id = id, user_id = key };
                        _context.Add(DocumentTypeReceiveModel);
                    }

                    _context.SaveChanges();
                }
                //user_manager 
                /////XÓA và edit lại
                var users_manager_old = _context.UserDocumentTypeModel.Where(d => d.document_type_id == id).ToList();
                _context.RemoveRange(users_manager_old);
                _context.SaveChanges();
                if (users_manager != null && users_manager.Count > 0)
                {
                    foreach (string key in users_manager)
                    {
                        UserDocumentTypeModel UserDocumentTypeModel = new UserDocumentTypeModel() { document_type_id = id, user_id = key };
                        _context.Add(UserDocumentTypeModel);
                    }

                    _context.SaveChanges();
                }
                /// Audittrail
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                var user = await UserManager.GetUserAsync(currentUser);
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Update.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã chỉnh sửa loại hồ sơ.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(documentTypeModel);
        }


        // GET: Admin/DocumentType/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.DocumentTypeModel == null)
            {
                return Problem("Entity set 'ItContext.DocumentTypeModel'  is null.");
            }
            var documentTypeModel = await _context.DocumentTypeModel.FindAsync(id);
            if (documentTypeModel != null)
            {
                documentTypeModel.deleted_at = DateTime.Now;
                _context.DocumentTypeModel.Update(documentTypeModel);
            }

            _context.SaveChanges();
            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Delete.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã xóa loại hồ sơ.";
            _context.Add(audit);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // GET: Admin/DocumentType/Up/5
        //public async Task<IActionResult> Up(int id)
        //{
        //    if (_context.DocumentTypeModel == null)
        //    {
        //        return Problem("Entity set 'ItContext.DocumentTypeModel'  is null.");
        //    }
        //    var documentTypeModel = await _context.DocumentTypeModel.FindAsync(id);
        //    if (documentTypeModel != null)
        //    {
        //        documentTypeModel.created_at = DateTime.Now;
        //        _context.DocumentTypeModel.Update(documentTypeModel);
        //    }

        //    _context.SaveChanges();
        //    return RedirectToAction(nameof(Index));
        //}

        [HttpPost]
        public async Task<JsonResult> Table()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var customerData = (from tempcustomer in _context.DocumentTypeModel select tempcustomer);
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
                    action = "<div class='btn-group'><a href='/admin/document/types/" + record.id + "' class='btn btn-success btn-sm' title='Xem ngay?'>"
                        + "<i class='fas fa-eye'>"
                        + "</i>"
                        + "</a><a href='/admin/" + _type + "/delete/" + record.id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>",
                    id = "<a href='/admin/" + _type + "/edit/" + record.id + "'><i class='fas fa-pencil-alt mr-2'></i> " + record.id + "</a>",
                    name = record.name,
                    symbol = record.symbol,
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
    }
}
