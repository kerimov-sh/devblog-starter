"""SQLite vektör veritabanı şeması ve bağlantı yardımcıları.

`documents` ve `chunks` bu fazda (chunk + DB oluşturma) doldurulur.
`vec_chunks` (sqlite-vec sanal tablosu) şema olarak burada hazırlanır;
embedding'ler bir sonraki fazda (embed.py) doldurulacaktır.
"""

from __future__ import annotations

import sqlite3
from pathlib import Path

import sqlite_vec

# Voyage embedding modelinin varsayılan çıktı boyutu (voyage-3.5, output_dimension=1024).
# Embedding fazında farklı bir model/boyut seçilirse bu sabit güncellenmelidir.
EMBEDDING_DIM = 1024

_SCHEMA = """
CREATE TABLE IF NOT EXISTS documents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    filename TEXT NOT NULL UNIQUE,
    title TEXT NOT NULL,
    order_index INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS chunks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    chunk_index INTEGER NOT NULL,
    heading TEXT NOT NULL,
    content TEXT NOT NULL,
    char_count INTEGER NOT NULL,
    embedded_at TEXT,
    UNIQUE(document_id, chunk_index)
);

CREATE INDEX IF NOT EXISTS idx_chunks_document_id ON chunks(document_id);
"""


def connect(db_path: Path) -> sqlite3.Connection:
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA foreign_keys = ON")

    conn.enable_load_extension(True)
    sqlite_vec.load(conn)
    conn.enable_load_extension(False)

    return conn


def init_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(_SCHEMA)

    existing = conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name='vec_chunks'"
    ).fetchone()
    if existing is None:
        conn.execute(
            f"""
            CREATE VIRTUAL TABLE vec_chunks USING vec0(
                chunk_id INTEGER PRIMARY KEY,
                embedding FLOAT[{EMBEDDING_DIM}]
            )
            """
        )

    conn.commit()
