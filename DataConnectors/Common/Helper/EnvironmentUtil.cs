using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnectors.Common.Helper
{
    public static class EnvironmentUtil
    {
        public static CultureInfo GetCultureFromTwoLetterCountryCode(string twoLetterISOCountryCode)
        {
            try
            {
                return CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures)
                                .Where(m => m.Name.EndsWith("-" + twoLetterISOCountryCode))
                                .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public static RegionInfo GetRegionByThreeLetterCountryCode(string threeLetterISOCountryCode)
        {
            CreateCountryCodeMappingList();

            return countryCodesMapping[threeLetterISOCountryCode];
        }

        private static Dictionary<string, RegionInfo> countryCodesMapping = null;

        private static void CreateCountryCodeMappingList()
        {
            if (countryCodesMapping == null)
            {
                countryCodesMapping = new Dictionary<string, RegionInfo>();

                CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

                foreach (var culture in cultures)
                {
                    try
                    {
                        var region = new RegionInfo(culture.LCID);
                        countryCodesMapping[region.ThreeLetterISORegionName] = region;
                    }
                    catch (CultureNotFoundException)
                    { }
                }
            }
        }
    }
}