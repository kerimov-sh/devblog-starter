# Context Window Yönetimi: /clear ve /compact Ne Zaman Kullanılır?

Claude Code ile uzun süre çalıştığınızda bir noktada yanıt kalitesinin düştüğünü fark edebilirsiniz. Verdiği kararlar daha önceki tutarsız görünmeye başlar, daha önce bildiği bir şeyi "unutmuş" gibi davranır, ya da alakasız bağlamı ön plana taşır. Bu bir hata değil — context window'un dolmasının kaçınılmaz sonucu.

## Masa Benzetmesi

Context window'u bir çalışma masası gibi düşünün. Masanın üzerinde sığabilecek kadar belge, not ve araç çıktısı duruyor. Siz çalıştıkça masaya yeni şeyler koyuyorsunuz: okuduğunuz dosyalar, çalıştırdığınız testlerin çıktıları, konuşma geçmişi. Masa dolduğunda yeni bir şey koymak için eski bir şeyi kaldırmak zorundasınız. Ve kaldırılan şey, bir önceki kararınıza referans olan kritik bir not olabilir.

Claude Code tam olarak böyle çalışır. Context window dolduğunda model, eski bilgiyi "unutmaya" başlar. Hangi bilginin kaldırılacağı deterministik değildir — bu belirsizlik, kalite düşüşünün öngörülmesini zorlaştırır.

## /clear ve /compact: Fark Ne?

İki komut farklı ihtiyaçlara yanıt verir.

`/clear` context'i tamamen sıfırlar. Konuşma geçmişi, okunan dosyalar, araç çıktıları — hepsi gider. Yeni bir görev başlatırken, önceki görevle ilgisi olmayan bir konuya geçerken ya da "sıfırdan başlayalım" demeniz gereken her durumda `/clear` doğru seçimdir. Masayı tamamen boşaltıp yeni bir çalışma alanı açmak gibi.

`/compact` ise context'i özetler. Claude Code o ana kadar biriken bilgiyi sıkıştırır, önemli olanı korur, detayları bırakır. Uzun süren bir görevin ortasında context şişmeye başladığında ama göreve devam etmeniz gerektiğinde `/compact` kullanılır. Masayı tamamen boşaltmak yerine, üzerindeki belgeleri özetlenmiş notlara dönüştürmek gibi.

## Ne Zaman Hangisi?

Görev değişiyorsa `/clear`. Aynı görev devam ediyorsa `/compact`. Bu ayrım pratikte çoğu kararı kapsar.

Daha ince bir kural: context'in ne kadarının hâlâ geçerli olduğunu sorun kendinize. Eğer son 10 araç çağrısının 7'si mevcut görevle ilgisizse, `/compact` bu alakasız bilgiyi temizler ama ilgili olanı korur. Eğer tüm context başka bir görevden kalmışsa, `/clear` ile başlamak daha temizdir.

## Kalite Düşüşünü Erken Fark Etmek

Context şişmesinin belirtileri genellikle şunlardır: Claude Code daha önce verdiği bir kararla çelişiyor, aynı dosyayı birden fazla kez okuyor, ya da verdiği yanıtlar giderek daha genel ve bağlamdan kopuk hale geliyor.

Bu belirtileri gördüğünüzde `/compact` ile müdahale edin. Görevin kritik noktalarında — büyük bir değişiklik yapmadan önce, önemli bir karar öncesinde — proaktif olarak `/compact` kullanmak da iyi bir alışkanlıktır. Kalite düştükten sonra değil, düşmeden önce müdahale etmek çok daha verimlidir.

## Uzun Görevlerde Strateji

Birkaç saate yayılan görevlerde context yönetimi bir mühendislik kararı haline gelir. Görevin doğal kesim noktalarını — bir feature tamamlandığında, bir test paketi geçtiğinde — `/compact` için fırsat olarak kullanın. Çok uzun sürecek görevleri alt görevlere bölün ve her alt görev arasında context'i temizleyin. Bu yaklaşım, uzun oturumlarda tutarlı kalite sağlamanın en güvenilir yoludur.

Context window sınırsız olsaydı bu kaygılar gereksiz olurdu. Ama sınırlı bir kaynak olarak doğru yönetilmediğinde, Claude Code'un gücünün önemli bir kısmı israf olur.
