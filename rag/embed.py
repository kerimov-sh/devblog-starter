"""chunks tablosundaki embed edilmemiş satırları Voyage API ile embed eder.

chunks.embedding (düz BLOB kolon) asıl tüketici olan .NET backend'in
RagChunkSeeder'ı tarafından okunur — sqlite-vec native extension'ına .NET
tarafında ihtiyaç duyulmaması için bilinçli olarak düz bir kolon kullanılır.
vec_chunks (sqlite-vec sanal tablosu) olası ileride Python tarafı sorgu
araçları için ayrıca doldurulur.

Kullanım:
    python embed.py [--db rag.db]
"""

from __future__ import annotations

import argparse
import array
import os
import sqlite3
import time
from pathlib import Path

import voyageai
from dotenv import load_dotenv

from db import EMBEDDING_DIM, connect, init_schema

MODEL = "voyage-3.5"
BATCH_SIZE = 8
# Rate limit: 3 RPM (requests per minute) for accounts without payment method.
# Wait at least 20 seconds between requests. Add extra buffer for safety.
BATCH_DELAY_SECONDS = 30


def _pack_embedding(values: list[float]) -> bytes:
    # float32 little-endian ham bayt dizisi: Python'ın array('f') ve .NET'in
    # Buffer.BlockCopy<float[]> yöntemi x86_64'te aynı bellek düzenini
    # kullanır, bu yüzden ek bir serileştirme formatına gerek yok.
    return array.array("f", values).tobytes()


def fetch_unembedded_chunks(conn: sqlite3.Connection) -> list[sqlite3.Row]:
    return conn.execute(
        "SELECT id, content FROM chunks WHERE embedded_at IS NULL ORDER BY id"
    ).fetchall()


def embed_and_store(
    conn: sqlite3.Connection, client: voyageai.Client, rows: list[sqlite3.Row]
) -> int:
    embedded_count = 0
    total_batches = (len(rows) + BATCH_SIZE - 1) // BATCH_SIZE

    for batch_idx, start in enumerate(range(0, len(rows), BATCH_SIZE)):
        batch = rows[start : start + BATCH_SIZE]
        texts = [row["content"] for row in batch]

        # Delay before batch request (except first batch) to respect rate limits
        if batch_idx > 0:
            print(f"  Rate limit beklemesi: {BATCH_DELAY_SECONDS} saniye...")
            time.sleep(BATCH_DELAY_SECONDS)

        result = client.embed(
            texts, model=MODEL, input_type="document", output_dimension=EMBEDDING_DIM
        )

        for row, embedding in zip(batch, result.embeddings):
            packed = _pack_embedding(embedding)

            conn.execute(
                """
                UPDATE chunks
                SET embedding = ?, embedding_model = ?, embedded_at = datetime('now')
                WHERE id = ?
                """,
                (packed, MODEL, row["id"]),
            )
            conn.execute(
                "INSERT OR REPLACE INTO vec_chunks (chunk_id, embedding) VALUES (?, ?)",
                (row["id"], packed),
            )
            embedded_count += 1

        conn.commit()
        print(f"  {embedded_count}/{len(rows)} chunk embed edildi (batch {batch_idx + 1}/{total_batches})")

    return embedded_count


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--db", type=Path, default=Path(__file__).parent / "rag.db")
    args = parser.parse_args()

    load_dotenv(Path(__file__).parent / ".env")
    api_key = os.environ.get("VOYAGE_API_KEY")
    if not api_key:
        raise SystemExit("VOYAGE_API_KEY .env dosyasında bulunamadı.")

    conn = connect(args.db)
    init_schema(conn)

    rows = fetch_unembedded_chunks(conn)
    if not rows:
        print("Embed edilecek chunk yok (hepsi zaten embed edilmiş).")
        conn.close()
        return

    client = voyageai.Client(api_key=api_key)
    total = embed_and_store(conn, client, rows)
    conn.close()

    print(f"\nTamamlandı: {total} chunk embed edildi -> {args.db}")


if __name__ == "__main__":
    main()
