# Agentic Loop: Claude Code Nasıl "Düşünür"?

GitHub Copilot veya benzeri bir araç kullandıysanız şu deneyimi yaşamışsınızdır: siz bir satır yazmaya başlarsınız, araç geri kalanını tamamlar. Güçlü, hızlı — ama temelde reaktif. Siz hareket edersiniz, araç cevap verir. Claude Code bu modelin dışında çalışır. Ona tek bir cümleyle bir görev verdiğinizde o görevi tamamlayana kadar bağımsız hareket eder. Bu bağımsızlığın altında "agentic loop" adı verilen bir döngü yatır.

## Döngünün Üç Adımı

Agentic loop üç adımdan oluşur: gözlem, karar ve eylem. Claude Code bir görevi aldığında önce mevcut durumu anlamaya çalışır — bu gözlem aşamasıdır. Hangi dosyalar var, kod nasıl organize edilmiş, bağımlılıklar neler? Bu soruları yanıtlamak için araç setini kullanır: Grep ile pattern arar, Read ile dosyaları açar, Bash ile komutlar çalıştırır.

Gözlem aşamasından sonra karar gelir. Claude Code topladığı bilgiye dayanarak bir sonraki adımı seçer: hangi dosyayı değiştirmeli, hangi testi çalıştırmalı, hangi bağımlılığı eklemeli? Bu kararlar sıralı değil, bağlamsal alınır — önceki adımda ne öğrenildiğine göre şekillenir.

Karar aşamasının ardından eylem gelir. Edit ile dosyayı değiştirir, Bash ile testi çalıştırır, Write ile yeni bir dosya oluşturur. Eylemin çıktısı yeni bir gözlem haline gelir ve döngü başa döner. Test geçti mi? Geçmediyse neyi kaçırdım? Dosyada başka bir sorun var mı?

## Döngü Ne Zaman Durur?

Claude Code, orijinal isteği karşıladığını değerlendirdiğinde durur. Bu "değerlendirme" kulağa belirsiz gelebilir, ama pratikte şu anlama gelir: görev net tanımlanmışsa döngü net bir noktada biter; görev muğlaksa Claude Code bir yoruma bağlanır ve o yorumu karşıladığında durur. Bu nedenle iyi tanımlanmış görevler daha öngörülebilir sonuçlar üretir.

Döngünün ortasında müdahale de mümkündür. Claude Code bir plan sunduğunda onu reddedebilir, yön değiştirebilirsiniz. Plan Mode tam olarak bu amaçla var: döngüyü başlatmadan önce planı onaylama fırsatı verir.

## Araç Zinciri

Agentic loop'un gücü, araçların birbirini beslemesinden kaynaklanır. Tek bir görevde şu zincir kurulabilir: Grep ile ilgili dosyaları bul → Read ile içeriklerini oku → problemi analiz et → Edit ile düzelt → Bash ile test çalıştır → sonucu gözlemle → gerekiyorsa tekrar et. Bu zinciri siz kurmuyorsunuz; Claude Code göreve göre kendisi kuruyor.

Copilot ile karşılaştırmak gerekirse: Copilot satır inşaatçısı gibidir, her tuğlayı siz yönlendiriyorsunuz. Claude Code ise müteahhit gibidir — hedefi siz belirliyorsunuz, inşaatın nasıl yürüyeceğini o yönetiyor. Müteahhidinizin nasıl çalıştığını anlamadan iş vermek nasıl sonuçlanır, bunu tahmin etmek zor değil.

## Pratikte Ne Anlama Geliyor?

Agentic loop'u anlamak, Claude Code'u daha iyi yönlendirmenizi sağlar. Döngünün gözlem aşamasında ne bulacağını biliyorsanız — hangi dosyaların orada olduğunu, hangi pattern'lerin var olduğunu — görevi o bilgiye göre çerçeveleyebilirsiniz. Karar aşamasında hangi kısıtları göz önünde bulundurmasını istediğinizi CLAUDE.md aracılığıyla iletebilirsiniz. Eylem aşamasında hangi araçları kullanabileceğini bağlam yoluyla şekillendirebilirsiniz.

Bu anlayış olmadan Claude Code güçlü ama öngörülemez bir araç olarak kalır. Bu anlayışla birlikte ise yönetilebilir, kalibre edilebilir bir sisteme dönüşür.
