using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("document_error")]
    public class DocumentErrorModel
    {
        public int id { get; set; }

        public int document_id { get; set; }

        public string message { get; set; }


    }
}
