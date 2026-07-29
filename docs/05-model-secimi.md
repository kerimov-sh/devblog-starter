# Model Seçimi: Haiku, Sonnet ve Opus Arasındaki Fark

Claude Code tek bir modelle çalışmaz. Arka planda farklı karmaşıklık seviyelerine hitap eden birden fazla model bulunur ve doğru modeli seçmek, hem kaliteyi hem de maliyeti doğrudan etkiler. Bu seçimi bilinçsizce bırakmak — her görevi aynı modelle çalıştırmak — ya gereksiz maliyet ya da gereksiz kalite kaybı anlamına gelir.

## Model Ailesi

Temmuz 2026 itibarıyla Claude Code'da kullanılan model ailesi dört üyeden oluşur.

Haiku 4.5 hız ve maliyet açısından en verimli modeldir. Basit, iyi tanımlanmış görevlerde — tek bir dosyada küçük bir değişiklik, boilerplate üretme, basit bir soru yanıtlama — Haiku yeterlidir ve diğer modellerden çok daha hızlı yanıt verir. Karmaşık akıl yürütme gerektirmeyen rutin işlerde Haiku'yu tercih etmek hem hızı hem ekonomiyi optimize eder.

Sonnet 5 denge modelidir. Çoğu geliştirme görevi için — yeni bir endpoint yazmak, bir bug fix uygulamak, orta karmaşıklıkta bir refactor yapmak — Sonnet yeterli derinliği ve hızı bir arada sunar. Özellikle belirtilmediğinde Claude Code'un varsayılan olarak yöneldiği model bu seviyededir.

Opus 4.8 en güçlü modeldir. Karmaşık mimari kararlar, büyük ölçekli refactoring, birden fazla sistemin entegrasyonu, ya da derin akıl yürütme gerektiren güvenlik analizi gibi görevlerde Opus'un kapasitesi fark yaratır. Ama bu güç bir maliyetle gelir — hem token başına ücret hem de yanıt süresi diğer modellerden yüksektir.

Fable 5 ise kod üretimi için özel olarak optimize edilmiş modeldir. Ağır kod üretimi görevlerinde — büyük bir modülü sıfırdan yazmak, kapsamlı test süiti oluşturmak — Fable'ın performansı diğer modellerden ayrışır.

## Effort Parametresi

Model seçiminin yanında effort parametresi de önemlidir. Bu parametre Claude Code'un bir göreve ne kadar "düşünme bütçesi" ayıracağını belirler. Yüksek effort, daha kapsamlı keşif ve daha dikkatli planlama anlamına gelir; düşük effort, daha hızlı ama daha az derinlemesine bir yaklaşım.

Effort ve model birbirinden bağımsız ayarlanabilir. Haiku ile yüksek effort, Opus ile düşük effort kombinasyonları anlamsız görünebilir ama bazı senaryolarda mantıklıdır. Örneğin basit ama hata toleransı düşük bir görevde — kritik bir konfigürasyon dosyasında küçük bir değişiklik — Haiku ile yüksek effort, hızı korurken dikkatli davranmayı sağlar.

## Pratik Karar Kuralları

Görevin karmaşıklığına ve sonucun geri alınabilirliğine göre model seçin. Basit ve geri alınabilir görevler için Haiku yeterlidir. Orta karmaşıklıkta görevler için Sonnet varsayılan seçimdir. Karmaşık, geri alınması maliyetli ya da derin akıl yürütme gerektiren görevler için Opus veya Fable değerlendirin.

Maliyet açısından somut bir çerçeve: Haiku ile çalışırken daha fazla iterasyona izin verebilirsiniz çünkü her iterasyonun maliyeti düşüktür. Opus ile çalışırken görevi daha iyi tanımlamak, daha az ama daha kaliteli iterasyona yönlendirmek, toplam maliyeti kontrol altında tutar.

## settings.json ile Yapılandırma

Model ve effort tercihleri `settings.json` üzerinden kalıcı olarak yapılandırılabilir. Proje bazında farklı varsayılanlar belirlemek mümkündür — güvenlik açısından kritik bir repoda varsayılan olarak Opus seçilebilirken, prototip geliştirme reposunda Haiku tercih edilebilir. Bu yapılandırma, her görevde manuel seçim yapmak zorunda kalmadan tutarlı bir strateji uygulamayı sağlar.
