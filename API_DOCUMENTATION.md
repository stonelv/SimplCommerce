# SimplCommerce Public API Documentation

## 1. 产品API

### 1.1 获取产品列表

**接口地址：** GET /api/v1/products

**功能描述：** 获取产品列表，支持分页、搜索、筛选和排序

**请求参数：**

| 参数名 | 类型 | 描述 | 是否必填 |
|--------|------|------|----------|
| page | int | 页码，默认1 | 否 |
| pageSize | int | 每页数量，默认20 | 否 |
| search | string | 搜索关键词 | 否 |
| categoryId | long | 分类ID | 否 |
| minPrice | decimal | 最低价格 | 否 |
| maxPrice | decimal | 最高价格 | 否 |
| sortBy | string | 排序字段（name, price, createdOn） | 否 |
| sortOrder | string | 排序方向（asc, desc） | 否 |

**响应示例：**
```json
{
  "total": 100,
  "page": 1,
  "pageSize": 20,
  "products": [
    {
      "id": 1,
      "name": "示例产品",
      "slug": "example-product",
      "price": 99.99,
      "originalPrice": 129.99,
      "shortDescription": "产品简短描述",
      "description": "产品详细描述",
      "isPublished": true,
      "isAllowToOrder": true,
      "stockQuantity": 100,
      "stockTrackingIsEnabled": true,
      "thumbnailUrl": "https://example.com/thumbnail.jpg",
      "createdOn": "2024-01-01T00:00:00Z"
    }
  ]
}
```

## 2. 购物车API

### 2.1 添加商品到购物车

**接口地址：** POST /api/v1/carts/{cartId}/items

**功能描述：** 将商品添加到购物车

**请求参数：**

| 参数名 | 类型 | 描述 | 是否必填 |
|--------|------|------|----------|
| cartId | long | 购物车ID | 是 |
| productId | long | 产品ID | 是 |
| quantity | int | 数量 | 是 |
| variationId | long | 产品变体ID | 否 |

**请求体示例：**
```json
{
  "productId": 1,
  "quantity": 2,
  "variationId": 100
}
```

**响应示例：**
```json
{
  "success": true,
  "message": "商品已添加到购物车",
  "cartItem": {
    "id": 1,
    "productId": 1,
    "quantity": 2,
    "price": 99.99,
    "subtotal": 199.98
  }
}
```

## 3. 订单API

### 3.1 创建订单

**接口地址：** POST /api/v1/orders

**功能描述：** 根据购物车商品创建订单

**请求参数：**

| 参数名 | 类型 | 描述 | 是否必填 |
|--------|------|------|----------|
| requestId | string | 请求ID（幂等校验） | 否 |
| shippingAddress | object | 收货地址 | 是 |
| paymentMethod | string | 支付方式 | 是 |

**请求体示例：**
```json
{
  "requestId": "unique-request-id-123",
  "shippingAddress": {
    "firstName": "张",
    "lastName": "三",
    "phone": "13800138000",
    "addressLine1": "北京市朝阳区某某街道",
    "addressLine2": "某某大厦10层",
    "city": "北京",
    "stateOrProvince": "北京市",
    "postalCode": "100000",
    "country": "中国"
  },
  "paymentMethod": "alipay"
}
```

**响应示例：**
```json
{
  "orderId": 1001,
  "orderNumber": "ORD-20240101-0001",
  "totalAmount": 199.98,
  "status": "Pending"
}
```

## 4. 支付API

### 4.1 支付回调

**接口地址：** POST /api/v1/payments/webhooks/{provider}

**功能描述：** 处理支付提供商的回调通知

**路径参数：**

| 参数名 | 类型 | 描述 | 是否必填 |
|--------|------|------|----------|
| provider | string | 支付提供商标识（alipay, wechatpay, paypal等） | 是 |

**请求体：** 由具体支付提供商定义，通常包含支付结果信息

**响应示例：**
```json
{
  "success": true,
  "message": "回调处理成功"
}
```

## 5. 错误响应格式

**响应示例：**
```json
{
  "error": "错误描述",
  "details": "详细错误信息"
}
```

## 6. 认证

所有API接口均需要认证，支持以下认证方式：

1. JWT Token：在请求头中携带 `Authorization: Bearer <token>`
2. Cookie认证：适用于浏览器环境

## 7. 版本控制

API使用URL版本控制，当前版本为v1。未来版本将通过URL路径区分，如 `/api/v2/products`。