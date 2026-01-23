using SimplCommerce.Module.Checkouts.Areas.Checkouts.ViewModels;

namespace SimplCommerce.Module.Orders.Areas.Orders.ViewModels
{
    public class CreateOrderRequest
    {
        /// <summary>
        /// 幂等性键，用于防止重复创建订单
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// 现有收货地址ID
        /// </summary>
        public long ShippingAddressId { get; set; }

        /// <summary>
        /// 新收货地址信息
        /// </summary>
        public ShippingAddressVm NewShippingAddress { get; set; }

        /// <summary>
        /// 配送方式
        /// </summary>
        public string ShippingMethod { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        public string PaymentMethod { get; set; }

        /// <summary>
        /// 支付手续费
        /// </summary>
        public decimal PaymentFeeAmount { get; set; }
    }
}