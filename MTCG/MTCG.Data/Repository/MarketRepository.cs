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
        public static async Task<bool> CreateTradeOffer(string username, int offeredCardId, int requestedCardId)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO trade_offers (username, offered_card_id, requested_card_id)
                  VALUES (@uname, @offeredCardId, @requestedCardId);", conn);

            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("offeredCardId", offeredCardId);
            cmd.Parameters.AddWithValue("requestedCardId", requestedCardId);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        /// <summary>
        /// Retrieves all trade offers.
        /// </summary>
        public static async Task<List<TradeOffer>> GetAllTradeOffers()
        {
            var result = new List<TradeOffer>();

            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT offer_id, username, offered_card_id, requested_card_id
                  FROM trade_offers;", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
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
        public static async Task<bool> AcceptTradeOffer(int offerId, string username)
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
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        Console.WriteLine("Offer does not exist");
                        return false; // Offer does not exist
                    }

                    string initialOwner = reader.GetString(0);
                    int offeredCardId = reader.GetInt32(1);
                    int requestedCardId = reader.GetInt32(2);

                    reader.Close();

                    // Check if the user owns the requested card and does not own the offered card
                    if (!(await UserOwnsCard(username, requestedCardId, conn, trans)) || (await UserOwnsCard(username, offeredCardId, conn, trans)))
                    {
                        Console.WriteLine("User does not own the requested card or already owns the offered card.");
                        return false;
                    }
                    if(!(await UserOwnsCard(initialOwner, offeredCardId, conn, trans)))
                    {
                        Console.WriteLine("Initial owner does not own the offered card.");
                        return false;
                    }

                    // Perform the trade
                    await SwapCards(username, initialOwner, offeredCardId, requestedCardId, conn, trans);

                    // Remove the offer
                    using (var deleteCmd = new NpgsqlCommand(
                        @"DELETE FROM trade_offers WHERE offer_id = @offerId;", conn, trans))
                    {
                        deleteCmd.Parameters.AddWithValue("offerId", offerId);
                        await deleteCmd.ExecuteNonQueryAsync();
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
        private static async Task<bool> UserOwnsCard(string username, int cardId, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM user_cards WHERE username = @uname AND card_id = @cardId;", conn, trans);

            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("cardId", cardId);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Swaps cards between two users.
        /// </summary>
        private static async Task<bool> SwapCards(string username, string initialOwner, int offeredCardId, int requestedCardId, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            // Remove the requested card from the user
            using (var cmd = new NpgsqlCommand(
                @"DELETE FROM user_cards WHERE username = @uname AND card_id = @requestedCardId;", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", username);
                cmd.Parameters.AddWithValue("requestedCardId", requestedCardId);
                await cmd.ExecuteNonQueryAsync();
            }

            // Add the offered card to the user
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO user_cards (username, card_id, is_in_deck) VALUES (@uname, @offeredCardId, FALSE);", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", username);
                cmd.Parameters.AddWithValue("offeredCardId", offeredCardId);
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new NpgsqlCommand(
                @"DELETE FROM user_cards WHERE username = @uname AND card_id = @requestedCardId;", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", initialOwner);
                cmd.Parameters.AddWithValue("requestedCardId", offeredCardId);
                await cmd.ExecuteNonQueryAsync();
            }

            // Add the offered card to the user
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO user_cards (username, card_id, is_in_deck) VALUES (@uname, @offeredCardId, FALSE);", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", initialOwner);
                cmd.Parameters.AddWithValue("offeredCardId", requestedCardId);
                await cmd.ExecuteNonQueryAsync();
            }
            return true;
        }
    }
} 