# BÁO CÁO ĐỐI SOÁT PHÂN HỆ 1 & 2: XÁC THỰC, HỒ SƠ & QUẢN TRỊ NGƯỜI DÙNG
**Phạm vi Use Case:** UC-001 đến UC-013  
**Vị trí trong tài liệu SRS:** Mục 2.1 (Authentication & Profile) và Mục 2.2 (User & Role Management)  
**Trạng thái đối soát:** Khớp nghiệp vụ 70%, sai lệch đường dẫn giao diện 30%

---

## 1. BẢNG TỔNG HỢP TRẠNG THÁI TỪNG USE CASE

| Mã UC | Tên Use Case | Source trong SRS | Source thực tế trong Code | Đánh giá |
| :---: | :--- | :--- | :--- | :---: |
| **UC-001** | Login to system | `BMWMS.Web/Pages/Auth/Login.cshtml` | `BMWMS.Web/Pages/Auth/Login.cshtml` | ✅ Khớp 100% |
| **UC-002** | Logout from system | `BMWMS.Web/Pages/Auth/Logout.cshtml` | `BMWMS.Web/Pages/Auth/Logout.cshtml` | ✅ Khớp 100% |
| **UC-003** | Forgot password | `BMWMS.Web/Pages/Auth/ForgotPassword.cshtml` | `BMWMS.Web/Pages/Auth/ForgotPassword.cshtml` | ✅ Khớp 100% |
| **UC-004** | Reset password | `BMWMS.Web/Pages/Auth/ResetPassword.cshtml` | `BMWMS.Web/Pages/Auth/ResetPassword.cshtml` | ✅ Khớp 100% |
| **UC-005** | View or update personal information | `BMWMS.Web/Pages/Admin/Profile.cshtml` | `BMWMS.Web/Pages/Admin/Profile.cshtml` | ✅ Khớp 100% |
| **UC-006** | Change password | `BMWMS.Web/Pages/Admin/Profile.cshtml` | `BMWMS.Web/Pages/Admin/Profile.cshtml` | ✅ Khớp 100% |
| **UC-007** | View user list | `BMWMS.Web/Pages/Admin/Users/Index.cshtml` | `BMWMS.Web/Pages/Admin/Users/Index.cshtml` | ✅ Khớp 100% |
| **UC-008** | Create user | `BMWMS.Web/Pages/Admin/Users/Create.cshtml` | `BMWMS.Web/Pages/Admin/Users/Create.cshtml` | ✅ Khớp 100% |
| **UC-009** | View user details | `BMWMS.Web/Pages/Admin/Users/Detail.cshtml` | `BMWMS.Web/Pages/Admin/Users/Detail.cshtml` | ✅ Khớp 100% |
| **UC-010** | Edit user | `BMWMS.Web/Pages/Admin/Users/Create.cshtml` | `BMWMS.Web/Pages/Admin/Users/Edit.cshtml` | ❌ Sai Source Wireframe |
| **UC-011** | Assign role to user | `BMWMS.Web/Pages/Admin/Users/Create.cshtml` | `BMWMS.Web/Pages/Admin/Users/AssignRole.cshtml` | ❌ Sai Source Wireframe |
| **UC-012** | View user operation history | `BMWMS.Web/Pages/Admin/Users/Index.cshtml` | `BMWMS.Web/Pages/Admin/Users/AuditLogs.cshtml` | ❌ Sai Source Wireframe |
| **UC-013** | View role list | `BMWMS.Web/Pages/Admin/Users/Index.cshtml` | Dropdown tại `AssignRole.cshtml` / API `RoleController` | ⚠️ Không có trang riêng |

---

## 2. CHI TIẾT CÁC ĐIỂM SAI LỆCH VÀ HƯỚNG DẪN ĐIỀU CHỈNH

### UC-010: Edit user (Chỉnh sửa người dùng)
* **Hiện trạng trong SRS:**
  * Source Wireframe ghi là: `Figure UC-010: Wireframe for Edit user. Source: BMWMS.Web/Pages/Admin/Users/Create.cshtml`.
  * Bảng Field Description liệt kê cả trường *Mật khẩu* và *Xác nhận mật khẩu*.
* **Thực tế trong Code:**
  * Hệ thống có trang chuyên biệt: [`BMWMS.Web/Pages/Admin/Users/Edit.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Users/Edit.cshtml) và [`Edit.cshtml.cs`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Users/Edit.cshtml.cs).
  * Trên form chỉnh sửa: Tên đăng nhập (`Username`) là **Read-only** (không được sửa). Chỉ sửa Họ tên, Email, Số điện thoại, Trạng thái (Khóa/Mở khóa), Ghi chú.
  * Form Edit **không nhập mật khẩu mới** (đổi mật khẩu có cơ chế riêng hoặc qua reset).
* **Nội dung cần chỉnh sửa trong SRS:**
  * Đổi Source thành: `BMWMS.Web/Pages/Admin/Users/Edit.cshtml`.
  * Sửa bảng Field Description: Tên đăng nhập (Read-only); bỏ trường Mật khẩu/Xác nhận mật khẩu; thêm trường Trạng thái hoạt động (`IsActive`).

---

### UC-011: Assign role to user (Phân vai trò người dùng)
* **Hiện trạng trong SRS:**
  * Source Wireframe ghi nhầm là: `BMWMS.Web/Pages/Admin/Users/Create.cshtml`.
* **Thực tế trong Code:**
  * Hệ thống có trang độc lập: [`BMWMS.Web/Pages/Admin/Users/AssignRole.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Users/AssignRole.cshtml) và API `PUT api/user/{id}/role` trong [`UserController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/UserController.cs).
  * Giao diện hiển thị thông tin người dùng (Read-only) và một danh sách Radio/Dropdown chọn 1 trong 7 vai trò chuẩn của hệ thống:
    1. `SYSTEM_ADMIN` (Quản trị viên)
    2. `WAREHOUSE_MANAGER` (Quản lý kho)
    3. `WAREHOUSE_STAFF` (Nhân viên kho)
    4. `PURCHASING_STAFF` (Nhân viên mua hàng)
    5. `SALES_STAFF` (Nhân viên bán hàng)
    6. `ACCOUNTANT` (Kế toán)
    7. `DIRECTOR` (Giám đốc)
* **Nội dung cần chỉnh sửa trong SRS:**
  * Đổi Source thành: `BMWMS.Web/Pages/Admin/Users/AssignRole.cshtml`.
  * Sửa bảng Field Description: Tên đăng nhập (Read-only), Họ tên (Read-only), Vai trò hiện tại (Display), Danh sách vai trò mới (Dropdown/Radio).

---

### UC-012: View user operation history (Xem lịch sử thao tác của người dùng)
* **Hiện trạng trong SRS:**
  * Source Wireframe ghi nhầm là: `BMWMS.Web/Pages/Admin/Users/Index.cshtml` (trang danh sách người dùng).
  * Bảng trường: Chỉ liệt kê Từ khóa, Trạng thái, Vai trò, Số dòng/trang.
* **Thực tế trong Code:**
  * Hệ thống có trang xem lịch sử thao tác audit log riêng cho user: [`BMWMS.Web/Pages/Admin/Users/AuditLogs.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Users/AuditLogs.cshtml) và phân hệ tổng thể [`BMWMS.Web/Pages/Admin/AuditLogs/Index.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/AuditLogs/Index.cshtml).
  * Dữ liệu nhật ký hiển thị: *Thời gian (`Timestamp`), Hành động (`ActionType`: LOGIN, CREATE, UPDATE, DELETE, APPROVE...), Thực thể tác động (`EntityName`), Giá trị cũ (`OldValues`), Giá trị mới (`NewValues`), Địa chỉ IP*.
* **Nội dung cần chỉnh sửa trong SRS:**
  * Đổi Source thành: `BMWMS.Web/Pages/Admin/Users/AuditLogs.cshtml`.
  * Sửa bảng Field Description phản ánh các cột nhật ký thao tác thực tế.

---

### UC-013: View role list (Xem danh sách vai trò)
* **Hiện trạng trong SRS:**
  * Source ghi: `BMWMS.Web/Pages/Admin/Users/Index.cshtml`.
* **Thực tế trong Code:**
  * Hệ thống không thiết kế trang danh sách vai trò riêng biệt mà cố định 7 Roles trong cơ sở dữ liệu và enum hệ thống.
  * API [`RoleController.cs`](file:///c:/bmwms/BMWMS.API/Controllers/RoleController.cs) cung cấp danh sách roles cho dropdown tại trang phân quyền.
* **Nội dung cần chỉnh sửa trong SRS:**
  * Ghi chú rõ: Vai trò là danh mục hệ thống cố định (System Roles), được tra cứu tại màn hình phân quyền người dùng [`Admin/Users/AssignRole.cshtml`](file:///c:/bmwms/BMWMS.Web/Pages/Admin/Users/AssignRole.cshtml).
