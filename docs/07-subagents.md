# Subagents: Context İzolasyonu ve Orkestrasyon Desenleri

Tek bir Claude Code oturumu güçlüdür ama sınırlıdır: tek bir context window, tek bir çalışma akışı, tek bir odak noktası. Karmaşık görevler büyüdükçe bu sınırlama belirginleşir. Subagent'lar bu sınırı aşmanın yoludur — birden fazla izole Claude Code instance'ını koordineli biçimde çalıştırma mekanizması.

## Subagent Nedir?

Subagent, kendi izole context'inde çalışan bağımsız bir Claude Code instance'ıdır. Ana agent (orchestrator) bir görevi subagent'a devreder; subagent kendi agentic loop'unu çalıştırır, görevi tamamlar ve sonucu geri bildirir. Orchestrator bu sonucu değerlendirir ve bir sonraki adıma karar verir.

Context izolasyonu bu mimarinin en kritik özelliğidir. Her subagent yalnızca kendisine verilen görevi ve ilgili bağlamı görür; orchestrator'ın tüm context'ini ya da diğer subagent'ların çalışmalarını görmez. Bu izolasyon iki şey sağlar: her subagent kendi görevine tam odaklanabilir, ve subagent'ların hataları birbirini kirletmez.

## İki Temel Mimari

**Master-Clone mimarisi**, aynı görevi birden fazla subagent'a paralel olarak verir ve en iyi sonucu seçer. Bir kod parçasını farklı yaklaşımlarla yazdırıp en temiz çözümü almak, aynı analizi farklı perspektiflerden yaptırıp en kapsamlı raporu elde etmek bu mimarinin kullanım alanlarıdır. "Best of N" pattern olarak da bilinen bu yaklaşım, deterministik olmayan görevlerde kalite güvencesi sağlar.

**Lead-Specialist mimarisi**, farklı uzmanlık alanlarına sahip subagent'ların koordinasyonuna dayanır. Bir lead (orchestrator) görevi analiz eder, alt görevlere böler ve her alt görevi ilgili specialist'e devreder. Backend specialist'i API katmanını yazar, frontend specialist'i UI bileşenini yazar, test specialist'i test suite'ini oluşturur — lead bu üçünü koordine eder ve entegre eder.

## Ne Zaman Hangi Mimari?

Master-Clone, görevin tek bir doğru cevabı olmadığı durumlarda güçlüdür: yaratıcı kod çözümleri, farklı implementasyon alternatiflerini değerlendirme, ya da kalite güvencesinin kritik olduğu tek seferlik işler. Paralel çalışmanın maliyeti önem taşımıyorsa ve en iyi sonucu bulmak öncelikliyse bu mimari tercih edilir.

Lead-Specialist, görevin doğal olarak farklı uzmanlık alanlarına bölünebildiği durumlarda daha verimlidir. Full-stack bir özellik geliştirme, backend ve frontend'in ayrı koordinasyonu gerektirdiği durumlar, ya da farklı teknoloji yığınlarının bir arada çalıştığı görevler bu kategoriye girer.

## Sıralı Bağımlılık Tuzağı

Subagent'ları paralel çalıştırmak her zaman daha hızlı değildir. Eğer subagent B'nin görevi subagent A'nın çıktısına bağımlıysa, paralel çalışma anlamsızlaşır — B, A bitene kadar beklemek zorundadır. Bu durumda paralel mimari, sıralı mimariye göre yalnızca ek karmaşıklık ekler.

Frontend'in backend'in ürettiği API şemasına bağımlı olduğu bir senaryoyu düşünün: frontend subagent'ı, backend subagent'ı API endpoint'lerini tanımlayana kadar anlamlı bir çalışma yapamaz. Bu bağımlılık, görünürde paralel olan bu iki görevin aslında sıralı yürütülmesi gerektiğini gösterir.

Subagent mimarisine karar vermeden önce bağımlılık grafiğini çizin. Gerçekten bağımsız olan görevler paralel çalışabilir; bağımlı olanlar sıralı yürütülmeli.

## Orchestrator'ın Sorumluluğu

Orchestrator subagent'ları çalıştırmakla kalmaz, sonuçlarını entegre etmekle de sorumludur. Her subagent kendi bağlamında tutarlı bir çıktı üretmiş olabilir, ama bu çıktıların birbirleriyle tutarlı olması garanti değildir. Orchestrator bu entegrasyon katmanını yönetir: çakışmaları çözer, eksikleri tespit eder, gerektiğinde subagent'lara geri döner.

Bu sorumluluk, orchestrator'ın tasarımının kritik önem taşıdığı anlamına gelir. İyi bir orchestrator dar kapsamlı görevler verir, net çıktı formatları tanımlar ve entegrasyon mantığını açıkça kurgular.
