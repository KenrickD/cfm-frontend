namespace cfm_frontend.Models
{
    public class LaborMaterialModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public decimal quantity { get; set; }
        public decimal unitPrice { get; set; }
        public decimal totalPrice => quantity * unitPrice;
    }
}
