"""Docs klasöründeki numaralandırılmış markdown makalelerini RAG için chunk'lara böler.

Strateji: her makale `##` başlıklarına göre bölümlere ayrılır (doğal, anlamsal
sınırlar). Bir bölüm MAX_CHARS'ı aşarsa, paragraf sınırlarına saygı gösteren
kaydırmalı (overlap) bir alt-bölme uygulanır. Her chunk'ın başına, embedding'in
bağlamı kaybetmemesi için "Doküman başlığı > Bölüm başlığı" öneki eklenir.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

MAX_CHARS = 1000
OVERLAP_CHARS = 150

_H1_RE = re.compile(r"^#\s+(.+)$", re.MULTILINE)
_H2_SPLIT_RE = re.compile(r"^##\s+(.+)$", re.MULTILINE)


@dataclass
class Chunk:
    chunk_index: int
    heading: str
    content: str

    @property
    def char_count(self) -> int:
        return len(self.content)


@dataclass
class ParsedDocument:
    filename: str
    title: str
    chunks: list[Chunk]


def _split_long_section(heading: str, text: str) -> list[str]:
    """MAX_CHARS'ı aşan bir bölümü paragraf sınırlarında, overlap ile böler."""
    paragraphs = [p.strip() for p in text.split("\n\n") if p.strip()]
    pieces: list[str] = []
    current = ""

    for paragraph in paragraphs:
        candidate = f"{current}\n\n{paragraph}".strip() if current else paragraph
        if len(candidate) <= MAX_CHARS or not current:
            current = candidate
        else:
            pieces.append(current)
            tail = current[-OVERLAP_CHARS:]
            current = f"{tail}\n\n{paragraph}".strip()

    if current:
        pieces.append(current)

    return pieces or [text]


def chunk_markdown(filename: str, raw_text: str) -> ParsedDocument:
    h1_match = _H1_RE.search(raw_text)
    title = h1_match.group(1).strip() if h1_match else filename

    body = raw_text[h1_match.end():] if h1_match else raw_text

    headings = ["Giriş"] + [m.group(1).strip() for m in _H2_SPLIT_RE.finditer(body)]
    sections = _H2_SPLIT_RE.split(body)

    section_texts = [sections[0]] + sections[2::2] if len(sections) > 1 else [body]

    chunks: list[Chunk] = []
    chunk_index = 0
    for heading, section_text in zip(headings, section_texts):
        section_text = section_text.strip()
        if not section_text:
            continue

        pieces = (
            [section_text]
            if len(section_text) <= MAX_CHARS
            else _split_long_section(heading, section_text)
        )

        for piece in pieces:
            prefixed = f"{title} > {heading}\n\n{piece}"
            chunks.append(Chunk(chunk_index=chunk_index, heading=heading, content=prefixed))
            chunk_index += 1

    return ParsedDocument(filename=filename, title=title, chunks=chunks)


def chunk_file(path: Path) -> ParsedDocument:
    raw_text = path.read_text(encoding="utf-8")
    return chunk_markdown(path.name, raw_text)
