
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using it.Areas.Admin.Models;
using it.Data;
using System.Collections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Spire.Xls;
using System.Data;

namespace it.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator,Manager Esign")]
    public class HistoryController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private string _type = "History";
        IConfiguration _configuration;
        public HistoryController(ItContext context, UserManager<UserModel> UserMgr, IConfiguration configuration) : base(context)
        {
            ViewData["controller"] = _type;
            UserManager = UserMgr;
            _configuration = configuration;
        }

        // GET: Admin/History
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> Table()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var filter_description = Request.Form["filter_description"].FirstOrDefault();
            var filter_type = Request.Form["filter_type"].FirstOrDefault();
            var search_date_range = Request.Form["search_date"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            var customerData = (from tempcustomer in _context.AuditTrailsModel select tempcustomer);

            if (!string.IsNullOrEmpty(search_date_range))
            {
                var explode = search_date_range.Split(" - ");
                if (explode.Length > 1)
                {
                    DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);
                    DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);

                    customerData = customerData.Where(m => m.DateTime.Date >= start_date.Date && m.DateTime.Date <= end_date.Date);
                }
            }
            if (!string.IsNullOrEmpty(filter_description))
            {
                customerData = customerData.Where(m => m.description.Contains(filter_description));
            }
            if (!string.IsNullOrEmpty(filter_type))
            {
                customerData = customerData.Where(m => m.Type.Contains(filter_type));
            }
            int recordsTotal = customerData.Count();
            int recordsFiltered = customerData.Count();
            var datapost = customerData.Include(d => d.user).OrderByDescending(d => d.Id).Skip(skip).Take(pageSize).ToList();
            var data = new ArrayList();
            foreach (var record in datapost)
            {
                var user = record.user;
                var user_name = "";
                if (user != null)
                {
                    user_name = user.FullName;

                }
                var data1 = new
                {
                    id = record.Id,
                    user = user_name,
                    datetime = record.DateTime.ToString("yyyy/MM/dd HH:mm:ss"),
                    type = record.Type,
                    tableName = record.TableName,
                    description = record.description,
                    oldValues = $"<div style='word-break:break-all;'>{record.OldValues}</div>",
                    newValues = $"<div style='word-break:break-all;'>{record.NewValues}</div>",
                    primaryKey = record.PrimaryKey,
                };
                data.Add(data1);
            }
            var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
            return Json(jsonData);
        }
        [HttpPost]
        public async Task<JsonResult> ExportExcel()
        {
            var search_date_range = Request.Form["search_date"].FirstOrDefault();

            var filter_description = Request.Form["filter_description"].FirstOrDefault();
            var filter_type = Request.Form["filter_type"].FirstOrDefault();
            var ngaythamdinh = new DateTime(2022, 12, 21);
            var customerData = _context.AuditTrailsModel.Where(d => (d.DateTime.Date != ngaythamdinh.Date && d.UserId != "5a375cd2-1908-4784-9b7b-d470e2d63376") || d.DateTime.Date == ngaythamdinh.Date);
            if (!string.IsNullOrEmpty(search_date_range))
            {
                var explode = search_date_range.Split(" - ");
                if (explode.Length > 1)
                {
                    DateTime start_date = DateTime.ParseExact(explode[0].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);
                    DateTime end_date = DateTime.ParseExact(explode[1].ToString(), "dd/MM/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);

                    customerData = customerData.Where(m => m.DateTime.Date >= start_date.Date && m.DateTime.Date <= end_date.Date);
                }
            }
            if (!string.IsNullOrEmpty(filter_description))
            {
                customerData = customerData.Where(m => m.description.Contains(filter_description));
            }
            if (!string.IsNullOrEmpty(filter_type))
            {
                customerData = customerData.Where(m => m.Type.Contains(filter_type));
            }
            int recordsTotal = customerData.Count();
            int recordsFiltered = customerData.Count();
            int stt = recordsTotal;
            var datapost = customerData.Include(d => d.user).OrderByDescending(d => d.Id).ToList();
            var data = new ArrayList();
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var viewPath = _configuration["Source:Path_Private"] + "\\excel\\template\\AuditTrails.xlsx";
            var documentPath = _configuration["Source:Path_Private"] + "\\excel\\AuditTrails\\" + timestamp + ".xlsx";
            Workbook workbook = new Workbook();
            workbook.LoadFromFile(viewPath);
            Worksheet sheet = workbook.Worksheets[0];

            ExcelFont fontItalic1 = workbook.CreateFont();
            fontItalic1.IsItalic = true;
            fontItalic1.Size = 10;
            fontItalic1.FontName = "Arial";
            fontItalic1.IsBold = true;
            var row_c = 1;
            var cur_r = 1;
            DataTable dt = new DataTable();
            dt.Columns.Add("Stt", typeof(int));
            dt.Columns.Add("FullName", typeof(string));
            dt.Columns.Add("DateTime", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("description", typeof(string));
            dt.Columns.Add("TableName", typeof(string));
            dt.Columns.Add("PrimaryKey", typeof(string));
            dt.Columns.Add("OldValues", typeof(string));
            dt.Columns.Add("NewValues", typeof(string));

            var start_r = 2;
            foreach (var record in datapost)
            {
                DataRow dr1 = dt.NewRow();

                dr1["Stt"] = stt--;
                dr1["FullName"] = record.user != null ? record.user.FullName : "";
                dr1["DateTime"] = record.DateTime.ToString("yyyy/MM/dd HH:mm:ss");
                dr1["Type"] = record.Type;
                dr1["description"] = record.description;
                dr1["TableName"] = record.TableName;
                dr1["PrimaryKey"] = record.PrimaryKey;
                dr1["OldValues"] = record.OldValues;
                dr1["NewValues"] = record.NewValues;

                dt.Rows.Add(dr1);
                start_r++;

                CellRange originDataRang = sheet.Range["A2:V2"];
                CellRange targetDataRang = sheet.Range["A" + start_r + ":V" + start_r];
                sheet.Copy(originDataRang, targetDataRang, true);
            }
            sheet.InsertDataTable(dt, false, 3, 1);
            sheet.DeleteRow(2);
            //AutoFit column width and row height
            sheet.AllocatedRange.AutoFitColumns();

            sheet.AllocatedRange.AutoFitRows();

            workbook.SaveToFile(documentPath, ExcelVersion.Version2013);


            string Domain = (HttpContext.Request.IsHttps ? "https://" : "http://") + HttpContext.Request.Host.Value;
            var jsonData = new { url = Domain + "/private/excel/AuditTrails/" + timestamp + ".xlsx" };
            return Json(jsonData);
        }
    }
}
