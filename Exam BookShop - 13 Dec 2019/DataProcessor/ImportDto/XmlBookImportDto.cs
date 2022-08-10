using BookShop.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml.Serialization;

namespace BookShop.DataProcessor.ImportDto
{
    [XmlType("Book")]
    public class XmlBookImportDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(30)]
        [XmlElement("Name")]
        public string Name { get; set; }

        [Required]
        [Range(1, 3)]
        public string Genre { get; set; }

        [Range(0.01, double.MaxValue)]

        public decimal Price { get; set; }

        [Range(50, 5000)]

        public int Pages { get; set; }

        [Required]

        public string PublishedOn { get; set; }
    }
}
