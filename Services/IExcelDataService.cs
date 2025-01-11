using ExcelDataImporter_.Models;

namespace ExcelDataImporter_.Services
{
    public interface IExcelDataService
    {
        Task ImportExcelFileAsync(string filePath);
        Task<ProductListViewModel> GetProductsWithCacheAsync(int page, int pageSize, string searchTerm);
    }
}
