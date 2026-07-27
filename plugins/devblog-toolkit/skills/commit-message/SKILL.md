---
name: commit-message
description: DevBlog Starter reposunda commit mesajı yazılacağı her durumda kullan — kullanıcı "commit at", "commit mesajı yaz", "bunu commitle", "değişiklikleri kaydet" dediğinde ya da git commit çağrılacağı her an bu skill'i çalıştır. Standart kısa/özet commit mesajı yerine; değişikliğin NE olduğunu, NEDEN yapıldığını ve hangi SONUCU/etkiyi doğurduğunu açıkça kuran, açıklayıcı ve detaylı bir commit mesajı üretir.
---

# Commit Message (DevBlog Starter)

Bu repo için commit mesajları standart "kısa özet" tarzının aksine **açıklayıcı, detaylı ve neden-sonuç ilişkisi kuran** bir formatta yazılır. Amaç, altı ay sonra `git log`'a bakan birinin (ya da bir sonraki Claude oturumunun) sadece "ne değişti"yi değil, "bu değişikliğe neden ihtiyaç duyuldu" ve "bunun sonucunda ne elde edildi/hangi risk ortadan kalktı" sorularını da cevaplayabilmesidir.

Bu skill tetiklendiğinde, `git commit` için varsayılan "1-2 cümlelik, why-odaklı" kısa mesaj kuralının yerine geçer — burada anlatılan detaylı format esas alınır.

## Neden bu format?

Bir commit mesajı sadece diff'in düz-yazıya çevrilmiş hali olursa (`"X.cs dosyasını güncelledi"`), diff'i zaten okuyabilen biri için hiçbir ek bilgi taşımaz. Değerli olan, diff'ten çıkarılamayan şeydir: hangi sorunun/ihtiyacın bu değişikliği tetiklediği, neden bu yaklaşımın seçildiği (varsa reddedilen alternatifler), ve bu değişikliğin sistemin geri kalanında ne değiştirdiği. Bu repo bilinçli olarak bir eğitim/starter projesi olduğu için (bkz. CLAUDE.md "Bilinen Borçlar"), commit mesajlarının hangi borcun kasıtlı bırakıldığını ya da hangi borcun bu commit'le kapatıldığını netleştirmesi de ayrıca değerlidir.

## Mesajı yazmadan önce topla

Mesajı yazmadan önce şunları çıkar:

1. **Diff'in kendisi** — `git diff --staged` (veya staged yoksa `git diff`) ve `git status` ile hangi dosyaların, hangi katmanların (Endpoint/Service/Repository/Model, ya da Angular tarafında component/service) etkilendiğini gör.
2. **Motivasyon (neden)** — konuşma geçmişinde kullanıcının neden bu değişikliği istediği geçtiyse (bir bug, bir eksik özellik, CLAUDE.md'deki bir "Bilinen Borç"un kapatılması, bir refactor kararı) onu kullan. Geçmiyorsa diff'ten ve dosya/fonksiyon isimlerinden makul bir çıkarım yap; emin değilsen kullanıcıya tek satırlık bir soru sorup teyit et — motivasyonu uydurma.
3. **Sonuç/etki** — bu değişiklik neyi mümkün kıldı, hangi hatayı ortadan kaldırdı, hangi endpoint/akış artık farklı davranıyor, performans/güvenlik/mimari açıdan ne değişti. Bu repoda özellikle şunlara dikkat et: hedef mimariye (Endpoint→Service→Repository ayrımı, bkz. CLAUDE.md "Mimari Kararlar") bir adım daha yaklaşıldı mı, yoksa bilinçli bir borç mu bırakıldı; naming convention'lara uyum sağlandı mı (`*Response` record'ları, `<Feature>Endpoint`, `<Domain>Service` vb.).

## Mesaj formatı

Konvansiyonel commit prefix'i (`feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`) ile başlayan kısa bir başlık satırı, ardından boş satır, ardından üç bölümlü gövde:

```
<tip>: <kısa başlık, emir kipi, ~50-70 karakter>

Ne değişti:
<Değişikliğin somut içeriği — hangi dosya/katman/endpoint/component, hangi davranış eklendi/kaldırıldı/değişti. Diff'i özetler ama satır satır tekrarlamaz.>

Neden:
<Bu değişikliğe neyin yol açtığı — çözülen sorun, karşılanan ihtiyaç, kapatılan "Bilinen Borç", ya da tetikleyici kullanıcı isteği. Mümkünse "X olduğu için Y yapıldı" şeklinde nedensellik kur.>

Sonuç:
<Bu değişiklikten sonra sistemin ne yaptığı/yapamadığı, hangi riskin azaldığı veya hangi yeni riskin/borcun bilerek açıldığı (ör. "servis katmanına taşındı ama repository ayrımı henüz yapılmadı"). Varsa etkilenen diğer alanlar (frontend/backend, migration gereksinimi, breaking change).>
```

Başlık satırı bu reponun git log'undaki mevcut üslupla uyumlu olmalı (bkz. örnekler: `Refactor backend to Clean Architecture: introduce Service and Repository layers`, `Add pagination to posts list and add planning docs`) — yani kısa, emir kipi, İngilizce. Gövde (Ne değişti / Neden / Sonuç) Türkçe veya İngilizce olabilir; kullanıcının o oturumda konuştuğu dille tutarlı ol.

Birden fazla bağımsız değişiklik tek commit'e karışmışsa (ör. hem bir bug fix hem alakasız bir refactor), bunu kullanıcıya belirt ve isterse ayrı commit'lere bölmeyi öner — tek mesaj içinde birbiriyle ilgisiz "Neden"leri birleştirip zorla tek bir hikaye anlatma.

## Örnek

**Girdi (özet):** `PostsEndpoint` içindeki slug üretim mantığı bir `PostService`'e taşındı; `AppDbContext` enjeksiyonu endpoint'ten kaldırıldı.

**Çıktı:**

```
refactor: extract slug generation into PostService

Ne değişti:
PostsEndpoint.Map içindeki slug üretim ve post oluşturma mantığı yeni bir
PostService.CreatePostAsync metoduna taşındı. Endpoint artık AppDbContext'i
doğrudan enjekte etmiyor, sadece PostService'i DI ile çağırıyor.

Neden:
CLAUDE.md'de "Bilinen Borçlar" altında işaretlenen PostsEndpoint'in
AppDbContext'e doğrudan bağımlılığı, hedef Endpoint→Service→Repository
mimarisine uyum sağlamak amacıyla kapatıldı; ayrıca bu bağımlılık slug
üretim mantığını mock'lanabilir bir sınır olmadan test edilemez hale
getiriyordu.

Sonuç:
POST /posts artık AppDbContext'e bağımlı değil; slug üretim mantığı artık
PostService üzerinden unit test edilebilir durumda. Repository katmanı
henüz eklenmedi (PostService hâlâ AppDbContext'i doğrudan kullanıyor),
bu bilinçli olarak sonraki bir adıma bırakıldı.
```

## Nasıl uygulanır

Kullanıcı "commit at" dediğinde: yukarıdaki bilgi toplama adımlarını uygula, mesajı bu formatta hazırla, sonra normal git commit akışını (staging, `git commit -m` ile heredoc, hook/pre-commit hatalarında düzeltip yeni commit açma vb.) izle. Mesajı commit etmeden önce kullanıcıya göstermek zorunlu değildir, ama "Neden" bölümünde tahmine dayalı bir çıkarım yaptıysan (kullanıcı motivasyonu açıkça belirtmediyse) bunu commit etmeden önce tek cümlede teyit ettir.
