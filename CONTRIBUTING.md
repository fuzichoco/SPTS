# Quy trình làm việc nhóm

## Nhánh

- `main`: nhánh tích hợp, luôn build được. Không push thẳng lên `main`.
- Mỗi việc một nhánh: `feature/<module>-<việc>`, `fix/<module>-<lỗi>`, `docs/<chủ-đề>`.
  Ví dụ: `feature/grades-nhap-diem`, `fix/users-phan-trang`.

## Commit

- Một commit một ý: `feat(grades): thêm trang nhập điểm`, `fix(auth): khóa sau 5 lần sai`.
- Không commit `bin/`, `obj/`, `.vs/`, chuỗi kết nối riêng.

## Pull Request

1. Cập nhật nhánh từ `main` trước khi mở PR: `git pull origin main`.
2. Chạy `dotnet build` và tự kiểm tra trang mình sửa.
3. Mở PR vào `main`, điền đủ mẫu PR, gắn người phụ trách module liên quan review.
4. Có migration mới: ghi rõ trong PR. Mỗi lúc chỉ một người thêm migration để tránh xung đột `ApplicationDbContextModelSnapshot.cs`.
5. Ít nhất 1 người review đồng ý mới merge; dùng **Squash and merge**.

## Sửa phần dùng chung

`Models/`, `Data/ApplicationDbContext.cs`, `Program.cs`, `Views/Shared/_Layout.cshtml` ảnh hưởng cả nhóm. Sửa các file này phải báo trước trong nhóm và nêu rõ trong PR. Xem [điểm giao nhau](docs/team-assignments.md).