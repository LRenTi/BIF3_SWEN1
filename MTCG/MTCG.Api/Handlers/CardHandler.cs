using MTCG.Data.Repository;
using MTCG.Security;
using MTCG;
using System.Text.Json.Nodes;
using MTCG.Core.Entities;

namespace MTCG;

public class CardHandler : Handler, IHandler
{
    [Route("GET", "cards")]
    private (int Status, JsonObject? Reply) GetAllCards(HttpSvrEventArgs e)
    {
        try
        {
            JsonObject reply = new() { ["success"] = false };

            (bool Success, User? User) auth = Token.Authenticate(e);
            if (!auth.Success || auth.User == null)
            {
                return Unauthorized();
            }
            else
            {
                var cards = CardRepository.GetAllCards();

                if (!cards.Any())
                {
                    reply["cards"] = new JsonArray();
                }
                else
                {
                    reply["success"] = true;
                    var cardArray = new JsonArray();
                    foreach (var c in cards)
                    {
                        var cobj = new JsonObject
                        {
                            ["id"] = c.Id,
                            ["name"] = c.Name,
                            ["damage"] = c.Damage,
                            ["element"] = c.Element,
                            ["cardtype"] = c.CardType
                        };
                        cardArray.Add(cobj);
                    }
                    reply["cards"] = cardArray;
                }

                return (HttpStatusCode.OK, reply);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Route("POST", "cards")]
    private (int Status, JsonObject? Reply) AddCard(HttpSvrEventArgs e)
    {
        (bool Success, User? User) auth = Token.Authenticate(e);

        if (!auth.Success || auth.User.IsAdmin == false)
        {
            Console.WriteLine(auth.User.Username);
            Console.WriteLine(auth.User.IsAdmin);
            return Unauthorized("Unauthorized access.");
        }

        JsonNode? json = JsonNode.Parse(e.Payload);
        if (json != null)
        {
            string name = (string)json["name"]!;
            int damage = (int)json["damage"]!;
            string element = (string)json["element"]!;
            string cardtype = (string)json["cardtype"]!;

            Card card;

            if (cardtype == "Spellcard")
            {
                card = new SpellCard(0, name, damage, element, cardtype);
            }
            //MonsterCard
            else
            {
                card = new MonsterCard(0, name, damage, element, cardtype);
            }

            CardRepository.AddCard(card);

            return Ok("Card created.");
        }
        else
        {
            return BadRequest("Invalid payload.");
        }
    }

    [Route("GET", "cards/me")]
    private (int Status, JsonObject? Reply) GetCards(HttpSvrEventArgs e)
    {
        JsonObject reply = new() { ["success"] = false, ["message"] = "" };
        int status = HttpStatusCode.BAD_REQUEST;

        try
        {
            (bool Success, User? User) auth = Token.Authenticate(e);
            if (!auth.Success || auth.User == null)
            {
                return Unauthorized();
            }
            else
            {
                var userCards = CardRepository.GetCardsByUser(auth.User.Username);

                status = HttpStatusCode.OK;
                reply["success"] = true;

                if (!userCards.Any())
                {
                    reply["cards"] = new JsonArray();
                }
                else
                {
                    var cardArray = new JsonArray();
                    foreach (var c in userCards)
                    {
                        var cobj = new JsonObject
                        {
                            ["id"] = c.Id,
                            ["name"] = c.Name,
                            ["damage"] = c.Damage,
                            ["element"] = c.Element
                        };
                        cardArray.Add(cobj);
                    }
                    reply["cards"] = cardArray;
                }
            }
        }
        catch (Exception ex)
        {
            reply["message"] = ex.Message;
        }

        return (status, reply);
    }

    [Route("POST", "deck")]
    private (int Status, JsonObject? Reply) AddCardToDeck(HttpSvrEventArgs e)
    {
        (bool Success, User? User) auth = Token.Authenticate(e);
        if (!auth.Success || auth.User == null)
        {
            return Unauthorized();
        }

        JsonNode? json = JsonNode.Parse(e.Payload);
        if (json == null || json["cardId"] == null)
        {
            return BadRequest("Invalid payload.");
        }

        int cardId = (int)json["cardId"]!;
        bool success = CardRepository.AddCardToDeck(auth.User.Username, cardId);

        if (success)
        {
            return Ok("Card added to deck.");
        }
        else
        {
            return BadRequest("Failed to add card to deck.");
        }
    }

    [Route("DELETE", "deck")]
    private (int Status, JsonObject? Reply) RemoveCardFromDeck(HttpSvrEventArgs e)
    {
        (bool Success, User? User) auth = Token.Authenticate(e);
        if (!auth.Success || auth.User == null)
        {
            return Unauthorized();
        }

        JsonNode? json = JsonNode.Parse(e.Payload);
        if (json == null || json["cardId"] == null)
        {
            return BadRequest("Invalid payload.");
        }

        int cardId = (int)json["cardId"]!;
        bool success = CardRepository.RemoveCardFromDeck(auth.User.Username, cardId);

        if (success)
        {
            return Ok("Card removed from deck.");
        }
        else
        {
            return BadRequest("Failed to remove card from deck.");
        }
    }

    [Route("GET", "deck")]
    private (int Status, JsonObject? Reply) GetDeckCards(HttpSvrEventArgs e)
    {
        (bool Success, User? User) auth = Token.Authenticate(e);
        if (!auth.Success || auth.User == null)
        {
            return Unauthorized();
        }

        var deckCards = CardRepository.GetDeckCards(auth.User.Username);

        JsonObject reply = new() { ["success"] = true };
        var cardArray = new JsonArray();

        foreach (var card in deckCards)
        {
            var cardObject = new JsonObject
            {
                ["id"] = card.Id,
                ["name"] = card.Name,
                ["damage"] = card.Damage,
                ["element"] = card.Element,
                ["cardtype"] = card.CardType
            };
            cardArray.Add(cardObject);
        }

        reply["cards"] = cardArray;
        return (HttpStatusCode.OK, reply);
    }

    [Route("PUT", "deck")]
    private (int Status, JsonObject? Reply) ConfigureDeck(HttpSvrEventArgs e)
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Invalid request." };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                // Authenticate user
                (bool Success, User? User) auth = Token.Authenticate(e);
                if (!auth.Success || auth.User == null)
                {
                    return Unauthorized();
                }

                // Parse payload
                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json is JsonObject jsonObj && jsonObj["cardIds"] is JsonArray cardIdsJson)
                {
                    var cardIds = new List<int>();
                    foreach (var cardIdNode in cardIdsJson)
                    {
                        if (cardIdNode is JsonValue cardIdValue && cardIdValue.TryGetValue(out int cardId))
                        {
                            cardIds.Add(cardId);
                        }
                    }

                    // Use repository to configure deck
                    CardRepository.ConfigureDeck(auth.User.Username, cardIds);

                    status = HttpStatusCode.OK;
                    reply["success"] = true;
                    reply["message"] = "Deck configured successfully.";
                }
                else
                {
                    reply["message"] = "Invalid card IDs.";
                }
            }
            catch (Exception ex)
            {
                reply["message"] = ex.Message;
            }

            return (status, reply);
        }
}