# Slash Commands: Tekrar Eden İş Akışlarını Otomatikleştirmek

Her projede tekrar eden görevler vardır. Yeni bir feature branch açmak ve iskelet kodu oluşturmak, bir modülü belirli bir standartta review etmek, release notları hazırlamak, ya da belirli bir pattern'de test dosyası oluşturmak — bunları her seferinde doğal dille tanımlamak hem zaman alır hem de tutarsızlık riski taşır. Slash commands bu tekrarı ortadan kaldırır.

## Temel Yapı

Slash commands `.claude/commands/` dizininde markdown dosyaları olarak tanımlanır. Her dosya adı, komutun adını belirler: `new-feature.md` dosyası `/new-feature` komutunu oluşturur. Dosyanın içeriği, komut çağrıldığında Claude Code'un izleyeceği prosedürü tanımlar.

En temel slash command tek bir talimat metnidir:

```markdown
Yeni bir feature branch oluştur, ismi $ARGUMENTS olsun.
Branch'i oluşturduktan sonra ilgili endpoint için iskelet dosyalarını hazırla.
```

Bu komut `/new-feature user-authentication` şeklinde çağrıldığında `$ARGUMENTS` değişkeni `user-authentication` ile değiştirilir ve Claude Code branch oluşturup iskelet dosyalarını hazırlar.

## $ARGUMENTS Kullanımı

`$ARGUMENTS` slash command'ı esnek kılan temel mekanizmadır. Komut çağrılırken komut adının ardından yazılan her şey `$ARGUMENTS` değişkenine aktarılır. Bu değişken komut içinde birden fazla yerde kullanılabilir, farklı bağlamlara yerleştirilebilir.

Birden fazla argüman için standart bir ayırıcı belirlemek iyi bir pratiktir. Örneğin `/create-endpoint posts GET` çağrısında `$ARGUMENTS` değeri `posts GET` olarak gelir; komut içinde bu değeri bölümlere ayırarak entity adını ve HTTP metodunu ayrı ayrı kullanabilirsiniz.

## Auto-invoke

Bazı komutların her seferinde manuel çağrılması yerine belirli koşullarda otomatik tetiklenmesi istenebilir. Auto-invoke mekanizması bu ihtiyacı karşılar: bir koşul tanımlanır ve o koşul gerçekleştiğinde komut otomatik olarak çalışır.

Bu özellik Skills'e benzer görünür ama önemli bir fark vardır. Skills prosedürel bilgi taşır ve trigger koşulunu kendisi değerlendirir. Auto-invoke ise bir slash command'ı belirli bir dosya değişikliğine, belirli bir hook'a ya da belirli bir Claude Code event'ına bağlar. Skills "ne yapılacağını" tanımlar; slash command "nasıl yapılacağını" tanımlar.

## Subagent Entegrasyonu

Slash commands'ın en güçlü kullanımı subagent'larla entegrasyondur. Bir komut, görevi doğrudan yürütmek yerine ilgili specialist subagent'a devredebilir.

```markdown
# /security-review komutu
$ARGUMENTS dosyasını security-specialist subagent'ına gönder.
Subagent şu kontrolleri yapmalı: authentication, authorization,
input validation, sensitive data exposure.
Bulguları risk seviyesine göre sırala ve özet rapor üret.
```

Bu yaklaşım, slash command'ı bir koordinasyon katmanına dönüştürür. Komut tetiklendiğinde ne yapılacağını bilir; nasıl yapılacağını ise ilgili subagent yönetir.

## Takım Genelinde Standardizasyon

Slash commands'ın en değerli boyutu takım standardizasyonudur. `.claude/commands/` dizini versiyon kontrolüne girer — takımdaki herkes aynı komutları kullanır, aynı prosedürü izler, aynı çıktı formatını alır.

Bu standardizasyon, "Claude Code'a nasıl sordunuzu" tartışmasını ortadan kaldırır. `/create-endpoint`, `/security-review`, `/release-notes` gibi komutlar takımın ortak dili haline gelir. Yeni bir takım üyesi bu komutları kurulum belgelerinde görür ve ilk günden aynı kaliteyle çalışmaya başlar.

Plugins ile birlikte düşünüldüğünde — ki slash commands bir plugin'in temel bileşenlerinden biridir — bu standardizasyon takım sınırlarını aşar ve organizasyon genelinde yayılabilir.
