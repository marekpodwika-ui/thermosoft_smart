#!/bin/bash
# BUILD_IPA_FOR_IPHONE.sh
# Skrypt do budowania IPA dla instalacji na iPhone przez Diawi.com

echo "═══════════════════════════════════"
echo "  BUILD IPA DLA iPhone (Ad-Hoc)    "
echo "═══════════════════════════════════"
echo ""

# 1. Sprawdź czy jesteś na Macu
if [[ "$OSTYPE" != "darwin"* ]]; then
  echo "❌ Ten skrypt działa tylko na macOS!"
  exit 1
fi

# 2. Sprawdź czy .NET jest zainstalowany
if ! command -v dotnet &> /dev/null; then
  echo "❌ .NET SDK nie jest zainstalowany!"
  echo "Pobierz z: https://dotnet.microsoft.com/download/dotnet/9.0"
  exit 1
fi

echo "✅ .NET SDK: $(dotnet --version)"

# 3. Sprawdź czy MAUI workload jest zainstalowany
if ! dotnet workload list | grep -q "maui-ios"; then
  echo "⚠️  MAUI iOS workload nie jest zainstalowany"
  echo "Instaluję..."
  dotnet workload install maui-ios
fi

# 4. Pobierz UDID iPhone
echo ""
echo "═══ KROK 1: Pobierz UDID iPhone ═══"
echo "Podłącz iPhone kablem USB i wprowadź Trust na telefonie"
echo ""
read -p "Naciśnij Enter gdy iPhone jest podłączony..."

# Lista urządzeń
xcrun xctrace list devices 2>&1 | grep -v "Simulator"

echo ""
echo "Skopiuj UDID iPhone (długi string hex)"
read -p "Wprowadź UDID: " UDID

if [ -z "$UDID" ]; then
  echo "❌ UDID jest wymagany!"
  exit 1
fi

echo "✅ UDID: $UDID"

# 5. Build IPA
echo ""
echo "═══ KROK 2: Budowanie IPA ═══"
echo "Projekt: thermosoft/thermosoft.csproj"
echo "Target: iOS (Debug, Ad-Hoc)"
echo ""

dotnet publish thermosoft/thermosoft.csproj \
  -f net9.0-ios \
  -c Debug \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:ArchiveOnBuild=true \
  -p:CodesignKey="iPhone Developer" \
  -p:CodesignProvision=Automatic \
  -p:IpaPackageDir="$(pwd)/ipa_output" \
  --verbosity minimal

if [ $? -eq 0 ]; then
  echo ""
  echo "✅ IPA ZBUDOWANY!"
  echo "═══════════════════════════════════"

  # Znajdź IPA
  IPA_FILE=$(find ipa_output -name "*.ipa" | head -n 1)

  if [ -f "$IPA_FILE" ]; then
	IPA_SIZE=$(du -h "$IPA_FILE" | cut -f1)
	echo "📦 Plik: $IPA_FILE"
	echo "📏 Rozmiar: $IPA_SIZE"
	echo ""
	echo "═══ KROK 3: Upload do Diawi ═══"
	echo "1. Wejdź na: https://www.diawi.com/"
	echo "2. Przeciągnij plik: $IPA_FILE"
	echo "3. Kliknij Upload"
	echo "4. Otrzymasz link (np. https://i.diawi.com/abc123)"
	echo "5. Otwórz link na iPhone w Safari → Install"
	echo ""
	echo "⚠️  Link wygasa po 1 dniu (free tier)"
	echo "═══════════════════════════════════"

	# Otwórz folder z IPA
	open ipa_output
  else
	echo "❌ Nie znaleziono pliku IPA!"
	echo "Sprawdź output powyżej pod kątem błędów."
  fi
else
  echo ""
  echo "❌ BUILD FAILED!"
  echo "Sprawdź błędy powyżej."
  exit 1
fi
