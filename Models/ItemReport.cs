using System.ComponentModel.DataAnnotations;

namespace CampusLostAndFound.Models
{
    public enum ReportType { Lost = 1, Found = 2 }
    public enum ItemStatus { Open = 1, PendingClaim = 2, Claimed = 3 }

    public class ItemReport
    {
        public int Id { get; set; }

        // Ki a hirdetés tulaja (AspNetUsers.Id)
        public string? OwnerId { get; set; }

        [Required]
        public ReportType Type { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int LocationId { get; set; }
        public Location? Location { get; set; }

        public DateTime DateReported { get; set; } = DateTime.Now;

        public ItemStatus Status { get; set; } = ItemStatus.Open;

        public string? PhotoPath { get; set; }

        public string ContactName { get; set; } = null!;
        public string ContactEmail { get; set; } = null!;
    }
}
