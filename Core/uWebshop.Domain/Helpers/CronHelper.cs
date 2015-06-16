﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uWebshop.Domain.Helpers
{
	/// <summary>
	/// 
	/// </summary>
	public class CronHelper
	{
		/// <summary>
		/// Generates the date time instances from a crontab expression starting now, until the end time or one year in advance if not specified.
		/// </summary>
		/// <param name="crontabExpression">The crontab expression.</param>
		/// <param name="endTime">The end time.</param>
		public static IEnumerable<DateTime> GenerateDateTimeInstancesFromCrontabExpressionStartingNow(string crontabExpression, DateTime? endTime = null)
		{
			return CrontabSchedule.CrontabSchedule.Parse(crontabExpression).GetNextOccurrences(DateTime.Now, endTime ?? DateTime.Now.AddYears(1));
		}

		/// <summary>
		/// Generates the date time instances from crontab expression.
		/// </summary>
		/// <param name="startDate">The start date.</param>
		/// <param name="endDate">The end date.</param>
		/// <param name="crontabExpression">The crontab expression.</param>
		public static IEnumerable<DateTime> GenerateDateTimeInstancesFromCrontabExpression(DateTime startDate, DateTime endDate, string crontabExpression)
		{
			return CrontabSchedule.CrontabSchedule.Parse(crontabExpression).GetNextOccurrences(startDate, endDate);
		}

		public static IEnumerable<DateTime> GenerateDateTimeInstancesFromOrderSeries(OrderSeries series)
		{
			var customCron = series.CronInterval;
			IEnumerable<string> cronParts = customCron.Split('|');
			var weekInterval = 1;
			if (cronParts.First().StartsWith("w"))
			{
				if (!int.TryParse(cronParts.First().Substring(1), out weekInterval))
				{
					weekInterval = 1;
				}
				cronParts = cronParts.Skip(1);
			}
			var today = DateTime.Today;
			var endDate = series.End ?? today.AddYears(2);
			if (endDate > today.AddYears(1))
			{
				endDate = today.AddYears(1);
			}
			var instancesCount = series.EndAfterInstances;
			if (instancesCount == 0) instancesCount = int.MaxValue;
			var cron = cronParts.First();
			var times = cronParts.Skip(1).FirstOrDefault();
			int weekCount = 0, previousweek = 0;
			foreach (var date in GenerateDateTimeInstancesFromCrontabExpression(series.Start, endDate, cron))
			{
				var week = GetWeekOfYear(date);
				if (week != previousweek)
				{
					weekCount++;
					previousweek = week;
				}
				if ((weekInterval == 1 || weekCount % weekInterval == 1) && instancesCount-- > 1 && date > today)
				{
					yield return date;
					
					if (!string.IsNullOrWhiteSpace(times))
					{
						foreach (var time in times.Split(','))
						{
							if (instancesCount-- > 0) // there's a minor issue here; the times might not be ordered, which then leads to unexpected behaviour at the last day
							{
								var timeParts = time.Split(':');
								var hour = int.Parse(timeParts[0]);
								var minute = int.Parse(timeParts[1]);
								var ts = new TimeSpan(hour, minute, 0);
								yield return date.Date + ts;
							}
						}
					}
				}
			}
		}
		private static int GetWeekOfYear(DateTime date)
		{
			DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
			Calendar cal = dfi.Calendar;
			return cal.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
		}
	}
}
