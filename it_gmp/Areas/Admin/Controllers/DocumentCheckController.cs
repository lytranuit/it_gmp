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
                    foreach (string namesign in names)
                    {
                        DocumentModel? document = null;
                        int? document_id = null;
                        if (namesign.Contains("GMP"))
                        {
                            var list = namesign.Split("-");
                            document_id = Int32.Parse(list[2]);
                            document = _context.DocumentModel.Where(d => d.id == document_id).Include(d => d.user).FirstOrDefault();

                        }
                        PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(namesign);
                        X509Certificate cert = pkcs7.GetSigningCertificate();
                        var Issuer = cert.IssuerDN;
                        var Subject = cert.SubjectDN;
                        var o = new ResponeVailidate
                        {
                            TrustedRoot = pkcs7.VerifySignatureIntegrityAndAuthenticity() && cert.IsValidNow,
                            IssuerCN = Issuer.ToString(),
                            SubjectCN = Subject.ToString(),
                            Document_id = document_id,
                            Document = document,
                            //SubjectL = cert.SubjectL,
                            //SubjectO = cert.SubjectO,
                            //SubjectOU = cert.SubjectOU,
                            //SubjectS = cert.SubjectS
                        };

                        items.Add(o);
                    }
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