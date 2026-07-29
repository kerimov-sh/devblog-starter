# Plan Mode: Önce Mimar, Sonra İşçi

Karmaşık bir görevi Claude Code'a verdiğinizde iki şey olabilir: doğrudan koda geçer ve bir yönde ilerlemeye başlar, ya da önce durur, düşünür ve size bir plan sunar. İkinci senaryo Plan Mode'un devreye girdiği andır.

Plan Mode, Claude Code'un eyleme geçmeden önce planlama yapmasını zorunlu kılan bir çalışma modudur. Görevi aldığında kodu değiştirmeye başlamak yerine önce analiz eder, olası yaklaşımları değerlendirir ve size bir plan sunar. Siz planı onaylamadan hiçbir değişiklik yapılmaz.

## Neden Plan Mode Gerekli?

Doğrudan eylem modunda Claude Code hızlıdır — ama hız her zaman avantaj değildir. Orta ve yüksek karmaşıklıktaki görevlerde hızlı başlamak, yanlış yönde hızlı ilerlemek anlamına gelebilir. Beş dosyayı değiştirdikten sonra yaklaşımın temelden yanlış olduğunu fark etmek, hem zamanı hem de context'i israf eder.

Plan Mode bu riski ortadan kaldırır. "Önce mimar gibi düşün, sonra işçi gibi inşa et" ilkesi Plan Mode'un özüdür. Mimar planı onaylanmadan inşaat başlamaz — bu kural küçük projeler için aşırıya kaçmak olabilir, ama karmaşık görevler için vazgeçilmezdir.

## Plan Mode Ne Zaman Kullanılmalı?

Her görev için Plan Mode kullanmak gerekmez. Tek dosyada küçük bir değişiklik, basit bir bug fix, tekrarlayan bir işlem — bunlar için doğrudan eylem daha verimlidir. Plan Mode'un değeri karmaşıklıkla orantılıdır.

Şu durumlarda Plan Mode düşünün: birden fazla dosyayı etkileyecek değişikliklerde, yeni bir mimari bileşen eklerken, mevcut sisteme entegrasyon gerektiren özelliklerde, ya da sonradan geri alınması maliyetli kararlar alınacaksa. Kısaca: "başlamadan önce planı görmek istiyorum" hissi uyandıran her görevde.

## Dahili Subagent'lar

Plan Mode arkasında üç dahili subagent çalışır. Explore subagent'ı keşif yaparak mevcut kodu, bağımlılıkları ve ilgili dosyaları anlar. Plan subagent'ı bu keşfe dayanarak olası yaklaşımları değerlendirir ve bir plan oluşturur. General-purpose subagent ise plan onaylandıktan sonra uygulamayı yürütür.

Bu üç subagent'ın her biri izole bir context'te çalışır. Explore'un context'i Plan'ı etkilemez, Plan'ın context'i uygulamayı etkilemez. Bu izolasyon önemlidir: her subagent kendi görevine odaklanır, önceki adımların gürültüsünü taşımaz.

## Plan Onay Süreci

Claude Code bir plan sunduğunda üç seçeneğiniz vardır: onaylayın ve uygulamaya geçin, reddedin ve farklı bir yaklaşım isteyin, ya da planı değiştirin.

Plan değiştirme pratikte en değerli seçenektir. Claude Code'un önerdiği yaklaşımı %80 oranında kabul edip kalan %20'yi değiştirmek — "bu adımı atla", "bu dosyayı da dahil et", "bu kütüphane yerine şunu kullan" — genellikle en iyi sonucu verir. Plan, Claude Code'un anlayışını yansıtır; sizin değişiklikleriniz domain bilginizi ekler. İkisi birleşince plan olgunlaşır.

## Plan Mode ve Context Tasarrufu

Plan Mode'un az konuşulan bir avantajı daha var: yanlış yönde harcanan context'i önler. Doğrudan eylem modunda yanlış bir yaklaşımın fark edilmesi için genellikle birkaç araç çağrısı, birkaç dosya değişikliği gerekir. Bu süreç context tüketir. Plan Mode ile yanlış yaklaşım, bir satır kod yazılmadan elenebilir.

Uzun ve karmaşık görevlerde bu fark belirginleşir. Context'i doğru yönetmek sadece `/compact` kullanmak değil, gereksiz yere context tüketmemektir — Plan Mode ikinci katkıyı sağlar.
