# Claude Code SDK: Programatik Pipeline Tasarımı

Claude Code terminal tabanlı bir araçtır — ama yalnızca terminal üzerinden kullanılmak zorunda değildir. Claude Code SDK, Claude Code'un yeteneklerini Python veya TypeScript programlarına library olarak entegre etmenizi sağlar. Bu entegrasyonla Claude Code artık bir komut satırı aracı olmaktan çıkar; scriptlerinizin, pipeline'larınızın ve otomasyon sistemlerinizin bir bileşeni haline gelir.

## SDK Ne Zaman Gerekli?

Terminal üzerinden etkileşimli kullanım çoğu geliştirme görevi için yeterlidir. Ama bazı senaryolar programatik kontrolü zorunlu kılar.

Batch işlemler bunların başında gelir. Yüzlerce dosyayı tek tek analiz ettirmek, bir repo'daki tüm endpoint'leri güvenlik açısından taramak, ya da bir doküman setini toplu olarak işlemek — bunları tek tek manuel çalıştırmak yerine bir script ile otomatize etmek SDK'nın birincil kullanım alanıdır.

CI/CD entegrasyonu ikinci büyük kullanım alanıdır. GitHub Actions ya da benzeri bir pipeline'a Claude Code görevleri eklemek — her pull request'te otomatik security audit çalıştırmak, her deploy öncesinde kod kalitesini değerlendirmek — bu entegrasyon SDK olmadan mümkün değildir.

Özel audit pipeline'ları da bu kategoriye girer. Dependency check, secret scanning, OWASP kontrolü gibi güvenlik denetimlerini tek bir script altında birleştirip düzenli aralıklarla çalıştırmak, SDK'nın pratik bir uygulama alanıdır.

## Python ile Temel Kullanım

```python
import anthropic

client = anthropic.Anthropic()

# Basit bir analiz görevi
result = client.claude_code.run(
    prompt="Bu Python dosyasındaki güvenlik açıklarını tespit et",
    files=["src/auth.py"],
    model="claude-sonnet-5"
)

print(result.output)
```

SDK, Claude Code'un agentic loop'unu programatik olarak tetikler. Prompt, dosya bağlamı, model seçimi ve diğer parametreler kod üzerinden tanımlanır; Claude Code görevi yürütür ve sonucu döner.

## Best of N Pattern

SDK'nın en ilginç kullanım örüntülerinden biri Best of N pattern'dir. Aynı görevi birden fazla kez çalıştırıp en iyi sonucu seçmek, deterministik olmayan görevlerde kalite güvencesi sağlar.

```python
results = []
for i in range(3):
    result = client.claude_code.run(
        prompt="Bu modül için test suite yaz",
        files=["src/payment.py"],
        model="claude-sonnet-5"
    )
    results.append(result)

# En kapsamlı test suite'i seç
best = max(results, key=lambda r: len(r.output))
```

Bu pattern özellikle yaratıcı görevlerde — kod üretimi, dokümantasyon yazma, test senaryosu oluşturma — değer üretir. Her çalıştırma farklı bir yaklaşım deneyebilir; en iyi sonucu seçmek otomatik ya da insan değerlendirmesiyle yapılabilir.

## Audit Pipeline Tasarımı

Güvenlik ve kalite denetimlerini SDK ile pipeline'a dönüştürmek somut bir örnek üzerinden açıklanabilir. Bir audit pipeline tipik olarak şu adımlardan oluşur: repo'yu tara, her kontrol kategorisi için ayrı bir Claude Code görevi çalıştır, sonuçları topla, risk matrisini oluştur, raporu yayımla.

Her kontrol kategorisi — dependency vulnerabilities, hardcoded secrets, OWASP Top 10, code quality — bağımsız bir görev olarak tanımlanır. Bu bağımsızlık, kategorilerin paralel çalıştırılmasını mümkün kılar: dört kategori art arda değil aynı anda çalışır, toplam süre en uzun kategorinin süresiyle sınırlı olur.

## GitHub Actions Entegrasyonu

SDK tabanlı bir pipeline'ı CI/CD'ye bağlamak, birkaç satır YAML konfigürasyonuyla mümkündür:

```yaml
- name: Security Audit
  run: python audit_pipeline.py --repo ${{ github.workspace }}
  env:
    ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
```

Bu entegrasyonla her pull request, her merge ya da belirli zaman aralıklarında audit pipeline otomatik tetiklenir. Bulgular pull request yorumu, Slack bildirimi ya da bir dashboard güncellemesi olarak yayımlanabilir.

SDK, Claude Code'u bireysel geliştirici aracından organizasyon genelinde çalışan bir otomasyon bileşenine dönüştürür. Bu dönüşüm, eğitim boyunca öğrenilen tüm katmanların — Skills, Subagents, MCP, Slash Commands — nihai entegrasyon noktasıdır.
