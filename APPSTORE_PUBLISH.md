# 📱 Publikacja thermosoft w Apple App Store

Kompletny przewodnik publikacji aplikacji .NET MAUI w Apple App Store.

---

## ✅ CHECKLIST PRZED ROZPOCZĘCIEM

- [ ] Konto Apple Developer Program aktywne ($99/rok)
- [ ] Bundle ID `com.thermosoft.v2` zarejestrowany w Apple Developer Portal
- [ ] Dostęp do MacinCloud (FF338.macincloud.com:6000 przez RDP)
- [ ] Aplikacja przetestowana i gotowa do publikacji
- [ ] Privacy Policy opublikowana online (wymagane przez App Store)

---

## KROK 1: Certyfikaty i Provisioning Profiles

### 1.1 Utwórz Distribution Certificate

1. Wejdź na: https://developer.apple.com/account/resources/certificates/list
2. Kliknij **+** (Create a Certificate)
3. Wybierz **Apple Distribution**
4. Kliknij **Continue**

5. **Na MacinCloud (przez RDP):**
   ```bash
   # Otwórz Terminal na Macu
   cd ~/Desktop

   # Wygeneruj Certificate Signing Request (CSR)
   # Keychain Access → Certificate Assistant → Request a Certificate from a Certificate Authority
   # Email: twój Apple ID email
   # Common Name: Thermosoft Distribution
   # Save to disk: CertificateSigningRequest.certSigningRequest
   ```

6. Wróć do przeglądarki → Upload CSR file
7. Download certyfikatu (distribution.cer)
8. **Na Macu:** Podwójnie kliknij distribution.cer → zainstaluje się w Keychain

### 1.2 Zarejestruj Bundle ID (jeśli jeszcze nie ma)

1. Wejdź na: https://developer.apple.com/account/resources/identifiers/list
2. Kliknij **+**
3. Wybierz **App IDs** → Continue
4. Type: **App**
5. Description: `Thermosoft Smart`
6. Bundle ID: `com.thermosoft.v2` (EXPLICIT)
7. Capabilities: zaznacz wymagane (np. Push Notifications, Bluetooth, jeśli używasz)
8. Kliknij **Continue** → **Register**

### 1.3 Utwórz App Store Provisioning Profile

1. Wejdź na: https://developer.apple.com/account/resources/profiles/list
2. Kliknij **+**
3. Wybierz **App Store** → Continue
4. App ID: wybierz `com.thermosoft.v2`
5. Certificate: wybierz swój Distribution certificate
6. Profile Name: `Thermosoft AppStore`
7. Download profilu (.mobileprovision)
8. **Na Macu:** Podwójnie kliknij profil → zainstaluje się automatycznie

---

## KROK 2: App Store Connect Setup

### 2.1 Utwórz aplikację w App Store Connect

1. Wejdź na: https://appstoreconnect.apple.com/apps
2. Kliknij **+** → **New App**
3. Wypełnij:
   - **Platforms:** iOS
   - **Name:** Thermosoft Smart (lub inna nazwa do wyświetlenia)
   - **Primary Language:** Polish lub English
   - **Bundle ID:** com.thermosoft.v2
   - **SKU:** THERMOSOFT001 (unikalny ID, dowolny)
   - **User Access:** Full Access

### 2.2 Wypełnij App Information

**App Information (lewy panel):**
- **Name:** Thermosoft Smart
- **Subtitle:** (opcjonalne, max 30 znaków)
- **Privacy Policy URL:** `https://twoja-domena.pl/privacy-policy` ⚠️ **WYMAGANE**
- **Category:** Primary: Utilities, Secondary: Productivity (dostosuj do swojej aplikacji)
- **Content Rights:** (jeśli masz prawa do contentu)

**Pricing and Availability:**
- **Price:** Free lub wybierz tier
- **Availability:** All countries lub wybierz konkretne

### 2.3 Przygotuj App Screenshots i Metadata

⚠️ **WYMAGANE** screenshoty dla:
- **iPhone 6.9" (iPhone 16 Pro Max):** min. 1 screenshot
- **iPhone 6.7" (iPhone 15 Pro Max):** min. 1 screenshot (backup dla starszych iOS)

**Zalecane rozmiary:**
- 6.9": 1320 x 2868 px
- 6.7": 1290 x 2796 px
- 6.5": 1284 x 2778 px
- 5.5": 1242 x 2208 px

**Jak zrobić screenshoty:**
1. **Na MacinCloud:** uruchom Xcode
2. Otwórz Simulator: `Xcode → Open Developer Tool → Simulator`
3. Wybierz **iPhone 16 Pro Max** z menu
4. Uruchom aplikację
5. `Cmd + S` → zapisz screenshot
6. Powtórz dla **iPhone 15 Pro Max**

**App Preview (opcjonalne):**
- Nagranie wideo aplikacji (max 30s)

**Description:**
```
PL:
Thermosoft Smart to aplikacja do sterowania inteligentnym termostatem...
[Opisz funkcje, korzyści, instrukcje]

EN:
Thermosoft Smart is an app for controlling smart thermostats...
```

**Keywords:** (max 100 znaków)
```
thermostat,smart home,heating,temperature,control
```

**Support URL:** `https://twoja-domena.pl/support`

**Marketing URL (opcjonalne):** `https://twoja-domena.pl`

---

## KROK 3: Build Signed IPA

### Opcja A: Build lokalnie na MacinCloud (POLECANE)

1. **Połącz się przez RDP:** `FF338.macincloud.com:6000`
2. **Sklonuj repo na Macu:**
   ```bash
   cd ~/Desktop
   git clone https://github.com/marekpodwika-ui/thermosoft_smart.git
   cd thermosoft_smart
   ```

3. **Zainstaluj .NET 9 SDK (jeśli nie ma):**
   ```bash
   # Download z: https://dotnet.microsoft.com/download/dotnet/9.0
   # Lub:
   brew install dotnet-sdk
   ```

4. **Zainstaluj MAUI workload:**
   ```bash
   dotnet workload install maui-ios
   ```

5. **Zbuduj Release IPA:**
   ```bash
   dotnet publish thermosoft/thermosoft.csproj \
	 -f net9.0-ios \
	 -c Release \
	 -p:RuntimeIdentifier=ios-arm64 \
	 -p:ArchiveOnBuild=true \
	 -p:CodesignKey="Apple Distribution" \
	 -p:CodesignProvision="Thermosoft AppStore" \
	 -o ./publish
   ```

6. **Sprawdź wynik:**
   ```bash
   ls -lh ./publish/*.ipa
   # Powinien pojawić się plik: thermosoft.ipa
   ```

### Opcja B: Build przez Xcode (alternatywa)

1. **Otwórz Xcode na MacinCloud**
2. **File → Open → wybierz** `thermosoft.sln` lub folder projektu
3. Xcode może otworzyć projekt .NET MAUI przez rozszerzenie
4. Wybierz **Any iOS Device (arm64)** jako target
5. **Product → Archive**
6. Poczekaj na build
7. **Distribute App → App Store Connect → Upload**

---

## KROK 4: Upload IPA do App Store Connect

### Metoda 1: Przez Xcode Organizer (najłatwiejsza)

1. **Xcode → Window → Organizer**
2. W zakładce **Archives** znajdź swój build
3. Kliknij **Distribute App**
4. Wybierz **App Store Connect**
5. Wybierz **Upload**
6. Wybierz Distribution certificate i Provisioning Profile
7. Kliknij **Upload**
8. Poczekaj (może zająć 5-30 minut)

### Metoda 2: Przez Transporter app

1. Otwórz **Transporter** (preinstalowany na macOS, lub pobierz z Mac App Store)
2. Zaloguj się swoim Apple ID
3. Przeciągnij plik `.ipa` do okna Transporter
4. Kliknij **Deliver**
5. Poczekaj na upload

### Metoda 3: Przez altool (CLI)

```bash
xcrun altool --upload-app \
  --type ios \
  --file ./publish/thermosoft.ipa \
  --username "twoj-apple-id@example.com" \
  --password "app-specific-password"
```

⚠️ Potrzebujesz **App-Specific Password** z https://appleid.apple.com

---

## KROK 5: Przygotuj build do Review

1. Wróć do **App Store Connect:** https://appstoreconnect.apple.com/apps
2. Wybierz swoją aplikację
3. Kliknij **+ Version** lub **Prepare for Submission**
4. **Build:** wybierz ostatnio uploadowany build (pojawi się po ~10 min)
5. Wypełnij **What's New in This Version** (Release Notes):
   ```
   Pierwsza wersja aplikacji Thermosoft Smart.
   - Sterowanie termostatem
   - Monitorowanie temperatury
   - Harmonogramy grzewcze
   ```

6. **App Review Information:**
   - **First Name / Last Name:** Twoje dane
   - **Phone / Email:** Kontakt dla Apple reviewera
   - **Sign-In Required:** Jeśli TAK, podaj demo account:
	 - Username: `demo@example.com`
	 - Password: `Demo1234!`
	 - Notes: "Demo account for review purposes"

7. **Version Release:** (wybierz jedną):
   - **Automatically release** (od razu po zatwierdzeniu)
   - **Manually release** (czekasz na Twoje kliknięcie)

8. **Export Compliance:** (encryption):
   - Jeśli NIE używasz custom encryption: **No**
   - Dodaj w Info.plist: `ITSAppUsesNonExemptEncryption = false` (już dodane)

9. **Advertising Identifier (IDFA):**
   - Jeśli NIE używasz reklam: **No**

10. **Content Rights:**
	- Zaznacz że masz prawa do contentu w aplikacji

---

## KROK 6: Submit for Review

1. **Sprawdź wszystkie pola** – muszą być wypełnione (ikony zielone, nie żółte)
2. Kliknij **Submit for Review** (prawy górny róg)
3. Potwierdź submission

**Status zmiany:**
- **Waiting For Review** → oczekiwanie w kolejce (1-3 dni)
- **In Review** → Apple testuje aplikację (1-2 dni)
- **Pending Developer Release** → zatwierdzona, czeka na Twoje wypuszczenie (jeśli wybrałeś manual release)
- **Ready for Sale** → dostępna w App Store! 🎉

**Rejection:**
Jeśli Apple odrzuci:
- Przeczytaj **Resolution Center** message w App Store Connect
- Popraw problemy
- Przebuduj i upload nowy build
- Submit ponownie

---

## 🚨 TYPOWE PROBLEMY I ROZWIĄZANIA

### 1. "Missing compliance" warning
**Rozwiązanie:** W Info.plist dodaj:
```xml
<key>ITSAppUsesNonExemptEncryption</key>
<false/>
```
(już dodane w projekcie)

### 2. "Missing screenshots"
**Rozwiązanie:** Dodaj min. 1 screenshot dla każdego wymaganego rozmiaru w App Store Connect.

### 3. "Invalid provisioning profile"
**Rozwiązanie:**
```bash
# Na Macu sprawdź zainstalowane profile:
ls ~/Library/MobileDevice/Provisioning\ Profiles/

# Usuń stare:
rm ~/Library/MobileDevice/Provisioning\ Profiles/*

# Pobierz świeże profile z Developer Portal i zainstaluj ponownie
```

### 4. "Code signing failed"
**Rozwiązanie:**
```bash
# Sprawdź dostępne certyfikaty:
security find-identity -v -p codesigning

# Powinien być: "Apple Distribution: Twoje Imię (TEAM_ID)"
# Jeśli nie ma, zainstaluj distribution.cer ponownie
```

### 5. "Missing Privacy Policy URL"
**Rozwiązanie:** Stwórz prostą stronę HTML z privacy policy i wgraj na swój serwer lub użyj GitHub Pages:
```
https://twoj-username.github.io/thermosoft-privacy-policy
```

### 6. Build failed w Xcode
**Rozwiązanie:**
```bash
# Wyczyść build cache:
dotnet clean
rm -rf thermosoft/bin thermosoft/obj

# Rebuild:
dotnet build thermosoft/thermosoft.csproj -f net9.0-ios -c Release
```

---

## 📊 TIMELINE

| Etap | Czas |
|------|------|
| Setup certyfikatów i profiles | 1-2 godziny |
| App Store Connect metadata | 2-3 godziny |
| Przygotowanie screenshots | 1-2 godziny |
| Build i upload IPA | 30 minut - 2 godziny |
| **Waiting For Review** | **1-3 dni** |
| **In Review** | **1-2 dni** |
| **Total do publikacji** | **~3-7 dni** |

---

## 🔗 PRZYDATNE LINKI

- **Apple Developer Portal:** https://developer.apple.com/account/
- **App Store Connect:** https://appstoreconnect.apple.com/
- **App Store Review Guidelines:** https://developer.apple.com/app-store/review/guidelines/
- **Human Interface Guidelines:** https://developer.apple.com/design/human-interface-guidelines/
- **.NET MAUI iOS Deployment:** https://learn.microsoft.com/en-us/dotnet/maui/ios/deployment/
- **MacinCloud Support:** https://www.macincloud.com/support

---

## ✅ FINAL CHECKLIST

Przed submissją sprawdź:

- [ ] Wszystkie screenshoty uploaded (min. 2 rozmiary)
- [ ] Privacy Policy URL działa
- [ ] Support URL działa
- [ ] Description wypełniony (PL i/lub EN)
- [ ] Keywords dodane
- [ ] Category wybrane
- [ ] Pricing ustawiony
- [ ] Build uploadowany i wybrany
- [ ] Release notes napisane
- [ ] Export Compliance wypełnione
- [ ] Demo account (jeśli wymagany login) dodany
- [ ] Wszystkie permission descriptions w Info.plist (jeśli używasz kamery/lokalizacji/bluetooth)
- [ ] Ikona aplikacji 1024x1024 px uploaded
- [ ] Testowane na prawdziwym urządzeniu iOS

---

## 🎉 GRATULACJE!

Po zatwierdzeniu przez Apple Twoja aplikacja będzie dostępna w App Store na całym świecie! 🚀

**Monitoruj:**
- App Analytics w App Store Connect
- Crashlytics/diagnostykę
- Opinie użytkowników w App Store

**Aktualizacje:**
- Zwiększ `ApplicationDisplayVersion` w csproj (np. 1.0 → 1.1)
- Zwiększ `ApplicationVersion` (build number) (np. 1 → 2)
- Zbuduj nowy IPA
- Upload do App Store Connect
- Submit nową wersję do review
