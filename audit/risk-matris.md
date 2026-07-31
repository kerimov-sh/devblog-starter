# Risk Matrisi

## Ozet

| Severity | Bulgu Sayisi |
|---|---|
| high | 1 |
| medium | 4 |
| low | 1 |

## Detay

### HIGH

| Finding | File | Line | Remediation |
|---|---|---|---|
| 'jwtSecret' degiskeni Program.cs icinde hard-coded bir secret string ile atanmis (kaynak kodla birlikte commitleniyor). | src\DevBlog.Api\Program.cs | 47 | Degeri kaynak koddan cikarip appsettings.json disinda tutun: development icin 'dotnet user-secrets set', production icin ortam degiskeni veya bir secret manager (ör. Azure Key Vault) kullanip IConfiguration uzerinden okuyun. |

### MEDIUM

| Finding | File | Line | Remediation |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.Sqlite guncel degil: mevcut surum 10.0.0, guncel kararli surum 10.0.10. | src\DevBlog.Api\DevBlog.Api.csproj | 13 | - |
| CORS politikasi 'AllowAnyMethod()' kullaniyor; bu asiri gevsek bir CORS ayaridir. | src\DevBlog.Api\Program.cs | 42 | Sadece kullanilan HTTP metotlarini WithMethods("GET", "POST", ...) ile belirtin. |
| CORS politikasi 'AllowAnyHeader()' kullaniyor; bu asiri gevsek bir CORS ayaridir. | src\DevBlog.Api\Program.cs | 43 | Sadece gereken header'lari WithHeaders(...) ile belirtin. |
| index.html icinde <meta name="description"> etiketi bulunamadi. | devblog-ui\src\index.html | - | Sayfayi 50-160 karakter arasinda ozetleyen bir <meta name="description" content="..."> etiketi ekleyin. |

### LOW

| Finding | File | Line | Remediation |
|---|---|---|---|
| <title> metni SEO icin onerilen uzunlukta degil ('DevBlog', 7 karakter). | devblog-ui\src\index.html | 5 | Title metnini yaklasik 50-60 karakter arasinda, marka + sayfa amaci iceren sekilde genisletin. |
