namespace CharmCore.Api;

/// <summary>
/// A wear step about to be applied to a pocketed valuable, offered to the handlers registered with
/// <see cref="CharmsApi.OnWearing"/>. Handlers run on the host, which applies all wear. Lower <see cref="Loss"/> to make the
/// valuable lose less value (for example a charm that protects other charms).
/// </summary>
public sealed class WearContext
{
	internal WearContext(ValuableObject valuable, PlayerAvatar? carrier, float loss)
	{
		Valuable = valuable;
		Carrier = carrier;
		Loss = loss;
	}

	/// <summary>The valuable that is wearing down.</summary>
	public ValuableObject Valuable { get; }

	/// <summary>The player who carries it in a pocket, when known.</summary>
	public PlayerAvatar? Carrier { get; }

	/// <summary>Dollars the valuable is about to lose.</summary>
	public float Loss { get; set; }
}
