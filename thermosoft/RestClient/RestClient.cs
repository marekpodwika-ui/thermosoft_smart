using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.IO;
using thermosoft.Models;
using thermosoft.SoketTCP;
using System.Net.WebSockets;
using System.ComponentModel;
using System.Threading;
using System.Net.Mail;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace thermosoft.RestClient
{
    public class RestClientBase
    {
        public static string? WebSocketUrl;
        public static string? SerwerWebSocked___;
        public static string? Adres___;
        public static int Port__;
        public static string? Haslo___;
        public static uint Topic___;

        public static byte ErrorWebsocket;
        public static byte FlagZajetosci;
        protected static int _activeRequestsCount = 0;

        protected const int WEBSOCKET_TIMEOUT_MS = 5000;
        protected const int WEBSOCKET_HISTORY_TIMEOUT_MS = 5000;
        protected const int TCP_CONNECT_TIMEOUT_MS = 5000;

        public static ClientWebSocket? ws = null;
        protected static readonly SemaphoreSlim _wsLock = new SemaphoreSlim(1, 1);
        public static bool IsAppInForeground = true;
        public static DateTime LastResumeTime = DateTime.MinValue;
    }

    public class RestClient<T> : RestClientBase
    {
        public byte ErrorStatus()
        {
            return ErrorWebsocket;
        }

        private void EnterBusyState()
        {
            Interlocked.Increment(ref _activeRequestsCount);
            FlagZajetosci = 1;
        }

        private void LeaveBusyState()
        {
            if (Interlocked.Decrement(ref _activeRequestsCount) <= 0)
            {
                _activeRequestsCount = 0; // Zabezpieczenie przed zejœciem poni¿ej zera
                FlagZajetosci = 0;
            }
        }

        private static async Task<string> ReceiveFullMessageAsync(ClientWebSocket webSocket, CancellationToken token)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new Exception("WebSocket zamkniêty przez serwer.");
                }
                if (buffer.Array != null && result.Count > 0)
                {
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
            } while (!result.EndOfMessage && !token.IsCancellationRequested);

            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms, Encoding.UTF8);
            return await reader.ReadToEndAsync(token); // W .NET MAUI / .NET 9 to dzia³a
        }

        public async void PingWebsocket()
        {
            if (ws == null || ws.State == WebSocketState.Closed || FlagZajetosci == 1)
            {
                return;
            }

            if (!_wsLock.Wait(0)) return; // Jeœli inny w¹tek u¿ywa gniazda, pomijamy ping, by nie zak³ócaæ odbioru

            try
            {
                var data = "{\"t\":8}";
                var encoded = Encoding.UTF8.GetBytes(data);
                var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

                using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                {
                    await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
                }

                if (ws?.State == WebSocketState.Open)
                {
                    using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                    {
                        var str = await ReceiveFullMessageAsync(ws, cts.Token);
                        if (str.Contains("{\"t\":9}")) return;
                    }
                }
            }
            catch
            {
                ws?.Abort();
                ws?.Dispose();
                ws = null;
            }
            finally
            {
                _wsLock.Release();
            }
        }

        public async Task CloseWebsocket()
        {
            await _wsLock.WaitAsync();
            try
            {
                if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived))
                {
                    using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cts.Token);
                    }
                }
            }
            catch { }
            finally
            {
                ws?.Abort();
                ws?.Dispose();
                ws = null;
                _wsLock.Release();
            }
        }

        public async Task OpenWebsocket()
        {
            await _wsLock.WaitAsync();
            try
            {
                int flagaPolaczenia = 0;
                if (ws == null) ws = new ClientWebSocket();

                if (ws.State == WebSocketState.Open)
                {
                    flagaPolaczenia = 0;
                }
                else
                {
                    ws?.Abort();
                    ws?.Dispose();
                    ws = new ClientWebSocket();

                    if (string.IsNullOrEmpty(WebSocketUrl)) return;

                    using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                    {
                        await ws.ConnectAsync(new Uri(WebSocketUrl), cts.Token);
                    }
                    flagaPolaczenia = 1;
                }

                if (ws?.State == WebSocketState.Open && flagaPolaczenia == 1)
                {
                    using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                    {
                        var str = await ReceiveFullMessageAsync(ws, cts.Token);
                        string[] separatingStrings = { "\"tsid\":", "}}" };
                        var words = str.Split(separatingStrings, StringSplitOptions.RemoveEmptyEntries);

                        if (words.Length > 1 && uint.TryParse(words[1].ToString(), out uint topic))
                        {
                            Topic___ = topic;
                        }
                    }

                    var data = "{\"t\":1,\"d\":{\"topic\":\"proxy:" + Topic___ + "\"}}";
                    var encoded = Encoding.UTF8.GetBytes(data);
                    var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

                    using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                    {
                        await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
                        await ReceiveFullMessageAsync(ws, cts.Token); // Odbieramy odpowiedŸ by oczyœciæ bufor
                    }
                }
            }
            catch
            {
                ws?.Abort();
                ws?.Dispose();
                ws = null;
            }
            finally
            {
                _wsLock.Release();
            }
        }

        public async Task<ObservableCollection<T>?> GetAsyncWs()
        {
            EnterBusyState();

            if (ws == null || ws.State == WebSocketState.Closed)
            {
                await OpenWebsocket();
            }

            await _wsLock.WaitAsync();
            try
            {
                if (ws?.State != WebSocketState.Open)
                {
                    ErrorWebsocket = 1;
                    return null;
                }

                var data = "{\"t\":7,\"d\":{\"topic\":\"proxy:" + Topic___ + "\",\"event\":\"message\",\"data\":{\"to\":\"" + SerwerWebSocked___ + "\",\"from\":\"" + Topic___ + "\",\"data\":{\"Ha\":\"" + Haslo___ + "\",\"Id\":250,\"Za\":\"0\",\"Ko\":0}}}}";
                var encoded = Encoding.UTF8.GetBytes(data);
                var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

                using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                {
                    await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
                }

                using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                {
                    var str = await ReceiveFullMessageAsync(ws, cts.Token);

                    if (str.Contains("endpoint not found"))
                    {
                        ErrorWebsocket = 2;
                        return null;
                    }

                    string[] separatingStrings = { "[", "]" };
                    var words = str.Split(separatingStrings, StringSplitOptions.RemoveEmptyEntries);

                    if (words.Length > 1)
                    {
                        str = "[" + words[1] + "]";
                        var json = JsonConvert.DeserializeObject<ObservableCollection<T>>(str);
                        ErrorWebsocket = 0;
                        return json;
                    }
                }
            }
            catch (Exception)
            {
                ErrorWebsocket = 1;
                ws?.Abort();
                ws?.Dispose();
                ws = null;
            }
            finally
            {
                LeaveBusyState();
                _wsLock.Release();
            }

            return null;
        }

        public async Task<Employee?> GetAsyncSelektWs(Employee t)
        {
            EnterBusyState();

            if (ws == null || ws.State == WebSocketState.Closed)
            {
                await OpenWebsocket();
            }

            await _wsLock.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(t);

                if (ws?.State != WebSocketState.Open)
                {
                    ErrorWebsocket = 1;
                    return null;
                }

                var data = "{\"t\":7,\"d\":{\"topic\":\"proxy:" + Topic___ + "\",\"event\":\"message\",\"data\":{\"to\":\"" + SerwerWebSocked___ + "\",\"from\":\"" + Topic___ + "\",\"data\":[{\"Ha\":\"" + Haslo___ + "\"}," + json + "]}}}";
                var encoded = Encoding.UTF8.GetBytes(data);
                var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

                using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                {
                    await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
                }

                using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                {
                    var str = await ReceiveFullMessageAsync(ws, cts.Token);

                    if (str.Contains("endpoint not found"))
                    {
                        ErrorWebsocket = 2;
                        return null;
                    }

                    // 1. Jeœli znajdujemy tablicê (autoryzacja + dane)
                    int kwadratStart = str.IndexOf('[');
                    int kwadratStop = str.LastIndexOf(']');

                    if (kwadratStart != -1 && kwadratStop != -1 && kwadratStop > kwadratStart)
                    {
                        try
                        {
                            var tablica = str.Substring(kwadratStart, kwadratStop - kwadratStart + 1);
                            var rootToken = JToken.Parse(tablica);

                            if (rootToken.Type == JTokenType.Array)
                            {
                                var e = rootToken.Count() > 1 
                                    ? rootToken[1].ToObject<Employee>() 
                                    : rootToken[0].ToObject<Employee>();

                                ErrorWebsocket = 0;
                                return e;
                            }
                        }
                        catch { } // Gdy zawiedzie, przechodzi do starej metody
                    }

                    // 2. Jeœli NIE MA tablicy (lub by³ b³¹d), lecimy star¹, si³ow¹ metod¹ z "{"Id""
                    int startIndex = str.IndexOf("{\"Id\"");
                    int stopIndex = str.LastIndexOf("}");

                    if (startIndex != -1 && stopIndex != -1 && stopIndex > startIndex)
                    {
                        var extractedJson = str.Substring(startIndex, stopIndex - startIndex + 1);
                        try
                        {
                            var result_employee = JsonConvert.DeserializeObject<Employee>(extractedJson);
                            ErrorWebsocket = 0;
                            return result_employee;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception)
            {
                ErrorWebsocket = 1;
                ws?.Abort();
                ws?.Dispose();
                ws = null;
            }
            finally
            {
                LeaveBusyState();
                _wsLock.Release();
            }

            return null;
        }

        public async Task<ObservableCollection<T>?> GetAsyncTcp2()
        {
            EnterBusyState();
            var client = new TcpClient();

            try
            {
                if (string.IsNullOrEmpty(Adres___))
                {
                    await ShowErrorAlert("B³¹d", "Adres serwera nie jest ustawiony");
                    return null;
                }

                using (var cts = new CancellationTokenSource(TCP_CONNECT_TIMEOUT_MS))
                {
                    await client.ConnectAsync(Adres___, Port__, cts.Token);
                }

                using (NetworkStream stream = client.GetStream())
                {
                    await SocketClient.SendStringTcp(stream, "{\"Ha\":\"" + Haslo___ + "\",\"Id\":250,\"Za\":\"0\",\"Ko\":0}\r\n").ConfigureAwait(false);
                    var str = await SocketClient.ReceiveStringTcp(stream, 20480).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(str))
                    {
                        ErrorWebsocket = 1;
                        return null;
                    }

                    // --- LOGIKA WYCINANIA TABLICY [ ] ---
                    int startIndex = str.IndexOf('[');
                    int stopIndex = str.LastIndexOf(']');

                    if (startIndex != -1 && stopIndex != -1 && stopIndex > startIndex)
                    {
                        // Wycinamy tekst razem z nawiasami [ i ]
                        var extractedJson = str.Substring(startIndex, stopIndex - startIndex + 1);
                        var ob = JsonConvert.DeserializeObject<ObservableCollection<T>>(extractedJson);
                        ErrorWebsocket = 0;
                        return ob;
                    }

                    // Jeœli nie znaleziono nawiasów, próbujemy parsowaæ ca³oœæ
                    var fallbackOb = JsonConvert.DeserializeObject<ObservableCollection<T>>(str);
                    ErrorWebsocket = fallbackOb != null ? (byte)0 : (byte)1;
                    return fallbackOb;
                }
            }
            catch (OperationCanceledException)
            {
                ErrorWebsocket = 1;
                return null;
            }
            catch (Exception)
            {
                ErrorWebsocket = 1;
                return null;
            }
            finally
            {
                LeaveBusyState();
                client.Dispose();
            }
        }


        public async Task<Employee?> GetAsyncSelektTcp2(Employee t)
        {
            EnterBusyState();
            var client = new TcpClient();

            try
            {
                if (string.IsNullOrEmpty(Adres___))
                {
                    await ShowErrorAlert("B³¹d", "Adres serwera nie jest ustawiony");
                    return null;
                }

                using (var cts = new CancellationTokenSource(TCP_CONNECT_TIMEOUT_MS))
                {
                    await client.ConnectAsync(Adres___, Port__, cts.Token);
                }

                var json = JsonConvert.SerializeObject(t);

                using (NetworkStream stream = client.GetStream())
                {
                    await SocketClient.SendStringTcp(stream, "[{\"Ha\":\"" + Haslo___ + "\"}," + json + "]\r\n").ConfigureAwait(false);
                    var str = await SocketClient.ReceiveStringTcp(stream, 2048).ConfigureAwait(false); // Zwiêkszy³em bufor, jeœli 254 to za ma³o dla Employee

                    if (string.IsNullOrEmpty(str))
                    {
                        ErrorWebsocket = 1;
                        return null;
                    }

                    // --- HYBRYDOWY MECHANIZM ODBIORU ---

                    // 1. Jeœli znajdujemy tablicê (autoryzacja + dane)
                    int kwadratStart = str.IndexOf('[');
                    int kwadratStop = str.LastIndexOf(']');

                    if (kwadratStart != -1 && kwadratStop != -1 && kwadratStop > kwadratStart)
                    {
                        try
                        {
                             var tablica = str.Substring(kwadratStart, kwadratStop - kwadratStart + 1);
                             var rootToken = JToken.Parse(tablica);

                             if (rootToken.Type == JTokenType.Array) 
                             {
                                 if (rootToken.Count() > 1) 
                                 {
                                     ErrorWebsocket = 0;
                                     return rootToken[1].ToObject<Employee>();
                                 }
                                 if (rootToken.Count() == 1) 
                                 {
                                     ErrorWebsocket = 0;
                                     return rootToken[0].ToObject<Employee>();
                                 }
                             }
                        }
                        catch (Exception ex)
                        {
                             System.Diagnostics.Debug.WriteLine($"[PARSE ARRAY EXCEPTION]: {ex.Message}");
                        }
                    }

                    // 2. Jeœli NIE MA tablicy (lub tablica by³a brudna), w³¹czamy si³owe szukanie "{"Id""
                    int startIndex = str.IndexOf("{\"Id\"");
                    int stopIndex = str.LastIndexOf("}");

                    if (startIndex != -1 && stopIndex != -1 && stopIndex > startIndex)
                    {
                        var extractedJson = str.Substring(startIndex, stopIndex - startIndex + 1);
                        try 
                        {
                            var emp = JsonConvert.DeserializeObject<Employee>(extractedJson);
                            ErrorWebsocket = 0;
                            return emp;
                        }
                        catch { }
                    }

                    ErrorWebsocket = 1;
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                ErrorWebsocket = 1;
                return null;
            }
            catch (Exception ex)
            {
                ErrorWebsocket = 1;
                return null;
            }
            finally
            {
                LeaveBusyState();
                client.Dispose();
            }
        }


        private async Task ShowErrorAlert(string title, string message)
        {
            // T³umimy okienka jeœli aplikacja jest w tle 
            // ORAZ przez pierwsze 4 sekundy od wybudzenia, kiedy to karta WiFi telefonu dopiero wstaje
            if (!IsAppInForeground || (DateTime.Now - LastResumeTime).TotalSeconds < 4) 
                return;

            try
            {
                if (Application.Current?.Windows.Count > 0)
                {
                    var page = Application.Current.Windows[0].Page;
                    if (page != null)
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await page.DisplayAlert(title, message, "OK");
                        });
                    }
                }
            }
            catch
            {
                // Obs³uga b³êdu wyœwietlania alertu
            }
        }

        public bool RegisterAdresSoket(string adres, string haslo)
        {
            string[] split = adres.Split(new Char[] { ':' });
            int i = 0;
            Haslo___ = haslo;
            foreach (string s in split)
            {
                if (s.Trim() != "")
                {
                    if (i == 0)
                    {
                        Adres___ = s;
                    }

                    if (i == 1)
                    {
                        int.TryParse(s, out Port__);
                    }
                }
                i++;
            }
            return true;
        }

        public bool RegisterAdresSerwer(string HostWebsoket, string adres, string haslo)
        {
            if (adres != null)
            {
                Haslo___ = haslo;
                SerwerWebSocked___ = adres;
                WebSocketUrl = "ws://" + HostWebsoket + "/ws";
                return true;
            }
            return false;
        }

        public async Task<List<Models.HistoryRecord>> GetHistoryWs(object query)
        {
            EnterBusyState();

            if (ws == null || ws.State == WebSocketState.Closed)
            {
                await OpenWebsocket();
            }

            await _wsLock.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(query);

                if (ws?.State != WebSocketState.Open)
                {
                    ErrorWebsocket = 1;
                    return null;
                }

                var data = "{\"t\":7,\"d\":{\"topic\":\"proxy:" + Topic___ + "\",\"event\":\"message\",\"data\":{\"to\":\"" + SerwerWebSocked___ + "\",\"from\":\"" + Topic___ + "\",\"data\":[{\"Ha\":\"" + Haslo___ + "\"}," + json + "]}}}";
                System.Diagnostics.Debug.WriteLine($"[HISTORY WS] Wysylam: {data}");
                var encoded = Encoding.UTF8.GetBytes(data);
                var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);

                using (var cts = new CancellationTokenSource(WEBSOCKET_TIMEOUT_MS))
                {
                    await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
                }

                using (var cts = new CancellationTokenSource(WEBSOCKET_HISTORY_TIMEOUT_MS))
                {
                    var str = await ReceiveFullMessageAsync(ws, cts.Token);
                    System.Diagnostics.Debug.WriteLine($"[HISTORY WS] Odpowiedz: {str}");

                    if (str.Contains("endpoint not found"))
                    {
                        ErrorWebsocket = 2;
                        return null;
                    }

                    // 1. Wycinamy tablicê ze œmieci lub kopert (zawsze szukamy [])
                    int startIndex = str.IndexOf('[');
                    int stopIndex = str.LastIndexOf(']');

                    if (startIndex != -1 && stopIndex != -1 && stopIndex > startIndex)
                    {
                        string cleanJson = str.Substring(startIndex, stopIndex - startIndex + 1);

                        // 2. Skoro to zawsze tablica, parsujemy od razu do JArray
                        var dataArray = JArray.Parse(cleanJson);

                        if (dataArray == null || dataArray.Count == 0)
                        {
                            ErrorWebsocket = 2;
                            return null;
                        }

                        // 3. Pomijamy element [0] autoryzacyjny i serializujemy listê
                        var recordTokens = dataArray.Skip(1).ToList();

                        if (recordTokens.Count == 0 || recordTokens[0]["t"] == null)
                        {
                            ErrorWebsocket = 2;
                            return null;
                        }

                        ErrorWebsocket = 0;
                        return new JArray(recordTokens).ToObject<List<Models.HistoryRecord>>();
                    }

                    ErrorWebsocket = 2;
                    return null;
                }
            }
            catch (Exception)
            {
                ErrorWebsocket = 1;
                ws?.Abort();
                ws?.Dispose();
                ws = null;
            }
            finally
            {
                LeaveBusyState();
                _wsLock.Release();
            }
            return null;
        }

        public async Task<List<Models.HistoryRecord>> GetHistoryTcp(object query)
        {
            EnterBusyState();
            var client = new TcpClient();
            try
            {
                if (string.IsNullOrEmpty(Adres___))
                {
                    await ShowErrorAlert("B³¹d", "Adres serwera nie jest ustawiony");
                    return null;
                }

                // Optymalizacja po³¹czenia
                client.NoDelay = true;

                using (var ctsConnect = new CancellationTokenSource(TCP_CONNECT_TIMEOUT_MS))
                {
                    await client.ConnectAsync(Adres___, Port__, ctsConnect.Token).ConfigureAwait(false);
                }

                var json = JsonConvert.SerializeObject(query);
                var tcpMsg = "[{\"Ha\":\"" + Haslo___ + "\"}," + json + "]\r\n";

                using (var stream = client.GetStream())
                {
                    // Wysy³anie zapytania do serwera
                    var sendBytes = Encoding.UTF8.GetBytes(tcpMsg);
                    await stream.WriteAsync(sendBytes, 0, sendBytes.Length).ConfigureAwait(false);
                    await stream.FlushAsync().ConfigureAwait(false);

                    using (var ctsRead = new CancellationTokenSource(WEBSOCKET_HISTORY_TIMEOUT_MS))
                    {
                        // Odbieranie danych ze zwiêkszonym limitem na d³ug¹ historiê z 3000 na 65536
                        var fullResponse = await SocketClient.ReceiveStringTcp(stream, 6000, ctsRead.Token).ConfigureAwait(false);

                        if (string.IsNullOrEmpty(fullResponse))
                        {
                            ErrorWebsocket = 2;
                            return null;
                        }

                        try
                        {
                            // 1. Wycinamy tablicê ze œmieci
                            int startIndex = fullResponse.IndexOf('[');
                            int stopIndex = fullResponse.LastIndexOf(']');

                            if (startIndex != -1 && stopIndex != -1 && stopIndex > startIndex)
                            {
                                string cleanJson = fullResponse.Substring(startIndex, stopIndex - startIndex + 1);

                                // 2. Skoro gwarantujesz tablicê, parsujemy od razu w JArray
                                var dataArray = JArray.Parse(cleanJson);

                                if (dataArray == null || dataArray.Count == 0)
                                {
                                    ErrorWebsocket = 2;
                                    return null;
                                }

                                // 3. Pomijamy element [0] (autoryzacja) i budujemy listê rekordów
                                var recordTokens = dataArray.Skip(1).ToList();

                                if (recordTokens.Count == 0 || recordTokens[0]["t"] == null)
                                {
                                    ErrorWebsocket = 2;
                                    return null;
                                }

                                ErrorWebsocket = 0;
                                return new JArray(recordTokens).ToObject<List<Models.HistoryRecord>>();
                            }

                            ErrorWebsocket = 2;
                            return null;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[HISTORY TCP PARSE ERROR] Error: {ex.Message}");
                            ErrorWebsocket = 1;
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HISTORY TCP] Error: {ex.Message}");
                ErrorWebsocket = 1;
                return null;
            }
            finally
            {
                LeaveBusyState();
                client.Dispose();
            }
        }




      
    }
}