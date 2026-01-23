namespace SimplCommerce.Module.Orders.Areas.Orders.ViewModels
{
    public class CreateOrderModel
    {
        public string PaymentMethod { get; set; }
        public decimal PaymentFeeAmount { get; set; }
        public string OrderNote { get; set; }
        public string IdempotencyKey { get; set; }
    }
}