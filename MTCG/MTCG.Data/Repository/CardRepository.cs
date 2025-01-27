using Npgsql;
using MTCG.Core.Entities;

namespace MTCG.Data.Repository;

    public static class CardRepository
    {

        public static async Task<bool> AddCard(Card card)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO cards (name, damage, element, card_type)
                  VALUES (@name, @damage, @element, @ctype);", conn);
            
            cmd.Parameters.AddWithValue("name", card.Name);
            cmd.Parameters.AddWithValue("damage", card.Damage);
            cmd.Parameters.AddWithValue("element", card.Element);
            cmd.Parameters.AddWithValue("ctype", card is MonsterCard ? "monster" : "spell");

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public static async Task<bool> AddUserCard(string username, string cardId, bool isInDeck)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO user_cards (username, card_id, is_in_deck)
                  VALUES (@uname, @cid, @isInDeck);", conn);

            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("cid", cardId);
            cmd.Parameters.AddWithValue("isInDeck", isInDeck);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public static async Task<List<Card>> GetCardsByUser(string username)
        {
            var result = new List<Card>();

            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT c.card_id, c.name, c.damage, c.element, c.card_type, uc.is_in_deck
                  FROM cards c
                  JOIN user_cards uc ON c.card_id = uc.card_id
                  WHERE uc.username = @uname;", conn);

            cmd.Parameters.AddWithValue("uname", username);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int cardId = reader.GetInt32(0);
                string cardName = reader.GetString(1);
                int damage = reader.GetInt32(2);
                string element = reader.GetString(3);
                string ctype = reader.GetString(4);
                bool isInDeck = reader.GetBoolean(5);

                Card card;
                if (ctype == "monster")
                {
                    card = new MonsterCard(cardId, cardName, damage, element,ctype);
                }
                else
                {
                    card = new SpellCard(cardId, cardName, damage, element, ctype);
                }
                result.Add(card);
            }

            return result;
        }

        public static async Task<Card?> GetCardById(int cardId)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT card_id, name, damage, element, card_type
                  FROM cards
                  WHERE card_id = @cid;", conn);

            cmd.Parameters.AddWithValue("cid", cardId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string cname = reader.GetString(1);
                int dmg = reader.GetInt32(2);
                string elem = reader.GetString(3);
                string ctype = reader.GetString(4);

                Card card = (ctype == "monster")
                    ? new MonsterCard(id, cname, dmg, elem,ctype)
                    : new SpellCard(id, cname, dmg, elem,ctype);

                return card;
            }
            return null;
        }

        public static async Task<List<Card>> GetAllCards()
        {
            var result = new List<Card>();

            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT card_id, name, damage, element, card_type
                  FROM cards;", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int cardId = reader.GetInt32(0);
                string cardName = reader.GetString(1);
                int damage = reader.GetInt32(2);
                string element = reader.GetString(3);
                string cardtype = reader.GetString(4);

                Card card;
                if (cardtype == "monster")
                {
                    card = new MonsterCard(cardId, cardName, damage, element, cardtype);
                }
                else
                {
                    card = new SpellCard(cardId, cardName, damage, element, cardtype);
                }
                result.Add(card);
            }

            return result;
        }

        public static async Task<bool> ConfigureDeck(string username, List<int> cardIds)
        {
            if (cardIds.Count > 4)
                throw new Exception("A Deck cannot have more than 4 cards");

            using var conn = Connection.GetConnection();
            using var trans = conn.BeginTransaction();

            try
            {
                using (var cmd = new NpgsqlCommand(
                    @"DELETE FROM user_cards WHERE username = @uname AND is_in_deck = TRUE", conn, trans))
                {
                    cmd.Parameters.AddWithValue("uname", username);
                    await cmd.ExecuteNonQueryAsync();
                }

                foreach (var cid in cardIds)
                {
                    using (var cmdCheck = new NpgsqlCommand(
                        @"SELECT username FROM user_cards WHERE card_id = @cid", conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("cid", cid);
                        object? ownerObj = await cmdCheck.ExecuteScalarAsync();
                    }

                    using (var cmdInsert = new NpgsqlCommand(
                        @"UPDATE user_cards SET is_in_deck = TRUE WHERE username = @uname AND card_id = @cid", conn, trans))
                    {
                        cmdInsert.Parameters.AddWithValue("uname", username);
                        cmdInsert.Parameters.AddWithValue("cid", cid);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                }

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                return false;
            }
        }

        public static async Task<List<Card>> GetDeckCards(string username)
        {
            var result = new List<Card>();

            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT c.card_id, c.name, c.damage, c.element, c.card_type
                  FROM user_cards uc
                  JOIN cards c ON uc.card_id = c.card_id
                  WHERE uc.username = @uname AND uc.is_in_deck = TRUE;", conn);
            cmd.Parameters.AddWithValue("uname", username);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int cardId = reader.GetInt32(0);
                string cardName = reader.GetString(1);
                int damage = reader.GetInt32(2);
                string element = reader.GetString(3);
                string cardType = reader.GetString(4);

                Card card;
                if (cardType == "monster")
                {
                    card = new MonsterCard(cardId, cardName, damage, element, cardType);
                }
                else
                {
                    card = new SpellCard(cardId, cardName, damage, element, cardType);
                }
                result.Add(card);
            }

            return result;
        }

        public static async Task<bool> AddCardToDeck(string username, int cardId)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"UPDATE user_cards SET is_in_deck = TRUE 
                  WHERE username = @uname AND card_id = @cid;", conn);
            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("cid", cardId);
            
            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public static async Task<bool> RemoveCardFromDeck(string username, int cardId)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"UPDATE user_cards SET is_in_deck = FALSE 
                  WHERE username = @uname AND card_id = @cid;", conn);
            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("cid", cardId);
            
            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    
        public static async Task<List<int>> GetDeckCardIds(string username)
        {
            var result = new List<int>();

            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT card_id 
                  FROM user_cards
                  WHERE username = @uname AND is_in_deck = TRUE;", conn);

            cmd.Parameters.AddWithValue("uname", username);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetInt32(0));
            }

            return result;
        }
    }