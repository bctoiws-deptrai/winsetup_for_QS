# 🛠️ BCT Winsetup Utility Suite v1.0

[![Framework](https://img.shields.io/badge/Framework-.NET%2010.0%20%7C%20WPF-blueviolet?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078d7?style=for-the-badge&logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](https://opensource.org/licenses/MIT)

**BCT Winsetup Utility Suite** là bộ công cụ tối ưu hóa và thiết lập hệ thống Windows sau khi cài đặt (Winuetup) được phát triển trên nền tảng **C# / WPF** và **.NET 10.0**. Ứng dụng cung cấp giao diện tối (Dark Mode) hiện đại, thanh lịch và tích hợp hàng loạt các tính năng hệ thống mạnh mẽ giúp chuẩn bị một môi trường Windows sạch sẽ, ổn định và đạt hiệu năng tối đa.

---

## 🌟 Tính Năng Nổi Bật

### 🏠 1. Trang Chủ & Thông Tin Hệ Thống (Home & Hardware Info)
* Hiển thị tổng quan cấu hình phần cứng chi tiết của máy tính (CPU, RAM, Mainboard, VGA, Ổ đĩa...).
* Thiết kế trực quan, dễ theo dõi.

### 💾 2. Sao Lưu & Khôi Phục Driver (Driver Backup & Restore)
* **Sao lưu:** Tự động quét và xuất toàn bộ driver bên thứ ba (Third-party drivers) đang hoạt động trên hệ thống sang một thư mục tùy chọn.
* **Khôi phục:** Tự động duyệt thư mục và cài đặt lại driver bằng công cụ DISM của hệ thống mà không cần phần mềm bên thứ ba.

### 📝 3. Định Danh Máy & OEM (Computer Identity)
* Thay đổi Tên máy tính (Computer Name) và Nhóm làm việc (Workgroup).
* Đăng ký thông tin nhà sản xuất OEM (Manufacturer, Model, Support URL) để cá nhân hóa hệ thống.
* Đăng ký tên chủ sở hữu (Registered Owner) và tổ chức (Registered Organization).

### 📦 4. Cài Đặt Phần Mềm Tự Động (Software Installer)
* Tự động quét thư mục ứng dụng, hiển thị danh sách các tệp tin cài đặt (`.exe`, `.msi`).
* Cho phép chọn hàng loạt và cài đặt tuần tự trong luồng nền (Background thread), giữ cho UI luôn mượt mà.

### 🅰️ 5. Cài Đặt Font Chữ Nhanh (Font Installer)
* Tự động quét tất cả các phân vùng đĩa cứng để tìm các thư mục chứa font chữ (ngoại trừ ổ đĩa cài Win C:).
* Cài đặt đồng loạt tất cả các font chữ được chọn (`.ttf`, `.otf`, `.ttc`) vào hệ thống, đăng ký Registry và gửi tín hiệu cập nhật nóng (`WM_FONTCHANGE`) để sử dụng được ngay mà không cần khởi động lại máy.

### 🧹 6. Xóa Nhật Ký Hệ Thống (Clear System Logs)
* Quét và xóa các tệp tin log rác, tệp tin tạm thời giúp giải phóng dung lượng đĩa:
  * Event Logs (Nhật ký Sự kiện Windows).
  * System Setup Logs (CBS, DISM, Panther, setupapi.log...).
  * Windows Error Reports & Dump files (Minidump, MEMORY.DMP).
  * Lịch sử dòng lệnh PowerShell (PSReadLine).
  * Prefetch & Temp Files.
  * Bộ nhớ đệm cập nhật Windows (Update Cache & Delivery Optimization).

### 🖥️ 7. Tối Ưu Giao Diện Explorer (GUI Optimization)
* Tự động sắp xếp biểu tượng Desktop (Auto Arrange).
* Mặc định mở File Explorer trực tiếp vào **This PC** thay vì Home/Quick Access.
* Hiện/Ẩn các tệp tin ẩn, thư mục ẩn và đuôi mở rộng của tệp tin.
* Hỗ trợ tự động khởi chạy lại Windows Explorer (`explorer.exe`) để áp dụng ngay lập tức.
* **Nút Khôi phục mặc định (Reset Defaults):** Trả các cài đặt giao diện về gốc ban đầu của Windows.

### ⚙️ 8. Tối Ưu Hệ Thống & Trạng Thái Dịch Vụ (System Optimization)
* **Kích hoạt Ultimate Performance:** Mở khóa và kích hoạt sơ đồ năng lượng hiệu năng cao nhất của CPU.
* **Bảo mật & Riêng tư:** Vô hiệu hóa Cortana, Bing Search Suggestions, Telemetry (DiagTrack) và ngăn các ứng dụng chạy ngầm (Background Apps).
* **Quản lý Windows Update:** Cho phép Bật/Tắt nhanh dịch vụ cập nhật (`wuauserv` & `UsoSvc`) để tránh cập nhật đột ngột gây chậm máy.
* **Quản lý Windows Defender:** Bật/Tắt chế độ bảo vệ thời gian thực (Real-time Protection) nhanh chóng qua PowerShell.
* **Nút Khôi phục mặc định (Reset Defaults):** Hoàn trả nhanh các tinh chỉnh hệ thống về mặc định.

### 🧼 9. Dọn Dẹp Chuyên Sâu (Deep Cleanup)
* **Dọn dẹp WinSxS:** Quét và loại bỏ sạch sẽ các gói cập nhật Windows cũ đã hết hạn bằng công cụ DISM (`/startcomponentcleanup /resetbase`).
* **Xóa thư mục Windows.old:** Chiếm quyền sở hữu (`takeown`), phân toàn quyền (`icacls`) và xóa sạch hoàn toàn thư mục sao lưu hệ điều hành cũ `C:\Windows.old` để giải phóng hàng chục GB bộ nhớ.

---

## 🛠️ Công Nghệ Sử Dụng

* **Ngôn ngữ:** C# / .NET 10.0 (WPF)
* **Giao diện:** XAML, HSL Tailored Dark Mode, Responsive Layout, Custom Controls.
* **Hệ thống điều khiển:** Win32 Registry API, Process Redirected Output (PowerShell, sc, dism, takeown, icacls).

---

## 🚀 Hướng Dẫn Sử Dụng

### Yêu cầu hệ thống:
* Hệ điều hành: Windows 10 hoặc Windows 11 (64-bit).
* Quyền chạy: **Administrator** (Bắt buộc để chỉnh sửa registry, dịch vụ và tệp tin hệ thống).

### Cách khởi chạy:
1. Tải về phiên bản độc lập đã đóng gói:
   👉 **`BctWinsetup.exe`**
2. Nhấp chuột phải vào tệp tin và chọn **"Run as administrator"** (Chạy dưới quyền quản trị viên).
3. Lựa chọn chức năng bạn cần sử dụng từ thanh Menu bên trái và làm theo hướng dẫn trên màn hình.

---

## 🏗️ Hướng Dẫn Biên Dịch (Dành cho Lập trình viên)

Để tự biên dịch ứng dụng từ mã nguồn, bạn cần cài đặt **.NET 10 SDK** và thực hiện các bước sau:

1. Clone repository về máy tính:
   ```bash
   git clone https://github.com/your-username/Winsetup.git
   cd Winsetup
   ```
2. Biên dịch kiểm tra:
   ```bash
   dotnet build
   ```
3. Đóng gói ứng dụng thành một tệp thực thi duy nhất (`Single-File Executable`, không cần cài đặt môi trường):
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```
   Tệp tin thực thi kèm biểu tượng sẽ được xuất ra tại thư mục:
   `bin\Release\net10.0-windows\win-x64\publish\BctWinsetup.exe`

---

## 📄 Giấy Phép (License)

Dự án được phân phối dưới giấy phép **MIT License**. Bạn có toàn quyền sử dụng, sửa đổi và chia sẻ cho mục đích cá nhân hoặc thương mại.
