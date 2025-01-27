using Npgsql;
using MTCG.Core.Entities;
using System.Collections.Generic;

namespace MTCG.Data.Repository
{
    public static class MarketRepository
    {
        /// <summary>
        /// Creates a trade offer.
        /// </summary>
        public static void CreateTradeOffer(string username, int offeredCardId, int requestedCardId)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO trade_offers (username, offered_card_id, requested_card_id)
                  VALUES (@uname, @offeredCardId, @requestedCardId);", conn);

            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("offeredCardId", offeredCardId);
            cmd.Parameters.AddWithValue("requestedCardId", requestedCardId);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves all trade offers.
        /// </summary>
        public static List<TradeOffer> GetAllTradeOffers()
        {
            var result = new List<TradeOffer>();

            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT offer_id, username, offered_card_id, requested_card_id
                  FROM trade_offers;", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int offerId = reader.GetInt32(0);
                string username = reader.GetString(1);
                int offeredCardId = reader.GetInt32(2);
                int requestedCardId = reader.GetInt32(3);

                result.Add(new TradeOffer(offerId, username, offeredCardId, requestedCardId));
            }

            return result;
        }

        /// <summary>
        /// Accepts a trade offer if the user owns the requested card and does not own the offered card.
        /// </summary>
        public static bool AcceptTradeOffer(int offerId, string username)
        {
            using var conn = Connection.GetConnection();
            using var trans = conn.BeginTransaction();

            try
            {
                // Check if the offer exists
                using (var cmd = new NpgsqlCommand(
                    @"SELECT username, offered_card_id, requested_card_id FROM trade_offers WHERE offer_id = @offerId;", conn, trans))
                {
                    cmd.Parameters.AddWithValue("offerId", offerId);
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        Console.WriteLine("Offer does not exist");
                        return false; // Offer does not exist
                    }

                    string initialOwner = reader.GetString(0);
                    int offeredCardId = reader.GetInt32(1);
                    int requestedCardId = reader.GetInt32(2);

                    reader.Close();

                    // Check if the user owns the requested card and does not own the offered card
                    if (!UserOwnsCard(username, requestedCardId, conn, trans) || UserOwnsCard(username, offeredCardId, conn, trans))
                    {
                        Console.WriteLine("User does not own the requested card or already owns the offered card.");
                        return false;
                    }
                    if(!UserOwnsCard(initialOwner, offeredCardId, conn, trans))
                    {
                        Console.WriteLine("Initial owner does not own the offered card.");
                        return false;
                    }

                    // Perform the trade
                    SwapCards(username, initialOwner, offeredCardId, requestedCardId, conn, trans);

                    // Remove the offer
                    using (var deleteCmd = new NpgsqlCommand(
                        @"DELETE FROM trade_offers WHERE offer_id = @offerId;", conn, trans))
                    {
                        deleteCmd.Parameters.AddWithValue("offerId", offerId);
                        deleteCmd.ExecuteNonQuery();
                    }
                }

                trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                trans.Rollback();
                Console.WriteLine(e);
                return false;
            }
        }

        /// <summary>
        /// Checks if a user owns a card.
        /// </summary>
        private static bool UserOwnsCard(string username, int cardId, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM user_cards WHERE username = @uname AND card_id = @cardId;", conn, trans);

            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("cardId", cardId);

            return (long)cmd.ExecuteScalar()! > 0;
        }

        /// <summary>
        /// Swaps cards between two users.
        /// </summary>
        private static void SwapCards(string username, string initialOwner, int offeredCardId, int requestedCardId, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            // Remove the requested card from the user
            using (var cmd = new NpgsqlCommand(
                @"DELETE FROM user_cards WHERE username = @uname AND card_id = @requestedCardId;", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", username);
                cmd.Parameters.AddWithValue("requestedCardId", requestedCardId);
                cmd.ExecuteNonQuery();
            }

            // Add the offered card to the user
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO user_cards (username, card_id, is_in_deck) VALUES (@uname, @offeredCardId, FALSE);", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", username);
                cmd.Parameters.AddWithValue("offeredCardId", offeredCardId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new NpgsqlCommand(
                @"DELETE FROM user_cards WHERE username = @uname AND card_id = @requestedCardId;", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", initialOwner);
                cmd.Parameters.AddWithValue("requestedCardId", offeredCardId);
                cmd.ExecuteNonQuery();
            }

            // Add the offered card to the user
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO user_cards (username, card_id, is_in_deck) VALUES (@uname, @offeredCardId, FALSE);", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", initialOwner);
                cmd.Parameters.AddWithValue("offeredCardId", requestedCardId);
                cmd.ExecuteNonQuery();
            }
        }
    }
} 