using PepperDash.Essentials.Core;

namespace DynFusion
{
	public class DynFusionStaticAssetJoinMap : JoinMapBaseAdvanced
	{
		[JoinName("PowerOn")]
		public JoinDataComplete PowerOn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1, 
				JoinSpan = 1
			}, 
			new JoinMetadata
			{
				Description = "Fusion static asset power on",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL, 
				JoinType = eJoinType.Digital
			});

		[JoinName("PowerOff")]
		public JoinDataComplete PowerOff = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Fusion static asset power off",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("Connected")]
		public JoinDataComplete Connected = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Fusion static asset connected",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("AssetUsage")]
		public JoinDataComplete AssetUsage = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1, 
				JoinSpan = 1
			}, 
			new JoinMetadata
			{
				Description = "Fusion static asset usage", 
				JoinCapabilities = eJoinCapabilities.FromSIMPL, 
				JoinType = eJoinType.Serial
			});

		[JoinName("AssetError")]
		public JoinDataComplete AssetError = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2, 
				JoinSpan = 1
			}, 
			new JoinMetadata
			{
				Description = "Fusion static asset error", 
				JoinCapabilities = eJoinCapabilities.FromSIMPL, 
				JoinType = eJoinType.Serial
			});

		public DynFusionStaticAssetJoinMap(uint joinStart)
			: base(joinStart)
		{
			
		}            
	}
}