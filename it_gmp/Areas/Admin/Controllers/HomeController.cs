
using CertificateManager;
using CertificateManager.Models;
using Fernandezja.ColorHashSharp;
using it.Areas.Admin.Models;
using it.Data;
using it.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Spire.Xls;
using System.Collections;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace it.Areas.Admin.Controllers
{

    public class HomeController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private readonly LoginMailPyme _LoginMailPyme;

        private readonly ViewRender _view;

        public HomeController(ItContext context, UserManager<UserModel> UserMgr, ViewRender view, LoginMailPyme LoginMailPyme) : base(context)
        {
            _view = view;
            UserManager = UserMgr;
            _LoginMailPyme = LoginMailPyme;
        }
        public async Task<IActionResult> Index()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;

            var user = await UserManager.GetUserAsync(currentUser);
            var user_id = user.Id;
            var is_admin = await UserManager.IsInRoleAsync(user, "Administrator");
            var is_manager = await UserManager.IsInRoleAsync(user, "Manager Esign");
            var is_first_login = user.is_first_login;
            var is_pyme = _LoginMailPyme.is_pyme(user.Email);
            //if (!is_pyme && (is_first_login == null || is_first_login == false))
            //{
            //	user.is_first_login = true;
            //	await UserManager.UpdateAsync(user);
            //	return Redirect("admin/member/changepassword");
            //}
            if (!is_admin && !is_manager)
                return Redirect("/admin/template");

            var document_count = _context.DocumentModel.Where(d => d.deleted_at == null);
            var document_wait_count = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id == 2);
            var document_done_count = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id >= 4);
            var document_cancle_count = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id == 3);
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    document_count = document_count.Where(d => type_gmp.Contains(d.type_id));
                    document_wait_count = document_wait_count.Where(d => type_gmp.Contains(d.type_id));
                    document_done_count = document_done_count.Where(d => type_gmp.Contains(d.type_id));
                    document_cancle_count = document_cancle_count.Where(d => type_gmp.Contains(d.type_id));
                }
                else
                {
                    document_count = document_count.Where(d => 0 == 1);
                    document_wait_count = document_wait_count.Where(d => 0 == 1);
                    document_done_count = document_done_count.Where(d => 0 == 1);
                    document_cancle_count = document_cancle_count.Where(d => 0 == 1);
                }
            }
            ViewBag.document_count = document_count.Count();
            ViewBag.document_wait_count = document_wait_count.Count();
            ViewBag.document_done_count = document_done_count.Count();
            ViewBag.document_cancle_count = document_cancle_count.Count();
            return View();
        }

        public async Task<JsonResult> datachart(string time_type, DateTime from, DateTime to)
        {
            var subsql = "";
            var subsql1 = "";
            var title = "";
            to = to.AddDays(1);
            if (time_type == "Week")
            {
                subsql = "CONCAT(datepart(year,date_finish),'-',RIGHT('0' + + RTRIM(datepart(week,date_finish)), 2))";
                subsql1 = "CONCAT(datepart(year,a.date),'-',RIGHT('0' + + RTRIM(datepart(week,a.date)), 2))";
                title = "CONCAT('Tuần ',ISNULL(b.time_type,ISNULL(c.time_type,a.time_type)))";
            }
            else if (time_type == "Month")
            {
                subsql = "CONCAT(datepart(year,date_finish),'-',RIGHT('0' + RTRIM(MONTH(date_finish)), 2))";
                subsql1 = "CONCAT(datepart(year,a.date),'-',RIGHT('0' + RTRIM(MONTH(a.date)), 2))";
                title = "CONCAT('Tháng ',ISNULL(b.time_type,ISNULL(c.time_type,a.time_type)))";
            }
            else if (time_type == "Year")
            {
                subsql = "datepart(year,date_finish)";
                subsql1 = "datepart(year,a.date)";
                title = "CONCAT('Năm ',ISNULL(b.time_type,ISNULL(c.time_type,a.time_type)))";
            }
            else
            {
                subsql = "convert(nvarchar(MAX), date_finish, 102)";
                subsql1 = "convert(nvarchar(MAX), a.date, 102)";
                title = "ISNULL(b.time_type,ISNULL(c.time_type,a.time_type))";
            }
            var sql_success = $"select {subsql} as time_type from document where deleted_at is null and status_id = 4 and date_finish is not null and date_finish >= @from and date_finish <= @to";
            var sql_cancle = $"select {subsql} as time_type from document where deleted_at is null and status_id = 3 and date_finish is not null and date_finish >= @from and date_finish <= @to";
            var sql_sign = $"select {subsql1} as time_type from (select document_id,date from document_signature where status = 2 and date >= @from and date <= @to AND document_id IN (select id from document where deleted_at is null)) as a";
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    var selectList = string.Join(", ", type_gmp);
                    sql_success += $" AND type_id IN({selectList})";
                    sql_cancle += $" AND type_id IN({selectList})";
                    sql_sign = $"select {subsql1} as time_type from (select document_id,date from document_signature where status = 2 and date >= @from and date <= @to AND document_id IN (select id from document where deleted_at is null and type_id IN({selectList}))) as a";

                }
                else
                {
                    sql_success += $" AND 0=1";
                    sql_cancle += $" AND 0=1";
                    sql_sign = $"select {subsql1} as time_type from (select document_id,date from document_signature where status = 2 and date >= @from and date <= @to AND document_id IN (select id from document where deleted_at is null and 0=1))) as a";

                }
            }

            var sql = "select " + title + " as title,ISNULL(b.time_type,ISNULL(c.time_type,a.time_type)) as time_type,ISNULL(a.count,0) as num_finish,ISNULL(b.count,0) as num_sign,ISNULL(c.count,0) as num_cancle FROM "
                + "(select time_type, count(1) as count FROM(" + sql_success + ") as a group by time_type) as a "
                + "FULL JOIN "
                + "(select time_type, count(1) as count FROM(" + sql_cancle + ") as a group by time_type) as c "
                + "ON a.time_type = c.time_type "
                + "FULL JOIN "
                + "(select time_type, count(1) as count FROM(" + sql_sign + ") as a group by time_type) as b "
                + "ON a.time_type = b.time_type";

            var pFrom = new SqlParameter("@from", System.Data.SqlDbType.DateTime);
            var pTo = new SqlParameter("@to", System.Data.SqlDbType.DateTime);
            pFrom.Value = from;
            pTo.Value = to;
            var data = _context.Chart
                 .FromSqlRaw(sql, pFrom, pTo).OrderBy(d => d.time_type);
            //Console.WriteLine(data.ToQueryString());
            var labels = data.Select(d => d.title).ToList();
            var datasets = new List<ChartDataSet2>();
            datasets.Add(new ChartDataSet2
            {
                label = "Đã ký",
                backgroundColor = "rgba(74, 199, 236, 0.7)",
                data = data.Select(d => d.num_sign).ToList(),
            });
            datasets.Add(new ChartDataSet2
            {
                label = "Hô sơ hoàn thành",
                backgroundColor = "#1ecab8",
                data = data.Select(d => d.num_finish).ToList(),
            });
            datasets.Add(new ChartDataSet2
            {
                label = "Hô sơ hủy",
                backgroundColor = "#f1646c",
                data = data.Select(d => d.num_cancle).ToList(),
            });

            return Json(new { labels = labels, datasets = datasets });
        }
        public async Task<JsonResult> datachartPie()
        {

            var sql = "select 1 as id, SUM(CASE WHEN status_id = 2 THEN 1 ELSE 0 END) as num_wait,SUM(CASE WHEN status_id IN(3) THEN 1 ELSE 0 END) as num_cancle ,SUM(CASE WHEN status_id IN(4) THEN 1 ELSE 0 END) as num_finish,SUM(CASE WHEN status_id IN(6) THEN 1 ELSE 0 END) as num_current,SUM(CASE WHEN status_id = 7 THEN 1 ELSE 0 END) as num_superseded,SUM(CASE WHEN status_id = 8 THEN 1 ELSE 0 END) as num_obsoleted from document where deleted_at is null";
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    var selectList = string.Join(", ", type_gmp);
                    sql += $" AND type_id IN({selectList})";
                }
                else
                {
                    sql += $" AND 0 = 1";
                }
            }
            var data = _context.ChartPie
                 .FromSqlRaw(sql);
            var d = data.FirstOrDefault();
            var labels = new List<string>() { "Đang chờ ký", "Ký hoàn thành", "Hiện hành", "Obsoleted", "Hủy" };
            var datasets = new List<ChartDataSet>();
            datasets.Add(new ChartDataSet
            {
                backgroundColor = new List<string>() { "#efc039", "#1ecab8", "#0cb2c6", "ff6000", "#f1646c" },
                data = new List<int?>() { d.num_wait, d.num_finish, d.num_current, d.num_obsoleted, d.num_cancle }
            });

            return Json(new { labels = labels, datasets = datasets });
        }
        public async Task<JsonResult> datachartType()
        {

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            var subsql = "";
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    var selectList = string.Join(", ", type_gmp);
                    subsql += $" AND type_id IN({selectList})";
                }
                else
                {
                    subsql += $" AND 0 = 1";
                }
            }
            var sql = $"select type_id,COUNT(1) as num from document where deleted_at is null and status_id >=4 {subsql} GROUP BY type_id";

            var data = _context.ChartType
                 .FromSqlRaw(sql);
            var d = data.Include(d => d.type).OrderByDescending(d => d.num).ToList();

            var labels = new List<string>() { };
            var datasets = new List<ChartDataSet>();
            var backgroundColor = new List<string>();
            var data1 = new List<int?>();
            foreach (var type in d)
            {
                labels.Add(type.type.name.ToString());
                backgroundColor.Add(type.type.color);
                data1.Add(type.num);
            }
            datasets.Add(new ChartDataSet
            {
                backgroundColor = backgroundColor,
                data = data1
            });

            return Json(new { labels = labels, datasets = datasets });
        }
        public async Task<JsonResult> datachartTypeGroup()
        {

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            var subsql = "";
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    var selectList = string.Join(", ", type_gmp);
                    subsql += $" AND a.type_id IN({selectList})";
                }
                else
                {
                    subsql += $" AND 0 = 1";
                }
            }
            var sql = $"select b.group_id,COUNT(1) as num from document as a join document_type as b ON a.type_id = b.id where a.deleted_at is null and a.status_id >= 4 {subsql} GROUP BY b.group_id";

            var data = _context.ChartTypeGroup
                 .FromSqlRaw(sql);
            var d = data.Include(d => d.group).OrderByDescending(d => d.num).ToList();

            var labels = new List<string>() { };
            var datasets = new List<ChartDataSet>();
            var backgroundColor = new List<string>();
            var data1 = new List<int?>();
            var md5 = MD5.Create();
            var colorHash = new ColorHash();
            foreach (var type in d)
            {
                labels.Add(type.group.name.ToString());
                var hex = "#" + colorHash.Hex(type.group.name);
                backgroundColor.Add(hex);
                data1.Add(type.num);
            }
            datasets.Add(new ChartDataSet
            {
                backgroundColor = backgroundColor,
                data = data1
            });

            return Json(new { labels = labels, datasets = datasets });
        }

        public JsonResult Root()
        {
            return Json(new { success = true });
            //return RedirectToAction("Index");
            // Generate private-public key pair
            var serviceProvider = new ServiceCollection()
                  .AddCertificateManager()
                  .BuildServiceProvider();

            var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();
            //X509Certificate2 rootCaL1 = new X509Certificate2("localhost_root_l1.pfx", "1234");
            X509Certificate2 rootCaL1 = createClientServerAuthCerts.NewRootCertificate(
                new DistinguishedName { CommonName = "ASTACA", Country = "IT" },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                3, "localhost");
            rootCaL1.FriendlyName = "ASTA certification authority";

            //// Server, Client L3 chained from Intermediate L2
            ////var serverL3 = createClientServerAuthCerts.NewServerChainedCertificate(
            ////    new DistinguishedName { CommonName = "Thien pyme" },
            ////    new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
            ////    "localhost", rootCaL1);

            ////serverL3.FriendlyName = "Thien";

            string password = "!PMP_it123456";
            var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();

            var rootCertInPfxBtyes = importExportCertificate.ExportRootPfx(password, rootCaL1);
            System.IO.File.WriteAllBytes("localhost_root.pfx", rootCertInPfxBtyes);

            var rootPublicKey = importExportCertificate.ExportCertificatePublicKey(rootCaL1);
            var rootPublicKeyBytes = rootPublicKey.Export(X509ContentType.Cert);
            System.IO.File.WriteAllBytes($"localhost_root.cer", rootPublicKeyBytes);
            //var serverCertL3InPfxBtyes = importExportCertificate.ExportChainedCertificatePfx(password, serverL3, rootCaL1);
            //System.IO.File.WriteAllBytes("private/pfx/thien.pfx", serverCertL3InPfxBtyes);


            Console.WriteLine("Certificates exported to pfx and cer files");
            return Json(new { success = true });
        }

        public IActionResult createmoc()
        {
            return Ok();

            //await UserManager.AddToRoleAsync(user, "Administrator");
            // Generate private-public key pair
            var serviceProvider = new ServiceCollection()
                  .AddCertificateManager()
                  .BuildServiceProvider();

            string passwordPublic = "!PMP_it123456";
            var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();

            X509Certificate2 rootCaL1 = new X509Certificate2("private\\rootca\\localhost_root.pfx", passwordPublic);
            var serverL3 = createClientServerAuthCerts.NewClientChainedCertificate(
                new DistinguishedName { CommonName = "Công ty cổ phần Pymepharco", OrganisationUnit = "Pymepharco" },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                "localhost", rootCaL1);
            var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();
            var serverCertL3InPfxBtyes = importExportCertificate.ExportChainedCertificatePfx(passwordPublic, serverL3, rootCaL1);
            System.IO.File.WriteAllBytes("private\\pfx\\dau_moc.pfx", serverCertL3InPfxBtyes);

            return Ok();
        }
        public async Task<IActionResult> import_user()
        {
            return Ok();
            // Khởi tạo workbook để đọc
            Spire.Xls.Workbook book = new Spire.Xls.Workbook();
            book.LoadFromFile("./private/check/Accounts_2022_07.xlsx", ExcelVersion.Version2013);

            Spire.Xls.Worksheet sheet = book.Worksheets[0];
            var lastrow = sheet.LastDataRow;
            // nếu vẫn chưa gặp end thì vẫn lấy data
            Console.WriteLine(lastrow);
            for (int rowIndex = 2; rowIndex < lastrow; rowIndex++)
            {
                // lấy row hiện tại
                var nowRow = sheet.Rows[rowIndex];
                if (nowRow == null)
                    continue;
                // vì ta dùng 3 cột A, B, C => data của ta sẽ như sau
                //int numcount = nowRow.Cells.Count;
                //for(int y = 0;y<numcount - 1 ;y++)
                var cellemail = nowRow.Cells[0];
                var cellFullName = nowRow.Cells[3];
                var celltype = nowRow.Cells[7];
                if (cellemail == null || cellFullName == null)
                {
                    continue;
                }
                var email = cellemail.Value;
                var FullName = cellFullName.Value;
                var type = celltype.Value;
                // Xuất ra thông tin lên màn hình
                Console.WriteLine("MS: {0} ", email);
                Console.WriteLine("name: {0} ", FullName);
                Console.WriteLine("type: {0} ", type);
                if (type != "Y")
                    continue;
                string password = "!PMP_it123456";
                UserModel user = new UserModel
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    FullName = FullName,
                    image_sign = "/private/images/tick.png",
                    image_url = "/private/images/user.webp"
                };
                IdentityResult result = await UserManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    //await UserManager.AddToRoleAsync(user, "Administrator");
                    // Generate private-public key pair
                    var serviceProvider = new ServiceCollection()
                          .AddCertificateManager()
                          .BuildServiceProvider();

                    string passwordPublic = "!PMP_it123456";
                    var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();

                    X509Certificate2 rootCaL1 = new X509Certificate2("private\\rootca\\localhost_root.pfx", passwordPublic);
                    var serverL3 = createClientServerAuthCerts.NewClientChainedCertificate(
                        new DistinguishedName { CommonName = user.FullName + "<" + user.Email + ">", OrganisationUnit = user.position },
                        new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                        "localhost", rootCaL1);
                    var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();
                    var serverCertL3InPfxBtyes = importExportCertificate.ExportChainedCertificatePfx(passwordPublic, serverL3, rootCaL1);
                    System.IO.File.WriteAllBytes("private\\pfx\\" + user.Id + ".pfx", serverCertL3InPfxBtyes);
                }
                //EquipmentModel EquipmentModel = new EquipmentModel { code = code, name = name_vn, name_en = name_en, created_at = DateTime.Now };
                //_context.Add(EquipmentModel);
                //_context.SaveChanges();
            }
            return Ok();
        }

        public async Task<IActionResult> sendmail_matkhau()
        {
            //return Ok();
            // Khởi tạo workbook để đọc
            Spire.Xls.Workbook book = new Spire.Xls.Workbook();
            book.LoadFromFile("./private/check/Permissions.xlsx", ExcelVersion.Version2013);

            Spire.Xls.Worksheet sheet = book.Worksheets[0];
            var lastrow = sheet.LastDataRow;
            // nếu vẫn chưa gặp end thì vẫn lấy data
            Console.WriteLine(lastrow);
            for (int rowIndex = 2; rowIndex < lastrow; rowIndex++)
            {
                // lấy row hiện tại
                var nowRow = sheet.Rows[rowIndex];
                if (nowRow == null)
                    continue;
                // vì ta dùng 3 cột A, B, C => data của ta sẽ như sau
                //int numcount = nowRow.Cells.Count;
                //for(int y = 0;y<numcount - 1 ;y++)
                var cellusername = nowRow.Cells[0];
                var cellpassword = nowRow.Cells[2];
                var cellmail = nowRow.Cells[3];
                var celltype = nowRow.Cells[4];
                if (cellusername == null || cellpassword == null)
                {
                    continue;
                }
                var username = cellusername.Value;
                var password = cellpassword.Value;
                var is_mail = cellmail.Value;
                var type = celltype.Value;
                // Xuất ra thông tin lên màn hình
                Console.WriteLine("MS: {0} ", username);
                Console.WriteLine("name: {0} ", password);
                Console.WriteLine("type: {0} ", type);
                if (is_mail != "1")
                    continue;
                if (username == null)
                    continue;
                var list = username.Split('\\');
                var mail_string = "";
                if (list.Length > 1)
                    mail_string = list[1].Trim() + "@astahealthcare.com";
                string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
                var body = _view.Render("Emails/NewUser1", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", password = password, username = username });
                var email = new EmailModel
                {
                    email_to = mail_string,
                    subject = "[File Server] Thông báo mật khẩu tài khoản",
                    body = body,
                    email_type = "newuserfile",
                    status = 1
                };
                _context.Add(email);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<JsonResult> TableWait()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var search_date_range = Request.Form["search_date"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var customerData = _context.DocumentModel.Where(m => m.deleted_at == null && m.status_id == 2);

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    customerData = customerData.Where(d => type_gmp.Contains(d.type_id));
                }
                else
                {
                    type_gmp = new List<int>();
                    customerData = customerData.Where(d => type_gmp.Contains(d.type_id));
                }
            }
            int recordsTotal = customerData.Count();
            var explode = search_date_range.Split(" - ");
            if (explode.Length > 1)
            {
                DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);
                DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);

                customerData = customerData.Where(m => m.created_at != null && m.created_at.Value.Date >= start_date.Date && m.created_at.Value.Date <= end_date.Date);
            }

            ////
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.code.Contains(searchValue) || m.name_vi.Contains(searchValue) || m.keyword.Contains(searchValue) || m.user_id.Contains(searchValue));
            }
            /////
            customerData = customerData
                .Include(d => d.type)
                .Include(d => d.user_next_signature)
                .Include(d => d.user);
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var user = record.user_next_signature;
                var html_user_signature = "";
                if (user != null)
                {
                    html_user_signature = "<div class='img-group'>";
                    //if (user == null)
                    //    continue;

                    html_user_signature += "<div class='user-avatar user-avatar-group' title='" + user.FullName + "'><img class='rounded-circle' width='30px' src='" + user.image_url + "' /></div>";
                    html_user_signature += "</div>";
                }
                var span_unread = "";
                var user_create = record.user != null ? record.user.FullName : "";
                var date_create = record.created_at != null ? record.created_at.Value.ToString("HH:mm dd/MM/yyyy") : "";
                var type_name = record.type != null ? record.type.name : "";
                var type_color = record.type != null ? record.type.color : "";
                var html_type = "<span class='badge text-white px-3 ml-2' style='background: " + type_color + "'>" + type_name + "</span>";

                var html_name_body = "<div><a class='text-dark'href='/admin/document/details/" + record.id + "'>[" + record.code + "] " + record.name_vi + span_unread + "</a></div>";
                html_name_body += "<div><span class='small'>Tạo bởi <i>" + user_create + "</i> lúc <i>" + date_create + "</i></span></div>";
                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>";
                var created_at = (DateTime)record.created_at;
                var date_finish = DateTime.Now;
                var time = (date_finish - created_at).TotalHours;
                var html_time = (int)time + " giờ";
                var data1 = new
                {
                    name = html_name,
                    user_signature = html_user_signature,
                    type = html_type,
                    time = html_time,
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> TableEmail()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            //var search_date_range = Request.Form["search_date"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            var search_status_request = Request.Form["search_status"].FirstOrDefault();
            var search_status = Int32.Parse(search_status_request);
            var customerData = _context.EmailModel.Where(d => d.id > 0);

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            int recordsTotal = customerData.Count();
            //var explode = search_date_range.Split(" - ");
            //if (explode.Length > 1)
            //{
            //	DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
            //					   System.Globalization.CultureInfo.InvariantCulture);
            //	DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
            //					   System.Globalization.CultureInfo.InvariantCulture);

            //	customerData = customerData.Where(m => m.created_at != null && m.created_at.Value.Date >= start_date.Date && m.created_at.Value.Date <= end_date.Date);
            //}

            ////
            if (search_status > 0)
            {
                customerData = customerData.Where(d => d.status == search_status);
            }
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(d => d.email_to.Contains(searchValue) || d.subject.Contains(searchValue));
            }
            /////
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var date = record.date != null ? record.date.Value.ToString("dd/MM/yyyy") : "";
                var html_status = "";
                if (record.status == 1)
                {
                    html_status = "<button class=\"resend badge badge-warning text-white px-3 ml-2 border-0\" data-id='" + record.id + "'>Chưa gửi</button>";
                }
                else if (record.status == 2)
                {
                    html_status = "<button class=\"resend badge badge-success text-white px-3 ml-2 border-0\" data-id='" + record.id + "'>Đã gửi</button>";
                }
                else
                {
                    html_status = "<button class=\"resend badge badge-danger text-white px-3 ml-2 border-0\" data-id='" + record.id + "'>Lỗi</button>";
                }
                var html_to = "";
                var email_to_list = record.email_to.Trim().Split(',');
                foreach (var email in email_to_list)
                {
                    html_to += $"<div>{email}</div>";
                }
                var data1 = new
                {
                    subject = record.subject,
                    email_to = html_to,
                    email_type = record.email_type,
                    error = record.error,
                    date = date,
                    status = html_status
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }

        [HttpPost]
        public async Task<JsonResult> TableUser()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            //var sql = "select user_sign,count(1) as num_sign from document_signature where status = 2 and user_sign is not null group by user_sign";
            var customerData = _context.DocumentSignatureModel.Include(d => d.document).Where(m => m.document.deleted_at == null && m.status == 2 && m.user_sign != null).GroupBy(x => x.user_sign, (x, y) => new
            {
                num_sign = y.Count(),
                user_sign = x
            }).Select(d => new
            {
                num_sign = d.num_sign,
                user_sign = d.user_sign,
                user_s = _context.UserModel.Where(x => d.user_sign == x.Id).FirstOrDefault()
            });
            //customerData = customerData.Where(m => m.status == 2 && m.user_sign != null);

            int recordsTotal = customerData.Count();


            ////
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.user_s.FullName.Contains(searchValue));
            }
            /////
            ///
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.num_sign).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var html_num_sign = record.num_sign;
                var user = record.user_s;

                var html_name = "<div class='media'><img src='" + user.image_url + "' class='thumb-sm rounded-circle mr-2'><div class='media-body'><div>" + user.FullName + "</div><div><span class='small'><i>" + user.Email + "</i></span></div></div></div>";
                var html_remind = "<button class='btn btn-sm btn-warning remind' data-id='" + user.Id + "'>Nhắc ký</button>";
                var data1 = new
                {
                    name = html_name,
                    num_sign = html_num_sign,
                    remind = html_remind
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> TableComment()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            //var sql = "select user_sign,count(1) as num_sign from document_signature where status = 2 and user_sign is not null group by user_sign";
            var customerData = _context.DocumentCommentModel.Include(d => d.document).Where(m => m.deleted_at == null);
            //customerData = customerData.Where(m => m.status == 2 && m.user_sign != null);
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    customerData = customerData.Include(d => d.document).Where(d => type_gmp.Contains(d.document.type_id));
                }
                else
                {

                    type_gmp = new List<int>();
                    customerData = customerData.Include(d => d.document).Where(d => type_gmp.Contains(d.document.type_id));
                }
            }
            int recordsTotal = customerData.Count();


            ////
            //if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
            //{
            //    customerData = customerData.Where(m => m.FullName.Contains(searchValue) || m.Email.Contains(searchValue));
            //}
            /////
            ///
            int recordsFiltered = customerData.Count();
            var datapost = customerData.Include(d => d.user).OrderByDescending(d => d.created_at).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var created_at = (DateTime)record.created_at;
                var user = record.user;
                var text = record.comment;
                if (record.comment == null)
                {
                    text = "Đã gửi đính kèm";
                }
                var html_comment = "<div class='media'>"
                            + "<img class='mr-3 rounded-circle' src='" + user.image_url + "' width='50'>"
                            + "<div class='media-body' style='display:grid;'>"
                            + "<h5 class='mt-0 mb-1'>" + user.FullName + " <small class='text-muted'> - " + created_at.ToString("HH:mm d/M/y") + "</small></h5>"
                            + "<div class='mb-2' style='white-space:pre-wrap''>" + text + "</div>"
                            + "<div class='mb-2 attach_file file-box-content''></div>"
                            + "</div>"
                        + "</div>";
                //var user = _context.UserModel.Find(record.user_sign);

                var action = "<a class='badge badge-success text-white px-3 ml-2' href='/admin/document/details/" + record.document_id + "'>Đi đến</a>";
                var data1 = new
                {
                    comment = html_comment,
                    action = action
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }

        [HttpPost]
        public async Task<JsonResult> TableNoSign()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var customerData = _context.DocumentSignatureModel.Include(d => d.document).Where(m => m.status == 3);
            //customerData = customerData.Where(m => m.status == 2 && m.user_sign != null);
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    customerData = customerData.Include(d => d.document).Where(d => type_gmp.Contains(d.document.type_id));
                }
                else
                {
                    type_gmp = new List<int>();
                    customerData = customerData.Include(d => d.document).Where(d => type_gmp.Contains(d.document.type_id));
                }
            }
            int recordsTotal = customerData.Count();


            ////
            //if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
            //{
            //    customerData = customerData.Where(m => m.FullName.Contains(searchValue) || m.Email.Contains(searchValue));
            //}
            /////
            ///
            int recordsFiltered = customerData.Count();
            var datapost = customerData.Include(d => d.document).Include(d => d.user).OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var document = record.document;
                var html_name_body = "<div><a class='text-dark'href='/admin/document/details/" + document.id + "'>[" + document.code + "] " + document.name_vi + "</a></div>";
                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>";
                var user = record.user;

                var html_user = "<div class='media'><img src='" + user.image_url + "' class='thumb-sm rounded-circle mr-2'><div class='media-body'><div>" + user.FullName + "</div><div><span class='small'><i>" + user.Email + "</i></span></div></div></div>";

                var data1 = new
                {
                    name = html_name,
                    user = html_user,
                    reason = record.reason
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> TableCancle()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var search_date_range = Request.Form["search_date"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var customerData = _context.DocumentModel.Where(m => m.deleted_at == null && m.status_id == 3 && m.date_finish != null);
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    customerData = customerData.Where(d => type_gmp.Contains(d.type_id));
                }
                else
                {
                    type_gmp = new List<int>();
                    customerData = customerData.Where(d => type_gmp.Contains(d.type_id));
                }
            }
            int recordsTotal = customerData.Count();
            var explode = search_date_range.Split(" - ");
            if (explode.Length > 1)
            {
                DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);
                DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);

                customerData = customerData.Where(m => m.date_finish != null && m.date_finish.Value.Date >= start_date.Date && m.date_finish.Value.Date <= end_date.Date);
            }

            ////
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.code.Contains(searchValue) || m.name_vi.Contains(searchValue) || m.keyword.Contains(searchValue) || m.user_id.Contains(searchValue));
            }
            /////
            customerData = customerData
                .Include(d => d.type)
                .Include(d => d.user);
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var span_unread = "";
                var user_create = record.user != null ? record.user.FullName : "";
                var date_create = record.created_at != null ? record.created_at.Value.ToString("HH:mm dd/MM/yyyy") : "";
                var type_name = record.type != null ? record.type.name : "";
                var type_color = record.type != null ? record.type.color : "";
                var html_type = "<span class='badge text-white px-3 ml-2' style='background: " + type_color + "'>" + type_name + "</span>";
                var html_name_body = "<div><a class='text-dark'href='/admin/document/details/" + record.id + "'>[" + record.code + "] " + record.name_vi + span_unread + "</a></div>";
                html_name_body += "<div><span class='small'>Tạo bởi <i>" + user_create + "</i> lúc <i>" + date_create + "</i></span></div>";
                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>";

                var data1 = new
                {
                    name = html_name,
                    type = html_type,
                    reason = record.reason
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> TableSuccess()
        {
            var type = Request.Form["type"].FirstOrDefault();
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var search_date_range = Request.Form["search_date"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var customerData = _context.DocumentModel.Where(m => m.deleted_at == null && m.status_id == 4);
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var user_current = await UserManager.GetUserAsync(currentUser);
            var is_manager = await UserManager.IsInRoleAsync(user_current, "Manager Esign");
            if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user_id).Select(d => d.document_type_id).ToList();
                if (type_gmp.Count() > 0)
                {
                    customerData = customerData.Where(d => type_gmp.Contains(d.type_id));
                }
                else
                {
                    type_gmp = new List<int>();
                    customerData = customerData.Where(d => type_gmp.Contains(d.type_id));
                }
            }
            int recordsTotal = customerData.Count();
            var explode = search_date_range.Split(" - ");
            if (explode.Length > 1)
            {
                DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);
                DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);

                customerData = customerData.Where(m => m.date_finish != null && m.date_finish.Value.Date >= start_date.Date && m.date_finish.Value.Date <= end_date.Date);
            }

            ////
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.code.Contains(searchValue) || m.name_vi.Contains(searchValue) || m.keyword.Contains(searchValue) || m.user_id.Contains(searchValue));
            }
            /////
            customerData = customerData
                .Include(d => d.type)
                .Include(d => d.user);
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var span_unread = "";
                var user_create = record.user != null ? record.user.FullName : "";
                var date_create = record.created_at != null ? record.created_at.Value.ToString("HH:mm dd/MM/yyyy") : "";
                var type_name = record.type != null ? record.type.name : "";
                var type_color = record.type != null ? record.type.color : "";
                var html_type = "<span class='badge text-white px-3 ml-2' style='background: " + type_color + "'>" + type_name + "</span>";

                var html_name_body = "<div><a class='text-dark'href='/admin/document/details/" + record.id + "'>[" + record.code + "] " + record.name_vi + span_unread + "</a></div>";
                html_name_body += "<div><span class='small'>Tạo bởi <i>" + user_create + "</i> lúc <i>" + date_create + "</i></span></div>";
                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>";
                var created_at = (DateTime)record.created_at;
                var date_finish = (DateTime)record.date_finish;
                var html_finish = date_finish.Date.ToShortDateString();
                var time = (date_finish - created_at).TotalHours;
                var html_time = (int)time + " giờ";
                var data1 = new
                {
                    name = html_name,
                    type = html_type,
                    date_finish = html_finish,
                    time = html_time
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> Remind(string user_id)
        {
            var user = _context.UserModel.Where(d => d.Id == user_id).FirstOrDefault();
            var data = _context.DocumentModel.Where(d => d.deleted_at == null && d.is_sign_parellel == false && d.user_next_signature_id == user_id && d.status_id == 2).ToList();

            var data2 = _context.DocumentModel.Where(d => d.deleted_at == null && d.status_id == 2 && d.is_sign_parellel == true).Join(_context.DocumentSignatureModel, d => d.id, ds => ds.document_id, (d, ds) => new
            {
                document = d,
                status = ds.status,
                user_id = ds.user_id,
            }).Distinct().Where(d => d.status == 1 && d.user_id == user_id).Select(d => d.document).ToList();
            data.AddRange(data2);

            foreach (var da in data)
            {
                var type = _context.DocumentTypeModel.Where(d => d.id == da.type_id).FirstOrDefault();
                da.type = type;
            }
            var mail_string = user.Email;
            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
            var body = _view.Render("Emails/RemindDocument", new { link_logo = Domain + "/images/clientlogo_astahealthcare.com_f1800.png", link = Domain + "/admin/document/wait", count = data.Count(), data = data });
            var email = new EmailModel
            {
                email_to = mail_string,
                subject = "[Nhắc nhở] Các hồ sơ đang cần chữ ký của bạn",
                body = body,
                email_type = "remind_document",
                status = 1
            };
            _context.Add(email);
            await _context.SaveChangesAsync();
            return Json(new
            {
                success = true,
                count = data.Count()
            });
        }
        [HttpPost]
        public async Task<JsonResult> TableTTB()
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
            //List<int> documents_unread = _context.DocumentUserUnreadModel.Where(d => d.user_id == user_id).Select(d => d.document_id).Distinct().ToList();
            ////

            var document_receive = _context.DocumentUserReceiveModel.Where(d => d.user_id == user_id).Select(d => d.document_id).ToList();
            customerData = customerData.Where(d => document_receive.Contains(d.id));

            customerData = customerData.Where(m => m.deleted_at == null && m.status_id == (int)DocumentStatus.Success && m.type_id == 1);
            int recordsTotal = customerData.Count();
            if (search_date_range != null)
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

            ////
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.code.Contains(searchValue) || m.name_vi.Contains(searchValue) || m.keyword.Contains(searchValue) || m.user_id.Contains(searchValue));
            }
            /////
            customerData = customerData
                .Include(d => d.type)
                .Include(d => d.user);
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var span_unread = "";
                var user_create = record.user != null ? record.user.FullName : "";
                var date_create = record.created_at != null ? record.created_at.Value.ToString("HH:mm dd/MM/yyyy") : "";
                var type_name = record.type != null ? record.type.name : "";
                var type_color = record.type != null ? record.type.color : "";
                var html_type = "<span class='badge text-white px-3 ml-2' style='background: " + type_color + "'>" + type_name + "</span>";

                var html_name_body = "<div><a class='text-dark'href='/admin/document/details/" + record.id + "'>[" + record.code + "] " + record.name_vi + span_unread + "</a></div>";
                html_name_body += "<div><span class='small'>Tạo bởi <i>" + user_create + "</i> lúc <i>" + date_create + "</i></span></div>";
                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>";
                var created_at = (DateTime)record.created_at;
                var date_finish = (DateTime)record.date_finish;
                var html_finish = date_finish.Date.ToShortDateString();
                var html_action = "<a class='badge badge-success text-white px-3 ml-2 processed' href='/admin/document/processedDocument/" + record.id + "'>Xử lý</a>"; ;

                var data1 = new
                {
                    name = html_name,
                    action = html_action,
                    date_finish = html_finish
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> TableNVL()
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
            //List<int> documents_unread = _context.DocumentUserUnreadModel.Where(d => d.user_id == user_id).Select(d => d.document_id).Distinct().ToList();
            ////

            var document_receive = _context.DocumentUserReceiveModel.Where(d => d.user_id == user_id).Select(d => d.document_id).ToList();
            customerData = customerData.Where(d => document_receive.Contains(d.id));

            customerData = customerData.Where(m => m.deleted_at == null && m.status_id == (int)DocumentStatus.Success && m.type_id == 51);
            int recordsTotal = customerData.Count();
            if (search_date_range != null)
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
            ////
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.code.Contains(searchValue) || m.name_vi.Contains(searchValue) || m.keyword.Contains(searchValue) || m.user_id.Contains(searchValue));
            }
            /////
            customerData = customerData
                .Include(d => d.type)
                .Include(d => d.user);
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var span_unread = "";
                var user_create = record.user != null ? record.user.FullName : "";
                var date_create = record.created_at != null ? record.created_at.Value.ToString("HH:mm dd/MM/yyyy") : "";
                var type_name = record.type != null ? record.type.name : "";
                var type_color = record.type != null ? record.type.color : "";
                var html_type = "<span class='badge text-white px-3 ml-2' style='background: " + type_color + "'>" + type_name + "</span>";

                var html_name_body = "<div><a class='text-dark'href='/admin/document/details/" + record.id + "'>[" + record.code + "] " + record.name_vi + span_unread + "</a></div>";
                html_name_body += "<div><span class='small'>Tạo bởi <i>" + user_create + "</i> lúc <i>" + date_create + "</i></span></div>";
                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>";
                var created_at = (DateTime)record.created_at;
                var date_finish = (DateTime)record.date_finish;
                var html_finish = date_finish.Date.ToShortDateString();
                var html_action = "<a class='badge badge-success text-white px-3 ml-2 processed' href='/admin/document/processedDocument/" + record.id + "'>Xử lý</a>"; ;

                var data1 = new
                {
                    name = html_name,
                    action = html_action,
                    date_finish = html_finish
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }

        public async Task<JsonResult> TableOther()
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
            //List<int> documents_unread = _context.DocumentUserUnreadModel.Where(d => d.user_id == user_id).Select(d => d.document_id).Distinct().ToList();
            ////

            var document_receive = _context.DocumentUserReceiveModel.Where(d => d.user_id == user_id).Select(d => d.document_id).ToList();
            customerData = customerData.Where(d => document_receive.Contains(d.id));

            customerData = customerData.Where(m => m.deleted_at == null && m.status_id == (int)DocumentStatus.Success && (m.type_id != 51 && m.type_id != 1));
            int recordsTotal = customerData.Count();
            if (search_date_range != null)
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
            ////
            if (!string.IsNullOrEmpty(searchValue)) /// Bnhf thường
			{
                customerData = customerData.Where(m => m.code.Contains(searchValue) || m.name_vi.Contains(searchValue) || m.keyword.Contains(searchValue) || m.user_id.Contains(searchValue));
            }
            /////
            customerData = customerData
                .Include(d => d.type)
                .Include(d => d.user);
            int recordsFiltered = customerData.Count();
            var datapost = customerData.OrderByDescending(d => d.id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            System.Globalization.CultureInfo cul = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");   // try with "en-US"
            foreach (var record in datapost)
            {
                var span_unread = "";
                var user_create = record.user != null ? record.user.FullName : "";
                var date_create = record.created_at != null ? record.created_at.Value.ToString("HH:mm dd/MM/yyyy") : "";
                var type_name = record.type != null ? record.type.name : "";
                var type_color = record.type != null ? record.type.color : "";
                var html_type = "<span class='badge text-white px-3 ml-2' style='background: " + type_color + "'>" + type_name + "</span>";

                var html_name_body = "<div><a class='text-dark'href='/admin/document/details/" + record.id + "'>[" + record.code + "] " + record.name_vi + span_unread + "</a></div>";
                html_name_body += "<div><span class='small'>Tạo bởi <i>" + user_create + "</i> lúc <i>" + date_create + "</i></span></div>";
                var html_name = "<div class='media'><i class='far fa-file-pdf mr-2 text-danger' style='font-size:30px;'></i><div class='media-body'>" + html_name_body + "</div></div>";
                var created_at = (DateTime)record.created_at;
                var date_finish = (DateTime)record.date_finish;
                var html_finish = date_finish.Date.ToShortDateString();
                var html_action = "<a class='badge badge-success text-white px-3 ml-2 processed' href='/admin/document/processedDocument/" + record.id + "'>Xử lý</a>"; ;

                var data1 = new
                {
                    name = html_name,
                    action = html_action,
                    date_finish = html_finish
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> GetOption(List<string> options)
        {
            var data = new Dictionary<string, object>();

            if (options.Contains("users"))
            {
                var obj = UserManager.Users.Where(u => u.deleted_at == null).Select(a => new SelectListItem()
                {
                    Value = a.Id,
                    Text = a.FullName + "< " + a.Email + " >"
                }).ToList();
                data.Add("users", obj);
            }
            if (options.Contains("types"))
            {
                var obj = _context.DocumentTypeModel.Where(a => a.deleted_at == null).Select(a => new SelectListItem()
                {
                    Value = a.id.ToString(),
                    Text = a.name
                }).ToList();
                data.Add("types", obj);
            }
            if (options.Contains("documents"))
            {
                var obj = _context.DocumentModel.Where(a => a.deleted_at == null).Select(a => new SelectListItem()
                {
                    Value = a.id.ToString(),
                    Text = a.code + " - " + a.name_vi
                }).ToList();
                data.Add("documents", obj);
            }
            var jsonData = new { data = data, options = options };
            return Json(jsonData);
        }
        public async Task<JsonResult> gettypegroups()
        {
            var DocumentTypeGroupModel = new List<DocumentTypeGroupModel>();

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await UserManager.GetUserAsync(currentUser);
            var is_admin = await UserManager.IsInRoleAsync(user, "Administrator");
            var is_manager = await UserManager.IsInRoleAsync(user, "Manager Esign");
            if (is_admin)
            {
                DocumentTypeGroupModel = _context.DocumentTypeGroupModel.Where(d => d.deleted_at == null)
                .Include(d => d.types.Where(d => d.deleted_at == null).OrderBy(d => d.stt))
                .ThenInclude(d => d.template)
                .OrderByDescending(d => d.created_at)
                .OrderBy(d => d.stt).ToList();
            }
            else if (is_manager)
            {
                var type_gmp = _context.UserDocumentTypeModel.Where(d => d.user_id == user.Id).Select(d => d.document_type_id).ToList();
                DocumentTypeGroupModel = _context.DocumentTypeGroupModel.Where(d => d.deleted_at == null)
                .Include(d => d.types.Where(d => d.deleted_at == null && (d.is_manager_create == false || (d.is_manager_create == true && type_gmp.Contains(d.id)))).OrderBy(d => d.stt))
                .ThenInclude(d => d.template)
                .OrderByDescending(d => d.created_at)
                .OrderBy(d => d.stt).ToList();
            }
            else
            {
                DocumentTypeGroupModel = _context.DocumentTypeGroupModel.Where(d => d.deleted_at == null)
                .Include(d => d.types.Where(d => d.deleted_at == null && d.is_manager_create == false).OrderBy(d => d.stt))
                .ThenInclude(d => d.template)
                .OrderByDescending(d => d.created_at)
                .OrderBy(d => d.stt).ToList();
            }
            return Json(DocumentTypeGroupModel);
        }
        public async Task<IActionResult> software()
        {
            return View();
        }

    }
}
