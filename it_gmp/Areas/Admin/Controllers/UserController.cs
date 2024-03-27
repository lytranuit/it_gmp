
using CertificateManager;
using CertificateManager.Models;
using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace it.Areas.Admin.Controllers
{

    [Authorize(Roles = "Administrator")]
    public class UserController : BaseController
    {
        IConfiguration _configuration;
        private UserManager<UserModel> UserManager;
        private RoleManager<IdentityRole> RoleManager;
        public UserController(ItContext context, UserManager<UserModel> UserMgr, RoleManager<IdentityRole> RoleMgr, IConfiguration configuration) : base(context)
        {
            UserManager = UserMgr;
            RoleManager = RoleMgr;
            _configuration = configuration;
        }
        // GET: UserController
        public ActionResult Index()
        {
            return View(UserManager.Users);
        }

        // GET: UserController/Create
        public ActionResult Create()
        {

            ViewData["groups"] = RoleManager.Roles.Select(a => new SelectListItem()
            {
                Value = a.Name,
                Text = a.Name
            }).ToList();
            ViewData["types"] = _context.DocumentTypeModel.Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();

            return View();
        }

        // POST: UserController/Create
        [HttpPost]
        public async Task<IActionResult> Create(UserModel User, string password, List<string> groups, bool is_active, List<int> types)
        {

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user_current = await UserManager.GetUserAsync(currentUser); // Get user id://string password = "!PMP_it123456";

            UserModel user = new UserModel
            {
                Email = User.Email,
                UserName = User.Email,
                EmailConfirmed = true,
                FullName = User.FullName,
                position = User.position,
                image_sign = User.image_sign,
                image_url = User.image_url,
                msnv = User.msnv,
                expiry_date = DateTime.Now + TimeSpan.FromDays(180),
                created_at = DateTime.Now,
            };
            if (is_active == false)
            {
                user.LockoutEnd = DateTime.Now.AddDays(360);
            }
            IdentityResult result = await UserManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                //return Ok(result);
                foreach (string group in groups)
                {
                    await UserManager.AddToRoleAsync(user, group);
                }

                //return Ok(result);
                foreach (int type in types)
                {
                    var userdocumenttype = new UserDocumentTypeModel()
                    {
                        document_type_id = type,
                        user_id = user.Id,
                    };
                    _context.Add(userdocumenttype);
                }
                _context.SaveChanges();
                var json = CreatePfx(user);

                /// Audittrail
                var audit = new AuditTrailsModel();
                audit.UserId = user_current.Id;
                audit.Type = AuditType.Create.ToString();
                audit.DateTime = DateTime.Now;
                audit.description = $"Tài khoản {user.FullName} đã tạo tài khoản mới.";
                audit.TableName = "UserModel";
                audit.PrimaryKey = user.Id;
                audit.NewValues = JsonConvert.SerializeObject(user);

                _context.Add(audit);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            else
                return Ok(result);

        }
        private JsonResult CreatePfx(UserModel user)
        {
            // Generate private-public key pair
            var serviceProvider = new ServiceCollection()
                  .AddCertificateManager()
                  .BuildServiceProvider();

            string passwordPublic = "!PMP_it123456";
            var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();

            X509Certificate2 rootCaL1 = new X509Certificate2(_configuration["Source:Path_Private"] + "\\rootca\\localhost_root.pfx", passwordPublic);
            var serverL3 = createClientServerAuthCerts.NewClientChainedCertificate(
                new DistinguishedName { CommonName = user.FullName + "<" + user.Email + ">", OrganisationUnit = user.position },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                "localhost", rootCaL1);
            var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();
            var serverCertL3InPfxBtyes = importExportCertificate.ExportChainedCertificatePfx(passwordPublic, serverL3, rootCaL1);
            System.IO.File.WriteAllBytes(_configuration["Source:Path_Private"] + "\\pfx\\" + user.Id + ".pfx", serverCertL3InPfxBtyes);

            user.signature = "/private/pfx/" + user.Id + ".pfx";
            _context.Update(user);
            _context.SaveChanges();
            return Json(new { success = true });

        }
        // GET: UserController/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["groups"] = RoleManager.Roles.Select(a => new SelectListItem()
            {
                Value = a.Id,
                Text = a.Name
            }).OrderBy(d => d.Text).ToList();

            ViewData["types"] = _context.DocumentTypeModel.Select(a => new SelectListItem()
            {
                Value = a.id.ToString(),
                Text = a.name
            }).ToList();

            UserModel User = await UserManager.FindByIdAsync(id);
            var RolesForThisUser = _context.UserRoleModel.Where(d => d.UserId == id).Select(d => d.RoleId).ToList();
            ViewData["RolesForThisUser"] = RolesForThisUser;
            var user_types = _context.UserDocumentTypeModel.Where(d => d.user_id == User.Id).Select(d => d.document_type_id).ToList();
            ViewData["user_types"] = user_types;
            return View(User);
        }

        // POST: UserController/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(string id, UserModel User, List<string> groups, bool is_active, List<int> types)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser); // Get user
            //return Ok(User);
            if (id != User.Id)
            {
                return Ok(User);
            }
            UserModel User_old = _context.UserModel.Where(d => d.Id == id).FirstOrDefault();
            var OldValues = JsonConvert.SerializeObject(User_old);
            User_old.Email = User.Email;
            User_old.UserName = User.Email;
            User_old.FullName = User.FullName;
            User_old.image_sign = User.image_sign;
            User_old.image_url = User.image_url;
            User_old.position = User.position;
            User_old.msnv = User.msnv;
            User_old.updated_at = DateTime.Now;
            if (is_active == false)
            {
                User_old.LockoutEnd = DateTime.Now.AddDays(360);
            }
            else
            {
                User_old.LockoutEnd = null;
            }
            //var description = $"Tài khoản {user.FullName} đã chỉnh sửa cho tài khoản {User_old.FullName}.";
            _context.Update(User_old);
            _context.SaveChanges();

            var UserRoleModel_old = _context.UserRoleModel.Where(d => d.UserId == id).Select(d => d.RoleId).ToList();
            IEnumerable<string> list_delete = UserRoleModel_old.Except(groups);
            IEnumerable<string> list_add = groups.Except(UserRoleModel_old);
            if (list_add != null)
            {
                foreach (string key in list_add)
                {

                    _context.Add(new UserRoleModel()
                    {
                        UserId = id,
                        RoleId = key,
                    });
                }
                _context.Save();
            }
            if (list_delete != null)
            {
                foreach (string key in list_delete)
                {
                    UserRoleModel UserRoleModel = _context.UserRoleModel.Where(d => d.UserId == id && d.RoleId == key).First();
                    _context.Remove(UserRoleModel);
                }
                _context.Save();
            }

            var UserDocumentTypeModel_old = _context.UserDocumentTypeModel.Where(d => d.user_id == id).Select(d => d.document_type_id).ToList();
            IEnumerable<int> list_delete1 = UserDocumentTypeModel_old.Except(types);
            IEnumerable<int> list_add1 = types.Except(UserDocumentTypeModel_old);
            if (list_add1 != null)
            {
                foreach (int key in list_add1)
                {

                    _context.Add(new UserDocumentTypeModel()
                    {
                        user_id = id,
                        document_type_id = key,
                    });
                }
                _context.SaveChanges();
            }
            if (list_delete1 != null)
            {
                foreach (int key in list_delete1)
                {
                    UserDocumentTypeModel UserDocumentTypeModel = _context.UserDocumentTypeModel.Where(d => d.user_id == id && d.document_type_id == key).First();
                    _context.Remove(UserDocumentTypeModel);
                }
                _context.SaveChanges();
            }

            return RedirectToAction("Index");

        }

        // GET: UserController/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            UserModel User = await UserManager.FindByIdAsync(id);
            if (User != null)
            {
                var OldValues = JsonConvert.SerializeObject(User);
                User.deleted_at = DateTime.Now;
                IdentityResult result = await UserManager.UpdateAsync(User);
                if (result.Succeeded)
                {
                    /// Audittrail
                    System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                    var user = await UserManager.GetUserAsync(currentUser); // Get user
                    var audit = new AuditTrailsModel();
                    audit.UserId = user.Id;
                    audit.Type = AuditType.Delete.ToString();
                    audit.DateTime = DateTime.Now;
                    audit.description = $"Tài khoản {user.FullName} đã xóa tài khoản {User.FullName}.";
                    audit.TableName = "UserModel";
                    audit.PrimaryKey = User.Id;
                    audit.NewValues = JsonConvert.SerializeObject(User);
                    audit.OldValues = OldValues;
                    _context.Add(audit);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                else
                    return Ok(result);
            }
            else
                ModelState.AddModelError("", "No User found");
            return View("Index", UserManager.Users);
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
            var customerData = (from tempcustomer in UserManager.Users select tempcustomer);
            customerData = customerData.Where(m => m.deleted_at == null);
            int recordsTotal = customerData.Count();
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.UserName.Contains(searchValue) || m.FullName.Contains(searchValue));
            }
            int recordsFiltered = customerData.Count();
            var datapost = customerData.Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var image = "<img src='" + record.image_url + "' class='thumb-sm rounded-circle'>";
                var sign = "<img src='" + record.image_sign + "' class='' width='100'>";
                var data1 = new
                {
                    action = "<div class='btn-group'><a href='/admin/User/delete/" + record.Id + "' class='btn btn-danger btn-sm' title='Xóa?' data-type='confirm'>'"
                        + "<i class='fas fa-trash-alt'>"
                        + "</i>"
                        + "</a></div>",
                    Id = "<a href='/admin/User/edit/" + record.Id + "'><i class='fas fa-pencil-alt mr-2'></i> " + record.Id + "</a>",
                    email = record.Email,
                    name = record.FullName,
                    sign = sign,
                    image = image
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }

        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            public string id { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }
        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(InputModel Input)
        {
            //Get User By Id
            var User = await UserManager.FindByIdAsync(Input.id);

            //Generate Token
            var token = await UserManager.GeneratePasswordResetTokenAsync(User);

            //Set new Password
            var changePasswordResult = await UserManager.ResetPasswordAsync(User, token, Input.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                ErrorMessage = "";
                foreach (var error in changePasswordResult.Errors)
                {
                    ErrorMessage += error.Description + "<br>";
                }
                return RedirectToAction("Edit", new { id = Input.id });
            }
            User.expiry_date = DateTime.Now + TimeSpan.FromDays(180);
            User.LockoutEnd = null;
            User.AccessFailedCount = 0;
            await UserManager.UpdateAsync(User);

            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser); // Get user
            var audit = new AuditTrailsModel();
            audit.UserId = user.Id;
            audit.Type = AuditType.Update.ToString();
            audit.DateTime = DateTime.Now;
            audit.description = $"Tài khoản {user.FullName} đã thay đổi mật khẩu tài khoản {User.FullName}.";
            _context.Add(audit);
            await _context.SaveChangesAsync();

            StatusMessage = "Mật khẩu đã được thay đổi";
            return RedirectToAction("Edit", new { id = Input.id });

        }
        [HttpPost]
        public async Task<JsonResult> Sync()
        {
            var client = new HttpClient();

            var content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"Session.login\",\"params\":{\"userName\":\"admin@astahealthcare.com\",\"password\":\"98d50eTl7\",\"application\":{\"name\":\"Example: Get all domains\",\"vendor\":\"Kerio Technologies s.r.o.\",\"version\":\"BUILD_HASH\"}}}");
            var url = "https://mail.astahealthcare.com:1000/admin/api/jsonrpc/";
            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                LoginAdminResponse responseJson = await response.Content.ReadFromJsonAsync<LoginAdminResponse>();
                var token = responseJson.result.token;
                var content1 = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"Users.get\",\"params\":{\"query\":{\"fields\":[\"id\",\"loginName\",\"fullName\",\"description\",\"userGroups\",\"isEnabled\"],\"start\":0,\"limit\":1000000,\"orderBy\":[{\"columnName\":\"loginName\",\"direction\":\"Asc\"}]},\"domainId\":\"keriodb://domain/f013f8e2-e4ca-416b-b0d5-ae4f9931e5b2\"}}");
                //var client1 = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Token", token);
                var response1 = await client.PostAsync(url, content1);
                if (response1.IsSuccessStatusCode)
                {
                    //var json = await response1.Content.ReadAsStringAsync();
                    LoginAdminResponse responseJson1 = await response1.Content.ReadFromJsonAsync<LoginAdminResponse>();
                    var list = responseJson1.result.list;
                    foreach (var item in list)
                    {
                        var find = _context.UserModel.Where(d => d.Email == item.loginName + "@astahealthcare.com").FirstOrDefault();
                        if (find != null)
                        {
                            if (item.isEnabled)
                            {
                                find.deleted_at = null;
                                find.LockoutEnd = null;

                                if (find.FullName != item.fullName)
                                {
                                    find.FullName = item.fullName;
                                    CreatePfx(find);
                                }

                            }
                            else if (find.deleted_at == null)
                            {
                                find.deleted_at = DateTime.Now;
                            }

                            _context.Update(find);
                            _context.SaveChanges();

                            //if (item.isEnabled)
                            //{
                            //    if (!await UserManager.IsInRoleAsync(find, "User"))
                            //        await UserManager.AddToRoleAsync(find, "User");
                            //}
                        }
                        else
                        {

                            if (item.isEnabled)
                            {
                                UserModel user = new UserModel
                                {
                                    Email = item.loginName + "@astahealthcare.com",
                                    UserName = item.loginName + "@astahealthcare.com",
                                    EmailConfirmed = true,
                                    FullName = item.fullName,
                                    image_sign = "/private/images/tick.png",
                                    image_url = "/private/images/user.webp",
                                };
                                user.deleted_at = null;
                                user.LockoutEnd = null;

                                _context.Add(user);
                                _context.SaveChanges();

                                CreatePfx(user);
                                await UserManager.AddToRoleAsync(user, "User");
                            }
                        }
                    }
                    return Json(new { success = true, reusult = responseJson1, token = token });
                }
            }
            return Json(new { success = false, error = "Fail!" });

        }

        public async Task<JsonResult> activeUser()
        {
            var list = UserManager.Users.ToList();
            foreach (var item in list)
            {
                if (item.deleted_at != null)
                    continue;
                if (!await UserManager.IsInRoleAsync(item, "User"))
                    await UserManager.AddToRoleAsync(item, "User");
            }


            return Json(new { success = true });

        }
    }
    public class LoginAdminResponse
    {
        [Key]
        public int id { get; set; }

        public ResultAdmin? result { get; set; }
    }
    public class ResultAdmin
    {
        public string? token { get; set; }
        public List<UserResult>? list { get; set; }
        public int? totalItems { get; set; }
    }
    public class UserResult
    {
        [Key]
        public string id { get; set; }
        public string loginName { get; set; }

        public string fullName { get; set; }
        public string description { get; set; }
        public bool isEnabled { get; set; }
    }
}
