using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geofence
{
	public class GeofencePeriod
	{
		public int VehicleId { get; set; }
		public DateTime EnterTime { get; set; }
		public DateTime ExitTime { get; set; }
	}
}
