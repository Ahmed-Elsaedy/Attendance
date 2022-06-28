using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Attendance.Web.Data;
using Attendance.Web.Data.Entities;
using Attendance.Web.DTOs.Students;
using System.Text;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.AspNetCore.Authorization;

namespace Attendance.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        static string personGroupId = "951f5c978aec4fca89f5b25f5d9a57d8";
        static string personGroupName = "StudentsGroup";

        const string SUBSCRIPTION_KEY = "951f5c978aec4fca89f5b25f5d9a57d8";
        const string ENDPOINT = "https://attendanceaifaceservice.cognitiveservices.azure.com/";

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> StudentsList()
        {
            var applicationDbContext = _context.Student.Include(s => s.Department).Include(s => s.Grade);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> StudentDetails(int? id)
        {
            if (id == null || _context.Student == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .Include(s => s.Department)
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        public IActionResult CreateStudent()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Set<Department>(), "Id", "Name");
            ViewData["GradeId"] = new SelectList(_context.Set<Grade>(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(CreateStudentDto student)
        {
            if (ModelState.IsValid)
            {
                var dbStudent = new Student()
                {
                    Birthdate = student.Birthdate,
                    Code = student.Code,
                    DepartmentId = student.DepartmentId,
                    Firstname = student.Firstname,
                    Lastname = student.Lastname,
                    GradeId = student.GradeId,
                    PhoneNumber = student.PhoneNumber
                };

                SavaStudentImages(dbStudent.Code, student.ProfileImage, out string profileImageUrl, out string imagesUrls, student.Images.ToArray());
                dbStudent.ProfileImageUrl = profileImageUrl;
                dbStudent.ImagesUrls = imagesUrls;

                dbStudent.PersonId = await CreateStudentPersonGroupPerson(dbStudent, student.Images.ToArray());

                _context.Add(dbStudent);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(StudentsList));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Set<Department>(), "Id", "Id", student.DepartmentId);
            ViewData["GradeId"] = new SelectList(_context.Set<Grade>(), "Id", "Id", student.GradeId);
            return View(student);
        }

        public async Task<IActionResult> EditStudent(int? id)
        {
            if (id == null || _context.Student == null)
            {
                return NotFound();
            }

            var student = await _context.Student.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var dto = new EditStudentDto()
            {
                Id = student.Id,
                Code = student.Code,
                Firstname = student.Firstname,
                Lastname = student.Lastname,
                Birthdate = student.Birthdate,
                ImagesUrls = student.ImagesUrls.Split(';').ToList(),
                ProfileImageUrl = student.ProfileImageUrl,
                PhoneNumber = student.PhoneNumber,
                DepartmentId = student.DepartmentId,
                GradeId = student.GradeId
            };

            ViewData["DepartmentId"] = new SelectList(_context.Set<Department>(), "Id", "Name", student.DepartmentId);
            ViewData["GradeId"] = new SelectList(_context.Set<Grade>(), "Id", "Name", student.GradeId);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(int id, EditStudentDto student)
        {
            var dbStudent = _context.Student.Find(id);
            if (dbStudent == null || id != student.Id)
                return NotFound();

            dbStudent.Code = student.Code;
            dbStudent.Firstname = student.Firstname;
            dbStudent.Lastname = student.Lastname;
            dbStudent.Birthdate = student.Birthdate;
            dbStudent.PhoneNumber = student.PhoneNumber;
            dbStudent.DepartmentId = student.DepartmentId;
            dbStudent.GradeId = student.GradeId;

            if (student.UploadNewImages)
            {
                SavaStudentImages(student.Code, student.ProfileImage, out string profileImageUrl, out string imagesUrls, student.Images.ToArray());
                dbStudent.ProfileImageUrl = profileImageUrl;
                dbStudent.ImagesUrls = imagesUrls;

                var images = new List<IFormFile>(student.Images);
                images.Add(student.ProfileImage);
                dbStudent.PersonId = await UpdateStudentPersonGroupPerson(dbStudent, images.ToArray());
            }

            _context.Update(dbStudent);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(StudentsList));
        }

        public async Task<IActionResult> DeleteStudent(int? id)
        {
            if (id == null || _context.Student == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .Include(s => s.Department)
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentConfirmed(int id)
        {
            if (_context.Student == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Student'  is null.");
            }
            var student = await _context.Student.FindAsync(id);
            if (student != null)
            {
                _context.Student.Remove(student);
            }

            await _context.SaveChangesAsync();
            await DeleteStudentPersonGroupPerson(student);
            
            string studentFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "Students", student.Code);
            if (Directory.Exists(studentFolder)) Directory.Delete(studentFolder, true);

            return RedirectToAction(nameof(StudentsList));
        }

        private void SavaStudentImages(string code,
                                       IFormFile profileImage,
                                       out string profileUrl,
                                       out string imagesUrl,
                                       params IFormFile[] images)
        {
            string studentFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "Students", code);
            if (!Directory.Exists(studentFolder)) Directory.CreateDirectory(studentFolder);

            string[] imagesUrls = new string[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                IFormFile file = images[i];

                var fileName = $"Image_{i + 1}.{file.FileName.Split('.').Last()}";
                using Stream fileStream = new FileStream(Path.Combine(studentFolder, fileName), FileMode.Create, FileAccess.Write);
                file.CopyTo(fileStream);
                imagesUrls[i] = fileName;
            }
            imagesUrl = string.Join(';', imagesUrls);

            var profileFileName = $"ProfileImage.{profileImage.FileName.Split('.').Last()}";
            Stream profileFileStream = new FileStream(Path.Combine(studentFolder, profileFileName), FileMode.Create, FileAccess.Write);
            profileImage.CopyTo(profileFileStream);
            profileUrl = profileFileName;
        }

        private async Task<Guid> CreateStudentPersonGroupPerson(Student student, IFormFile[] images)
        {
            var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };

            var personGroup = await faceClient.PersonGroup.GetAsync(personGroupId);
            if (personGroup == null)
                await faceClient.PersonGroup.CreateAsync(personGroupId, personGroupName);

            // Create new group person
            var person = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, student.Code, userData: student.ProfileImageUrl);

            // Add face for group person
            foreach (var file in images)
            {
                using Stream imageStream = file.OpenReadStream();
                await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, imageStream);
            }

            await TrainStudentsRecognitionModel();

            return person.PersonId;
        }
        private async Task<Guid> UpdateStudentPersonGroupPerson(Student student, IFormFile[] images)
        {
            var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };

            var personGroup = await faceClient.PersonGroup.GetAsync(personGroupId);
            if (personGroup == null)
                await faceClient.PersonGroup.CreateAsync(personGroupId, personGroupName);

            // Remove old group person
            if (student.PersonId.HasValue)
            {
                var currentPerson = await faceClient.PersonGroupPerson.GetAsync(personGroupId, student.PersonId.Value);
                if (currentPerson != null)
                    await faceClient.PersonGroupPerson.DeleteAsync(personGroupId, student.PersonId.Value);
            }

            // Create new group person
            var person = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, student.Code, userData: student.ProfileImageUrl);

            // Add face for group person
            foreach (var file in images)
            {
                using Stream imageStream = file.OpenReadStream();
                await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, imageStream);
            }

            await TrainStudentsRecognitionModel();

            return person.PersonId;
        }
        private async Task DeleteStudentPersonGroupPerson(Student student)
        {
            try
            {
                var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };
                if (student.PersonId.HasValue)
                {
                    var groupPerson = await faceClient.PersonGroupPerson.GetAsync(personGroupId, student.PersonId.Value);
                    if (groupPerson != null)
                        await faceClient.PersonGroupPerson.DeleteAsync(personGroupId, student.PersonId.Value);
                }
            }
            catch (APIErrorException ex)
            {
            }

            await TrainStudentsRecognitionModel();
        }
        public static async Task TrainStudentsRecognitionModel()
        {
            var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };
            await faceClient.PersonGroup.TrainAsync(personGroupId);
        }
        private bool StudentExists(int id)
        {
            return _context.Student.Any(e => e.Id == id);
        }
    }
}
