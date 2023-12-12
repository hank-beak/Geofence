using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geofence
{
	public class Geofence
	{
		private readonly string _csv;
		private const double MAX_HOURS_PER_WEEK = 42.5;
		public Geofence(string csv)
		{
			_csv = csv;
		}

		/// <summary>
		/// Gets the number of hours where no vehicles are available across all weeks
		/// </summary>
		/// <returns></returns>
		public string GetAverageUnavailability()
		{
			string result = "Number of vehicles sold\t\tNumber of hours per week during which no vehicles are available (inside the geofence)\n";
			var parsedCSV = ParseCSV(_csv);
			
			parsedCSV = parsedCSV.OrderBy(v => v.EnterTime).ToList();

			// Iterate through each "sold" vehicle
			for (int soldCount = 0; soldCount <= parsedCSV.Select(v => v.VehicleId).Distinct().Count(); soldCount++)
			{
				var dataByWeek = parsedCSV.GroupBy(v => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(v.EnterTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday));

				double totalHoursWithNoVehicles = 0;
				
				totalHoursWithNoVehicles += CalculateHoursOfUnavailability(parsedCSV, soldCount);		

				var averageHours = totalHoursWithNoVehicles / dataByWeek.Count();

				result += $"{soldCount}\t\t{averageHours}\n";
			}

			return result;
		}

		private List<GeofencePeriod> ParseCSV(string filePath)
		{
			List<GeofencePeriod> vehicleData = new List<GeofencePeriod>();
			string[] lines = File.ReadAllLines(filePath);

			// Skip the header 
			for (int i = 1; i < lines.Length; i++)
			{
				string[] columns = lines[i].Split(',');

				int vehicleID = int.Parse(columns[0]);
				DateTime entryTime = DateTime.ParseExact(columns[1], "yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
				DateTime exitTime = DateTime.ParseExact(columns[2], "yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

				vehicleData.Add(new GeofencePeriod { VehicleId = vehicleID, EnterTime = entryTime, ExitTime = exitTime });
			}

			return vehicleData;
		}


		private double CalculateHoursOfUnavailability(List<GeofencePeriod> availabilityTimes, int soldCount)
		{
			double totalHoursOfUnavailability = 0;
			var availabilityCopy = new List<GeofencePeriod>(availabilityTimes);
			int originalWeeksCount = availabilityTimes
				.GroupBy(v => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(v.EnterTime, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
				.Count();
			for (int i = 0; i < soldCount; i++)
			{
				var vehicleIDToRemove = availabilityCopy.Select(v => v.VehicleId).Distinct().First();
				availabilityCopy.RemoveAll(v => v.VehicleId== vehicleIDToRemove);
			}

			// Calculate total hours of unavailability for the remaining vehicles
			var groupedByWeek = availabilityCopy.GroupBy(v => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(v.EnterTime, CalendarWeekRule.FirstDay, DayOfWeek.Monday));

			foreach (var weekData in groupedByWeek)
			{
				totalHoursOfUnavailability += CalculateWeekHoursOfUnavailability(weekData);
			}

			// Calculate the remaining weeks of unavailabilty if they were removed from selling vehicles
			if (groupedByWeek.Count() != originalWeeksCount)
			{
				totalHoursOfUnavailability += MAX_HOURS_PER_WEEK * (originalWeeksCount - groupedByWeek.Count());
			}

			return totalHoursOfUnavailability;
		}

		private double CalculateWeekHoursOfUnavailability(IEnumerable<GeofencePeriod> weekData)
		{
			double totalHoursOfUnavailability = 0;

			// Iterate over each day within the week
			DateTime mondayOfWeek = weekData.Min(v => v.EnterTime).Date;
			while (mondayOfWeek.DayOfWeek != DayOfWeek.Monday)
			{
				mondayOfWeek = mondayOfWeek.AddDays(-1);
			}
			for (DateTime currentDate = mondayOfWeek; currentDate.DayOfWeek <= DayOfWeek.Friday; currentDate = currentDate.AddDays(1))
			{
				if (currentDate.DayOfWeek <= DayOfWeek.Friday)
				{
					// Define business hours (8:30 am to 5:00 pm)
					DateTime businessStart = currentDate.AddHours(8.5);
					DateTime businessEnd = currentDate.AddHours(17);

					// Iterate over 15 MINUTES	
					for (DateTime currentInterval = businessStart; currentInterval < businessEnd; currentInterval = currentInterval.AddMinutes(15))
					{
						// Check if any vehicle is available during the current interval
						bool isAvailable = weekData.Any(v => currentInterval >= v.EnterTime && currentInterval <= v.ExitTime);

						// If no vehicles are available, add 15 minutes or 0.25 hours
						if (!isAvailable)
						{
							totalHoursOfUnavailability += 0.25; 
						}
					}

				}
			}

			return totalHoursOfUnavailability;
		}
	}
}
