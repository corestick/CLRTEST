using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Globalization;

public partial class UserDefinedFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlDateTime Udf_Lunar2Solar(SqlDateTime sDt)
    {
        DateTime dt = (DateTime)sDt;
        bool bExistLeap = false;

        KoreanLunisolarCalendar kr_Lunar = new KoreanLunisolarCalendar();
        int _lunnarYY = dt.Year;
        int _lunnarMM = dt.Month;
        int _lunnarDD = dt.Day;

        int _leapMonth;

        if (kr_Lunar.GetMonthsInYear(_lunnarYY) > 12)
        {
            bExistLeap = kr_Lunar.IsLeapMonth(_lunnarYY, _lunnarMM);
            _leapMonth = kr_Lunar.GetLeapMonth(_lunnarYY);
            if (bExistLeap)
                _lunnarMM++;
            if (_lunnarMM > _leapMonth)
                _lunnarMM++;

        }

        return kr_Lunar.ToDateTime(_lunnarYY, _lunnarMM, _lunnarDD, 0, 0, 0, 0);
    }
};

