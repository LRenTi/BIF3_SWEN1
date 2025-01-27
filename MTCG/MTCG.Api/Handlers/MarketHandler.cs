using MTCG.Data.Repository;
using MTCG.Security;
using MTCG.Core.Entities;
using System.Net;
using System.Text.Json.Nodes;

namespace MTCG;

    public class MarketHandler : Handler, IHandler
    {
        [Route("POST", "market")]
        private async Task<(int Status, JsonObject? Reply)> CreateTradeOffer(HttpSvrEventArgs e)
        {
            try
            {

                (bool Success, User? User) auth = await Token.Authenticate(e);
                if (!auth.Success || auth.User == null)
                {
                    return Unauthorized();
                }

                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json != null)
                {
                    int offeredCardId = json["offeredCardId"]!.GetValue<int>();
                    int requestedCardId = json["requestedCardId"]!.GetValue<int>();

                    await MarketRepository.CreateTradeOffer(auth.User.Username, offeredCardId, requestedCardId);

                    JsonObject reply = new();
                    reply["success"] = true;
                    reply["message"] = "Trade offer created successfully.";
                    return (HttpStatusCode.OK, reply);

                }
                else
                {
                    return BadRequest("Invalid payload.");
                }
                
               
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to create trade offer: " + ex.Message);
            }
        }

        [Route("GET", "market")]
        private async Task<(int Status, JsonObject? Reply)> GetAllTradeOffers(HttpSvrEventArgs e)
        {
            try
            {
                var offers = await MarketRepository.GetAllTradeOffers();
                JsonArray offerArray = new();

                foreach (var offer in offers)
                {
                    offerArray.Add(new JsonObject
                    {
                        ["offerId"] = offer.OfferId,
                        ["username"] = offer.Username,
                        ["offeredCardId"] = offer.OfferedCardId,
                        ["requestedCardId"] = offer.RequestedCardId
                    });
                }

                JsonObject reply = new();
                reply["offers"] = offerArray;
                return (HttpStatusCode.OK, reply);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to retrieve trade offers: " + ex.Message);
            }
        }

        [Route("PUT", "market/{offerId}")]
        private async Task<(int Status, JsonObject? Reply)> AcceptTradeOffer(HttpSvrEventArgs e)
        {
            try
            {
                JsonObject reply = new();
                
                (bool Success, User? User) auth = await Token.Authenticate(e);

                if (!auth.Success || auth.User == null)
                {
                    return Unauthorized();
                }

                if (!e.RouteParams.TryGetValue("offerId", out var offerId))
                {
                    reply["message"] = "No offerId provided.";
                    return (HttpStatusCode.BAD_REQUEST, reply);
                }

                bool success = await MarketRepository.AcceptTradeOffer(int.Parse(offerId), auth.User.Username);
                
                if (success)
                {
                    reply["success"] = true;
                    reply["message"] = "Trade offer accepted successfully.";
                    return (HttpStatusCode.OK, reply);
                }
                else
                {
                    reply["success"] = false;
                    reply["message"] = "Failed to accept trade offer.";
                    return (HttpStatusCode.BAD_REQUEST, reply);
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to accept trade offer: " + ex.Message);
            }
        }
    }