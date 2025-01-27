using MTCG.Core.Entities;
using MTCG.Security;
using MTCG.Data.Repository;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace MTCG;

    public class PackageHandler : Handler, IHandler
    {
        [Route("POST", "package")]
        private (int Status, JsonObject? Reply) CreatePackage(HttpSvrEventArgs e)
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Ungültige Anfrage." };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                // Authentifizieren
                (bool Success, User? User) auth = Token.Authenticate(e);
                if (!auth.Success || auth.User == null || auth.User.IsAdmin == false)
                {
                    return Unauthorized();
                }
                else
                {
                    // Payload parsen
                    JsonNode? json = JsonNode.Parse(e.Payload);
                    
                    
                    // Erwarte: JSON-Array von Packages:
                    // [
                    //   {
                    //     "Id": "...",
                    //     "Price": 8,            // optional
                    //     "Cards": [ {...}, {...}, ... ]
                    //   },
                    //   { "Id": "...", "Price": 5, "Cards": [ {...}, ... ] }
                    // ]
                    if (json is JsonArray packageArray)
                    {
                        foreach (var node in packageArray)
                        {
                            if (node is not JsonObject packageJson)
                                continue;

                            // "Id" Pflichtfeld
                            int? packageId = packageJson["Id"]?.GetValue<int>();
                            if (packageId == null)
                                continue;

                            Console.WriteLine("HERE");
                            // "Price" optional, default 5
                            int price = 5;
                            if (packageJson["Price"] != null)
                            {
                                // versuch, price einzulesen
                                int parsedPrice = packageJson["Price"]!.GetValue<int>();
                                if (parsedPrice > 0)
                                {
                                    price = parsedPrice;
                                }
                            }

                            // "Cards" muss ein Array mit 5 cards sein
                            if (packageJson["Cards"] is not JsonArray cardsJson)
                                continue;

                            // Sammeln der 5 Karten
                            List<Card> cardList = new();
                            foreach (var cardNode in cardsJson)
                            {
                                if (cardNode is not JsonObject cobj)
                                    continue;

                                int cardId = 0;
                                string cardName = cobj["name"]?.ToString() ?? "";
                                string element = cobj["element"]?.ToString() ?? "normal";
                                int damage = cobj["damage"]?.GetValue<int>() ?? 0;
                                string cardType = cobj["type"]?.ToString() ?? "monster";

                                Card newCard = (cardType == "monster")
                                    ? new MonsterCard(cardId, cardName, damage, element, cardType)
                                    : new SpellCard(cardId, cardName, damage, element, cardType);

                                cardList.Add(newCard);
                            }
                            
                            PackageRepository.CreatePackage(packageId, cardList, price);
                        }

                        status = HttpStatusCode.OK;
                        reply["success"] = true;
                        reply["message"] = "Packages erstellt.";
                    }
                }
            }
            catch (Exception ex)
            {
                reply["message"] = ex.Message;
            }
            
            return (status, reply);
        }
        
        //[Route("POST", "package/purchase/{id}")]
        
        /// <summary>
        /// Retrieves all packages with their prices and returns them as a JSON object.
        /// </summary>
        [Route("GET", "package")]
        private (int Status, JsonObject? Reply) GetAllPackages(HttpSvrEventArgs e)
        {
            var packages = PackageRepository.GetAllPackagesWithCards();
            var packageInfo = new JsonArray();

            foreach (var package in packages)
            {
                var packageJson = new JsonObject
                {
                    ["PackageId"] = package.Id,
                    ["Price"] = package.Price,
                    ["Cards"] = new JsonArray(package.Cards.Select(card => new JsonObject
                    {
                        ["CardId"] = card.Id,
                        ["Name"] = card.Name,
                        ["Damage"] = card.Damage,
                        ["Element"] = card.Element,
                        ["CardType"] = card is MonsterCard ? "monster" : "spell"
                    }).ToArray())
                };

                packageInfo.Add(packageJson);
            }

            var reply = new JsonObject
            {
                ["Packages"] = packageInfo
            };

            return (HttpStatusCode.OK, reply);
        }

        [Route("POST", "purchase/{packageId}")]
        private (int Status, JsonObject? Reply) PurchasePackage(HttpSvrEventArgs e)
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Fehler beim Kauf des Pakets." };
            int status = HttpStatusCode.BAD_REQUEST;

            try
            {
                // Authentifizieren
                (bool Success, User? User) auth = Token.Authenticate(e);
                if (!auth.Success || auth.User == null)
                {
                    return Unauthorized();
                }

                if (!e.RouteParams.TryGetValue("packageId", out var packageId))
                {
                    reply["message"] = "Keine Paket-ID angegeben.";
                    return (status, reply);
                }
                
                bool success = PackageRepository.PurchasePackage(packageId, auth.User);

                if (success)
                {
                    status = HttpStatusCode.OK;
                    reply["success"] = true;
                    reply["message"] = "Package purchased.";
                }
                else
                {
                    status = HttpStatusCode.BAD_REQUEST;
                    reply["success"] = false;
                    reply["message"] = "Something went wrong.";
                }

            }
            catch (Exception ex)
            {
                reply["message"] = ex.Message;
            }

            return (status, reply);
        }
    }