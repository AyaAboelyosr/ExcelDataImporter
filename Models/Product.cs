namespace ExcelDataImporter_.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int Band { get; set; }
        public string CategoryCode { get; set; }
        public string Manufacturer { get; set; }
        public string PartSKU { get; set; }
        public string ItemDescription { get; set; }
        public decimal ListPrice { get; set; }
        public decimal MinDiscount { get; set; }
        public decimal DiscountPrice { get; set; }

    }
}
