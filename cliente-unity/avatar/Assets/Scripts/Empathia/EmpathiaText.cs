using System.Text;

namespace Empathia
{
    /// <summary>
    /// Decodifica/limpia texto de B para TMP (evita U+FFFD y mojibake).
    /// </summary>
    public static class EmpathiaText
    {
        public static string FromHttpBody(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            // Quitar BOM UTF-8 si viene.
            var offset = 0;
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                offset = 3;

            var utf8 = Encoding.UTF8.GetString(data, offset, data.Length - offset);
            if (utf8.IndexOf('\uFFFD') < 0)
                return utf8;

            // Si UTF-8 produjo replacement chars, probar Windows-1252 (B a veces responde así).
            try
            {
                var latin = Encoding.GetEncoding(1252).GetString(data, offset, data.Length - offset);
                if (latin.IndexOf('\uFFFD') < 0)
                    return latin;
            }
            catch
            {
                // keep utf8
            }

            return utf8;
        }

        public static string ForUi(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? "";

            // U+FFFD no está en LiberationSans SDF → warning TMP. Sustituir por '?' legible.
            return value.Replace('\uFFFD', '?').Replace('\u2026', '.');
        }
    }
}
