# Backend Requirements — Tibia Webstore

## Overview

Backend system for a webstore selling virtual goods and services for Tibia (Aurera / Eternia), including automated delivery, payments, and customer management.

---

## Core Features

### Products

* Sell: Gold, Items, Characters, Tibia Coins, Scripts, Macros, Services
* Product types support (digital, manual delivery, automated delivery)
* Categorization system
* SEO-friendly slugs

### Inventory

* Stock tracking per product
* Reserved stock for pending orders
* Prevent overselling

### Orders

* Order creation and tracking
* Order items with quantity and price snapshot
* Order status lifecycle (Pending, Paid, Cancelled)
* Order status history tracking

### Payments

* Mercado Pago integration
* Payment entity tracking
* Webhook handling for automatic confirmation
* Payment logs for debugging

### Delivery System

* Automatic delivery (scripts, macros, digital goods)
* Manual delivery (characters, gold)
* Delivery status tracking

### Customer Area

* User authentication (JWT + refresh tokens)
* Order history
* Order tracking

### Custom Orders

* Request system for:

  * Scripts
  * Macros
* Status tracking (Pending, InProgress, Delivered)

### Scripts & Macros Marketplace

* Free and paid items
* File download system

### Notifications

* WhatsApp notifications per sale
* Optional email notifications
* Background processing system

### Admin Dashboard

* Manage products
* Manage orders
* Manage stock
* Manage users
* Integration with existing dashboard

### Security

* HTTPS enforcement
* Password hashing
* Email verification
* Password reset

### SEO

* Slugs for products and categories
* Metadata support

### Responsiveness

* Backend supports frontend for mobile, tablet, desktop

---

## Technical Stack

### Technologies

* C#
* .NET

### Libraries

* ASP.NET Core (Web API)
* Entity Framework Core
* FluentValidation
* AutoMapper
* Mercado Pago SDK

### Database

* PostgreSQL

### Architecture

* Clean Architecture with layers for API, Application, Domain, and Infrastructure
* DDD (Domain-Driven Design)

---

## Domain Entities

### User

* Id
* Name
* Email
* PasswordHash
* Role
* CreatedAt
* UpdatedAt

### Session

* Id
* UserId
* Token
* ExpiresAt

### RefreshToken

* Id
* UserId
* Token
* ExpiresAt

### Product

* Id
* Name
* Description
* Price
* Type
* IsDigital
* RequiresManualDelivery
* Slug
* CreatedAt
* UpdatedAt

### Category

* Id
* Name
* Slug
* ParentId

### ProductCategory

* ProductId
* CategoryId

### Stock

* Id
* ProductId
* Quantity
* ReservedQuantity
* UpdatedAt

### Cart

* Id
* UserId
* TotalAmount
* CreatedAt
* UpdatedAt

### CartItem

* Id
* CartId
* ProductId
* Quantity

### Order

* Id
* UserId
* TotalAmount
* Status
* CreatedAt
* UpdatedAt

### OrderItem

* Id
* OrderId
* ProductId
* Quantity
* UnitPrice

### OrderStatusHistory

* Id
* OrderId
* Status
* ChangedAt

### Payment

* Id
* OrderId
* Amount
* Status
* CreatedAt
* UpdatedAt

### PaymentWebhookLog

* Id
* Payload
* Processed
* CreatedAt

### Delivery

* Id
* OrderId
* Status
* DeliveryType
* Data
* DeliveredAt

### Notification

* Id
* Type
* Payload
* Status
* SentAt

### CustomOrder

* Id
* UserId
* Type
* Description
* Status
* Price
* CreatedAt

### Coupon

* Code
* DiscountType
* Value
* ExpiresAt

### AuditLog

* Id
* Entity
* Action
* Data
* CreatedAt

---

## Background Jobs

* Payment confirmation processing
* WhatsApp notifications
* Delivery automation
* Retry failed operations

---

## Domain Events

* OrderCreated
* OrderPaid
* OrderCancelled

---

## Integrations

* Mercado Pago (payments)
* WhatsApp API (notifications)

---

## Non-Functional Requirements

* Scalability
* Security
* Reliability
* Maintainability
* Logging and monitoring

---

## Future Enhancements

* Anti-fraud system
* Rate limiting
* Advanced analytics
* Multi-server support
