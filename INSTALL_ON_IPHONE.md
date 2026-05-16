# INSTRUKCJA: Instalacja aplikacji na iPhone

## 📱 METODA PRZEZ DIAWI.COM (NAJSZYBSZA)

### Wymagania:
- ✅ MacinCloud (masz: FF338.macincloud.com)
- ✅ iPhone podłączony USB do Maca LUB WiFi w tej samej sieci
- ✅ Apple ID (darmowy)

---

## KROK PO KROKU:

### 1. Połącz się z MacinCloud przez RDP

**Na Windows:**
```
Windows + R → mstsc
Computer: FF338.macincloud.com:6000
User: user297455
Password: sfj55879xfn
```

---

### 2. Sklonuj projekt na Macu (jeśli jeszcze nie)

**Na Macu otwórz Terminal:**
```bash
cd ~/Desktop
git clone https://github.com/marekpodwika-ui/thermosoft_smart.git
cd thermosoft_smart
```

---

### 3. Pobierz UDID iPhone

**Opcja A: Przez USB**
```bash
# Podłącz iPhone kablem USB do Maca
# Trust device na iPhone

# Sprawdź listę urządzeń:
xcrun xctrace list devices
```

**Opcja B: Przez Settings na iPhone**
```
Settings → General → About → skopiuj identyfikator (długi string)
```

Przykład UDID:
```
00008030-000A1234567890AB
```

---

### 4. Zbuduj IPA

**Terminal na Macu:**
```bash
cd ~/Desktop/thermosoft_smart

# Nadaj uprawnienia skryptowi:
chmod +x BUILD_IPA_FOR_IPHONE.sh

# Uruchom skrypt:
./BUILD_IPA_FOR_IPHONE.sh
```

**LUB ręcznie:**
```bash
dotnet publish thermosoft/thermosoft.csproj \
  -f net9.0-ios \
  -c Debug \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:ArchiveOnBuild=true \
  -p:CodesignKey="iPhone Developer" \
  -o ./ipa_output
```

---

### 5. Upload IPA na Diawi.com

1. **Na Macu** otwórz przeglądarkę
2. Wejdź na: https://www.diawi.com/
3. **Przeciągnij plik IPA** (z folderu `ipa_output`)
4. Kliknij **Upload**
5. Poczekaj 1-2 minuty
6. **Skopiuj link** (np. `https://i.diawi.com/abc123`)

---

### 6. Zainstaluj na iPhone

**Na iPhone:**
1. Otwórz **Safari** (MUSI być Safari, nie Chrome!)
2. Wklej link z Diawi
3. Kliknij **Install**
4. Potwierdź instalację
5. Po instalacji: **Settings → General → VPN & Device Management**
6. Kliknij na swój Apple ID
7. Kliknij **Trust**

✅ **Aplikacja zainstalowana!**

---

## ⚠️ PROBLEMY I ROZWIĄZANIA

### "Untrusted Enterprise Developer"
**Rozwiązanie:**
```
Settings → General → VPN & Device Management
→ Twój Apple ID → Trust
```

### "Unable to Install"
**Przyczyny:**
1. UDID iPhone nie jest dodany do provisioning profile
2. Certyfikat wygasł
3. Link Diawi wygasł (1 dzień)

**Rozwiązanie:**
- Zbuduj ponownie IPA z poprawnym UDID
- Upload ponownie na Diawi

### USB nie działa przez RDP
**Rozwiązanie:**
Użyj **WiFi Deploy**:
```bash
# Na Macu w Xcode:
Window → Devices and Simulators
→ Wybierz iPhone → Connect via network
```

---

## 🚀 ALTERNATYWY

### TestFlight (dla długoterminowego testowania)
1. Zbuduj signed IPA (Release)
2. Upload do App Store Connect
3. Dodaj siebie jako Internal Tester
4. Zainstaluj przez TestFlight app

**Zalety:**
- ✅ Nie wygasa po 1 dniu
- ✅ Automatyczne updatey
- ✅ Do 100 testerów

**Wady:**
- ❌ Wymaga Apple Developer ($99/rok)
- ❌ Processing ~10 minut per build

---

### Xcode Direct Deploy (jeśli USB/WiFi działa)
1. Otwórz projekt w Xcode
2. Wybierz iPhone jako target
3. Cmd+R (Run)

**Zalety:**
- ✅ Instant deployment
- ✅ Debugging

**Wady:**
- ❌ Wymaga USB lub WiFi w tej samej sieci

---

## 📋 CHECKLIST

- [ ] RDP połączony z MacinCloud
- [ ] Projekt sklonowany na Macu
- [ ] .NET 9 SDK zainstalowany
- [ ] MAUI workload zainstalowany
- [ ] UDID iPhone uzyskany
- [ ] IPA zbudowany
- [ ] IPA uploadowany na Diawi
- [ ] Link otwarty na iPhone w Safari
- [ ] Developer trusted na iPhone
- [ ] Aplikacja uruchomiona

---

## 🆘 POMOC

Jeśli coś nie działa:
1. Sprawdź logi buildu w Terminal
2. Sprawdź czy iPhone jest w trusted devices
3. Sprawdź czy certyfikat jest ważny
4. Napisz do MacinCloud support jeśli USB nie działa

**Sukcesu z instalacją!** 🎉
