# ? Checklist Bezpieczeñstwa - OlmedDataBus Webhooks

## ?? Dla Nowych Deweloperów

Po sklonowaniu repozytorium:

- [ ] Przeczytaj [QUICK-START.md](QUICK-START.md)
- [ ] Uruchom `.\setup-user-secrets.ps1`
- [ ] Zweryfikuj User Secrets: `dotnet user-secrets list`
- [ ] Uruchom aplikacjê: `dotnet run`
- [ ] SprawdŸ logi - powinno byæ po³¹czenie z baz¹ danych

---

## ?? Dla Deployment do Produkcji

Przed deploymentem:

- [ ] Zbuduj aplikacjê: `.\publish-to-iis.ps1`
- [ ] Uruchom `.\setup-production-env.ps1` (jako Administrator)
- [ ] WprowadŸ **produkcyjne** dane (nie DEV!)
- [ ] Zweryfikuj zmienne œrodowiskowe w IIS
- [ ] Restart Application Pool
- [ ] Przetestuj endpoint `/api/webhook`
- [ ] SprawdŸ logi w `Logs/`

---

## ?? Rotacja Kluczy/Hase³

Co 90 dni:

- [ ] Wygeneruj nowe has³a/klucze
- [ ] **Development:** Uruchom `.\setup-user-secrets.ps1` z nowymi wartoœciami
- [ ] **Production:** Uruchom `.\setup-production-env.ps1` z nowymi wartoœciami
- [ ] Restart Application Pool (produkcja)
- [ ] Zweryfikuj dzia³anie aplikacji
- [ ] Zaktualizuj dokumentacjê (jeœli potrzebne)

---

## ?? Weryfikacja Bezpieczeñstwa

Regularnie sprawdzaj:

- [ ] `appsettings.json` **NIE** zawiera wra¿liwych danych
- [ ] `appsettings.Local.json` jest w `.gitignore`
- [ ] User Secrets s¹ skonfigurowane dla wszystkich deweloperów
- [ ] Zmienne œrodowiskowe s¹ ustawione w produkcji
- [ ] Logi **NIE** zawieraj¹ hase³/kluczy
- [ ] Ró¿ne has³a dla DEV/PROD

---

## ?? Troubleshooting

Jeœli aplikacja nie dzia³a:

### Problem: "No connection string configured"

- [ ] Development: Uruchom `.\setup-user-secrets.ps1`
- [ ] Production: Uruchom `.\setup-production-env.ps1`
- [ ] SprawdŸ: `dotnet user-secrets list`

### Problem: "Database connection failed"

- [ ] SprawdŸ dostêp do serwera: `192.168.88.210`
- [ ] Zweryfikuj has³o w User Secrets/Env Variables
- [ ] SprawdŸ logi w `Logs/`

### Problem: "Webhook decryption failed"

- [ ] Zweryfikuj EncryptionKey
- [ ] Zweryfikuj HmacKey
- [ ] SprawdŸ raw data w `WebhookData/raw/`

---

## ?? Dokumentacja

Przeczytaj przed prac¹:

- [ ] [SECURITY-CONFIGURATION.md](SECURITY-CONFIGURATION.md) - Pe³na dokumentacja
- [ ] [QUICK-START.md](QUICK-START.md) - Szybki start
- [ ] [IIS-DEPLOYMENT.md](IIS-DEPLOYMENT.md) - Deployment
- [ ] [SECURITY-IMPLEMENTATION-SUMMARY.md](SECURITY-IMPLEMENTATION-SUMMARY.md) - Podsumowanie zmian

---

## ?? NIGDY NIE:

- [ ] ? Commituj `appsettings.Local.json` do Git
- [ ] ? Udostêpniaj pliku `secrets.json` innym
- [ ] ? U¿ywaj tych samych hase³ w DEV i PROD
- [ ] ? Loguj wra¿liwych danych (has³a, klucze)
- [ ] ? Przechowuj hase³ w plain text w innych plikach

---

## ?? Kontakt

W razie problemów:
- **Dokumentacja:** [SECURITY-CONFIGURATION.md](SECURITY-CONFIGURATION.md)
- **Zespó³ Backend:** [kontakt]
- **Admin Systemu:** [kontakt]

---

**Status:** ? Ready for Production  
**Ostatnia aktualizacja:** 2024
