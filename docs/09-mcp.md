# MCP Nedir? Claude Code'u Dış Sistemlere Bağlamak

Claude Code varsayılan olarak kendi araç setiyle çalışır: dosya okuma, yazma, düzenleme, bash komutu çalıştırma. Bu araçlar çoğu geliştirme görevi için yeterlidir. Ama gerçek projeler izole yaşamaz — veritabanları, harici API'ler, proje yönetim araçları, deployment sistemleri, loglama platformları. Claude Code'u bu sistemlere bağlamak için Model Context Protocol (MCP) var.

## USB-C Benzetmesi

MCP'yi anlamanın en hızlı yolu USB-C standardını düşünmektir. USB-C öncesinde her üretici kendi konnektörünü icat ediyordu — telefon şarj kablosu, laptop kablosu, monitör kablosu hepsi farklıydı. USB-C bu kaosun yerine ortak bir protokol koydu: aynı konnektör, farklı sistemler.

MCP de aynı şeyi yapıyor. Claude Code'u bir veritabanına, bir API'ye ya da bir dosya sistemine bağlamak için her sistem için özel bir entegrasyon yazmak yerine, MCP protokolünü konuşan bir server yazıyorsunuz. Claude Code MCP'yi anlıyor — gerisi otomatik.

## Üç Temel Primitiv

MCP üç temel kavram üzerine inşa edilmiştir.

**Tools** eylem primitividir. Claude Code'un çağırabileceği fonksiyonlardır: bir veritabanından kayıt çekmek, bir API endpoint'ine istek atmak, bir dosyayı işlemek. Her tool bir input schema'sı ve bir handler fonksiyonu tanımlar. Kullanıcı "son 10 blog postunu getir" dediğinde Claude Code `get_recent_posts` tool'unu çağırır, tool veritabanına bağlanır, sonucu döner.

**Resources** veri primitividir. Dinamik olarak sunulan içeriklerdir — bir veritabanı tablosunun anlık durumu, bir API'nin dökümanı, bir konfigürasyon dosyasının içeriği. Tools aksiyonu temsil eder; Resources bilgiyi temsil eder. Claude Code bir resource'u context'ine ekleyebilir ve bu bilgiyi sonraki kararlarında kullanabilir.

**Prompts** şablon primitividir. Sık kullanılan talimat kalıplarını yeniden kullanılabilir hale getirir. "Bu endpoint için standart hata yönetimi ekle" gibi bir işlem, Prompts aracılığıyla parametrize edilmiş bir şablona dönüştürülebilir.

## MCP Tool Search: Lazy Loading

Bir MCP server çok sayıda tool tanımlayabilir. Tüm tool'ların Claude Code başlarken context'e yüklenmesi, büyük server'larda ciddi bir context israfına yol açar. MCP Tool Search bu problemi çözer.

Tool Search, araçları ihtiyaç anında arar. Claude Code bir göreve başladığında tüm MCP araçlarını değil, yalnızca ilgili olanları getirir. Bu lazy loading yaklaşımı context'i korur ve özellikle büyük MCP server'larda belirgin bir verimlilik sağlar. Telefon rehberini ezbere bilmek yerine, ihtiyaç anında aramak — mantık aynı.

## Hazır Server'lar ve Özel Geliştirme

MCP ekosistemi iki katmandan oluşur.

Hazır server'lar — GitHub, Jira, Slack, dosya sistemi, veritabanı connectors gibi — kurulum ve bağlantı konfigürasyonu ile hemen kullanılabilir. Bu server'lar yaygın geliştirme araçlarını Claude Code'a bağlar ve çoğu takım ihtiyacını karşılar.

Özel MCP server geliştirme ise şirket içi sistemlere erişim için gereklidir. Şirketin kendi veritabanı, özel API'ler, iç araçlar — bunların hiçbiri hazır bir MCP server'ı ile gelmiyor. Python'da FastMCP ya da TypeScript'te MCP SDK kullanarak özel server yazabilirsiniz. Bir MCP server'ın çekirdeği basittir: tool tanımları, her tool için bir handler, ve bir transport katmanı.

## Claude Code'a Bağlama

Bir MCP server'ı Claude Code'a bağlamak `settings.json` üzerinden yapılır. Server'ın adresi, kimlik doğrulama bilgileri ve hangi tool'ların erişime açık olacağı bu konfigürasyonda tanımlanır. Bağlantı kurulduktan sonra Claude Code, o server'ın sunduğu tool'ları kendi araç setiyle aynı şekilde kullanır — kullanıcı perspektifinden bir fark yoktur.

Bu mimarinin sonucu şudur: Claude Code'un yetenekleri artık sabit değildir. Yeni bir MCP server eklediğinizde Claude Code yeni bir sistemi anlayabilir, yeni bir kaynaktan veri çekebilir, yeni bir platforma yazabilir hale gelir. Genişletilebilirlik, MCP'nin temel tasarım hedefidir.
