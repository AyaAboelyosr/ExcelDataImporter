namespace ExcelDataImporter_.Models
{
    public class ProductListViewModel
    {
        public List<ProductViewModel> Products { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }  // Add TotalPages to handle pagination
        public string SearchTerm { get; set; }  // Add SearchTerm property for filtering
    }


}
