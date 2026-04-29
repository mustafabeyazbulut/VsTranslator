using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VsTranslator.Services
{
    internal sealed class TranslationService : IDisposable
    {
        private const int MaxChunkChars = 480;
        private const string ApiBase = "https://api.mymemory.translated.net/get";

        private static readonly Regex CamelCaseSplit = new Regex(
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
            RegexOptions.Compiled);

        private readonly HttpClient _http;

        public TranslationService()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("VsTranslator/1.0");
        }

        public async Task<string> TranslateAsync(string text, string from, string to, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var prepared = PrepareForTranslation(text);
            var chunks = SplitIntoChunks(prepared, MaxChunkChars);
            var sb = new StringBuilder();
            foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();
                var translated = await TranslateChunkAsync(chunk, from, to, ct).ConfigureAwait(false);
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(translated);
            }
            return sb.ToString();
        }

        private static string PrepareForTranslation(string text)
        {
            // snake_case ve kebab-case'i bosluga cevir, sonra camelCase/PascalCase parcala.
            // Bu sayede "TaskCompletionSource" -> "Task Completion Source" -> "Gorev Tamamlama Kaynagi" gibi
            // anlamli ceviri mumkun olur.
            text = text.Replace('_', ' ');
            text = CamelCaseSplit.Replace(text, " ");
            return text;
        }

        private async Task<string> TranslateChunkAsync(string chunk, string from, string to, CancellationToken ct)
        {
            var url = ApiBase
                + "?q=" + Uri.EscapeDataString(chunk)
                + "&langpair=" + Uri.EscapeDataString(from + "|" + to);

            using (var response = await _http.GetAsync(url, ct).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        "MyMemory API hatasi: HTTP " + (int)response.StatusCode);
                }

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var serializer = new DataContractJsonSerializer(typeof(MyMemoryResponse));
                    var parsed = (MyMemoryResponse)serializer.ReadObject(stream);

                    if (parsed?.ResponseData == null || string.IsNullOrEmpty(parsed.ResponseData.TranslatedText))
                    {
                        throw new InvalidOperationException(
                            "Cevirilmis metin alinamadi. ResponseStatus: " + (parsed?.ResponseStatus ?? 0));
                    }

                    return DecodeHtmlEntities(parsed.ResponseData.TranslatedText);
                }
            }
        }

        private static IEnumerable<string> SplitIntoChunks(string text, int maxLen)
        {
            if (text.Length <= maxLen)
            {
                yield return text;
                yield break;
            }

            var index = 0;
            while (index < text.Length)
            {
                var remaining = text.Length - index;
                if (remaining <= maxLen)
                {
                    yield return text.Substring(index);
                    yield break;
                }

                var chunkEnd = index + maxLen;
                var splitAt = -1;
                for (var i = chunkEnd; i > index; i--)
                {
                    var c = text[i];
                    if (c == '.' || c == '!' || c == '?' || c == '\n')
                    {
                        splitAt = i + 1;
                        break;
                    }
                }
                if (splitAt < 0)
                {
                    for (var i = chunkEnd; i > index; i--)
                    {
                        if (char.IsWhiteSpace(text[i]))
                        {
                            splitAt = i + 1;
                            break;
                        }
                    }
                }
                if (splitAt < 0)
                {
                    splitAt = chunkEnd;
                }

                yield return text.Substring(index, splitAt - index);
                index = splitAt;
            }
        }

        private static string DecodeHtmlEntities(string s)
        {
            return s
                .Replace("&quot;", "\"")
                .Replace("&#39;", "'")
                .Replace("&apos;", "'")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");
        }

        public void Dispose()
        {
            _http?.Dispose();
        }

        [DataContract]
        private class MyMemoryResponse
        {
            [DataMember(Name = "responseData")]
            public ResponseData ResponseData { get; set; }

            [DataMember(Name = "responseStatus")]
            public int ResponseStatus { get; set; }
        }

        [DataContract]
        private class ResponseData
        {
            [DataMember(Name = "translatedText")]
            public string TranslatedText { get; set; }

            [DataMember(Name = "match")]
            public double Match { get; set; }
        }
    }
}
