using Attendance.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace Attendance.Web.Controllers
{
    public class TrainingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        static string personGroupId = "951f5c978aec4fca89f5b25f5d9a57d8";
        static string personGroupName = "StudentsGroup";

        const string SUBSCRIPTION_KEY = "951f5c978aec4fca89f5b25f5d9a57d8";
        const string ENDPOINT = "https://attendanceaifaceservice.cognitiveservices.azure.com/";

        public TrainingController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            // Authenticate Face client
            var trainingData = await TrainStudentsModel();
            trainingData.ToList().ForEach(x =>
            {
                x.UserData = Path.Combine("Images", "Students", x.Name, x.UserData);
            });
            return View(trainingData);
        }

        [HttpGet]
        public IActionResult Scanner()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Scanner(IFormFile file)
        {
            try
            {
                using Stream fileStream = file.OpenReadStream();
                var result = await IdentityStudent(fileStream);

                if (result.Count > 0)
                {
                    var student = _context.Student.FirstOrDefault(x => x.Code == result.FirstOrDefault().Value);
                    return RedirectToAction(actionName: "Details", controllerName: "Students", new { id = student.Id });
                }
            }
            catch (APIErrorException ex)
            {

            }
            catch (Exception ex)
            {

                throw;
            }

            return View();
        }

        public IActionResult ScanResult()
        {

            return View();
        }

        private async Task<IList<Person>> TrainStudentsModel()
        {
            var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };

            // Delete group if it already exists
            var groups = await faceClient.PersonGroup.ListAsync();
            foreach (var group in groups)
                await faceClient.PersonGroup.DeleteAsync(group.PersonGroupId);

            // Create person group
            await faceClient.PersonGroup.CreateAsync(personGroupId, personGroupName);

            // Get all students
            var allStudents = _context.Student.ToList();
            foreach (var student in allStudents)
            {
                await Task.Delay(250);

                // Create new group person
                var person = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, student.Code, userData: student.ProfileImageUrl);
                student.PersonId = person.PersonId;
                _context.Student.Update(student);

                // Add face for group person
                var studentImagesPaths = Directory.EnumerateFiles(Path.Combine(_webHostEnvironment.WebRootPath, "Images", "Students", student.Code));
                foreach (var imagePath in studentImagesPaths)
                {
                    using var imageData = System.IO.File.OpenRead(imagePath);
                    await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, imageData);
                }
            }
            _context.SaveChanges();

            // Train the model
            await faceClient.PersonGroup.TrainAsync(personGroupId);

            // Wait for training to complete
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId);
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }

            var people = await faceClient.PersonGroupPerson.ListAsync(personGroupId);
            return people;
        }

        public async Task<Dictionary<Guid, string>> IdentityStudent(Stream imageData)
        {
            var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };

            var detectedFaces = await faceClient.Face.DetectWithStreamAsync(imageData);

            // Get faces
            if (detectedFaces.Count > 0)
            {
                // Get a list of face IDs
                var faceIds = detectedFaces.Select(f => f.FaceId.Value).ToList();

                // Identify the faces in the people group
                var recognizedFaces = await faceClient.Face.IdentifyAsync(faceIds, personGroupId);

                // Get names for recognized faces
                var faceNames = new Dictionary<Guid, string>();
                if (recognizedFaces.Count > 0)
                {
                    foreach (var face in recognizedFaces)
                    {
                        var person = await faceClient.PersonGroupPerson.GetAsync(personGroupId, face.Candidates[0].PersonId);
                        faceNames.Add(face.FaceId, person.Name);
                    }
                }

                return faceNames;
            }
            else
                return null;
        }
    }
}
