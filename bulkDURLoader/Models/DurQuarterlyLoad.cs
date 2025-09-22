using System;
using System.ComponentModel.DataAnnotations;

namespace bulkDURLoader.Models
{
    /// <summary>
    /// Data model for ccBulk_DUR_QTR_LOAD table
    /// Represents a quarterly DUR (Drug Utilization Review) load record
    /// </summary>
    public class DurQuarterlyLoad
    {
        /// <summary>
        /// Unique identifier for the record
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Member ID for the DUR record
        /// </summary>
        [Required]
        [StringLength(50)]
        public string MemberId { get; set; } = string.Empty;

        /// <summary>
        /// Prescription number or identifier
        /// </summary>
        [StringLength(50)]
        public string? PrescriptionNumber { get; set; }

        /// <summary>
        /// National Drug Code (NDC)
        /// </summary>
        [StringLength(11)]
        public string? Ndc { get; set; }

        /// <summary>
        /// Date of service
        /// </summary>
        public DateTime? ServiceDate { get; set; }

        /// <summary>
        /// Prescribing provider ID
        /// </summary>
        [StringLength(50)]
        public string? ProviderId { get; set; }

        /// <summary>
        /// Pharmacy ID where prescription was filled
        /// </summary>
        [StringLength(50)]
        public string? PharmacyId { get; set; }

        /// <summary>
        /// Drug name
        /// </summary>
        [StringLength(255)]
        public string? DrugName { get; set; }

        /// <summary>
        /// Drug strength
        /// </summary>
        [StringLength(50)]
        public string? DrugStrength { get; set; }

        /// <summary>
        /// Quantity dispensed
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Days supply
        /// </summary>
        public int? DaysSupply { get; set; }

        /// <summary>
        /// Paid amount
        /// </summary>
        public decimal? PaidAmount { get; set; }

        /// <summary>
        /// DUR alert code
        /// </summary>
        [StringLength(10)]
        public string? DurAlertCode { get; set; }

        /// <summary>
        /// DUR alert description
        /// </summary>
        [StringLength(500)]
        public string? DurAlertDescription { get; set; }

        /// <summary>
        /// Quarter of the year (Q1, Q2, Q3, Q4)
        /// </summary>
        [Required]
        [StringLength(2)]
        public string Quarter { get; set; } = string.Empty;

        /// <summary>
        /// Year of the quarterly load
        /// </summary>
        [Required]
        public int Year { get; set; }

        /// <summary>
        /// Load batch identifier
        /// </summary>
        [StringLength(50)]
        public string? BatchId { get; set; }

        /// <summary>
        /// Date when record was created/loaded
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when record was last updated
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Status of the record (e.g., 'PENDING', 'PROCESSED', 'ERROR')
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// Any error message associated with processing
        /// </summary>
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional data in JSON format for flexibility
        /// </summary>
        public string? AdditionalData { get; set; }
    }
}