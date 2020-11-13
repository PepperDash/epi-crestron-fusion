using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace DynFusion
{
	public class BoolWithFeedback
	{
		private bool _value;
		public BoolFeedback Feedback;
		public bool value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
				Feedback.FireUpdate();
			}
		}
		public BoolWithFeedback()
		{
			Feedback = new BoolFeedback(() => value);
		}



	}
}