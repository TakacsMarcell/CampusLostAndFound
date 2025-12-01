using System.ComponentModel.DataAnnotations;

namespace CampusLostAndFound.Models
{
    public enum ClaimStatus { New = 1, Approved = 2, Rejected = 3 }

    public class Claim
    {
        public int Id { get; set; }

        public int ItemReportId { get; set; }
        public ItemReport? ItemReport { get; set; }

        [Required]
        public string ClaimerName { get; set; } = null!;

        [Required]
        public string ClaimerEmail { get; set; } = null!;

        public string? Message { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.New;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
