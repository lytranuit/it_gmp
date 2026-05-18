using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it.Areas.Admin.Models
{
    [Table("related_esign")]
    public class RelatedEsignModel
    {
        [Key]
        public int id { get; set; }
        public int esign_id { get; set; }
        public int? related_id { get; set; }
        public string? related_string_id { get; set; }
        public string type { get; set; }

        public DateTime? created_at { get; set; }

    }

}
