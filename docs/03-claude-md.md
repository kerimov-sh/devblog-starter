# CLAUDE.md Stratejileri: Proje Belleğini Doğru Kurmak

Claude Code her oturumda sıfırdan başlar. Dün aldığınız mimari kararı, geçen hafta belirlediğiniz naming convention'ı, takımın test stratejisini — bunların hiçbirini hatırlamaz. CLAUDE.md bu problemi çözmek için var: Claude Code'un her oturumda otomatik okuduğu, projeye özel bir bağlam belgesi.

Ama CLAUDE.md'yi "Claude Code'a not bırakma alanı" olarak tanımlamak eksik kalır. Daha doğru bir tanım şudur: CLAUDE.md, bir takım arkadaşına verilen oryantasyon belgesidir. İlk gün işe başlayan birine "bizim ekipte şöyle çalışıyoruz" diye anlattığınız her şey — kodlama standartları, mimari kararlar, test beklentileri, kaçınılması gereken pattern'ler — CLAUDE.md'nin içeriğidir.

## Üç Seviye, Üç Kapsam

CLAUDE.md tek bir dosya değil, bir hiyerarşidir. User-level dosya `~/.claude/CLAUDE.md` konumunda durur ve kişisel tercihlerinizi taşır: her projede geçerli olmasını istediğiniz dil tercihleri, yanıt formatı beklentileri, kişisel iş akışı notları. Bu dosya sizin için çalışır, takımınız için değil.

Project-level dosya repo kökündedir ve takım genelinde geçerlidir. Versiyon kontrolüne girer, herkesin Claude Code'u aynı kurallara göre çalışır. Mimari kararlar, naming convention, hangi kütüphanelerin tercih edildiği, hangi pattern'lerden kaçınıldığı buraya yazılır.

Local dosya `.claude/CLAUDE.local.md` konumundadır ve `.gitignore`'a eklenir. Kişisel geçici notlar, henüz takımla paylaşmaya hazır olmadığınız denemeler için uygundur.

## Ne Yazılmalı, Ne Yazılmamalı?

CLAUDE.md'nin en yaygın hatası, zaten koddan anlaşılabilecek şeyleri tekrar etmektir. "Bu proje .NET kullanıyor" yazmak gereksizdir — Claude Code repo'yu taradığında bunu zaten görür. CLAUDE.md'ye yazılacak şeyler, taramadan anlaşılamayanlar olmalıdır.

Mimari kararlar bu kategorinin en önemli örneğidir. "Repository pattern kullanıyoruz, endpoint'lerden doğrudan DbContext erişimi yok" bir tarama talimatı değil, bir tasarım kararıdır. Claude Code kodu okuyarak mevcut yapıyı anlayabilir, ama bu yapının kasıtlı mı yoksa teknik borç mu olduğunu anlayamaz. CLAUDE.md bu ayrımı netleştirir.

Test stratejisi de aynı kategoridedir. "Unit test için xUnit, integration test için TestContainers kullanıyoruz; mock yerine in-memory veritabanı tercih ediyoruz" bilgisi, Claude Code'un test yazarken doğru araçları seçmesini sağlar.

## Hedef Mimari vs Mevcut Durum

CLAUDE.md'nin az bilinen ama güçlü bir kullanımı, hedef mimariyi belgelemektir. Mevcut kod her zaman hedefle örtüşmez — teknik borç birikir, eski modüller refactor edilmeyi bekler. CLAUDE.md'ye "hedef mimari şu, ama şu dosyalar henüz bu hedefe ulaşmadı" yazmak, Claude Code'un yeni kod yazarken hedef yönünde ilerlemesini sağlar.

Bu ayrım kritiktir: `/init` komutu mevcut durumu tarar ve bir taslak CLAUDE.md üretir. Bu taslak, kodun şu anki halini yansıtır. Hedef mimariyi, teknik borç notlarını ve "şu pattern'den kaçın" uyarılarını yalnızca siz ekleyebilirsiniz. Tarama bulabilir, insan yargısı hedefi yazabilir.

## Bakım

CLAUDE.md statik bir belge değildir. Mimari kararlar değişir, yeni kütüphaneler benimsenir, test stratejisi evrilir. Bu değişikliklerin koda yansıtıldığı gibi CLAUDE.md'ye de yansıtılması gerekir. Bir takım arkadaşına verilen oryantasyon belgesi güncellenmezse yanlış yönlendirir; CLAUDE.md de öyle.

Pratik bir öneri: her sprint sonunda CLAUDE.md'yi gözden geçirin. O sprint'te alınan mimari kararlar, benimsenen yeni pattern'ler veya kaldırılan eski yaklaşımlar varsa belgeyi güncelleyin. Bu alışkanlık, CLAUDE.md'nin zamanla "eski notlar yığını" haline gelmesini önler.
