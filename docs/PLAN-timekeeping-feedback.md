 # Plan: Timekeeping & Customer Feedback

> 2 tính năng dùng bảng DB đã có sẵn, chỉ cần thêm DTOs + Service + Controller.

---

## Chức năng 1: Chấm công (Timekeeping) — HR Service

### Có sẵn
- **Entity**: `Timekeeping.cs` ✅ (EmployeeId, CheckInTime, CheckOutTime, LocationGps, CheckInImageUrl, IsLate)
- **DbContext**: `HrDbContext.Timekeepings` ✅ (full EF config)
- **DB table**: `hr.timekeepings` ✅

### Cần tạo

#### [NEW] `TimekeepingDtos.cs`
Path: `HR/Application/DTOs/TimekeepingDtos.cs`

```csharp
// Request DTOs
CheckInDto       { LocationGps?, CheckInImageUrl? }
CheckOutDto      { LocationGps? }

// Response DTO
TimekeepingDto   { Id, EmployeeId, EmployeeName, CheckInTime, CheckOutTime, 
                   LocationGps, CheckInImageUrl, IsLate, WorkHours }
TimekeepingSummaryDto { EmployeeId, EmployeeName, TotalDays, LateDays, 
                        TotalHours, AvgHoursPerDay }
```

#### [NEW] `ITimekeepingService.cs`
Path: `HR/Application/Interfaces/ITimekeepingService.cs`

```
CheckInAsync(storeId, appUserId, dto) → TimekeepingDto
CheckOutAsync(storeId, appUserId, dto) → TimekeepingDto
GetHistoryAsync(storeId, employeeId?, from?, to?, page, pageSize) → paged list
GetSummaryAsync(storeId, month, year) → List<TimekeepingSummaryDto>
GetTodayStatusAsync(storeId, appUserId) → TimekeepingDto?
```

#### [NEW] `TimekeepingService.cs`
Path: `HR/Infrastructure/Services/TimekeepingService.cs`

**Business rules**:
- Check-in: Tìm employee theo AppUserId + StoreId → tạo record với CheckInTime = UTC now
- Check-out: Tìm record check-in hôm nay chưa check-out → cập nhật CheckOutTime
- IsLate: Nếu CheckInTime > 9:00 AM (configurable) → isLate = true
- Không cho check-in 2 lần trong 1 ngày
- WorkHours = CheckOutTime - CheckInTime (computed)

#### [NEW] `TimekeepingController.cs`
Path: `HR/API/Controllers/TimekeepingController.cs`

| Method | Route | Auth | Mô tả |
|--------|-------|------|-------|
| POST | `/api/timekeeping/check-in` | Staff+ | Nhân viên check-in |
| POST | `/api/timekeeping/check-out` | Staff+ | Nhân viên check-out |
| GET | `/api/timekeeping/today` | Staff+ | Trạng thái hôm nay |
| GET | `/api/timekeeping` | Manager/Owner | Lịch sử chấm công (filter) |
| GET | `/api/timekeeping/summary` | Manager/Owner | Tổng hợp tháng |

---

## Chức năng 2: Feedback khách hàng — CRM Service

### Có sẵn
- **Entity**: `CustomerFeedback.cs` ✅ (CustomerId, Content, Rating, Source, CreatedByEmployeeId)
- **DbContext**: `CrmDbContext.CustomerFeedbacks` ✅ (full EF config)
- **DB table**: `crm.customer_feedbacks` ✅

### Cần tạo

#### [MODIFY] `CrmDtos.cs` (thêm Feedback DTOs)
Path: `CRM/Application/DTOs/CrmDtos.cs`

```csharp
// Request
CreateFeedbackDto    { CustomerId, Content, Rating (1-5), Source? }

// Response
FeedbackDto          { Id, CustomerId, CustomerName, Content, Rating, 
                       Source, CreatedByEmployeeId, CreatedAt }
FeedbackSummaryDto   { AvgRating, TotalCount, RatingDistribution (1→5) }
```

#### [NEW] `IFeedbackService.cs` + `FeedbackService.cs`
Path: `CRM/Application/Services/` (cùng pattern với `ICustomerService`)

```
CreateAsync(storeId, dto, employeeId?) → FeedbackDto
GetByCustomerAsync(customerId, storeId, page, pageSize) → paged list
GetByStoreAsync(storeId, rating?, from?, to?, page, pageSize) → paged list
GetSummaryAsync(storeId) → FeedbackSummaryDto
```

#### [NEW] `FeedbackController.cs`
Path: `CRM/API/Controllers/FeedbackController.cs`

| Method | Route | Auth | Mô tả |
|--------|-------|------|-------|
| POST | `/api/feedback` | Staff+ | Tạo feedback cho khách |
| GET | `/api/feedback` | Staff+ | Danh sách feedback (filter) |
| GET | `/api/feedback/summary` | Manager/Owner | Tổng hợp rating |
| GET | `/api/customers/{id}/feedback` | Staff+ | Feedback theo customer |

**DI Registration**: Thêm `builder.Services.AddScoped<IFeedbackService, FeedbackService>()` vào `CRM/API/Program.cs`

---

## Verification Plan

### Build
```bash
dotnet build 360Retail.sln
```

### Docker Test
```bash
docker-compose up -d --build

# Timekeeping
POST /hr/timekeeping/check-in → 200
POST /hr/timekeeping/check-in (lần 2) → 400 "Already checked in"
POST /hr/timekeeping/check-out → 200
GET  /hr/timekeeping/today → record with WorkHours
GET  /hr/timekeeping?from=2026-02-01&to=2026-02-28 → list
GET  /hr/timekeeping/summary?month=2&year=2026 → summary

# Feedback
POST /crm/feedback → 201
POST /crm/feedback (rating=0) → 400 "Rating must be 1-5"
GET  /crm/feedback → list
GET  /crm/feedback/summary → avg rating + distribution
GET  /crm/customers/{id}/feedback → customer's feedback
```

---

## Thứ tự thực hiện

```
1. Timekeeping DTOs + Interface + Service + Controller + DI
2. Customer Feedback DTOs + Interface + Service + Controller + DI  
3. Build verify
4. Docker rebuild + API test
```
