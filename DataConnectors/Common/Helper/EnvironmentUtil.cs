﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnectors.Common.Helper
{
    public static class EnvironmentUtil
    {
        public static CultureInfo GetCultureFromString(string cultureString)
        {
            CultureInfo culture = null;
            if (string.IsNullOrEmpty(cultureString))
            {
                return culture;
            }

            if (cultureString.Length == 5 && cultureString.Contains("-"))
            {
                // de-DE, en-US,...
                culture = EnvironmentUtil.GetCultureFromFiveLetterName(cultureString);
            }
            else if (cultureString.Length == 3)
            {
                // DEU, USA, ...
                var region = EnvironmentUtil.GetRegionByThreeLetterCountryCode(cultureString);
                culture = EnvironmentUtil.GetCulturesFromRegion(region).FirstOrDefault();
            }
            else if (cultureString.Length == 2)
            {
                // DE, US, ...
                culture = EnvironmentUtil.GetCultureFromTwoLetterCountryCode(cultureString);
            }

            return culture;
        }

        public static CultureInfo GetCultureFromTwoLetterCountryCode(string twoLetterISOCountryCode)
        {
            if (string.IsNullOrEmpty(twoLetterISOCountryCode))
            {
                return null;
            }

            try
            {
                return CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures)
                                  .FirstOrDefault(m => m.Name.EndsWith("-" + twoLetterISOCountryCode));
            }
            catch
            {
                return null;
            }
        }

        public static CultureInfo GetCultureFromFiveLetterName(string fiveLetterName)
        {
            if (string.IsNullOrEmpty(fiveLetterName))
            {
                return null;
            }

            try
            {
                return CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures)
                                  .FirstOrDefault(m => m.Name == fiveLetterName);
            }
            catch
            {
                return null;
            }
        }

        public static IEnumerable<CultureInfo> GetCulturesFromRegion(RegionInfo region)
        {
            if (region == null)
            {
                return new List<CultureInfo>();
            }

            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                              .Where(x => (new RegionInfo(x.Name)).GeoId == region.GeoId);
        }

        public static RegionInfo GetRegionByThreeLetterCountryCode(string threeLetterISOCountryCode)
        {
            if (string.IsNullOrEmpty(threeLetterISOCountryCode))
            {
                return null;
            }

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