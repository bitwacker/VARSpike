using System.Collections.Generic;

namespace VARSpike
{
    public class MarketPriceRepository
    {
        public List<MarketPrice> GetPrices()
        {
            return SmallDBHelper.ExecuteQuery("Data Source=localhost;Initial Catalog=NimbusT;Integrated Security=SSPI;",
                r =>
                {
                    return new MarketPrice()
                    {
                        Date = r.GetDateTime(0),
                        Value = (double)r.GetDecimal(1)
                    };
                },
                @"SELECT Date, Price  FROM MarketPrice WHERE 
	[Index]=30004 -- Brent DTD
	AND [Type]=100068 -- Closing
	AND [Source]=3 -- Bulldog
	AND [Date]>='2013-1-1' AND [Date]<='2013-12-31'");
        }
    }
}