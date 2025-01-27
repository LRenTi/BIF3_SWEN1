
namespace MTCG.Core.Entities;

public class TradeOffer
{
    public int OfferId { get; }
    public string Username { get; }
    public int OfferedCardId { get; }
    public int RequestedCardId { get; }

    public TradeOffer(int offerId, string username, int offeredCardId, int requestedCardId)
    {
        OfferId = offerId;
        Username = username;
        OfferedCardId = offeredCardId;
        RequestedCardId = requestedCardId;
    }
}