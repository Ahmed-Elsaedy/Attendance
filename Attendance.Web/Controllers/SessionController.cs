using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Attendance.Web.Data;
using Attendance.Web.Data.Entities;
using Attendance.Web.DTOs.Sessions;
using Microsoft.AspNetCore.Identity;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Attendance.Web.Controllers
{
    [Authorize(Roles = "Admin,Instructor")]
    public class SessionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SessionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ApplicationUser applicationUser = await _userManager.GetUserAsync(User);
            if (applicationUser != null)
            {
                var isInstructor = await _userManager.IsInRoleAsync(applicationUser, "Instructor");
                if (isInstructor)
                {
                    var instructorSessions = await _context.Sessions.Include(s => s.Instructor).Where(x => x.InstructorId == applicationUser.Id).ToListAsync();
                    return View(instructorSessions);
                }
            }
            var sessions = await _context.Sessions.Include(s => s.Instructor).ToListAsync();
            return View(sessions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Sessions == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            var studentSessions = await _context.StudentSessions
                .Include(x => x.Student.Grade)
                .Include(x => x.Student.Department)
                .Where(x => x.SessionId == id)
                .ToListAsync();


            if (session == null)
            {
                return NotFound();
            }

            ViewBag.StudentSessions = studentSessions;
            return View(session);
        }

        public async Task<IActionResult> Create()
        {

            ApplicationUser applicationUser = await _userManager.GetUserAsync(User);
            if (applicationUser != null && await _userManager.IsInRoleAsync(applicationUser, "Instructor"))
            {
                ViewData["InstructorId"] = new SelectList(new List<ApplicationUser> { applicationUser },
                    "Id", "DisplayName", applicationUser.Id);
            }
            else
            {
                var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
                ViewData["InstructorId"] = new SelectList(instructors, "Id", "DisplayName");
            }
            return View(new CreateSessionDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSessionDto sessionDto)
        {
            if (ModelState.IsValid)
            {
                var fromDate = new DateTime(sessionDto.Date.Year,
                                            sessionDto.Date.Month,
                                            sessionDto.Date.Day,
                                            sessionDto.TimeFrom.Hour,
                                            sessionDto.TimeFrom.Minute,
                                            0);

                var toDate = new DateTime(sessionDto.Date.Year,
                                            sessionDto.Date.Month,
                                            sessionDto.Date.Day,
                                            sessionDto.TimeTo.Hour,
                                            sessionDto.TimeTo.Minute,
                                            0);

                if (toDate < fromDate)
                {
                    ModelState.AddModelError("TimeTo", "Time to Should be after Time from");
                    ModelState.AddModelError("TimeFrom", "Time from Should be before Time to");
                }
                else if (toDate < DateTime.Now)
                {
                    ModelState.AddModelError("", "Session date and time cannot be in the past");
                }
                else
                {
                    var student = new Session()
                    {
                        Code = Guid.NewGuid(),
                        CourseName = sessionDto.CourseName,
                        InstructorId = sessionDto.InstructorId,
                        Subject = sessionDto.Subject,
                        DateFrom = fromDate,
                        DateTo = toDate
                    };
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            ApplicationUser applicationUser = await _userManager.GetUserAsync(User);
            if (applicationUser != null && await _userManager.IsInRoleAsync(applicationUser, "Instructor"))
            {
                ViewData["InstructorId"] = new SelectList(new List<ApplicationUser> { applicationUser },
                    "Id", "DisplayName", applicationUser.Id);
            }
            else
            {
                var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
                ViewData["InstructorId"] = new SelectList(instructors, "Id", "DisplayName", sessionDto.InstructorId);
            }
            return View(sessionDto);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Sessions == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            var editModel = new EditSessionDto()
            {
                Id = session.Id,
                CourseName = session.CourseName,
                Date = session.DateFrom.Date,
                TimeFrom = session.DateFrom,
                TimeTo = session.DateTo,
                InstructorId = session.InstructorId,
                Subject = session.Subject
            };

            ApplicationUser applicationUser = await _userManager.GetUserAsync(User);
            if (applicationUser != null && await _userManager.IsInRoleAsync(applicationUser, "Instructor"))
            {
                ViewData["InstructorId"] = new SelectList(new List<ApplicationUser> { applicationUser },
                    "Id", "DisplayName", applicationUser.Id);
            }
            else
            {
                var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
                ViewData["InstructorId"] = new SelectList(instructors, "Id", "DisplayName");
            }

            return View(editModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditSessionDto sessionDto)
        {
            if (id != sessionDto.Id)
            {
                return NotFound();
            }

            var fromDate = new DateTime(sessionDto.Date.Year,
                                        sessionDto.Date.Month,
                                        sessionDto.Date.Day,
                                        sessionDto.TimeFrom.Hour,
                                        sessionDto.TimeFrom.Minute,
                                        0);

            var toDate = new DateTime(sessionDto.Date.Year,
                                        sessionDto.Date.Month,
                                        sessionDto.Date.Day,
                                        sessionDto.TimeTo.Hour,
                                        sessionDto.TimeTo.Minute,
                                        0);

            if (toDate < fromDate)
            {
                ModelState.AddModelError("TimeTo", "Time to Should be after Time from");
                ModelState.AddModelError("TimeFrom", "Time from Should be before Time to");
            }
            else if (toDate < DateTime.Now)
            {
                ModelState.AddModelError("", "Session date and time cannot be in the past");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var session = _context.Sessions.Find(sessionDto.Id);
                    session.InstructorId = sessionDto.InstructorId;
                    session.CourseName = sessionDto.CourseName;
                    session.Code = Guid.NewGuid();
                    session.DateFrom = fromDate;
                    session.DateTo = toDate;
                    session.Subject = sessionDto.Subject;

                    _context.Update(session);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SessionExists(sessionDto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ApplicationUser applicationUser = await _userManager.GetUserAsync(User);
            if (applicationUser != null && await _userManager.IsInRoleAsync(applicationUser, "Instructor"))
            {
                ViewData["InstructorId"] = new SelectList(new List<ApplicationUser> { applicationUser },
                    "Id", "DisplayName", applicationUser.Id);
            }
            else
            {
                var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
                ViewData["InstructorId"] = new SelectList(instructors, "Id", "DisplayName", sessionDto.InstructorId);
            }

            return View(sessionDto);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Sessions == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Sessions == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Sessions'  is null.");
            }
            var session = await _context.Sessions.FindAsync(id);
            if (session != null)
            {
                _context.Sessions.Remove(session);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SessionExists(int id)
        {
            return _context.Sessions.Any(e => e.Id == id);
        }
    }
}
