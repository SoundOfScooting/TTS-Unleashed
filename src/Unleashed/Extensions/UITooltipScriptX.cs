namespace Unleashed.Extensions;

static class UITooltipScriptX
{
	extension(UITooltipScript @this)
	{
		public void RestartTooltip()
		{
			if (UIHoverText.text == @this.translatedTooltip)
			{
				@this.CancelTooltip();
				@this.OnHover(true);
			}
			else if (
				UIHoverText.text == @this.translatedDelayTooltip && @this.translatedTooltip == "" ||
				UIHoverText.text == @this.translatedTooltip + UIHoverText.DelayTooltipSpacer + @this.translatedDelayTooltip
			){
				@this.CancelTooltip();
				@this.OnHover(true);
				@this.CancelInvoke(nameof(@this.InvokeDelayTooltip));
				@this.InvokeDelayTooltip();
			}
		}
	}
}

