using ExcelDataImporter_.Models;
using ExcelDataImporter_.Services;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace ExcelDataImporter_.Controllers
{
    public class ProductController : Controller
    {
        private readonly IExcelDataService excelDataService;

        public ProductController(ExcelDataService excelDataService)
        {
            this.excelDataService = excelDataService;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

      
        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid Excel file.";
                return RedirectToAction("Index", "Home");
            }

            string uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }

            string filePath = Path.Combine(uploads, file.FileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                await excelDataService.ImportExcelFileAsync(filePath); 

                TempData["SuccessMessage"] = "Excel file imported successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex switch
                {
                    IOException => "The file is in use or cannot be accessed.",
                    UnauthorizedAccessException => "Access denied. Please check file permissions.",
                    _ => $"An unexpected error occurred: {ex.Message}"
                };
            }

            return RedirectToAction("Index", "Home");
        }





        [HttpGet]
        public async Task<IActionResult> GetProducts(int page = 1, string searchTerm = "")
        {
            const int pageSize = 10;
            var model = await excelDataService.GetProductsWithCacheAsync(page, pageSize, searchTerm);
            return View("showProducts", model);
        }


        [HttpPost]
        public async Task<IActionResult> ExportToExcel(string searchTerm = "")
        {
         
            var products = await excelDataService.GetProductsWithCacheAsync(1, int.MaxValue, searchTerm);

         
            var fileContents = await ExportProductsToExcel(products.Products);

            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Products.xlsx");
        }

        private async Task<byte[]> ExportProductsToExcel(List<ProductViewModel> products)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Products");

              
                worksheet.Cells[1, 1].Value = "Band #";
                worksheet.Cells[1, 2].Value = "Category Code";
                worksheet.Cells[1, 3].Value = "Manufacturer";
                worksheet.Cells[1, 4].Value = "Part SKU";
                worksheet.Cells[1, 5].Value = "Item Description";
                worksheet.Cells[1, 6].Value = "List Price";
                worksheet.Cells[1, 7].Value = "Min Discount";
                worksheet.Cells[1, 8].Value = "Discount Price";

              
                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    worksheet.Cells[i + 2, 1].Value = product.Band;
                    worksheet.Cells[i + 2, 2].Value = product.CategoryCode;
                    worksheet.Cells[i + 2, 3].Value = product.Manufacturer;
                    worksheet.Cells[i + 2, 4].Value = product.PartSKU;
                    worksheet.Cells[i + 2, 5].Value = product.ItemDescription;
                    worksheet.Cells[i + 2, 6].Value = product.ListPrice;
                    worksheet.Cells[i + 2, 7].Value = product.MinDiscount;
                    worksheet.Cells[i + 2, 8].Value = product.DiscountPrice;
                }

            
                return package.GetAsByteArray();
            }
        }


    }
}
