import json
import re
import urllib.error
import urllib.request
from pathlib import Path

import anyio
from claude_agent_sdk import AssistantMessage, ClaudeAgentOptions, TextBlock, query

PROMPT = (
    "API'nin Program.cs dosyasini oku ve kac adet TODO oldugunu soyle."
)

OPTIONS = ClaudeAgentOptions(allowed_tools=["Read"])

CSPROJ_PATH = Path(__file__).resolve().parent.parent / "src" / "DevBlog.Api" / "DevBlog.Api.csproj"
PACKAGE_ID = "Microsoft.EntityFrameworkCore.Sqlite"
NUGET_INDEX_URL = f"https://api.nuget.org/v3-flatcontainer/{PACKAGE_ID.lower()}/index.json"

PROGRAM_CS_PATH = Path(__file__).resolve().parent.parent / "src" / "DevBlog.Api" / "Program.cs"

# "var"/"string" ile atanan, adinda secret/password/key/token gecen ve degeri
# duz bir string literal (config/env okumasi degil) olan degiskenleri yakalar.
SECRET_ASSIGNMENT_PATTERN = re.compile(
    r'(?:var|string)\s+(?P<name>\w*(?:secret|password|apikey|api_key|key|token)\w*)'
    r'\s*=\s*"(?P<value>[^"]+)"\s*;',
    re.IGNORECASE,
)

SECRET_REMEDIATION = (
    "Degeri kaynak koddan cikarip appsettings.json disinda tutun: development icin "
    "'dotnet user-secrets set', production icin ortam degiskeni veya bir secret "
    "manager (ör. Azure Key Vault) kullanip IConfiguration uzerinden okuyun."
)

# check_owasp(): sadece CORS politikasina (AddCors bloguna) bakar.
CORS_BLOCK_PATTERN = re.compile(r"AddCors\s*\(.*?\)\);", re.DOTALL)

CORS_RISK_TOKENS = {
    "AllowAnyOrigin": (
        "high",
        "WithOrigins(...) ile guvenilen origin'leri acikca beyaz listeye alin, "
        "AllowAnyOrigin kullanmayin.",
    ),
    "AllowAnyMethod": (
        "medium",
        "Sadece kullanilan HTTP metotlarini WithMethods(\"GET\", \"POST\", ...) "
        "ile belirtin.",
    ),
    "AllowAnyHeader": (
        "medium",
        "Sadece gereken header'lari WithHeaders(...) ile belirtin.",
    ),
}

# check_seo(): index.html icindeki <title> ve meta description'a bakar.
INDEX_HTML_PATH = Path(__file__).resolve().parent.parent / "devblog-ui" / "src" / "index.html"

TITLE_PATTERN = re.compile(r"<title>(.*?)</title>", re.IGNORECASE | re.DOTALL)
META_DESCRIPTION_PATTERN = re.compile(
    r'<meta\s+name=["\']description["\']\s+content=["\'](.*?)["\']\s*/?>',
    re.IGNORECASE | re.DOTALL,
)


async def count_todos_via_llm() -> None:
    async for message in query(prompt=PROMPT, options=OPTIONS):
        if isinstance(message, AssistantMessage):
            for block in message.content:
                if isinstance(block, TextBlock):
                    print(block.text)


def _find_package_reference(csproj_path: Path, package_id: str) -> tuple[str, int] | None:
    pattern = re.compile(
        rf'<PackageReference\s+Include="{re.escape(package_id)}"\s+Version="([^"]+)"',
        re.IGNORECASE,
    )
    with csproj_path.open(encoding="utf-8") as f:
        for line_number, line in enumerate(f, start=1):
            match = pattern.search(line)
            if match:
                return match.group(1), line_number
    return None


def _latest_stable_version(package_id: str) -> str | None:
    with urllib.request.urlopen(NUGET_INDEX_URL, timeout=10) as response:
        data = json.load(response)
    stable_versions = [v for v in data["versions"] if "-" not in v]
    return stable_versions[-1] if stable_versions else None


def check_dependencies() -> dict:
    reference = _find_package_reference(CSPROJ_PATH, PACKAGE_ID)

    if reference is None:
        return {
            "finding": f"{PACKAGE_ID} paket referansi csproj icinde bulunamadi.",
            "severity": "unknown",
            "file": str(CSPROJ_PATH),
            "line": None,
        }

    current_version, line_number = reference

    try:
        latest_version = _latest_stable_version(PACKAGE_ID)
    except (urllib.error.URLError, KeyError, ValueError) as exc:
        return {
            "finding": f"NuGet'ten guncel surum bilgisi alinamadi: {exc}",
            "severity": "unknown",
            "file": str(CSPROJ_PATH),
            "line": line_number,
        }

    if latest_version is None:
        result = {
            "finding": f"{PACKAGE_ID} icin kararli bir surum bulunamadi.",
            "severity": "unknown",
            "file": str(CSPROJ_PATH),
            "line": line_number,
        }
    elif current_version == latest_version:
        result = {
            "finding": f"{PACKAGE_ID} guncel kararli surumde ({current_version}).",
            "severity": "info",
            "file": str(CSPROJ_PATH),
            "line": line_number,
        }
    else:
        result = {
            "finding": (
                f"{PACKAGE_ID} guncel degil: mevcut surum {current_version}, "
                f"guncel kararli surum {latest_version}."
            ),
            "severity": "medium",
            "file": str(CSPROJ_PATH),
            "line": line_number,
        }

    return result


def detect_hardcoded_secrets() -> list[dict]:
    findings: list[dict] = []

    with PROGRAM_CS_PATH.open(encoding="utf-8") as f:
        for line_number, line in enumerate(f, start=1):
            match = SECRET_ASSIGNMENT_PATTERN.search(line)
            if match is None:
                continue

            var_name = match.group("name")
            findings.append({
                "finding": (
                    f"'{var_name}' degiskeni Program.cs icinde hard-coded bir "
                    f"secret string ile atanmis (kaynak kodla birlikte commitleniyor)."
                ),
                "severity": "high",
                "file": str(PROGRAM_CS_PATH),
                "line": line_number,
                "remediation": SECRET_REMEDIATION,
            })

    return findings


def check_owasp() -> list[dict]:
    """Sadece CORS politikasini (Program.cs'teki AddCors blogu) denetler."""
    findings: list[dict] = []

    with PROGRAM_CS_PATH.open(encoding="utf-8") as f:
        content = f.read()

    block_match = CORS_BLOCK_PATTERN.search(content)
    if block_match is None:
        return [{
            "finding": "Program.cs icinde bir CORS politikasi (AddCors) tanimi bulunamadi.",
            "severity": "medium",
            "file": str(PROGRAM_CS_PATH),
            "line": None,
            "remediation": "CORS politikasini acikca tanimlayip yalnizca guvenilen origin/metot/header'lara izin verin.",
        }]

    block_text = block_match.group(0)
    block_start_line = content[:block_match.start()].count("\n") + 1

    for token, (severity, remediation) in CORS_RISK_TOKENS.items():
        token_match = re.search(re.escape(token), block_text)
        if token_match is None:
            continue

        line_number = block_start_line + block_text[:token_match.start()].count("\n")
        findings.append({
            "finding": f"CORS politikasi '{token}()' kullaniyor; bu asiri gevsek bir CORS ayaridir.",
            "severity": severity,
            "file": str(PROGRAM_CS_PATH),
            "line": line_number,
            "remediation": remediation,
        })

    if not findings:
        findings.append({
            "finding": "CORS politikasinda AllowAnyOrigin/AllowAnyMethod/AllowAnyHeader kullanimi tespit edilmedi.",
            "severity": "info",
            "file": str(PROGRAM_CS_PATH),
            "line": block_start_line,
            "remediation": None,
        })

    return findings


def check_seo() -> list[dict]:
    """index.html icindeki <title> ve meta description'i denetler."""
    findings: list[dict] = []

    with INDEX_HTML_PATH.open(encoding="utf-8") as f:
        content = f.read()

    title_match = TITLE_PATTERN.search(content)
    if title_match is None:
        findings.append({
            "finding": "index.html icinde <title> etiketi bulunamadi.",
            "severity": "high",
            "file": str(INDEX_HTML_PATH),
            "line": None,
            "remediation": "Her sayfa icin benzersiz, aciklayici bir <title> etiketi ekleyin (yaklasik 50-60 karakter).",
        })
    else:
        title_text = title_match.group(1).strip()
        title_line = content[:title_match.start()].count("\n") + 1

        if not title_text:
            findings.append({
                "finding": "<title> etiketi bos.",
                "severity": "high",
                "file": str(INDEX_HTML_PATH),
                "line": title_line,
                "remediation": "Sayfa icerigini yansitan, benzersiz bir title metni ekleyin.",
            })
        elif len(title_text) < 10 or len(title_text) > 60:
            findings.append({
                "finding": (
                    f"<title> metni SEO icin onerilen uzunlukta degil "
                    f"('{title_text}', {len(title_text)} karakter)."
                ),
                "severity": "low",
                "file": str(INDEX_HTML_PATH),
                "line": title_line,
                "remediation": "Title metnini yaklasik 50-60 karakter arasinda, marka + sayfa amaci iceren sekilde genisletin.",
            })

    description_match = META_DESCRIPTION_PATTERN.search(content)
    if description_match is None:
        findings.append({
            "finding": 'index.html icinde <meta name="description"> etiketi bulunamadi.',
            "severity": "medium",
            "file": str(INDEX_HTML_PATH),
            "line": None,
            "remediation": 'Sayfayi 50-160 karakter arasinda ozetleyen bir <meta name="description" content="..."> etiketi ekleyin.',
        })
    else:
        description_text = description_match.group(1).strip()
        description_line = content[:description_match.start()].count("\n") + 1

        if not description_text:
            findings.append({
                "finding": '<meta name="description"> etiketi bos.',
                "severity": "medium",
                "file": str(INDEX_HTML_PATH),
                "line": description_line,
                "remediation": "Aciklayici bir meta description metni ekleyin.",
            })
        elif len(description_text) < 50 or len(description_text) > 160:
            findings.append({
                "finding": f"Meta description SEO icin onerilen uzunlukta degil ({len(description_text)} karakter).",
                "severity": "low",
                "file": str(INDEX_HTML_PATH),
                "line": description_line,
                "remediation": "Meta description'i 50-160 karakter arasina getirin.",
            })

    if not findings:
        findings.append({
            "finding": "title ve meta description SEO en iyi uygulamalarina uygun gorunuyor.",
            "severity": "info",
            "file": str(INDEX_HTML_PATH),
            "line": None,
            "remediation": None,
        })

    return findings


RISK_MATRIX_PATH = Path(__file__).resolve().parent / "risk-matris.md"
SEVERITY_ORDER = ["high", "medium", "low", "info", "unknown"]
REPO_ROOT = Path(__file__).resolve().parent.parent


def _severity_sort_key(severity: str) -> tuple[int, str]:
    severity_lower = severity.lower()
    if severity_lower in SEVERITY_ORDER:
        return (SEVERITY_ORDER.index(severity_lower), severity_lower)
    return (len(SEVERITY_ORDER), severity_lower)


def _markdown_cell(value) -> str:
    if value is None:
        return "-"
    return str(value).replace("|", "\\|").replace("\n", " ")


def _display_path(file_path) -> str:
    try:
        return str(Path(file_path).relative_to(REPO_ROOT))
    except (TypeError, ValueError):
        return str(file_path)


def build_risk_matrix_markdown(results: list[dict]) -> str:
    grouped: dict[str, list[dict]] = {}
    for finding in results:
        severity = (finding.get("severity") or "unknown").lower()
        grouped.setdefault(severity, []).append(finding)

    severities = sorted(grouped, key=_severity_sort_key)

    lines = ["# Risk Matrisi", "", "## Ozet", "", "| Severity | Bulgu Sayisi |", "|---|---|"]
    for severity in severities:
        lines.append(f"| {severity} | {len(grouped[severity])} |")

    lines += ["", "## Detay"]
    for severity in severities:
        lines += [
            "",
            f"### {severity.upper()}",
            "",
            "| Finding | File | Line | Remediation |",
            "|---|---|---|---|",
        ]
        for finding in grouped[severity]:
            lines.append(
                "| {} | {} | {} | {} |".format(
                    _markdown_cell(finding.get("finding")),
                    _markdown_cell(_display_path(finding.get("file"))),
                    _markdown_cell(finding.get("line")),
                    _markdown_cell(finding.get("remediation")),
                )
            )

    return "\n".join(lines) + "\n"


def write_risk_matrix(results: list[dict]) -> Path:
    RISK_MATRIX_PATH.write_text(build_risk_matrix_markdown(results), encoding="utf-8")
    return RISK_MATRIX_PATH


def main() -> list[dict]:
    results: list[dict] = []

    for check in (check_dependencies, detect_hardcoded_secrets, check_owasp, check_seo):
        result = check()
        results.extend(result if isinstance(result, list) else [result])

    print(json.dumps(results, ensure_ascii=False, indent=2))
    write_risk_matrix(results)
    return results


if __name__ == "__main__":
    main()
