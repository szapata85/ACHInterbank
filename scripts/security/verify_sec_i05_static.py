#!/usr/bin/env python3
"""SEC-I05 offline source checks. Does not replace dotnet build, nginx -t or UAT."""
import hashlib
import json
from pathlib import Path
import sys

SOURCE_SEED_SHA256 = "5aa78003c215fb9f3315cbf097a593018f65d2b3580e657a306bacbec6ce41e7"  # File fingerprint, not a password hash.

def verify(repo: Path):
    api = (repo / "src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs").read_text(encoding="utf-8-sig")
    security_headers = (repo / "src/Cfa.ACHInterbank.Api/Middleware/SecurityHeadersMiddleware.cs").read_text(encoding="utf-8-sig")
    client_ip = (repo / "src/Cfa.ACHInterbank.Api/Security/RateLimitClientIdentity.cs").read_text(encoding="utf-8-sig")
    program = (repo / "src/Cfa.ACHInterbank.Api/Program.cs").read_text(encoding="utf-8-sig")
    nginx = (repo / "web/ach-interbank-ui/nginx.conf").read_text(encoding="utf-8-sig")
    seed = (repo / "src/Cfa.ACHInterbank.Persistence/Configuration/UserConfiguration.cs").read_bytes()
    results = {
        "seed_file_preserved": hashlib.sha256(seed).hexdigest() == SOURCE_SEED_SHA256,
        "docs_default_off_non_development": 'app.Environment.IsDevelopment()' in api and 'Documentation:EnableInNonDevelopment' in api and 'if (documentationEnabled)' in api,
        "docs_root_redirect_safe": api.count('Redirect(documentationEnabled ? "/scalar" : "/health/live")') == 2,
        "rate_limiter_uses_proxy_allowlist": 'RateLimitClientIdentity.GetClientKey(context)' in api and 'Security:TrustedReverseProxyIps' in client_ip,
        "no_untrusted_rate_limiter_client_header": 'AddressIp.GetAddressIp(context.Request)' not in api,
        "nginx_sanitizes_forwarded_for": '$proxy_add_x_forwarded_for' not in nginx and nginx.count('proxy_set_header X-Forwarded-For $remote_addr;') >= 5,
        "nginx_preserves_upload_limit": 'client_max_body_size 50m;' in nginx,
        "nginx_static_headers_and_dotfile_guard": 'server_tokens off;' in nginx and 'location ~ /\.(?!well-known' in nginx and 'X-Frame-Options "DENY"' in nginx,
        "api_no_hpkp": 'Headers["Public-Key-Pins"]' not in security_headers and 'Headers.Add("Public-Key-Pins"' not in security_headers,
        "api_hsts_only_https": 'context.Request.IsHttps && !_environment.IsDevelopment()' in security_headers,
        "kestrel_hide_version": 'ConfigureKestrel(option => option.AddServerHeader = false)' in program,
        "test_source_created": (repo / "tests/Cfa.ACHInterbank.Tests/RateLimitClientIdentitySecI05Tests.cs").exists(),
    }
    return results

if __name__ == "__main__":
    project = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2]
    checks = verify(project)
    print(json.dumps({"status": "PASS_STATIC" if all(checks.values()) else "FAIL_STATIC", "checks": checks}, indent=2))
    sys.exit(0 if all(checks.values()) else 1)
