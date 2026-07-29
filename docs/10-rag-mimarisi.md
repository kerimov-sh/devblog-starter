# RAG Mimarisi: Chunking, Embedding ve Benzerlik Araması

Bir dil modeli eğitildiği verinin sınırları içinde çalışır. Eğitim kesim tarihinden sonraki gelişmeleri bilmez, şirket içi dokümanlardan habersizdir, proje spesifik bilgiye erişimi yoktur. Bu sınırı aşmanın iki yolu vardır: modeli yeniden eğitmek (pahalı, yavaş, pratikte nadiren mümkün) ya da sorgu anında ilgili bilgiyi modele sunmak. İkinci yol RAG'dır — Retrieval-Augmented Generation.

## Açık Kitap Sınavı

RAG'ı anlamanın en sezgisel yolu açık kitap sınavı benzetmesidir. Kapalı sınavda her şeyi ezberden bilmek zorundasınızdır — model eğitimi bu kategoridedir. Açık sınavda ise doğru sayfayı bulup açabildiğiniz sürece cevap verebilirsiniz. RAG ikinci yaklaşımı otomatize eder: doğru "sayfa"yı (doküman parçasını) bulur ve modelin bağlamına ekler.

## Dört Adımlı Akış

RAG'ın tüm akışı dört adıma indirgenebilir: hazırlık (indexing) ve sorgu zamanı işlemleri.

**Chunking**, hazırlık aşamasının ilk adımıdır. Dokümanlar — markdown dosyaları, PDF'ler, kod dosyaları — küçük parçalara bölünür. Her parçaya "chunk" denir. Chunking stratejisi kritiktir: çok büyük chunk'lar alakasız bilgi taşır, çok küçük chunk'lar ise anlam bütünlüğünü bozar. Ortalama 300-500 token, çoğu senaryo için iyi bir başlangıç noktasıdır. Ancak doküman türü bu kararı etkiler — kod dosyaları için fonksiyon sınırları doğal chunk sınırlarıdır; düz metin için paragraf sınırları daha mantıklıdır.

**Embedding**, her chunk'ı sayısal bir vektöre dönüştürür. Bu vektör, chunk'ın anlamını yüksek boyutlu bir uzayda bir nokta olarak temsil eder. Anlamca benzer metinler bu uzayda birbirine yakın noktalara düşer — "context window yönetimi" ve "/compact komutu" farklı ifadeler olsa da anlamca ilişkilidir ve vektör uzayında yakın olurlar. Embedding modeli bu dönüşümü yapar; OpenAI, Cohere ya da açık kaynak alternatifleri bu iş için kullanılır.

**Vektör veritabanı**, üretilen embedding'leri depolar ve hızlı benzerlik araması için optimize edilmiş bir yapı sunar. Pgvector, Chroma, Pinecone, Weaviate bu kategorinin örnekleridir. Geleneksel veritabanları tam eşleşme arar; vektör veritabanları anlam yakınlığını arar. "Claude Code'da bellek nasıl yönetilir?" sorusu, "context window" veya "compact" kelimelerini içermeyen ama anlamca ilişkili chunk'ları da bulabilir.

**Retrieval ve augmentation**, sorgu zamanında gerçekleşir. Kullanıcı bir soru sorduğunda soru da embedding'e dönüştürülür. Vektör veritabanında bu sorunun embedding'ine en yakın chunk'lar bulunur. Bu chunk'lar modelin context'ine eklenir. Model artık hem soruyu hem de ilgili doküman parçalarını görür ve bu bilgiyle yanıt üretir.

## Chunking Stratejisinin Önemi

Kötü chunking kötü RAG demektir. Bu kural basit görünür ama pratikte sık gözden kaçar.

Bir chunk'ın anlamlı olması için tek başına okunduğunda bağlamı koruyabilmesi gerekir. "Bu yaklaşım daha iyi performans sunar" ile biten ve "çünkü I/O operasyonlarını minimize eder" ile başlayan iki chunk düşünün — her ikisi de bağlamından kopuk hale gelmiştir. Chunk sınırları anlam sınırlarıyla örtüşmelidir.

Overlap stratejisi bu problemi kısmen çözer: ardışık chunk'lar belirli oranda örtüşür, böylece sınır noktasındaki bağlam iki chunk'ta da bulunur. Yüzde 10-20 overlap çoğu senaryo için yeterlidir.

## RAG'ın Sınırları

RAG her problemi çözmez. Çok spesifik sayısal verileri (bir tablodaki belirli bir hücre değeri), gerçek zamanlı bilgiyi, ya da dokümanlar arasındaki karmaşık ilişkileri yakalamak zordur. Ayrıca retrieval kalitesi embedding modelinin kalitesiyle doğrudan ilişkilidir — zayıf bir embedding modeli alakasız chunk'ları "ilgili" olarak döndürebilir.

Bu sınırları bilmek, RAG'ın uygun olduğu senaryoları doğru tanımlamayı sağlar: orta büyüklükte doküman setleri, anlam bazlı arama gerektiren sorgular, ve modelin eğitim verisinde olmayan özel bilgi alanları — RAG bu senaryolarda en yüksek değeri üretir.
