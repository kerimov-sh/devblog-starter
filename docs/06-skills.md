# Skills: Claude Code'a Koşullu Uzmanlık Öğretmek

CLAUDE.md projeye genel bağlam verir — mimari kararlar, naming convention, test stratejisi. Ama bazı bilgiler her zaman geçerli değil, belirli koşullarda devreye girmesi gerekiyor. "Bir migration oluşturulurken şu kontrolleri yap", "bir pull request açılırken şu adımları izle", "güvenlik açısından kritik bir dosya değiştirilirken şu sorguları sor" — bu tür koşullu uzmanlık için Skills var.

Skill, Claude Code'a "ne zaman, ne yapacağını" öğreten bir prosedür modülüdür. Her zaman aktif değildir; tanımladığınız koşul karşılandığında otomatik olarak tetiklenir.

## Frontmatter Yapısı

Bir Skill dosyası YAML frontmatter ile başlar. Bu frontmatter üç kritik alan içerir: `name` (skill'in tanımlayıcı adı), `description` (ne yaptığı ve ne zaman devreye gireceği), ve `triggers` (hangi ifadelerin veya koşulların bu skill'i aktif ettiği).

```yaml
---
name: migration-safety-check
description: >
  EF Core migration oluşturulduğunda tetiklenir. Non-nullable integer
  kolonlar için sessiz sıfır default riskini değerlendirir ve backfill
  stratejisi onayı alır.
triggers:
  - "add migration"
  - "migration oluştur"
  - "dotnet ef migrations add"
---
```

Frontmatter'ın ardından skill'in içeriği gelir: ne kontrol edilmeli, hangi sorular sorulmalı, hangi adımlar izlenmeli. Bu içerik bir prosedür tanımıdır — Claude Code bu prosedürü trigger koşulu karşılandığında uygular.

## Skill ile CLAUDE.md Arasındaki Fark

Bu ayrım pratikte sık karıştırılır. CLAUDE.md her oturumda, her görevde aktiftir — genel kurallar ve bağlam için. Skill ise yalnızca belirli bir koşul tetiklendiğinde aktif olur — prosedürel ve reaktif bilgi için.

Bir kural her görevde geçerliyse CLAUDE.md'ye yazın. Yalnızca belirli bir eylem gerçekleştiğinde geçerliyse Skill yazın. "Her zaman repository pattern kullan" CLAUDE.md'ye gider. "Migration eklenirken şu kontrolleri yap" Skill'e gider.

## Pratik Örnekler

Code review skill'i, bir pull request veya kod inceleme talebi algılandığında devreye girer. İncelenmesi gereken boyutları — güvenlik, performans, test coverage, naming convention uyumu — sırayla değerlendirir ve bulguları yapılandırılmış bir formatta sunar.

Security check skill'i, güvenlik açısından hassas alanlara dokunulduğunda — authentication kodu, veri validasyonu, dış API entegrasyonu — otomatik olarak tetiklenir. OWASP Top 10'dan ilgili maddeleri kontrol eder, potansiyel açıkları işaretler.

Migration safety skill'i, veritabanı şemasını değiştiren işlemler sırasında aktif olur. Özellikle non-nullable kolon eklemek gibi production'da sessiz veri bozulmasına yol açabilecek değişiklikler için backfill stratejisi onayı ister.

## Skill Yazarken Dikkat Edilecekler

İyi bir skill dar kapsamlıdır. Tek bir sorumluluk alanını kapsar, birden fazla farklı konuyu tek bir skill'e sıkıştırmaz. Geniş kapsamlı skill'ler hem bakımı zorlaştırır hem de tetikleme koşullarını muğlaklaştırır.

Trigger tanımları spesifik olmalıdır. Çok geniş trigger'lar skill'in yanlış anlarda devreye girmesine neden olur; çok dar trigger'lar ise gerektiğinde tetiklenemez. Trigger'ları, o koşulda gerçekten ne söylendiğini düşünerek yazın.

Son olarak: skill'ler versiyonlanabilir. Zamanla geliştirilen, takım geri bildirimiyle olgunlaştırılan canlı belgelerdir. İlk versiyonun mükemmel olmasını beklemeyin — çalışan bir skill yazın, sonra gerçek kullanımdan öğrendiklerinizle iyileştirin.
