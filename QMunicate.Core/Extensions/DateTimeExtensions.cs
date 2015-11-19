﻿using System;

namespace Quickblox.Sdk.Builder
{
    /// <summary>
    /// DateTimeExtensions class.
    /// </summary>
    public static class DateTimeExtensions
    {
        #region Fields

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        #endregion

        #region Public Members

        public static Int64 ToUnixEpoch(this DateTime dateTime)
        {
            var timeSpan = dateTime.Subtract(UnixEpoch);
            return (Int64)timeSpan.TotalMilliseconds;
        }

        public static DateTime ToDateTime(this long unixTime)
        {
            return UnixEpoch.AddSeconds(unixTime).ToLocalTime();
        }

        #endregion
    }
}
