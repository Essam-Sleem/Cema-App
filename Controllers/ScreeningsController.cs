using CemaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScreeningsController : Controller
    {
        private readonly AppDbContext _context;

        public ScreeningsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Screenings
        public async Task<IActionResult> Index()
        {
            var screenings = await _context.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return View(screenings);
        }

        // GET: Screenings/Create
        public IActionResult Create()
        {
            ViewData["MovieId"] = new SelectList(_context.Movies.Where(m => m.IsActive), "Id", "Title");
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name");
            return View();
        }

        // POST: Screenings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Screening screening)
        {
            if (screening.StartTime < DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Screening date and time cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(screening);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieId"] = new SelectList(_context.Movies.Where(m => m.IsActive), "Id", "Title", screening.MovieId);
            ViewData["HallId"] = new SelectList(_context.Halls, "Id", "Name", screening.HallId);
            return View(screening);
        }

        // GET: Screenings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var screening = await _context.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (screening == null) return NotFound();

            ViewBag.HasBookings = screening.Bookings.Any();

            return View(screening);
        }

        // POST: Screenings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var screening = await _context.Screenings
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (screening != null)
            {
                if (screening.Bookings.Any())
                {
                    TempData["ErrorMessage"] = "Cannot cancel this screening because it has active/associated bookings. You must cancel or remove the bookings first.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                try
                {
                    _context.Screenings.Remove(screening);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"[DELETE SCREENING ERROR] Database exception: {ex.Message}");
                    TempData["ErrorMessage"] = "A database error occurred. The screening could not be cancelled.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
