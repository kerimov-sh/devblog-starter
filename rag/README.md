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
```

Varsayılan olarak `../docs` içindeki makaleleri okuyup `rag/rag.db` dosyasını
oluşturur/günceller. `--docs-dir` ve `--db` argümanlarıyla özelleştirilebilir.

## Sonraki Adım (henüz yapılmadı)

Bu faz yalnızca chunk + SQLite oluşturmayı kapsıyor. Bir sonraki adım:

- `embed.py`: `chunks` tablosundaki (henüz `embedded_at IS NULL` olan) satırları
  Voyage API (`voyageai` SDK) ile embed edip `vec_chunks` sanal tablosuna yazacak.
- `query.py`: bir soru metnini embed edip `vec_chunks` üzerinde benzerlik
  araması yaparak en alakalı chunk'ları döndürecek (retrieval).
