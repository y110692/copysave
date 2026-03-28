using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CopySave.Windows
{
    internal static class ClipboardPayloadReader
    {
        private static readonly uint HtmlFormat = NativeMethods.RegisterClipboardFormat("HTML Format");

        public static bool HasSavableClipboard()
        {
            return NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_UNICODETEXT)
                || NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_TEXT)
                || NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_OEMTEXT)
                || (HtmlFormat != 0 && NativeMethods.IsClipboardFormatAvailable(HtmlFormat));
        }

        public static bool TryReadPayload(out string payload)
        {
            payload = null;

            try
            {
                if (Clipboard.ContainsFileDropList())
                {
                    return false;
                }

                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    payload = Normalize(Clipboard.GetText(TextDataFormat.UnicodeText));
                    return payload.Length > 0;
                }

                if (Clipboard.ContainsText())
                {
                    payload = Normalize(Clipboard.GetText());
                    return payload.Length > 0;
                }

                if (Clipboard.ContainsText(TextDataFormat.Html))
                {
                    payload = NormalizeHtml(Clipboard.GetText(TextDataFormat.Html));
                    return payload.Length > 0;
                }
            }
            catch (ExternalException)
            {
                return false;
            }

            payload = null;
            return false;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("\0", string.Empty);
        }

        private static string NormalizeHtml(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Replace("<!--StartFragment-->", string.Empty)
                .Replace("<!--EndFragment-->", string.Empty)
                .Trim();
        }
    }
}
