using PepperDash.Essentials.Core;

namespace DynFusion
{
	public class BoolWithFeedback
	{
		private bool _value;
		public readonly BoolFeedback Feedback;

		public bool Value
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
            Feedback = new BoolFeedback(() => _value);
		}
	}
}