// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Core;

namespace DynFusionEPI 
{
	public class DynFusionDevice : EssentialsBridgeableDevice
	{
	    private DynFusionConfigObjectTemplate _config;

		public DynFusionDevice(string key, string name, DynFusionConfigObjectTemplate config)
			: base(key, name)
		{
            Debug.Console(0, this, "Constructing new DynFusionDevice instance");
		    _config = config;
		}

		private void Initialize () 
		{

		}

	    #region Overrides of EssentialsBridgeableDevice

	    public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
	    {
	        var joinMap = new EssentialsPluginBridgeJoinMapTemplate(joinStart);

	        // This adds the join map to the collection on the bridge
	        if (bridge != null)
	        {
	            bridge.AddJoinMap(Key, joinMap);
	        }

	        var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

	        if (customJoins != null)
	        {
	            joinMap.SetCustomJoinData(customJoins);
	        }

	        Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
	        Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);


	        trilist.OnlineStatusChange += (o, a) =>
	        {
	            if (a.DeviceOnLine)
	            {
	                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
	            }
	        };
	    }


	    #endregion
	}
}

