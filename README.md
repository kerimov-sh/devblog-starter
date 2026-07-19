# DevBlog Starter

DevBlog Starter, **.NET Minimal API** (backend) ve **Angular** (frontend) ile geliştirilmiş, JWT kimlik doğrulama destekli örnek bir blog uygulamasıdır.

## İçindekiler
- [Özellikler](#özellikler)
- [Teknoloji Yığını](#teknoloji-yığını)
- [Proje Yapısı](#proje-yapısı)
- [Gereksinimler](#gereksinimler)
- [Kurulum](#kurulum)
- [Uygulamayı Çalıştırma](#uygulamayı-çalıştırma)
- [API Uç Noktaları](#api-uç-noktaları)
- [Varsayılan Geliştirme Hesabı](#varsayılan-geliştirme-hesabı)
- [Notlar](#notlar)

## Özellikler
- Blog yazılarını listeleme ve detay görüntüleme
- Yazılara yorum ekleme
- JWT tabanlı giriş sistemi
- Yetkili kullanıcı ile yeni yazı oluşturma
- SQLite tabanlı kalıcı veri katmanı
- İlk çalıştırmada otomatik migration ve seed işlemi

## Teknoloji Yığını

### Backend
- .NET 10 Minimal API
- Entity Framework Core
- SQLite
- JWT tabanlı kimlik doğrulama
- OpenAPI

### Frontend
- Angular 22
- TypeScript
- RxJS

## Proje Yapısı

```text
devblog-starter/
├── src/
│   └── DevBlog.Api/        # .NET backend
├── devblog-ui/             # Angular frontend
└── docs/                   # Ek dokümantasyon alanı
```

## Gereksinimler
- .NET SDK 10.0+
- Node.js 20+
- npm 10+

## Kurulum

```bash
git clone <repo-url>
cd devblog-starter
```

### Frontend bağımlılıkları

```bash
cd devblog-ui
npm install
```

## Uygulamayı Çalıştırma

### 1) Backend

```bash
dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj
```

### 2) Frontend

Yeni bir terminal açın:

```bash
cd devblog-ui
npm start
```

## API Uç Noktaları

| Metot | Uç Nokta | Açıklama | Yetki |
|---|---|---|---|
| POST | `/auth/login` | JWT token üretir | Hayır |
| GET | `/posts` | Yazı listesini döner | Hayır |
| GET | `/posts/{slug}` | Yazı detayını döner | Hayır |
| POST | `/posts` | Yeni yazı oluşturur | Evet |
| POST | `/posts/{slug}/comments` | İlgili yazıya yorum ekler | Hayır |

## Varsayılan Geliştirme Hesabı

Seed verisi ile gelen kullanıcı:
- **Kullanıcı adı:** `admin`
- **Şifre:** `admin`

## Notlar
- Veritabanı dosyası varsayılan olarak backend çalışma dizininde `devblog.db` olarak oluşturulur.
- CORS ayarları geliştirme odaklıdır (`AllowAnyOrigin/Method/Header`).
- Bu proje geliştirme amaçlıdır; production ortamı için güvenlik iyileştirmeleri (ör. güçlü parola hashleme, gizli anahtar yönetimi) gereklidir.
