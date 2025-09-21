# Booking API – Home Availability

This project is a sample **ASP.NET Core 9 Web API** that demonstrates how to build a clean and testable application for querying homes available in a given date range.  

The goal was not only to return correct results, but also to design the codebase in a way that **a real production team could extend and maintain**.  
That’s why you’ll see **N-Tier architecture**, **CQRS with MediatR**, validation, logging pipeline behaviors, and proper test coverage.

---

## How to Run the Application

1. **Clone the repository**

   ```bash
   git clone https://github.com/HamidMammadov/Booking.git
   cd booking
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Run the API**

   ```bash
   dotnet run --project src/Booking.Api
   ```

   By default the API listens on:  
   [http://localhost:5087](http://localhost:5087)

4. **Swagger UI**

   You can explore and test the API interactively via:  
   [http://localhost:5087/swagger](http://localhost:5087/swagger)

---

## How to Test the Application

We treat tests as **first-class citizens**. Every feature is covered by either unit or integration tests.

1. Run all tests:

   ```bash
   dotnet test Booking.sln
   ```

2. The test suite includes:
   - **Validation tests** – incorrect inputs (bad date format, overly large ranges) must fail fast with `400 Bad Request`.
   - **Handler unit tests** – the filtering logic is tested in isolation.
   - **Integration tests** – spin up the full ASP.NET host (`WebApplicationFactory`) and call the real HTTP endpoints.
   - **Pagination tests** – verify metadata (`page`, `pageSize`, `total`, `totalPages`) as well as headers (`Link`, `X-Total-Count`).

The philosophy: *if it’s not tested, it doesn’t exist*.

---

## Architecture and Filtering Logic

### Architecture

We intentionally kept a **clean separation of concerns**:

- **Domain**  
  Defines entities (`Property`, `PropertyId`) and abstractions (`IPropertyReadRepository`).  
  No dependencies on infrastructure.

- **Application**  
  Encapsulates all use-cases. Here we use **CQRS with MediatR** (`GetAvailableHomesQuery`).  
  Validation is handled with FluentValidation.  
  Pipeline behaviors (logging, validation) ensure cross-cutting concerns don’t pollute the handlers.

- **Infrastructure**  
  Implements the repository with a lightweight **in-memory seed**.  
  The seed is randomized but designed to look realistic: some homes overlap, some don’t.

- **API**  
  Very thin controllers. They:
  - Parse and validate query parameters.
  - Dispatch requests through MediatR.
  - Return typed responses (`GetAvailableHomesResult`), enriched with proper HTTP status codes, Swagger annotations, and pagination headers.

- **Tests**  
  xUnit + FluentAssertions, following the rule that **business logic is always covered by automated tests**.

---

### Filtering Logic

The filtering is intentionally straightforward, but implemented cleanly:

1. **Input**  
   - Query params: `startDate`, `endDate` (`yyyy-MM-dd`), and optional `page`, `pageSize`.

2. **Process**  
   - Build the closed date range `[startDate..endDate]`.  
   - For each property:
     - Compute the intersection of that range with the property’s available slots.
     - If intersection is empty → skip.
     - If not empty → return only those intersecting slots.
   - Order results by `HomeName` (then `HomeId` for stability).
   - Apply pagination (`Skip`/`Take`).

3. **Output**  
   - Response JSON includes:
     - `status`, `page`, `pageSize`, `total`, `totalPages`
     - `homes[]` with `homeId`, `homeName`, `availableSlots`
   - Response headers include `Link` (first/prev/next/last) and `X-Total-Count`.

---

## Example Request

```http
GET /api/available-homes?startDate=2025-07-07&endDate=2025-07-17&page=1&pageSize=5
```

### Example Response

```json
{
  "status": "OK",
  "page": 1,
  "pageSize": 5,
  "total": 42,
  "totalPages": 9,
  "homes": [
    {
      "homeId": "1001",
      "homeName": "Baku Panoramic Villa #1001",
      "availableSlots": [ "2025-07-08", "2025-07-09", "2025-07-11" ]
    }
  ]
}
```

---

## Final Notes

This repository is designed as a **teaching-quality sample**, but the patterns (CQRS, MediatR, validation, thin controllers, integration testing) are the same ones you’d see in a **real-world production system**.  

The takeaway: clean separation, predictable behavior, and testability always win in the long run.
