using CemaApp.Models;
using CemaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CemaApp.Controllers
{
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;

        public MoviesController(AppDbContext context)
        {
            _context = context;
        }
        // Public View: Anyone can see the list of movies with optional filtering
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchString, string? genre, int page = 1)
        {
            var query = _context.Movies.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var trimmedSearch = searchString.Trim();
                query = query.Where(m => m.Title.Contains(trimmedSearch));
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(m => m.Genre == genre);
            }

            // Pagination parameters
            int pageSize = 10;
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var movies = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Get unique genres for the filter dropdown
            ViewBag.Genres = await _context.Movies
                .AsNoTracking()
                .Select(m => m.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentGenre = genre;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(movies);
        }
        // Public View: Anyone can see movie details
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var movie = await _context.Movies
                    .Include(m => m.Screenings)
                    .ThenInclude(s => s.Hall)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (movie == null)
                {
                    return NotFound();
                }
                return View(movie);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DETAILS ERROR] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        // GET: Movies/Create
        // This simply returns the empty form to the Admin
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(MovieCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                Movie newMovie = new Movie
                {
                    Title = model.Title,
                    Description = model.Description,
                    Genre = model.Genre,
                    DurationMinutes = model.DurationMinutes,
                    ReleaseDate = model.ReleaseDate,
                    PosterUrl = model.PosterUrl?.Trim(),
                    TrailerUrl = model.TrailerUrl,
                    IsActive = true
                };

                _context.Movies.Add(newMovie);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }
        // GET: Movies/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            var model = new MovieEditViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Genre = movie.Genre,
                DurationMinutes = movie.DurationMinutes,
                ReleaseDate = movie.ReleaseDate,
                IsActive = movie.IsActive,
                PosterUrl = movie.PosterUrl ?? string.Empty,
                TrailerUrl = movie.TrailerUrl
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var movie = await _context.Movies.FindAsync(model.Id);
                if (movie == null) return NotFound();

                movie.Title = model.Title;
                movie.Description = model.Description;
                movie.Genre = model.Genre;
                movie.DurationMinutes = model.DurationMinutes;
                movie.ReleaseDate = model.ReleaseDate;
                movie.IsActive = model.IsActive;
                movie.PosterUrl = model.PosterUrl?.Trim();
                movie.TrailerUrl = model.TrailerUrl;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
        // GET: Movies/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies
                .Include(m => m.Screenings)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            ViewBag.HasScreenings = movie.Screenings.Any();

            return View(movie);
        }

        // POST: Movies/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.Screenings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie != null)
            {
                if (movie.Screenings.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete this movie because it has scheduled screenings. Delete the screenings first or mark the movie as Inactive.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                try
                {
                    _context.Movies.Remove(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"[DELETE MOVIE ERROR] Database exception: {ex.Message}");
                    TempData["ErrorMessage"] = "A database error occurred. The movie could not be deleted.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}