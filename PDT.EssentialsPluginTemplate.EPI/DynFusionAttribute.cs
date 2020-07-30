using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PDT.DynFusion.EPI
{
	public class DynFusionAttribute
	{
		[JsonProperty("SignalType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eJoinType	SignalType { get; set; }

		[JsonProperty("JoinNumber")]
		public UInt32		JoinNumber { get; set; }
		
		[JsonProperty("Name")]
		public string		Name { get; set; }
		[JsonProperty("RwType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public string		RwType { get; set; }
		public string		StringValue
		{
			get
			{
				return _StringValue;

			}
			set
			{
				_StringValue = value;
				StringValueFeedback.FireUpdate();
			}
		}
		public UInt32 UShortValue
		{
			get	
			{
				return _UShortValue;

			}
			set
			{
				_UShortValue = value;
				UShortValueFeedback.FireUpdate();

			}
		}
		public bool	BoolValue 
		{ 
			get
			{
				return _BoolValue;
				
			}
			set
			{
				_BoolValue = value;
				BoolValueFeedback.FireUpdate();

			}
		}
		public string	_StringValue { get; set; }
		public UInt32	_UShortValue { get; set; }
		public bool		_BoolValue { get; set; }
		public StringFeedback	StringValueFeedback { get; set; }
		public IntFeedback		UShortValueFeedback { get; set; }
		public BoolFeedback		BoolValueFeedback { get; set; }

	}
	public enum eReadWrite
	{
		Read = 1,
		Write = 2,
		R = 1, 
		W = 2,
		ReadWrite = Read | Write,
		RW = R | W
	}
}