# RAG Pipeline — docs/ makaleleri

`docs/` klasöründeki numaralandırılmış 12 makaleyi (`01-agentic-loop.md` … `12-claude-code-sdk.md`)
bir RAG (Retrieval-Augmented Generation) altyapısına hazırlayan Python araçları.

Bu fazda tamamlananlar:

- **Chunking** (`chunking.py`): her makaleyi `##` başlıklarına göre anlamsal
  bölümlere ayırır; 1000 karakteri aşan bölümler paragraf sınırlarına saygılı,
  150 karakter overlap'li şekilde alt-bölünür. Her chunk `"Başlık > Alt başlık"`
  öneki ile bağlamını korur.
- **SQLite şeması** (`db.py`): [sqlite-vec](https://github.com/asg017/sqlite-vec)
  extension'ı ile `documents`, `chunks` (chunk metni + metadata) ve `vec_chunks`
  (embedding'ler için sanal vektör tablosu, henüz boş) tabloları oluşturulur.
- **Ingest** (`ingest.py`): `docs/*.md` dosyalarını okur, chunk'lar, `rag.db`
  SQLite dosyasına yazar. Yeniden çalıştırıldığında idempotent'tir (aynı
  dosyanın eski chunk'ları silinip yeniden yazılır).
- **Embed** (`embed.py`): `chunks` tablosundaki henüz embed edilmemiş
  (`embedding IS NULL`) satırları Voyage API (`voyageai` SDK, `voyage-3.5`
  modeli) ile embed edip aynı tabloda `chunks.embedding` sütununa (düz BLOB)
  yazar — .NET tarafındaki `RagChunkSeeder`'ın okuduğu asıl kolon budur;
  `vec_chunks` sanal tablosu ayrıca doldurulur ama olası ileride Python tarafı
  sorgu araçları içindir, .NET backend'i tüketmez. Yeniden çalıştırıldığında
  yalnızca embed edilmemiş satırları işler.

## Kurulum

```bash
cd rag
python -m venv .venv
./.venv/Scripts/activate   # Windows
pip install -r requirements.txt
```

`VOYAGE_API_KEY` `.env` dosyasında tutulur (`.env.example`'a bakın). `.env`
`.gitignore`'da hariç tutulmuştur — commit'lenmemelidir.

## Çalıştırma

```bash
python ingest.py
python embed.py
```

`ingest.py` varsayılan olarak `../docs` içindeki makaleleri okuyup `rag/rag.db`
dosyasını oluşturur/günceller (`--docs-dir` ve `--db` argümanlarıyla
özelleştirilebilir). `embed.py` ardından çalıştırılıp henüz embed edilmemiş
chunk'ları Voyage API ile embed eder.

## Taze bir clone'da `rag.db`'yi yeniden üretme

`rag/rag.db` bilinçli olarak `.gitignore`'dadır (Voyage API'ye para ödenerek
üretilen bir artifact'tır, kaynak değil). Taze bir clone'da `/chat`
endpoint'inin çalışması için:

```bash
cd rag
cp .env.example .env   # VOYAGE_API_KEY'inizi .env'e girin
python ingest.py
python embed.py
```

Bu, `docs/*.md` makalelerinden (repo'da version control altındadır)
`rag/rag.db`'yi baştan üretir; API her başladığında `RagChunkSeeder` bu
dosyayı okuyup `RagChunks` tablosuna import eder. `rag.db` yoksa veya
embed edilmiş chunk içermiyorsa API yine de başlar, sadece `/chat` endpoint'i
503 döner.
