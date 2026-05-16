# ✅ PRE-SUBMISSION CHECKLIST - Thermosoft iOS App

Użyj tej listy aby upewnić się że aplikacja jest gotowa do publikacji w App Store.

## 📋 DOKUMENTY I KONTA

- [ ] Konto Apple Developer aktywne ($99/rok zapłacone)
- [ ] Apple ID email zweryfikowany
- [ ] Dwuskładnikowa autentykacja (2FA) włączona na Apple ID
- [ ] Payment method dodany (karta kredytowa dla płatnych aplikacji)

---

## 🔐 CERTYFIKATY I PROFILE

- [ ] Distribution Certificate utworzony i zainstalowany w Keychain na Macu
- [ ] Bundle ID `com.thermosoft.v2` zarejestrowany w Developer Portal
- [ ] App Store Provisioning Profile utworzony i pobrany
- [ ] Provisioning Profile zainstalowany na Macu (double-click .mobileprovision)
- [ ] Certyfikat i profil widoczne w Xcode (Settings → Accounts → Certificates)

**Sprawdź w Terminal:**
```bash
# Lista certyfikatów:
security find-identity -v -p codesigning | grep "Apple Distribution"

# Lista profili:
ls ~/Library/MobileDevice/Provisioning\ Profiles/ | wc -l
```

---

## 📱 KONFIGURACJA PROJEKTU

- [ ] `ApplicationId` = `com.thermosoft.v2` w thermosoft.csproj (linia 27)
- [ ] `ApplicationDisplayVersion` = `1.0` w thermosoft.csproj (linia 30)
- [ ] `ApplicationVersion` = `1` w thermosoft.csproj (linia 31)
- [ ] Info.plist `CFBundleShortVersionString` = `1.0` (linia 31)
- [ ] Info.plist `CFBundleVersion` = `1` (linia 33)
- [ ] Entitlements.plist istnieje w `thermosoft/Platforms/iOS/`
- [ ] Wszystkie używane permissions mają descriptions w Info.plist:
  - [ ] NSCameraUsageDescription (jeśli używasz kamery)
  - [ ] NSPhotoLibraryUsageDescription (jeśli używasz galerii)
  - [ ] NSLocationWhenInUseUsageDescription (jeśli używasz GPS)
  - [ ] NSBluetoothAlwaysUsageDescription (jeśli używasz Bluetooth)
  - [ ] NSLocalNetworkUsageDescription (jeśli używasz sieci lokalnej)

---

## 🎨 ASSETS I GRAFIKA

- [ ] **App Icon** 1024x1024 px (bez alpha channel, PNG lub JPEG)
  - Lokalizacja: `thermosoft/Resources/AppIcon/appicon.svg` lub Assets.xcassets
- [ ] Icon widoczny w App Store Connect po buildzie
- [ ] **Screenshots** przygotowane (min. 2 rozmiary iPhone):
  - [ ] iPhone 6.9" (1320x2868 px) - min. 1 screenshot
  - [ ] iPhone 6.7" (1290x2796 px) - min. 1 screenshot
- [ ] Screenshots pokazują główne funkcje aplikacji
- [ ] Screenshots BEZ tekstu z promises/benefits (zgodnie z App Store Guidelines)
- [ ] **App Preview video** (opcjonalne, max 30s, .mp4 lub .mov)

---

## 📝 APP STORE CONNECT METADATA

- [ ] Aplikacja utworzona w App Store Connect
- [ ] **App Name:** unikalna, dostępna, bez słów kluczowych (max 30 znaków)
- [ ] **Subtitle:** (opcjonalne, max 30 znaków)
- [ ] **Primary Language:** Polish lub English
- [ ] **Description:** napisany, atrakcyjny, opisuje funkcje (max 4000 znaków)
- [ ] **Keywords:** max 100 znaków, oddzielone przecinkami (bez spacji)
- [ ] **Support URL:** działa, zawiera info o wsparciu technicznym
- [ ] **Marketing URL:** (opcjonalne) link do strony produktu
- [ ] **Privacy Policy URL:** WYMAGANE, działa, zawiera politykę prywatności
- [ ] **Category:** Primary i Secondary wybrane poprawnie
- [ ] **Age Rating:** wypełniony kwestionariusz w App Store Connect
- [ ] **Pricing:** Free lub wybrany tier
- [ ] **Availability:** kraje wybrane (All lub konkretne)

---

## 🏗️ BUILD I SIGNING

- [ ] Projekt buduje się lokalnie w Release mode bez błędów:
  ```bash
  dotnet build thermosoft/thermosoft.csproj -f net9.0-ios -c Release
  ```
- [ ] IPA zbudowane z Distribution Certificate:
  ```bash
  dotnet publish thermosoft/thermosoft.csproj \
	-f net9.0-ios -c Release -p:ArchiveOnBuild=true
  ```
- [ ] IPA rozmiar < 4 GB (limit App Store)
- [ ] IPA signed poprawnie (sprawdź w Xcode Organizer lub przez codesign):
  ```bash
  codesign -dvvv ./publish/thermosoft.ipa
  ```
- [ ] Build uploadowany do App Store Connect (przez Xcode/Transporter/altool)
- [ ] Build przeszedł processing (~10 min) i jest widoczny w App Store Connect

---

## ✅ APP STORE CONNECT - SUBMISSION FIELDS

- [ ] **Build selected:** najnowszy build wybrany z listy
- [ ] **What's New in This Version:** Release Notes napisane (co nowego)
- [ ] **Promotional Text:** (opcjonalne, można aktualizować bez review)
- [ ] **App Review Information:**
  - [ ] Imię, Nazwisko
  - [ ] Telefon, Email (dla Apple reviewera)
  - [ ] **Sign-In Required:** jeśli TAK, dodaj demo account (username/password/notes)
  - [ ] **Notes for Reviewer:** (opcjonalne) dodatkowe instrukcje dla reviewera
- [ ] **Version Release:**
  - [ ] Automatically release (default) LUB
  - [ ] Manually release (czekasz na swoje kliknięcie po approval)
- [ ] **Export Compliance:**
  - [ ] "Does your app use encryption?" → No (jeśli tylko HTTPS)
  - [ ] Info.plist ma `ITSAppUsesNonExemptEncryption = false` ✅
- [ ] **Advertising Identifier (IDFA):**
  - [ ] "Does this app use the Advertising Identifier (IDFA)?" → No (jeśli nie używasz reklam)
- [ ] **Content Rights:**
  - [ ] Potwierdzenie że masz prawa do contentu

---

## 🧪 TESTOWANIE

- [ ] Aplikacja przetestowana na prawdziwym urządzeniu iOS (nie tylko symulator)
- [ ] Główne funkcje działają poprawnie
- [ ] Brak crashy podczas podstawowych operacji
- [ ] Permissions (Bluetooth, sieć lokalna) działają poprawnie
- [ ] UI wyświetla się poprawnie na różnych rozmiarach iPhone (Pro Max, Standard, SE)
- [ ] Orientacja ekranu (portrait/landscape) działa zgodnie z założeniami
- [ ] Dark mode wspierany (jeśli deklarowany)
- [ ] Lokalizacje (PL/EN) działają (jeśli multi-language)

---

## 📚 ZGODNOŚĆ Z APP STORE GUIDELINES

- [ ] Aplikacja nie narusza praw autorskich (ikony, grafiki, muzyka)
- [ ] Brak treści obraźliwych/niebezpiecznych
- [ ] Brak ukrytych funkcji lub easter eggs
- [ ] Privacy Policy zawiera info o zbieranych danych (jeśli zbierasz)
- [ ] Jeśli aplikacja wymaga external hardware (termostat), jest to jasno opisane
- [ ] Nie ma duplikatów funkcji iOS (np. nie jest to prosta webview wrapper)
- [ ] Aplikacja nie wymaga jailbreak
- [ ] Brak linków do konkurencyjnych platform (Android)
- [ ] Brak prośby o feedback/reviews wewnątrz aplikacji (tylko przez StoreKit API)

**App Store Review Guidelines:** https://developer.apple.com/app-store/review/guidelines/

---

## 🚀 BEFORE CLICKING "SUBMIT FOR REVIEW"

- [ ] Wszystkie pola w App Store Connect wypełnione (ikony zielone ✅, nie żółte ⚠️)
- [ ] Screenshots i metadata sprawdzone pod kątem literówek
- [ ] Privacy Policy URL działa i jest aktualna
- [ ] Support URL działa
- [ ] Build uploadowany i processing zakończony
- [ ] Demo account (jeśli wymagany) przetestowany
- [ ] Team powiadomiony o submission
- [ ] Harmonogram release (automatic vs manual) wybrany

---

## 📞 EMERGENCY CONTACTS

- **Apple Developer Support:** https://developer.apple.com/support/
- **App Store Connect Help:** https://help.apple.com/app-store-connect/
- **MacinCloud Support:** https://www.macincloud.com/support

---

## 🎯 FINAL CHECK

**Przejrzyj ponownie:**
1. Screenshots (min. 2 rozmiary)
2. Privacy Policy URL (działa!)
3. Demo account credentials (jeśli login required)
4. Release notes (no typos)
5. Build selected (najnowszy)

**Kliknij: Submit for Review** ✅

---

## ⏱️ CO DALEJ

Po submission:
- Status: **Waiting For Review** (1-3 dni)
- Status: **In Review** (1-2 dni)
- Status: **Pending Developer Release** (jeśli manual release)
- Status: **Ready for Sale** 🎉

**Monitoruj:**
- Email notifications od Apple
- App Store Connect → My Apps → Activity
- Resolution Center (jeśli rejection)

---

## 📧 NOTIFICATIONS SETTINGS

Upewnij się że masz włączone notyfikacje email w App Store Connect:
- Users and Access → (twój user) → Notifications
- Zaznacz: **App Status Changes**, **Version Releases**, **Rejections**

---

## 🆘 IF REJECTED

1. Przeczytaj **Resolution Center** message w App Store Connect
2. Zidentyfikuj problem (często: brakujące permission descriptions, metadata issues)
3. Popraw problem w kodzie/metadata
4. Jeśli zmiana w kodzie: zbuduj nowy build, zwiększ `ApplicationVersion` (build number)
5. Upload nowego builda
6. W Resolution Center odpowiedz na Apple reviewer message
7. Kliknij **Resubmit**

Nie panikuj – rejection jest normalny, ~40% aplikacji jest reject przy pierwszej submissji! 😊

---

**Data utworzenia checklisty:** ${new Date().toISOString().split('T')[0]}
**Projekt:** thermosoft (com.thermosoft.v2)
**Target:** iOS App Store
