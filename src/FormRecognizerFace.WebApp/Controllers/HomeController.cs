using FormRecognizerFace.CosmosDb;
using FormRecognizerFace.Storage;
using FormRecognizerFace.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FormRecognizerFace.WebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IBlobStorageRepository blobStorageRepository;
        private readonly ICosmosDbRepository<Company> formRecognizerCosmosDbRepository;

        public HomeController(IBlobStorageRepository blobStorageRepository, ICosmosDbRepository<Company> formRecognizerCosmosDbRepository)
        {
            this.blobStorageRepository = blobStorageRepository;
            this.formRecognizerCosmosDbRepository = formRecognizerCosmosDbRepository;
        }

        public async Task<IActionResult> Index()
        {
            var companies = await formRecognizerCosmosDbRepository.GetAllAsync(); 

            var companiesSelection =  new List<SelectListItem>();
            foreach (var company in companies)
            {
                companiesSelection.Add(new SelectListItem(company.Name, company.Id));
            }

            return View(new HomeViewModel() { Companies = companiesSelection });
        }

        [HttpPost("FileUpload")]
        public async Task<IActionResult> FileUpload(List<IFormFile> files, string companyId)
        {
            var containerName = HttpContext.User.Claims.FirstOrDefault(t => t.Type.ToLower() == "name")?.Value;

            if (!string.IsNullOrWhiteSpace(containerName))
            {
                _ = await blobStorageRepository.CreateContainerAsync(containerName, false);
                foreach (var formFile in files)
                {
                    if (formFile.Length > 0)
                    {
                        // add metadata modelId searching cosmosDb
                        var company = await formRecognizerCosmosDbRepository.GetByIdAsync(companyId);
                        var fileName = $"{company.Name}/{formFile.FileName}";
                        await blobStorageRepository.UploadBlobAsync(formFile.OpenReadStream(), containerName, fileName); 
                        
                        var metadata = new Dictionary<string, string>()
                        {
                            { "modelId", company.FormRecognizerModelId }
                        };

                        await blobStorageRepository.SetMetadataAsync(containerName, fileName, metadata);
                    }
                }
            }            

            return View("Done");
        }

        public IActionResult Done()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
