using SimplCommerce.Module.Payments.Models;

namespace SimplCommerce.Module.Payments.Areas.Payments.ViewModels
{
    public class PaymentWebhookModel
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public decimal PaymentFee { get; set; }
        public PaymentStatus Status { get; set; }
        public string FailureMessage { get; set; }
    }
}