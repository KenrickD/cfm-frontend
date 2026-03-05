using System.ComponentModel.DataAnnotations;

namespace cfm_frontend.DTOs.Inventory
{
    /// <summary>
    /// Request DTO for creating or updating inventory transactions
    /// Maps to backend InventoryPayloadDto in InventoryModelDTO.cs
    /// Used by POST /api/v1/inventories (create) and PUT /api/v1/inventories (update)
    /// </summary>
    public class InventoryPayloadDto
    {
        /// <summary>
        /// Inventory transaction ID (0 for create, > 0 for update)
        /// </summary>
        public int IdInventoryTransactionHistory { get; set; } = 0;

        /// <summary>
        /// Client ID
        /// </summary>
        [Required(ErrorMessage = "Client ID is required")]
        public int IdClient { get; set; }

        /// <summary>
        /// Inventory transaction status enum ID
        /// Options: Stock Increase, Stock Usage, Stock Return
        /// From enum category: inventoryTransactionStatus
        /// </summary>
        [Required(ErrorMessage = "Transaction status is required")]
        public int InventoryStatus_IdEnum { get; set; }

        /// <summary>
        /// Transaction date
        /// </summary>
        [Required(ErrorMessage = "Transaction date is required")]
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Material job code ID
        /// </summary>
        [Required(ErrorMessage = "Material is required")]
        public int Material_IdJobCode { get; set; }

        /// <summary>
        /// Quantity of material in transaction
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public double Quantity { get; set; }

        /// <summary>
        /// Optional description of transaction
        /// </summary>
        public string? Description { get; set; }
    }
}
