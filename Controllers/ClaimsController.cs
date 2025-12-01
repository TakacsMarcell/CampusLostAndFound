using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CampusLostAndFound.Data;
using CampusLostAndFound.Models;
using Microsoft.AspNetCore.Authorization;


namespace CampusLostAndFound.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClaimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Claims
        public async Task<IActionResult> Index()
        {
            var claims = _context.Claims
                .Include(c => c.ItemReport);

            return View(await claims.ToListAsync());
        }

        // GET: Claims/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var claim = await _context.Claims
                .Include(c => c.ItemReport)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null)
                return NotFound();

            return View(claim);
        }

        // GET: Claims/Create?itemReportId=5
        public IActionResult Create(int itemReportId)
        {
            var item = _context.ItemReports.Find(itemReportId);
            if (item == null) return NotFound();

            // Ha már Claimed, ne engedjük a claimet
            if (item.Status == ItemStatus.Claimed)
            {
                // Visszadobjuk az item részletezőre
                return RedirectToAction("Details", "ItemReports", new { id = itemReportId });
            }

            var claim = new Claim
            {
                ItemReportId = itemReportId
            };

            return View(claim);
        }

        // POST: Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim)
        {
            if (!ModelState.IsValid)
            {
                return View(claim);
            }

            var item = await _context.ItemReports.FindAsync(claim.ItemReportId);
            if (item == null)
            {
                return NotFound();
            }

  
            if (item.Status == ItemStatus.Claimed)
            {
                return RedirectToAction("Details", "ItemReports", new { id = claim.ItemReportId });
            }

            claim.Status = ClaimStatus.New;
            claim.CreatedAt = DateTime.Now;

            _context.Claims.Add(claim);

  
            if (item.Status == ItemStatus.Open)
            {
                item.Status = ItemStatus.PendingClaim;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "ItemReports", new { id = claim.ItemReportId });
        }


        // GET: Claims/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var claim = await _context.Claims
                .Include(c => c.ItemReport)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
                return NotFound();

            ViewData["ItemReportId"] = new SelectList(_context.ItemReports, "Id", "Title", claim.ItemReportId);

            return View(claim);
        }

        // POST: Claims/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Claim claim)
        {
            if (id != claim.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["ItemReportId"] = new SelectList(_context.ItemReports, "Id", "Title", claim.ItemReportId);
                return View(claim);
            }

            try
            {
                _context.Update(claim);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClaimExists(claim.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Claims/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var claim = await _context.Claims
                .Include(c => c.ItemReport)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null)
                return NotFound();

            return View(claim);
        }

        // POST: Claims/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.ItemReport)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim != null)
            {
                // Ha töröljük a claimet, az item státuszt vissza lehet állítani
                if (claim.ItemReport != null && claim.Status == ClaimStatus.New)
                {
                    claim.ItemReport.Status = ItemStatus.Open;
                }

                _context.Claims.Remove(claim);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // APPROVE
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.ItemReport)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Approved;
            if (claim.ItemReport != null)
            {
                claim.ItemReport.Status = ItemStatus.Claimed;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // REJECT
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.ItemReport)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Rejected;
            if (claim.ItemReport != null)
            {
                claim.ItemReport.Status = ItemStatus.Open;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClaimExists(int id)
        {
            return _context.Claims.Any(e => e.Id == id);
        }
    }
}
