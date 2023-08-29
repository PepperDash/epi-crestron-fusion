
namespace DynFusion
{
	public static class DebugExtensions
	{
		public static uint Trace { get; set; }
		public static uint Warn { get; set; }
		public static uint Verbose { get; set; }

		static DebugExtensions()
		{
			ResetLevels();
		}

		public static void SetLevels(uint level)
		{
			Trace = level;
			Warn = level;
			Verbose = level;
		}

		public static void ResetLevels()
		{
			Trace = 0;
			Warn = 1;
			Verbose = 2;
		}
	}	
}