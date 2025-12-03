using System;
using System.Linq;
using System.Threading.Tasks;
using CampusLostAndFound.Data;
using CampusLostAndFound.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Controllers
{
    [Authorize] // minden bejelentkezett user elérheti (Create, stb.)
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClaimsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Claims (ADMIN LISTA)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var claims = _context.Claims
                .Include(c => c.ItemReport);

            return View(await claims.ToListAsync());
        }

        // GET: Claims/Create?itemReportId=5
        public async Task<IActionResult> Create(int itemReportId)
        {
            var item = await _context.ItemReports.FindAsync(itemReportId);
            if (item == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            // 1) SAJÁT ITEMET NE LEHESSEN CLAIMELNI
            if (!string.IsNullOrEmpty(item.OwnerId) && item.OwnerId == currentUserId)
            {
                return RedirectToAction("Details", "ItemReports", new { id = itemReportId });
            }

            // 2) CSAK OPEN STÁTUSZÚ ITEMET LEHESSEN CLAIMELNI
            //    (PendingClaim vagy Claimed esetén ne)
            if (item.Status != ItemStatus.Open)
            {
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

            var currentUserId = _userManager.GetUserId(User);

            // 1) SAJÁT ITEMET NE LEHESSEN CLAIMELNI
            if (!string.IsNullOrEmpty(item.OwnerId) && item.OwnerId == currentUserId)
            {
                return RedirectToAction("Details", "ItemReports", new { id = claim.ItemReportId });
            }

            // 2) CSAK OPEN STÁTUSZÚ ITEMET LEHESSEN CLAIMELNI
            if (item.Status != ItemStatus.Open)
            {
                return RedirectToAction("Details", "ItemReports", new { id = claim.ItemReportId });
            }

            // Ha minden OK: létrejön az új claim
            claim.Status = ClaimStatus.New;
            claim.CreatedAt = DateTime.Now;

            _context.Claims.Add(claim);

            // Item státusz → PendingClaim
            item.Status = ItemStatus.PendingClaim;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "ItemReports", new { id = claim.ItemReportId });
        }

        // APPROVE – csak ADMIN
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

        // REJECT – csak ADMIN
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
    }
}
