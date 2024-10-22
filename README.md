1. Tạo project webapi

dotnet new webapi -n ten_project
(webapp, mvc, webapi)

2. Cài đặt thư viện cho Entity Framework (data model)

dotnet add package Pomele.EtityFrameworkCore.MySql (Pomelo MySql Provider)
dotnet add package Microsoft.EntityFrameworkCore.Tools

3. Đồng bộ hóa với DB (Tạo Migration)

dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate

4. Cập nhật vào DB

dotnet ef database update