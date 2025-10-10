using System.ComponentModel;
using System.Globalization;

// to suppress possibly null reference warnings.
#pragma warning disable CS8600, CS8604, CS8602, CS8625

namespace KeyCard.Core.Middlewares
{
    public static class StringEnumExtension
    {
        /// <summary>
        /// Get description.
        /// </summary>
        /// <param name="e"> Model.</param>
        /// <typeparam name="T"> Class.</typeparam>
        /// <returns> Get description from the enum.</returns>
        public static string GetDescription<T>(this T e)
                    where T : IConvertible
        {
            var description = string.Empty;

            if (e is not Enum)
            {
                return description;
            }

            var type = e.GetType();
            var values = Enum.GetValues(type);

            foreach (int val in values)
            {
                if (val != e.ToInt32(CultureInfo.InvariantCulture))
                {
                    continue;
                }

                var memInfo = type.GetMember(type.GetEnumName(val));
                var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descriptionAttributes.Length > 0)
                {
                    description = ((DescriptionAttribute)descriptionAttributes[0]).Description;
                }

                break;
            }

            return description;
        }
    }
}
