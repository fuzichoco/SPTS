# Làm việc với GitHub

## Lần đầu

1. Chấp nhận lời mời vào repo `fuzichoco/SPTS` (repo Private).
2. Clone và chạy theo [hướng dẫn môi trường](setup.md).

## Mỗi lần làm việc

```powershell
git checkout main
git pull origin main
git checkout -b feature/<module>-<viec>
# ... sửa code, dotnet build, chạy thử ...
git add .
git commit -m "feat(<module>): <mô tả>"
git push -u origin feature/<module>-<viec>
```

Sau đó mở Pull Request vào `main` trên GitHub theo [quy trình](../CONTRIBUTING.md).

## Xung đột migration

Hai người cùng thêm migration sẽ xung đột `ApplicationDbContextModelSnapshot.cs`. Cách xử lý: người sau xóa migration của mình (`dotnet ef migrations remove`), pull `main`, rồi tạo lại migration.