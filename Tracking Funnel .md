Kế Hoạch Tích Hợp Tracking Funnel (AARRR) Cho Frontend

Tài liệu này cung cấp hướng dẫn chi tiết để team Frontend tích hợp các chỉ số chuyển đổi (Funnel) từ Landing Page đến tính phí vào Super Admin Dashboard.

 **1\. Yêu Cầu Tổng Quan**

Mục tiêu của hệ thống là theo dõi hai phễu chuyển đổi chính:  
1\. Landing Page \-\> Đăng Ký Tài Khoản (Signup)  
2\. Đăng Ký Xài Thử (Trial) \-\> Mua Gói Trả Phí (Paid)

Dữ liệu sẽ được phục vụ cho 2 đối tượng chính:  
\- Hệ thống Landing Page (Ghi nhận traffic công khai)  
\- Super Admin Dashboard (Xem báo cáo thống kê)

—-----------------------------------------------------------------------------------------------------------------------

 **2\. Danh Sách Các API Liên Quan**

 **2.1. API Tracking Lượt Xem Landing Page (Ghi nhận)**

   Endpoint: \`POST /api/tracking/page-view\` (Thuộc Identity Service port mặc định \`5001\`)  
   Chức năng: Tăng bộ đếm lượt view của ngày hôm nay lên \+1 trong Redis.  
   Header Authorization: Không yêu cầu (\`AllowAnonymous\`)  
   Body: Không yêu cầu truyền nội dung body.  
   Mục đích: Để Frontend Landing Page chủ động gọi lên server thông báo "vừa có 1 người vào trang chủ".

Code mẫu (JS/React/Vue):  
\`\`\`javascript  
// Gọi API này 1 lần duy nhất khi người dùng load trang Landing Page (trang chủ giới thiệu)  
// Có thể đặt trong useEffect (React) hoặc mounted (Vue)  
fetch('https://identity-api.domain.com/api/tracking/page-view', {  
    method: 'POST'  
})  
.then(res \=\> res.json())  
.then(data \=\> console.log('Tracked view:', data.date));  
\`\`\`

 **2.2. API Lấy Phễu Landing Page \-\> Đăng Ký (Báo cáo)**

   Endpoint: \`GET /api/super-admin/users/stats/funnel/landing-to-signup?from={YYYY-MM-DD}\&to={YYYY-MM-DD}\` (Thuộc Identity Service)  
   Chức năng: Trả về biểu diễn dữ liệu của phễu: số Views, số Signups, và Tỷ lệ chuyển đổi tính theo từng ngày.  
   Header Authorization: Yêu cầu JWT Token (Role: \[SuperAdmin\](file:///c:/Users/tranl/OneDrive/Desktop/EXE/\_360Retail-Backend/src/Services/Identity/Infrastructure/Services/SuperAdmin/SuperAdminUserService.cs18-23))  
   Query Params:  
       \`from\`: (Tùy chọn) Ngày bắt đầu, format \`yyyy-MM-dd\`. Mặc định 30 ngày trước.  
       \[to\](file:///c:/Users/tranl/OneDrive/Desktop/EXE/\_360Retail-Backend/src/Services/Identity/Application/DTOs/SuperAdmin/Tracking/TrackingStatsDto.cs9-16): (Tùy chọn) Ngày kết thúc, format \`yyyy-MM-dd\`. Mặc định là hôm nay.

Dữ liệu trả về:  
\`\`\`json  
{  
  "success": true,  
  "data": \[  
    {  
      "date": "2023-10-01",  
      "landingPageViews": 1000,  
      "signups": 50,  
      "conversionRate": 5.00  
    }  
  \]  
}

 **2.3. API Lấy Phễu Trial \-\> Paid** 

   Endpoint: \`GET /api/super-admin/saas/dashboard/overview\` (Thuộc Saas Service port mặc định \`5004\`)  
   Chức năng: Trả về các chỉ số cấp cao (MRR, Số cửa hàng), bao gồm Tỷ lệ chuyển đổi từ việc Xài thử sang Trả tiền.  
   Header Authorization: Yêu cầu JWT Token (Role: \[SuperAdmin\](file:///c:/Users/tranl/OneDrive/Desktop/EXE/\_360Retail-Backend/src/Services/Identity/Infrastructure/Services/SuperAdmin/SuperAdminUserService.cs18-23))

Dữ liệu trả về (Chú ý trường \`trialToPaidConversionRate\`):  
\`\`\`json  
{  
  "success": true,  
  "data": {  
    "totalRevenue": 15000000.0,  
    "monthlyRecurringRevenue": 5000000.0,  
    "activeStores": 25,  
    "trialStores": 120,  
    "expiredStores": 15,  
    "trialToPaidConversionRate": 17.24 // Phễu chuyển đổi (%)   
  }  
}

—----------------------------------------------------------------------------------------------

 **3\. Kế Hoạch Triển Khai**  
 Bước 1: Landing Page \- Tích hợp Tracking View  
Trách nhiệm: Team phụ trách Landing Page (Website giao diện giới thiệu).  
   Thêm đoạn code \`fetch POST\` gọi \`/api/tracking/page-view\`.  
   Đảm bảo chỉ gọi API này 1 lần duy nhất trên lần load trang đầu tiên (tránh spam API khi User chuyển hướng qua lại các section trên Single Page App).  
   (Tùy chọn) Chỉ gọi API trên môi trường Production, để không làm rác data khi code ở máy Local.

 Bước 2: Super Admin Dashboard \- Màn Hình Tổng Quan (Overview)  
Trách nhiệm: Team phụ trách trang quản trị Super Admin.  
   Tạo thẻ (Card) "Tỷ Lệ Chốt Gói (Trial \-\> Paid)" trên màn hình Dashboard chính.  
   Gọi API \`GET /api/super-admin/saas/dashboard/overview\` và hiển thị giá trị nằm trong field \`trialToPaidConversionRate\`.

 Bước 3: Super Admin Dashboard \- Màn Hình Phễu Đăng Ký  
Trách nhiệm: Team phụ trách trang quản trị Super Admin  
   Tích hợp thư viện biểu đồ chữ nhật chóp (Funnel Chart) hoặc Line/Bar Chart (Ví dụ: \`Chart.js\`, \`ApexCharts\`, \`Recharts\`).  
   Tạo bộ lọc ngày tháng (Mặc định chọn chế độ "30 ngày gần đây").  
   Gọi API \`GET /api/super-admin/users/stats/funnel/landing-to-signup\` theo ngày tháng User đã chọn.  
   Vẽ biểu đồ đường (Line Chart) biểu diễn lượt Views vs Lượt Signups cùng nằm trên hệ trục hoành.  
   Hoặc hiển thị một Funnel Chart cộng dồn lượng data của danh sách trả về.

 Bước 4: Kiểm thử  
   Dùng tài khoản Email: \`admin@360retail.com\` | Mật khẩu: \`123456\` để Login vào Dashboard và xác thực dữ liệu.  
   Dùng tab Incognito (Ẩn danh) mở Landing page để kiểm tra xem lượt view trả về bảng báo cáo Admin Dashboard có tăng nhẹ hay không.

