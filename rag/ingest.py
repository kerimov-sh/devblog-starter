"""docs/ altındaki numaralandırılmış makaleleri chunk'layıp SQLite'a yazar.

Bu faz yalnızca chunk üretimi + SQLite şema/veri oluşturmayı kapsar.
Embedding üretimi (Voyage API) ayrı bir sonraki adımdır (embed.py).

Kullanım:
    python ingest.py [--docs-dir ../docs] [--db rag.db]
"""

from __future__ import annotations

import argparse
import re
import sqlite3
from pathlib import Path

from chunking import chunk_file
from db import connect, init_schema

_NUMBERED_DOC_RE = re.compile(r"^(\d+)-.+\.md$")


def find_numbered_docs(docs_dir: Path) -> list[Path]:
    candidates = [
        p for p in docs_dir.glob("*.md") if _NUMBERED_DOC_RE.match(p.name)
    ]
    return sorted(candidates, key=lambda p: int(_NUMBERED_DOC_RE.match(p.name).group(1)))


def ingest_document(conn: sqlite3.Connection, path: Path, order_index: int) -> int:
    parsed = chunk_file(path)

    conn.execute(
        """
        INSERT INTO documents (filename, title, order_index)
        VALUES (?, ?, ?)
        ON CONFLICT(filename) DO UPDATE SET
            title = excluded.title,
            order_index = excluded.order_index
        """,
        (parsed.filename, parsed.title, order_index),
    )
    document_id = conn.execute(
        "SELECT id FROM documents WHERE filename = ?", (parsed.filename,)
    ).fetchone()["id"]

    # Yeniden ingest edilebilirlik: aynı dokümanın eski chunk'larını temizle.
    conn.execute("DELETE FROM chunks WHERE document_id = ?", (document_id,))

    conn.executemany(
        """
        INSERT INTO chunks (document_id, chunk_index, heading, content, char_count)
        VALUES (?, ?, ?, ?, ?)
        """,
        [
            (document_id, c.chunk_index, c.heading, c.content, c.char_count)
            for c in parsed.chunks
        ],
    )

    return len(parsed.chunks)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--docs-dir", type=Path, default=Path(__file__).parent.parent / "docs"
    )
    parser.add_argument("--db", type=Path, default=Path(__file__).parent / "rag.db")
    args = parser.parse_args()

    docs = find_numbered_docs(args.docs_dir)
    if not docs:
        raise SystemExit(f"'{args.docs_dir}' altında numaralandırılmış makale bulunamadı.")

    conn = connect(args.db)
    init_schema(conn)

    total_chunks = 0
    for order_index, path in enumerate(docs, start=1):
        chunk_count = ingest_document(conn, path, order_index)
        total_chunks += chunk_count
        print(f"  {path.name}: {chunk_count} chunk")

    conn.commit()
    conn.close()

    print(f"\nTamamlandı: {len(docs)} doküman, {total_chunks} chunk -> {args.db}")


if __name__ == "__main__":
    main()
