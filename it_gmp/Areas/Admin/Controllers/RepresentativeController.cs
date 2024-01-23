using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace it.Areas.Admin.Controllers
{
    public class RepresentativeController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "Representative";
        public RepresentativeController(ItContext context, UserManager<UserModel> UserMgr) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
        }

        // GET: Admin/Representative
        public IActionResult Index()
        {
            return View();
        }


        // GET: Admin/Representative/Create
        public IActionResult Create()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null && u.Id != user_id).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "< " + a.Email + " >"
            }).ToList();
            ViewData["list"] = _context.RepresentativeModel
               .Where(d => d.deleted_at == null).ToList();
            return View();
        }

        // POST: Admin/Representative/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(string representative_id, DateTime date_from, DateTime date_to)
        {
            if (ModelState.IsValid)
            {
                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                string user_id = UserManager.GetUserId(currentUser); // Get user id:
                RepresentativeModel RepresentativeModel = new RepresentativeModel()
                {
                    created_at = DateTime.Now,
                    representative_id = representative_id,
                    date_from = date_from,
                    date_to = date_to,
                    user_id = user_id
                };
                _context.Add(RepresentativeModel);
                _context.SaveChanges();

                /// Audittrail
                var user = await UserManager.GetUserAsync(currentUser);
                var audit = new AuditTrailsModel();
                audit.UserId = user.Id;
                audit.Type = AuditType.Create.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã thêm 1 ủy quyền.";
                _context.Add(audit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return Ok(ModelState);
        }

        // GET: Admin/Representative/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || _context.RepresentativeModel == null)
            {
                return NotFound();
            }

            var RepresentativeModel = _context.RepresentativeModel
                .Where(d => d.id == id).FirstOrDefault();
            if (RepresentativeModel == null)
            {
                return NotFound();
            }

            ViewData["users"] = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.FullName + "<" + a.Email + ">"
            }).ToList();
            return View(RepresentativeModel);
        }

        // POST: Admin/Representative/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, RepresentativeModel RepresentativeModel, List<string> users_follow)
        {

            if (id != RepresentativeModel.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var RepresentativeModel_old = await _context.RepresentativeModel.FindAsync(id);
                    RepresentativeModel_old.updated_at = DateTime.Now;

                    foreach (string key in HttpContext.Request.Form.Keys)
                    {
                        var prop = RepresentativeModel_old.GetType().GetProperty(key);

                        dynamic val = Request.Form[key].FirstOrDefault();

                        if (prop != null)
                        {
                            prop.SetValue(RepresentativeModel_old, val);
                        }
                    }
                    _context.Update(RepresentativeModel_old);
                    _context.SaveChanges();

                    /// Audittrail
                    System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                    var user = await UserManager.GetUserAsync(currentUser);
                    var audit = new AuditTrailsModel();
                    audit.UserId = user.Id;
                    audit.Type = AuditType.Update.ToString();
                    audit.DateTime = DateTime.Now;
                    audit.description = $"Tài khoản {user.FullName} đã sửa ủy quyền.";
                    _context.Add(audit);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View(RepresentativeModel);
        }


        // GET: Admin/Representative/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.RepresentativeModel == null)
            {
                return Problem("Entity set 'ItContext.RepresentativeModel'  is null.");
            }
            var RepresentativeModel = await _context.RepresentativeModel.FindAsync(id);
            if (RepresentativeModel != null)
            {
                RepresentativeModel.deleted_at = DateTime.Now;
                _context.RepresentativeModel.Update(RepresentativeModel);
            }

            _context.SaveChanges();
            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Delete.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã xóa ủy quyền.";
            _context.Add(audit);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<JsonResult> Table()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var customerData = (from tempcustomer in _context.RepresentativeModel.Where(d => d.user_id == user_id) select tempcustomer);
            int recordsTotal = customerData.Count();
            customerData = customerData.Where(m => m.deleted_at == null);
            if (!string.IsNullOrEmpty(searchValue))
            {
                //customerData = customerData.Where(m => m.name.Contains(searchValue));
            }
            int recordsFiltered = customerData.Count();
            customerData = customerData.Include(d => d.representative);
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var date_from = (DateTime)record.date_from;
                var date_to = (DateTime)record.date_to;
                UserModel representative = record.representative;
                var data1 = new
                {
                    action = "<div class='btn-group'><a href='/admin/" + _type + "/delete/" + record.id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>'"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>",
                    id = "<a href='#'>" + record.id + "</a>",
                    date_from = date_from.ToString("yyyy-MM-dd"),
                    date_to = date_to.ToString("yyyy-MM-dd"),
                    representative = representative.FullName
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
    }
}
