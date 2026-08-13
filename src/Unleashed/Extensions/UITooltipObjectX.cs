namespace Unleashed.Extensions;

static class UITooltipObjectX
{
	extension(UITooltipObject @this)
	{
		public void ChangeTooltip(string text)
		{
			@this.Tooltip = text;
			@this.Apply();
		}
		public void ChangeDelayTooltip(string text)
		{
			@this.DelayTooltip = text;
			@this.Apply();
		}

		bool IsStarting
			=> UIHoverText.text == @this.translatedTooltip;
		bool IsFinished
			=> UIHoverText.text == @this.translatedDelayTooltip && @this.translatedTooltip == ""
			|| UIHoverText.text == @this.translatedTooltip + UIHoverText.DelayTooltipSpacer + @this.translatedDelayTooltip;

		public void Apply()
		{
			if (@this.IsStarting)
				@this.Restart(false);
			else if (@this.IsFinished)
				@this.Restart(true);
		}
		void Restart(bool immediate)
		{
			@this.CancelTooltip();
			@this.OnHover(true);
			if (immediate)
			{
				@this.CancelInvoke(nameof(@this.InvokeDelayTooltip));
				@this.InvokeDelayTooltip();
			}
		}
	}
}

