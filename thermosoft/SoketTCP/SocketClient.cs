using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sockets.Plugin;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace thermosoft.SoketTCP
{
    static class SocketClient
    {
        public static async Task<string> ReceiveStringTcp(this NetworkStream stream, int maxBytes, CancellationToken ct = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // Czytamy pierwszy bajt, żeby sprawdzić nagłówek 0x81
            byte[] firstByteBuf = new byte[1];
            int r = await stream.ReadAsync(firstByteBuf, 0, 1, ct).ConfigureAwait(false);
            if (r == 0) return null;

            if (firstByteBuf[0] == 0x81)
            {
                // --- LOGIKA NAGŁÓWKA 0x81 (bez zmian) ---
                var headerRest = new byte[3];
                await ReadExactAsync(stream, headerRest, 0, 3, ct).ConfigureAwait(false);
                if (headerRest[0] != 126) throw new InvalidOperationException("Invalid header");

                int length = (headerRest[1] << 8) | headerRest[2];
                if (length < 0 || length > maxBytes) throw new InvalidOperationException("Message too large");

                var payload = new byte[length];
                if (length > 0) await ReadExactAsync(stream, payload, 0, length, ct).ConfigureAwait(false);
                return Encoding.UTF8.GetString(payload);
            }
            else
            {
                // --- ZOPTYMALIZOWANA LOGIKA TEKSTOWA ---
                using var ms = new MemoryStream();
                ms.WriteByte(firstByteBuf[0]);

                byte[] buffer = new byte[8192];

                while (ms.Length < maxBytes)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read == 0) break;

                    // Zapisujemy całą odebraną paczkę naraz do strumienia w pamięci (bardzo szybkie)
                    ms.Write(buffer, 0, read);

                    // Sprawdzamy, czy serwer przysłał znak końca komendy (zazwyczaj '\n')
                    if (Array.IndexOf(buffer, (byte)'\n', 0, read) != -1)
                    {
                        // Jeśli odbieramy ogromne JSONY np na raty i znak uciął się w połowie, 
                        // upewniamy się, czy na pewno serwer nie wysyła teraz jeszcze kawałka
                        if (!stream.DataAvailable)
                        {
                            break;
                        }
                    }
                    else if (!stream.DataAvailable && ms.Length > 0)
                    {
                        // Jeśli strumień zamilkł, nie ma sensu dalej go trzymać
                        // Pozwala to na zwrócenie poprawnego JSONa, który np. nie ma \n na końcu
                        await Task.Delay(10, ct); // Bardzo krótki bufor czasowy
                        if (!stream.DataAvailable) break;
                    }
                }

                // Dekodujemy paczkę z pamięci. 
                // Używamy .Trim(), żeby wyczyścić pozostałości typu \r \n z końca JSONa
                return Encoding.UTF8.GetString(ms.ToArray()).Trim();
            }
        }



        //public static async Task<string> ReceiveStringTcp(this NetworkStream stream, int maxBytes, CancellationToken ct = default)
        //{
        //    if (stream == null) throw new ArgumentNullException(nameof(stream));

        //    // Czytamy pierwszy bajt, żeby sprawdzić nagłówek 0x81
        //    byte[] firstByteBuf = new byte[1];
        //    int r = await stream.ReadAsync(firstByteBuf, 0, 1, ct).ConfigureAwait(false);
        //    if (r == 0) return null;

        //    if (firstByteBuf[0] == 0x81)
        //    {
        //        // --- LOGIKA NAGŁÓWKA 0x81 (bez zmian, jest szybka) ---
        //        var headerRest = new byte[3];
        //        await ReadExactAsync(stream, headerRest, 0, 3, ct).ConfigureAwait(false);
        //        if (headerRest[0] != 126) throw new InvalidOperationException("Invalid header");

        //        int length = (headerRest[1] << 8) | headerRest[2];
        //        if (length < 0 || length > maxBytes) throw new InvalidOperationException("Message too large");

        //        var payload = new byte[length];
        //        if (length > 0) await ReadExactAsync(stream, payload, 0, length, ct).ConfigureAwait(false);
        //        return Encoding.UTF8.GetString(payload);
        //    }
        //    else
        //    {
        //        // --- TURBO LOGIKA TEKSTOWA (Obsługa \r i \n bez opóźnień) ---
        //        using var ms = new MemoryStream();
        //        ms.WriteByte(firstByteBuf[0]);

        //        byte[] buffer = new byte[4096]; // Czytamy w dużych kawałkach dla prędkości
        //        while (ms.Length < maxBytes)
        //        {
        //            // Czytamy co jest dostępne w danej chwili
        //            int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
        //            if (read == 0) break;

        //            for (int i = 0; i < read; i++)
        //            {
        //                byte b = buffer[i];
        //                // Kluczowe: Reagujemy natychmiast na \r lub \n
        //                if (b == (byte)'\r' || b == (byte)'\n')
        //                {
        //                    // Zwracamy to co mamy, ignorując resztę bufora (działa przy GetAsyncTcp2, 
        //                    // bo i tak zamykasz połączenie po tej metodzie).
        //                    return Encoding.UTF8.GetString(ms.ToArray());
        //                }
        //                ms.WriteByte(b);
        //            }
        //        }
        //        return Encoding.UTF8.GetString(ms.ToArray());
        //    }
        //}

        private static async Task ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException();
                totalRead += read;
            }
        }



        public static Task SendStringTcp(this NetworkStream c, string s)
        {

            var msgBytes = Encoding.UTF8.GetBytes(s);
            return c.WriteAsync(msgBytes, 0, msgBytes.Length);

        }



    }
}
