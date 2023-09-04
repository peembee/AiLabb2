using AiLabb2.Models;
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

        //display all image in Index-Page (partialView)
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


        //checking the url
        public async Task<IActionResult> ProcessUrl(string url)
        {
            imageDictionary.Clear();
            bool imageFound = false;
            string sendUrlLinkToProcessUrlView = url;
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
                url = "wwwroot/Images/" + url;
                using (var imageData = System.IO.File.OpenRead(url))
                {
                    var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);

                    // get image captions
                    string DescriptionKey = "Description";
                    string DescriptionValue = "";
                    foreach (var caption in analysis.Description.Captions)
                    {
                        DescriptionValue += $"Description: {caption.Text} - Confidence: {caption.Confidence.ToString("P")}<br />";
                    }
                    imageDictionary.Add(DescriptionKey, DescriptionValue);


                    // Get image tags
                    string TagsKey = "Tags";
                    string TagsValue = "";
                    if (analysis.Tags.Count > 0)
                    {
                        foreach (var tag in analysis.Tags)
                        {
                            TagsValue += $"{tag.Name} - Confidence: {tag.Confidence.ToString("P")}<br />";
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
                        categoriesValue += $"{category.Name} - Confidence: {category.Score.ToString("P")}<br />";
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
                            brandsValue += $"{brand.Name} - Confidence: {brand.Confidence.ToString("P")}<br />";
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
                            objectsValue += $"{detectedObject.ObjectProperty} - Confidence: {detectedObject.Confidence.ToString("P")}<br />";

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
                    moderationValue += $"Ratings: Adult: {analysis.Adult.IsAdultContent} <br /> Racy: {analysis.Adult.IsRacyContent} <br /> Gore: {analysis.Adult.IsGoryContent}";
                    imageDictionary.Add(moderationKey, moderationValue);
                }
                imageFound = true;
            }
            // if no matches the image-url, let user know
            catch (Exception ex)
            {
                ViewBag.Url = "No match";
                await DisplayImages();
            }


            if (imageFound)
            {
                ViewBag.SingleImage = sendUrlLinkToProcessUrlView;

                return View(imageDictionary);
            }
            else
            {
                return View("index");
            }
        }


        // if the image is an online-image-adress
        public async Task<IActionResult> ProcessOnlineUrl(string url)
        {
            bool imageFound = false;
            if (string.IsNullOrWhiteSpace(url))
            {

                ViewBag.Url = "Missing Text";
                await DisplayImages();
            }
            else
            {
                imageDictionary.Clear();                

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
                        string base64Data = url.Substring(url.IndexOf(',') + 1); // Antar att kommat (,) separerar metadata och Base64-data

                        // Dekodera base64-data till en byte-array
                        byte[] imageBytes = Convert.FromBase64String(base64Data);

                        // Skapa en MemoryStream från byte-array
                        using (var imageData = new MemoryStream(imageBytes))
                        {
                            // Använd imageData för att analysera bilden med Computer Vision API
                            var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);


                            // get image captions
                            string DescriptionKey = "Description";
                            string DescriptionValue = "";
                            foreach (var caption in analysis.Description.Captions)
                            {
                                DescriptionValue += $"Description: {caption.Text} - Confidence: {caption.Confidence.ToString("P")}<br />";
                            }
                            imageDictionary.Add(DescriptionKey, DescriptionValue);


                            // Get image tags
                            string TagsKey = "Tags";
                            string TagsValue = "";
                            if (analysis.Tags.Count > 0)
                            {
                                foreach (var tag in analysis.Tags)
                                {
                                    TagsValue += $"{tag.Name} - Confidence: {tag.Confidence.ToString("P")}<br />";
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
                                categoriesValue += $"{category.Name} - Confidence: {category.Score.ToString("P")}<br />";
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
                                    brandsValue += $"{brand.Name} - Confidence: {brand.Confidence.ToString("P")}<br />";
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
                                    objectsValue += $"{detectedObject.ObjectProperty} - Confidence: {detectedObject.Confidence.ToString("P")}<br />";

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
                            moderationValue += $"Ratings: Adult: {analysis.Adult.IsAdultContent} <br /> Racy: {analysis.Adult.IsRacyContent} <br /> Gore: {analysis.Adult.IsGoryContent}";
                            imageDictionary.Add(moderationKey, moderationValue);
                        }
                        imageFound = true;
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Url = "No match";
                    await DisplayImages();
                }
            }
            if (imageFound)
            {
                ViewBag.SingleImageOnline = url;

                return View(imageDictionary);
            }
            else
            {
                return View("Index");
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateThumbnail(string url, int widht, int height)
        {
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(azureKey);
            cvClient = new ComputerVisionClient(credentials)
            {
                Endpoint = azureEndpoint
            };
            url = "wwwroot/Images/" + url;
            // Generate a thumbnail
            try
            {
                using (var imageData = System.IO.File.OpenRead(url))
                {
                    ViewBag.CreatedThumbnail3 = "inne i using";

                    // Get thumbnail data
                    if (imageData != null)
                    {
                        var thumbnailStream = await cvClient.GenerateThumbnailInStreamAsync(widht, height, imageData, true);

                        // Save thumbnail image
                        string thumbnailFileName = $"wwwroot/Thumbnails/thumbnail";
                        using (Stream thumbnailFile = System.IO.File.Create(thumbnailFileName))
                        {
                            thumbnailStream.CopyTo(thumbnailFile);
                        }
                        ViewBag.CreatedThumbnail = $"Success! Thumbnail saved in Map: {thumbnailFileName}";
                    }
                    else
                    {
                        ViewBag.CreatedThumbnail4 = "Could not create a Thumbnail..";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.CreatedThumbnail = "No image found";
            }
            return View("GetThumbnail");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}