using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KeyCard.Core.Middlewares
{
    public static class StringExtension
    {
        /// <summary>
        /// Check if is valid json.
        /// </summary>
        public static bool IsValidJson(this string text)
        {
            text = text.Trim();

            if ((!text.StartsWith("{") || !text.EndsWith("}")) && (!text.StartsWith("[") || !text.EndsWith("]")))
            {
                return false;
            }

            try
            {
                JToken.Parse(text);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
