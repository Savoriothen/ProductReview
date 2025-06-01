# ProductReview.Api

This project is a REST-based API that allows users to submit and fetch reviews for various products, sorted in reverse chronological order. The purpose of the project is to demonstrate the use of Azure Table Storage and business logic implementation in API development.

---

## 🧱 Project Structure

- **ASP.NET Core Web API** – For HTTP API implementation
- **Azure.Data.Tables** – For Azure Table Storage access (locally via Azurite)
- **Swagger / Swashbuckle** – For API documentation and testing
- **Docker + Azurite** – Local storage emulator setup
- **Unit Tests** – To validate critical business logic

---

## 🔧 Design Decisions and Business Logic

| Requirement | Implementation |
|-------------|----------------|
| Product name-based identification | `PartitionKey` is the product name in Azure Table Storage |
| Reverse chronological review listing | Sorted by `CreatedAt` in descending order |
| Allow review submission only if user has seen the latest | `lastSeenReviewTimestamp` is compared to the latest review |
| Review length restriction | `[MaxLength(500)]` validation on `ReviewRequest.Text` |
| Swagger documentation | Via `[ProducesResponseType]` attributes, XML comments, and JSON schemas |
| Error handling | 400 and 409 handled automatically with `[ApiController]` |

---

## 🚀 Running in Development

### Requirements
- .NET 8 SDK or later
- Docker (for Azurite)

### Local Storage: Azurite
```bash
docker-compose up -d
```

### Start Application
```bash
dotnet run --project ProductReview.Api
```

---

## 🌐 Swagger UI – API Documentation

Available in development mode at:

🔗 [https://localhost:7058/swagger](https://localhost:7058/swagger)  
💡 Appears under the `/swagger` path.

---

## 📋 Examples

### `GET /reviews/baba`
Lists reviews for product "baba".

### `POST /reviews/baba`
```json
{
  "text": "This is a test review.",
  "lastSeenReviewTimestamp": "2025-05-23T12:00:00Z"
}
```

