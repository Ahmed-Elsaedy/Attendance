using Attendance.Web.Data;
using Attendance.Web.Data.Entities;
using Attendance.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Text;
using System.Web;

namespace Attendance.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<StudentController> _logger;

        static string personGroupId = "951f5c978aec4fca89f5b25f5d9a57d8";
        const string SUBSCRIPTION_KEY = "951f5c978aec4fca89f5b25f5d9a57d8";
        const string ENDPOINT = "https://attendanceaifaceservice.cognitiveservices.azure.com/";

        public StudentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<StudentController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public IActionResult Camera([FromQuery] string code)
        {
            var bytes = Convert.FromBase64String(code);
            string encodedStr = Encoding.UTF8.GetString(bytes);
            var str = HttpUtility.UrlDecode(encodedStr);
            var data = str.Split('_');

            if (data.Length != 3)
                return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.InvalidCode });

            DateTime dateTime = DateTime.Parse(data[2], null, System.Globalization.DateTimeStyles.AdjustToUniversal);
            _logger.LogInformation($"QR Date: {dateTime} | UtcNow: {DateTime.UtcNow}");
            if (DateTime.UtcNow > dateTime)
                return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.ExpiredCode });

            var validCodeGuid = Guid.TryParse(data[1], out Guid codeGuid);
            if (!validCodeGuid)
                return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.InvalidCode });

            var session = _context.Sessions.FirstOrDefault(x => x.Code == codeGuid);
            if (session == null)
                return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.SessionNotFound });

            if (session.DateFrom < DateTime.Now && session.DateTo < DateTime.Now)
                return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.SessionEnded });

            if (session.DateFrom > DateTime.Now && session.DateTo > DateTime.Now)
                return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.SessionNotStartedYet });

            if (session.DateFrom <= DateTime.Now && session.DateTo > DateTime.Now)
            {
                ViewBag.Code = session.Code;
                ViewBag.Encoded = code;
                return View();
            }

            return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.UnProcessable });
        }

        [HttpPost]
        public IActionResult Snapshot(string encoded, string image, string code)
        {
            ViewData["ImageSrc"] = image;
            ViewData["Code"] = code;
            ViewData["Encoded"] = encoded;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SnapshotConfirm(string image, string code)
        {
            Student dbStudent = null;
            Session dbSession = null;

            var imageValue = image.Split("base64,")[1];
            byte[] data = Convert.FromBase64String(imageValue);
            using (MemoryStream ms = new MemoryStream(data))
            {
                var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };
                IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(ms);
                if (detectedFaces.Count == 1)
                {
                    var faceIds = detectedFaces.Select(f => f.FaceId.Value).ToList();
                    var recognizedFaces = await faceClient.Face.IdentifyAsync(faceIds, personGroupId);
                    var faceNames = new Dictionary<Guid, string>();
                    foreach (var face in recognizedFaces)
                    {
                        if (face.Candidates.Count > 0)
                        {
                            var person = await faceClient.PersonGroupPerson.GetAsync(personGroupId, face.Candidates[0].PersonId);
                            faceNames.Add(face.FaceId, person.Name);
                        }
                    }
                    if (faceNames.Count == 0)
                        return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.UnRecognizedPerson });

                    dbStudent = _context.Student.FirstOrDefault(x => x.Code == faceNames.First().Value);
                }
                else if (detectedFaces.Count > 1)
                    return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.MultipleFacesDetected });

                else if (detectedFaces.Count == 0)
                    return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.NoFacesDetected });

                if (dbStudent == null)
                    return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.StudentDataCouldNotBeFound });

                dbSession = _context.Sessions.FirstOrDefault(x => x.Code == new Guid(code));
                if (dbSession == null)
                    return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.SessionNotFound });

                if (dbSession.DateFrom < DateTime.Now && dbSession.DateTo < DateTime.Now)
                    return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.SessionEnded });

                if (dbSession.DateFrom > DateTime.Now && dbSession.DateTo > DateTime.Now)
                    return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.SessionNotStartedYet });

                if (dbSession.DateFrom <= DateTime.Now && dbSession.DateTo > DateTime.Now)
                {
                    var attended = _context.StudentSessions.Any(x => x.StudentId == dbStudent.Id && x.SessionId == dbSession.Id);
                    if (attended)
                        return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.AlreadyRegisteredForThisSession });
                    else
                    {
                        _context.StudentSessions.Add(new StudentSession()
                        {
                            SessionId = dbSession.Id,
                            StudentId = dbStudent.Id
                        });
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.Success });
                    }
                }
            }
            return RedirectToAction(nameof(Error), new { error = SessionAttendanceCodeValidationEnum.UnProcessable });
        }

        public IActionResult Error(SessionAttendanceCodeValidationEnum error)
        {
            return View(error);
        }
    }
}
