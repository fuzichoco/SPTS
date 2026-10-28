# Quy ước đặt tên

| Loại | Quy ước | Ví dụ |
| --- | --- | --- |
| Entity | Danh từ số ít, PascalCase | `User`, `Grade`, `TrainingProgram` |
| Bảng | Số nhiều (theo DbSet hoặc `[Table]`) | `Users`, `Programs`, `Staff` |
| Khóa chính | `<Entity>Id` | `UserId`, `GradeId` |
| Mã nghiệp vụ | `<Entity>Code`, `varchar` | `StudentCode`, `CourseCode` |
| ViewModel | `<Tên>ViewModel`; form thêm/sửa `<Tên>EditModel` | `UserFilterViewModel`, `UserEditModel` |
| Controller | Số nhiều + `Controller` | `UsersController`, `GradesController` |
| Action trả JSON cho AJAX | Hậu tố `Ajax` hoặc tên động từ | `List`, `CreateAjax`, `DeleteAjax` |
| Hành động nhật ký | IN HOA, gạch dưới | `LOGIN_SUCCESS`, `CREATE`, `UPDATE_GRADE` |
| File JS | `<trang>.js`, chữ thường, gạch nối | `users-ajax.js` |
| Nhánh git | `feature/<module>-<việc>` | `feature/grades-nhap-diem` |

Tránh đặt tên trùng với lớp hệ thống: entity CTĐT tên `TrainingProgram` (không phải `Program`, trùng với lớp sinh từ `Program.cs`).