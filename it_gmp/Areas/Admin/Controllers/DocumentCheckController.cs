using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using it.Areas.Admin.Models;
using it.Data;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.X509;
using Microsoft.CodeAnalysis;

namespace it.Areas.Admin.Controllers
{
    public class DocumentCheckController : BaseController
    {
        IConfiguration _configuration;
        public DocumentCheckController(ItContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;

        }

        // GET: Admin/DocumentCheck
        public IActionResult Index()
        {
            return View();
        }
        public async Task<JsonResult> check()
        {
            var files = Request.Form.Files;
            if (files != null && files.Count > 0)
            {
                var file = files[0];
                var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                string name = file.FileName;

                var newName = timeStamp + " - " + name;
                var filePath = _configuration["Source:Path_Private"] + "\\check\\" + newName;
                using (var fileSrteam = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileSrteam);
                }
                try
                {

                    PdfDocument pdfDoc = new PdfDocument(new PdfReader(filePath));
                    SignatureUtil signUtil = new SignatureUtil(pdfDoc);
                    IList<string> names = signUtil.GetSignatureNames();

                    var items = new List<ResponeVailidate>();
                    if (names.Count() > 0)
                    {
                        bool isCoversWholeDoc = signUtil.SignatureCoversWholeDocument(names[names.Count() - 1]);
                        foreach (string namesign in names)
                        {
                            DocumentModel? document = null;
                            int? document_id = null;

                            // ===== 1. Parse document_id an toàn =====
                            if (!string.IsNullOrEmpty(namesign) && namesign.Contains("GMP1"))
                            {
                                var list = namesign.Split("-");
                                if (list.Length >= 3 && int.TryParse(list[2], out int parsedId))
                                {
                                    document_id = parsedId;
                                    document = _context.DocumentModel
                                        .Where(d => d.id == document_id)
                                        .Include(d => d.user)
                                        .FirstOrDefault();
                                }
                            }

                            // ===== 2. Đọc chữ ký =====
                            PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(namesign);
                            X509Certificate cert = pkcs7.GetSigningCertificate();

                            var issuer = cert.IssuerDN?.ToString();
                            var subject = cert.SubjectDN?.ToString();

                            // ===== 3. Validate =====
                            bool isValidSignature = pkcs7.VerifySignatureIntegrityAndAuthenticity();
                            bool isCertValid = cert.IsValidNow;

                            // ===== 4. Xác định trạng thái =====
                            string status =
                                !isValidSignature ? "SIGNATURE_INVALID" :
                                !isCoversWholeDoc ? "DOCUMENT_MODIFIED" :
                                !isCertValid ? "CERT_EXPIRED" :
                                "VALID";

                            // ===== 5. Build response =====
                            var o = new ResponeVailidate
                            {
                                TrustedRoot = isValidSignature && isCoversWholeDoc && isCertValid,
                                IssuerCN = issuer,
                                SubjectCN = subject,
                                Document_id = document_id,
                                Document = document,

                                // 👉 thêm field nên có
                                //IsValidSignature = isValidSignature,
                                //IsCoversWholeDocument = isCoversWholeDoc,
                                //IsCertValid = isCertValid,
                                //Status = status
                            };

                            items.Add(o);
                        }
                    }
                    pdfDoc.Close();
                    var groups = items.GroupBy(d => new { d.Document_id, d.Document }, (x, y) => new
                    {
                        Document_id = x.Document_id,
                        Document = x.Document,
                        list = y.ToList()
                    });
                    return Json(new { message = 1, items = items, groups = groups });

                }
                catch (Exception ex)
                {
                    return Json(new { message = "Định dạng PDF không hợp lệ!" });
                }

            }
            return Json(new { message = "Yêu cầu file!" });
        }
    }
    class ResponeVailidate
    {
        public bool TrustedRoot { get; set; }
        public int? Document_id { get; set; }
        public string IssuerCN { get; set; }
        public string SubjectCN { get; set; }
        public DocumentModel? Document { get; set; }
    }
}