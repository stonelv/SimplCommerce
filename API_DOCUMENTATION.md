# SimplCommerce Public API Documentation

## 1. GET /api/v1/products：商品搜索与列表

### 功能描述
获取商品列表，支持分页、排序和过滤

### 请求参数
| 参数名 | 类型 | 描述 | 示例 |
|--------|------|------|------|
| searchTerm | string | 搜索关键词 | "iphone" |
| categoryId | long | 分类ID | 1 |
| brandId | long | 品牌ID | 2 |
| minPrice | decimal | 最低价格 | 100 |
| maxPrice | decimal | 最高价格 | 1000 |
| sortBy | string | 排序字段（name/price） | "price" |
| sortOrder | string | 排序顺序（asc/desc） | "desc" |
| page | int | 页码 | 1 |
| pageSize | int | 每页数量 | 20 |

### 响应示例
```json
{
  "TotalItems": 100,
  "Page": 1,
  "PageSize": 20,
  "TotalPages": 5,
  "Items": [
    {
      "Id": 1,
      "Name": "iPhone 15",
      "ShortDescription": "最新款iPhone",
      "Price": 8999,
      "OldPrice": 9999,
      "SpecialPrice": null,
      "ThumbnailUrl": "https://example.com/iphone15.jpg",
      "IsFeatured": true
    }
  ]
}
```

## 2. POST /api/v1/carts/{cartId}/items：加入购物车

### 功能描述
将商品加入购物车，支持合并行项、校验可售与数量

### 请求参数
| 参数名 | 类型 | 描述 | 示例 |
|--------|------|------|------|
| cartId | long | 购物车ID | 1 |
| ProductId | long | 商品ID | 1 |
| Quantity | int | 数量 | 2 |

### 响应示例
```json
{
  "message": "Item added to cart successfully"
}
```

## 3. POST /api/v1/orders：从购物车创建订单

### 功能描述
从购物车创建订单，支持价格快照、库存扣减、优惠校验、事务一致性和幂等

### 请求参数
| 参数名 | 类型 | 描述 | 示例 |
|--------|------|------|------|
| CartId | long | 购物车ID | 1 |
| PaymentMethod | string | 支付方式 | "Alipay" |
| PaymentMethodAdditionalFee | decimal | 支付方式附加费 | 0 |

### 响应示例
```json
{
  "id": 1
}
```

## 4. POST /api/v1/payments/webhooks/{provider}：支付回调

### 功能描述
支付回调，支持签名校验和幂等更新订单支付状态

### 请求参数
| 参数名 | 类型 | 描述 | 示例 |
|--------|------|------|------|
| provider | string | 支付提供商 | "Alipay" |
| OrderId | long | 订单ID | 1 |
| Status | string | 支付状态（paid/failed/refunded） | "paid" |
| IdempotencyKey | string | 幂等键 | "unique-key-123" |

### 响应示例
```json
{
  "message": "Webhook processed successfully"
}
```