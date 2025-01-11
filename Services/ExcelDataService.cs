using ExcelDataImporter_.Models;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using System.Data;
using EFCore.BulkExtensions;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;





namespace ExcelDataImporter_.Services
{
    public class ExcelDataService : IExcelDataService
    {
        private readonly ApplicationDbContext context;
        private readonly IMemoryCache memoryCache;

        public ExcelDataService(ApplicationDbContext context , IMemoryCache memoryCache)
        {
            this.context = context;
            this.memoryCache = memoryCache;
        }


        public async Task ImportExcelFileAsync(string filePath)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            const int batchSize = 5000;
            var productsToInsert = new List<Product>();
            var productsToUpdate = new List<Product>();

            var existingProducts = await context.Products
                .AsNoTracking()
                .Select(p => new { p.PartSKU, Product = p })
                .ToDictionaryAsync(p => p.PartSKU);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration { UseHeaderRow = true }
                });

                foreach (DataTable table in result.Tables)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        var partSKU = row["Part SKU"]?.ToString();
                        if (string.IsNullOrWhiteSpace(partSKU)) continue;

                        if (!existingProducts.TryGetValue(partSKU, out var existingProduct))
                        {
                            productsToInsert.Add(new Product
                            {
                                Band = int.Parse(row["Band #"].ToString()),
                                CategoryCode = row["Category Code"].ToString(),
                                Manufacturer = row["Manufacturer"].ToString(),
                                PartSKU = partSKU,
                                ItemDescription = row["Item Description"].ToString(),
                                ListPrice = decimal.Parse(row["List Price"].ToString()),
                                MinDiscount = decimal.Parse(row["Minimum Discount"].ToString()),
                                DiscountPrice = decimal.Parse(row["Discounted Price"].ToString())
                            });
                        }
                        else
                        {
                            existingProduct.Product.Band = int.Parse(row["Band #"].ToString());
                            existingProduct.Product.CategoryCode = row["Category Code"].ToString();
                            existingProduct.Product.Manufacturer = row["Manufacturer"].ToString();
                            existingProduct.Product.ItemDescription = row["Item Description"].ToString();
                            existingProduct.Product.ListPrice = decimal.Parse(row["List Price"].ToString());
                            existingProduct.Product.MinDiscount = decimal.Parse(row["Minimum Discount"].ToString());
                            existingProduct.Product.DiscountPrice = decimal.Parse(row["Discounted Price"].ToString());
                            productsToUpdate.Add(existingProduct.Product);
                        }

                        if (productsToInsert.Count >= batchSize || productsToUpdate.Count >= batchSize)
                        {
                            await SaveChangesAsync(productsToInsert, productsToUpdate);
                        }
                    }
                }
            }

            if (productsToInsert.Any() || productsToUpdate.Any())
            {
                await SaveChangesAsync(productsToInsert, productsToUpdate);
            }
        }


        private async Task SaveChangesAsync(List<Product> productsToInsert, List<Product> productsToUpdate)
        {
            if (productsToInsert.Any())
            {
              
                await context.BulkInsertAsync(productsToInsert);
              
            }

            if (productsToUpdate.Any())
            {
                

                await context.BulkUpdateAsync(productsToUpdate);
              
            }
        }

        public async Task<ProductListViewModel> GetProductsWithCacheAsync(int page, int pageSize, string searchTerm)
        {
            if (page <= 0)
            {
                page = 1;
            }

            var cacheKey = $"Products_Page_{page}_PageSize_{pageSize}_Search_{searchTerm}";

            if (!memoryCache.TryGetValue(cacheKey, out ProductListViewModel cachedData))
            {
                var productsQuery = context.Products.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchTermLower = searchTerm.ToLower();

                    productsQuery = productsQuery.Where(p =>
                        (p.PartSKU != null && p.PartSKU.ToLower().Contains(searchTermLower)) ||
                        (p.CategoryCode != null && p.CategoryCode.ToLower().Contains(searchTermLower)) ||
                        (p.Manufacturer != null && p.Manufacturer.ToLower().Contains(searchTermLower)) ||
                        (p.ItemDescription != null && p.ItemDescription.ToLower().Contains(searchTermLower)) ||
                        EF.Functions.Like(p.Band.ToString(), $"%{searchTermLower}%") ||
                        EF.Functions.Like(p.ListPrice.ToString(), $"%{searchTermLower}%") ||
                        EF.Functions.Like(p.MinDiscount.ToString(), $"%{searchTermLower}%") ||
                        EF.Functions.Like(p.DiscountPrice.ToString(), $"%{searchTermLower}%")
                    );
                }

                var totalRecords = await productsQuery.CountAsync();
                var products = await productsQuery
                    .OrderBy(p => p.PartSKU)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductViewModel
                    {
                        Band = p.Band,
                        PartSKU = p.PartSKU,
                        CategoryCode = p.CategoryCode,
                        Manufacturer = p.Manufacturer,
                        ItemDescription = p.ItemDescription,
                        ListPrice = p.ListPrice,
                        MinDiscount = p.MinDiscount,
                        DiscountPrice = p.DiscountPrice,
                    })
                    .ToListAsync();

                cachedData = new ProductListViewModel
                {
                    Products = products,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    SearchTerm = searchTerm
                };

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };
                memoryCache.Set(cacheKey, cachedData, cacheEntryOptions);
            }

            return cachedData;
        }








    }
}