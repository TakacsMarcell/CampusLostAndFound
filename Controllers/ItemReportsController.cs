using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CampusLostAndFound.Data;
using CampusLostAndFound.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Controllers
{
    public class ItemReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;

        public ItemReportsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        // LISTA + DETAILS lehessen anonim is
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var items = _context.ItemReports
                .Include(i => i.Category)
                .Include(i => i.Location);
            return View(await items.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var itemReport = await _context.ItemReports
                .Include(i => i.Category)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (itemReport == null) return NotFound();

            return View(itemReport);
        }

        // Csak bejelentkezett user hozhat létre hirdetést
        [Authorize]
        public IActionResult Create(ReportType? type)
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["LocationId"] = new SelectList(_context.Locations, "Id", "Name");

            var model = new ItemReport();

            if (type.HasValue)
            {
                model.Type = type.Value;  // Lost vagy Found
            }

            return View(model);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemReport itemReport, IFormFile? photo)
        {
            // Tulaj beállítása
            itemReport.OwnerId = _userManager.GetUserId(User);

            // FOTO UPLOAD
            if (photo != null && photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                itemReport.PhotoPath = "/uploads/" + uniqueFileName;
            }

            _context.Add(itemReport);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // EDIT – csak tulaj vagy admin
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var itemReport = await _context.ItemReports.FindAsync(id);
            if (itemReport == null) return NotFound();

            if (!CanEditOrDelete(itemReport))
                return Forbid();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", itemReport.CategoryId);
            ViewData["LocationId"] = new SelectList(_context.Locations, "Id", "Name", itemReport.LocationId);

            return View(itemReport);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemReport itemReport, IFormFile? photo)
        {
            var existing = await _context.ItemReports.FindAsync(id);
            if (existing == null) return NotFound();

            if (!CanEditOrDelete(existing))
                return Forbid();

            // Frissítés
            existing.Type = itemReport.Type;
            existing.Title = itemReport.Title;
            existing.Description = itemReport.Description;
            existing.CategoryId = itemReport.CategoryId;
            existing.LocationId = itemReport.LocationId;
            existing.ContactName = itemReport.ContactName;
            existing.ContactEmail = itemReport.ContactEmail;

            // Foto csere
            if (photo != null && photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                existing.PhotoPath = "/uploads/" + uniqueFileName;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE – csak tulaj vagy admin
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var itemReport = await _context.ItemReports
                .Include(i => i.Category)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (itemReport == null) return NotFound();

            if (!CanEditOrDelete(itemReport))
                return Forbid();

            return View(itemReport);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var itemReport = await _context.ItemReports.FindAsync(id);
            if (itemReport == null) return NotFound();

            if (!CanEditOrDelete(itemReport))
                return Forbid();

            _context.ItemReports.Remove(itemReport);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ItemReportExists(int id)
        {
            return _context.ItemReports.Any(e => e.Id == id);
        }

        // Segédfüggvény: tulaj vagy admin?
        private bool CanEditOrDelete(ItemReport item)
        {
            // Admin mindenhez hozzáfér, még claimed itemhez is
            if (User.IsInRole("Admin"))
                return true;

            // Sima felhasználó nem szerkeszthet/törölhet claimed itemet
            if (item.Status == ItemStatus.Claimed)
                return false;

            // Sima felhasználó csak a SAJÁT itemét módosíthatja
            var userId = _userManager.GetUserId(User);
            return item.OwnerId == userId;
        }

    }
}
