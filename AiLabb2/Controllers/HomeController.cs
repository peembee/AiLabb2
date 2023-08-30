﻿using AiLabb2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Xml;
using System.Drawing;


namespace AiLabb2.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration configuration;

        private ComputerVisionClient cvClient;

        private readonly ILogger<HomeController> _logger;

        private string azureKey = string.Empty;
        private string azureEndpoint = string.Empty;

        Dictionary<string, string> imageDictionary = new Dictionary<string, string>();

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
            azureKey = this.configuration["azureKey"];
            azureEndpoint = this.configuration["azureEndpoint"];
            
        }


        public async Task<IActionResult> Index()
        {
            await DisplayImages();
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }


        public async Task<IActionResult> DisplayImages()
        {
            List<string> imagesList = new List<string>();

            string getImageMap = "wwwroot/Images";
            var imageFiles = Directory.GetFiles(getImageMap);

            foreach (var imageName in imageFiles)
            {
                imagesList.Add(Path.GetFileName(imageName));
            }       
            return View("DisplayImages", imagesList);
        }


        public async Task<IActionResult> ProcessUrl(string url)
        {
            bool imageFound = false;

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

                ViewBag.Url = "Ingen fil vald";
                await DisplayImages();
            }
            else
            {
                if (url.StartsWith("data:image"))
                {
                    await ProcessOnlineUrl(url);
                }
                else
                {                    
                    try
                    {
                        url = "wwwroot/Images/" + url;
                        using (var imageData = System.IO.File.OpenRead(url))
                        {
                            var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);

                            // get image captions
                            string DescriptionKey = "Description";
                            string DescriptionValue = "";                           
                            foreach (var caption in analysis.Description.Captions)
                            {
                                DescriptionValue += $"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})";
                                //imageDictionary.Add("Description", $"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})");
                                //descriptions.Add($"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})");
                            }
                            imageDictionary.Add(DescriptionKey, DescriptionValue);


                            // Get image tags
                            string TagsKey = "Tags";
                            string TagsValue = "";
                            if (analysis.Tags.Count > 0)
                            {                                
                                //descriptions.Add("\nTags:");
                                foreach (var tag in analysis.Tags)
                                {
                                    TagsValue += $"{tag.Name} (confidence: {tag.Confidence.ToString("P")})";
                                    
                                    //imageDictionary.Add($"Tags", ($" -{tag.Name} (confidence: {tag.Confidence.ToString("P")})"));
                                    //descriptions.Add($" -{tag.Name} (confidence: {tag.Confidence.ToString("P")})");
                                }                                
                            }
                            else
                            {
                                TagsValue += "Found no tags..";
                            }
                            imageDictionary.Add(TagsKey, TagsValue);


                            // Get image categories
                            string categoriesKey = "Categories";
                            string categoriesValue = "";
                            foreach (var category in analysis.Categories)
                            {
                                categoriesValue += $"{category.Name} (confidence: {category.Score.ToString("P")})";
                            }                          
                            imageDictionary.Add(categoriesKey, categoriesValue);


                            // Get brands in the image
                            string brandsKey = "Brands";
                            string brandsValue = "";
                            if (analysis.Brands.Count > 0)
                            {
                                Console.WriteLine("Brands:");
                                foreach (var brand in analysis.Brands)
                                {
                                    brandsValue +=$"{brand.Name} (confidence: {brand.Confidence.ToString("P")})";
                                }
                            }
                            else
                            {
                                brandsValue += "Found no brands..";
                            }
                            imageDictionary.Add(brandsKey, brandsValue);


                            // Get objects in the image
                            string objectsKey = "Objects";
                            string objectsValue = "";
                            if (analysis.Objects.Count > 0)
                            {

                                // Prepare image for drawing
                                Image image = Image.FromFile(url);
                                Graphics graphics = Graphics.FromImage(image);
                                Pen pen = new Pen(Color.Cyan, 3);
                                Font font = new Font("Arial", 16);
                                SolidBrush brush = new SolidBrush(Color.Black);

                                foreach (var detectedObject in analysis.Objects)
                                {
                                    // Print object name
                                    objectsValue += $"{detectedObject.ObjectProperty} (confidence: {detectedObject.Confidence.ToString("P")})";

                                    // Draw object bounding box
                                    var r = detectedObject.Rectangle;
                                    Rectangle rect = new Rectangle(r.X, r.Y, r.W, r.H);
                                    graphics.DrawRectangle(pen, rect);
                                    graphics.DrawString(detectedObject.ObjectProperty, font, brush, r.X, r.Y);

                                }                                
                            }
                            else
                            {
                                objectsValue += "Found no objects..";
                            }
                            imageDictionary.Add(objectsKey, objectsValue);


                            // Get moderation ratings
                            string moderationKey = "Moderation";
                            string moderationValue = "";
                            moderationValue += $"Ratings: Adult: {analysis.Adult.IsAdultContent} Racy: {analysis.Adult.IsRacyContent} Gore: {analysis.Adult.IsGoryContent}";                          
                            imageDictionary.Add(moderationKey, moderationValue);
                        }
                        imageFound = true;
                    }
                    catch (Exception ex)
                    {

                        ViewBag.Url = "No match: " + url;
                        await DisplayImages();
                    }
                }
            }

            if (imageFound)
            {
                return View(imageDictionary);
            }
            else
            {
                return View("index");
            }
        }


        private async Task ProcessOnlineUrl(string url)
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
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(url);
                    using (var imageData = new MemoryStream(imageBytes))
                    {
                        var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);

                        foreach (var caption in analysis.Description.Captions)
                        {
                            //descriptions.Add($"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})");
                        }
                    }
                }

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
            catch (Exception ex)
            {
                ViewBag.Url = "No match in the online part";
                await DisplayImages();
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}