---
name: ef-migration
description: DevBlog Starter reposunda herhangi bir EF Core migration ihtiyacı doğduğunda kullan — kullanıcı "migration ekle", "migration oluştur", "dotnet ef migrations add", "veritabanını güncelle", "database update" dediğinde, ya da bir Model (Post/Comment/User) veya AppDbContext.OnModelCreating değişikliği migration gerektirdiğinde bu skill'i çalıştır. Migration önerilmeden önce non-nullable sütun eklemelerinde eski kayıtlara uygulanacak default değeri açıkça belirtip onay alır; veri kaybı riski varsa geliştiriciyi uyarır ve onay almadan asla veritabanını güncellemez.
---

# EF Core Migration Güvenlik Kontrolü (DevBlog Starter)

Bu repoda `Program.cs` başlangıçta `db.Database.Migrate()` çalıştırıyor (bkz. CLAUDE.md "Mevcut Mimari") — yani **API'yi çalıştırmak bile bekleyen migration'ları otomatik olarak veritabanına uygular**. Bu yüzden "sadece migration dosyası oluşturmak" ile "veritabanını gerçekten güncellemek" arasındaki sınır bu repoda özellikle incedir: `dotnet run` komutu da fiilen bir `database update`'tir. Bu skill, migration dosyası oluşturmadan önce ve veritabanına her ne şekilde uygulanacaksa uygulanmadan önce devreye girer.

Amaç: geliştiricinin, elinde SQLite'ta (`devblog.db`) zaten oturmuş veri varken, bunun farkında olmadan sessizce değiştirilmesini veya kaybedilmesini önlemek.

## Migration önerilmeden önce yapılacak analiz

Model değişikliğini (`src/DevBlog.Api/Models/*.cs`, `AppDbContext.OnModelCreating`) inceleyip migration'ı `dotnet ef migrations add` ile taslak olarak oluşturmadan **önce** şunları kontrol et:

### 1. Non-nullable yeni sütun kontrolü

Eğer değişiklik bir entity'ye **non-nullable yeni bir property/sütun** ekliyorsa:

1. İlgili tabloda halihazırda kayıt olup olmadığını kontrol et (örn. bu repo için `src/DevBlog.Api/devblog.db` dosyasını sorgula — Python'un `sqlite3` modülü veya benzeri bir yolla `SELECT COUNT(*) FROM <Tablo>`). Kayıt yoksa bu adım risksizdir, devam et.
2. Kayıt **varsa**, EF Core'un bu sütunu var olan satırlara doldurmak için hangi değeri kullanacağını netleştir — ya `.HasDefaultValue(...)` ile Fluent API'de açıkça verilmiş bir değer, ya da migration'ın `AddColumn` çağrısında üretilecek `defaultValue`/`defaultValueSql` parametresi. Eğer kod hiçbir default belirtmiyorsa (sadece `string.Empty`, `0`, `false` gibi C# tipi varsayılanına düşüyorsa) bunu da olduğu gibi söyle — sessizce "otomatik hallolur" deme.
3. Geliştiriciye şu şekilde **açıkça** sor ve onay almadan migration dosyasını üretme:
   > "`<Tablo>` tablosunda şu an `<N>` kayıt var. Eklenecek `<Sütun>` (non-nullable, tip `<Tip>`) için mevcut kayıtlara `<Değer>` değeri atanacak. Bu değeri onaylıyor musun?"
4. Onay gelmeden `dotnet ef migrations add` çalıştırma; geliştirici farklı bir default isterse (örn. nullable yapmak, `HasDefaultValueSql` ile hesaplanmış bir değer, ya da ayrı bir backfill script'i) onu Model/Fluent API'ye yansıtıp yeniden bu kontrolden geç.

### 2. Veri kaybı riski taraması

Aşağıdaki durumların herhangi biri varsa, migration'ı önermeden **önce açık bir uyarı** ver — uyarıda hangi veri kaybedilebileceğini somut olarak yaz (hangi tablo/sütun, kaç kayıt etkileniyor):

- Bir sütunun **kaldırılması** (`DropColumn`) — o sütundaki tüm veri gider.
- Bir tablonun **kaldırılması** (`DropTable`).
- Bir sütunun **tipinin daraltılması** ya da **uzunluğunun kısaltılması** (ör. `string` `MaxLength(500)` → `MaxLength(100)`, `decimal` hassasiyetinin düşürülmesi) — mevcut değerler kesilebilir/kırpılabilir.
- EF Core'un bir **rename**'i "eski sütunu sil + yeni sütun ekle" olarak algılaması (property adı değiştiğinde migration genelde bunu `DropColumn` + `AddColumn` çifti olarak üretir, `RenameColumn` değil) — bu durumda veri taşınmaz, kaybolur; migration dosyası oluştuktan sonra `Up`/`Down` metodlarını mutlaka gözden geçir ve gerekirse elle `RenameColumn`'a çevir ya da bir veri taşıma adımı ekle.
- Bir FK/ilişkinin `OnDelete(DeleteBehavior.Cascade)` gibi davranışla bağlı olduğu ve migration'ın var olan satırları cascade silmeye yol açabileceği durumlar.

### 3. Onay olmadan veritabanını asla güncelleme

Migration dosyasını (`dotnet ef migrations add <Name>`) oluşturmak — sadece taslak ürettiği ve veritabanına dokunmadığı için — 1 ve 2'deki onaylar netleşmeden de yapılabilir; ancak şu adımlardan **hiçbiri geliştiricinin açık onayı olmadan çalıştırılmaz**, çünkü bu repoda hepsi veritabanını fiilen değiştirir:

- `dotnet ef database update`
- `dotnet run` / `dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj` (başlangıçta otomatik `Migrate()` çalıştırdığı için)
- Migration dosyasının içindeki `Up()` metodunu üretilmiş SQL olarak elle çalıştırmak

Onay istenirken 1. ve 2. adımdaki bulgular (default değer + varsa veri kaybı riski) tek bir mesajda özetlenip **birlikte** sorulur; geliştirici "evet"/"onaylıyorum" demeden bu adımlardan hiçbiri çalıştırılmaz. Geliştirici reddederse ya da farklı bir yaklaşım isterse, migration dosyasını gerekirse `dotnet ef migrations remove` ile geri al ve model/fluent API üzerinde yeniden düzenleyip baştan geç.

## Uygulama akışı özeti

1. Model/`OnModelCreating` diff'ini incele.
2. Non-nullable yeni sütun var mı → varsa tablo kayıt sayısını kontrol et → varsa default değeri belirle.
3. Veri kaybı riski taşıyan bir operasyon var mı → varsa somut şekilde listele.
4. `dotnet ef migrations add <Name>` ile taslağı oluştur, üretilen `Up()`/`Down()` içeriğini oku ve 2-3'teki bulguları teyit et (bazen gerçek risk migration üretilene kadar netleşmez).
5. Bulguları (default değer + veri kaybı riski, varsa) tek mesajda geliştiriciye sun, açık onay al.
6. Onay geldiyse `dotnet ef database update` ya da `dotnet run` ile uygula; onay yoksa uygulama adımını atla ve gerekirse migration'ı geri al.
