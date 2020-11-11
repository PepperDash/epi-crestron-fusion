﻿using PepperDash.Essentials.Core;

namespace DynFusion
{
	public class DynFusionAssetOccupancySensorJoinMap : JoinMapBaseAdvanced
	{
		public JoinDataComplete StringIO = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Label = "String IO", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });


		public DynFusionAssetOccupancySensorJoinMap(uint joinStart) 
            :base(joinStart)
		{
			
		}
	}
}