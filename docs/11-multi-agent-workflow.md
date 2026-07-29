# Multi-Agent Workflow: Git Worktree ile Paralel Geliştirme

Tek bir Claude Code oturumu güçlüdür ama sıralı çalışır — bir görevi bitirip diğerine geçer. Bağımsız görevlerin bu sırayla işlenmesi, her görevin bir öncekinin bitmesini beklemesi anlamına gelir. Görevler gerçekten bağımsızsa bu bekleme süresi saf kayıptır. Multi-agent workflow bu kaybı ortadan kaldırır: birden fazla Claude Code instance'ı aynı anda farklı görevler üzerinde çalışır.

## Paralel Geliştirmenin Önündeki Engel

İki Claude Code instance'ını aynı repo üzerinde aynı anda çalıştırmanın doğal bir engeli vardır: çakışma. Instance A bir dosyayı değiştirirken Instance B aynı dosyayı değiştirirse ne olur? Git bu çakışmayı çözmek için sizin müdahalenizi bekler — ve bu müdahale sırasında paralelliğin kazandırdığı zaman harcanır.

Git worktree bu engeli ortadan kaldırır. Aynı repo'nun farklı branch'lerini farklı dizinlere bağlamanızı sağlar. Her dizin bağımsız bir çalışma alanıdır: kendi dosya sistemi durumuna sahip, diğer worktree'lerin değişikliklerinden etkilenmeyen. İki Claude Code instance'ı bu iki dizinde bağımsız olarak çalışır — aynı repo, çakışmasız paralel geliştirme.

## Git Worktree Kurulumu

```bash
# Ana repo'dan yola çıkarak iki worktree oluştur
git worktree add ../devblog-feature-a feature/user-notifications
git worktree add ../devblog-bugfix-b fix/pagination-edge-case
```

Bu komutlar iki ayrı dizin oluşturur. Her dizinde bağımsız bir Claude Code oturumu başlatabilirsiniz. İlk oturum `feature/user-notifications` branch'inde yeni bir özellik geliştirirken ikinci oturum `fix/pagination-edge-case` branch'inde bir bug fix uygular. İkisi birbirinden habersiz, paralel olarak çalışır.

## claude -p ile Programatik Mod

Birden fazla Claude Code instance'ını manuel olarak başlatmak yerine `claude -p` (programmatic mode) ile bu süreci otomatikleştirebilirsiniz. Programmatic mod, Claude Code'u bir komut satırı aracı olarak çağırır: standart input'tan görevi alır, görevi yürütür, çıktıyı standart output'a yazar.

Bu mod shell scriptleri, Python scriptleri ya da herhangi bir orkestrasyon aracıyla entegre olur. Birden fazla görevi paralel başlatmak, çıktıları toplamak ve sonuçları değerlendirmek artık programatik olarak mümkündür.

## Hangi Görevler Paralel Çalışabilir?

Paralelleştirmenin değeri, görevlerin gerçek bağımsızlığına bağlıdır. İki görev arasında veri bağımlılığı varsa — A'nın çıktısı B'nin girdisidir — paralel çalışma anlamsızdır.

Paralel çalışmaya uygun görev örnekleri: farklı modüllerdeki bug fix'ler, bağımsız özellik geliştirmeleri, farklı dosya setlerini etkileyen refactor işlemleri, ve bağımsız test suite'lerinin yazılması. Paralel çalışmaya uygun olmayan örnekler: API tasarımı tamamlanmadan UI geliştirme, şema değişikliği tamamlanmadan repository katmanı güncelleme.

Pratik kural: bağımlılık grafiğini çizin. Birbirine bağlı olmayan düğümler paralel yürütülebilir; birbirine bağlı düğümler sıralı yürütülmelidir.

## Sonuçların Entegrasyonu

Paralel çalışan instance'lar tamamlandıktan sonra sonuçların entegrasyonu gerekir. Git merge ya da rebase bu entegrasyonu sağlar — ve bu noktada çakışmalar gündeme gelebilir. Ancak worktree'lerin farklı dosya alanlarında çalışması, çakışma olasılığını önemli ölçüde azaltır.

Entegrasyon sürecinin kendisi de Claude Code ile yönetilebilir. Merge conflict'leri çözmek, entegre edilmiş kodu test etmek, ya da bütünleşme noktasındaki uyumsuzlukları gidermek için yeni bir Claude Code oturumu başlatılabilir. Bu oturum, tamamlanan iki branch'in durumunu bağlam olarak alır ve entegrasyon görevine odaklanır.
