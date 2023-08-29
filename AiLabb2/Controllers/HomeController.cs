using AiLabb2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.CodeAnalysis;

namespace AiLabb2.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration configuration;

        private ComputerVisionClient cvClient;

        private readonly ILogger<HomeController> _logger;

        private string azureKey = string.Empty;
        private string azureEndpoint = string.Empty;



        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
            azureKey = this.configuration["azureKey"];
            azureEndpoint = this.configuration["azureEndpoint"];
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> ProcessUrl(string url)
        {
            // Authenticate Computer Vision client
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(azureKey);
            cvClient = new ComputerVisionClient(credentials)
            {
                Endpoint = azureEndpoint
            };

            // Specify features to be retrieved
            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Adult
            };

            // Get image analysis
            if (string.IsNullOrWhiteSpace(url))
            {
                ViewBag.TestValue = "Ingen fil vald";
            }
            else
            {
                var base64Data = url.Substring(url.IndexOf(',') + 1);
                var imageBytes = Convert.FromBase64String(base64Data);

                using (var imageData = new MemoryStream(imageBytes))
                {
                    var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);

                    var descriptions = new List<string>();

                    // get image captions
                    foreach (var caption in analysis.Description.Captions)
                    {
                        descriptions.Add($"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})");
                    }

                    ViewBag.Descriptions = descriptions;

                    // Get image tags
                    // ...

                    // Get image categories
                    // ...

                    // Get brands in the image
                    // ...

                    // Get objects in the image
                    // ...

                    // Get moderation ratings
                    // ...
                }
            }
            return View("Index");
        }







        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}