using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gift_of_the_givers.Models
{
    public class Donation
    {
        [Key]
        public int DonationId { get; set; }

        // Nullable because anonymous donations are supported.
        [ForeignKey(nameof(User))]
        public string? UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999999.99)]
        public decimal Amount { get; set; }

        [Required]
        public DonationCurrency Currency { get; set; }

        [Required]
        public DonationType DonationType { get; set; }

        public DateTime DonationDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? TaxCertificateReference { get; set; }

        public bool IsAnonymous { get; set; } = false;

        // Navigation property
        public ApplicationUser? User { get; set; }
    }

    public enum DonationType
    {
        OneTime,
        Recurring
    }

    public enum DonationCurrency
    {
        ZAR,
        USD,
        EUR
    }
}
