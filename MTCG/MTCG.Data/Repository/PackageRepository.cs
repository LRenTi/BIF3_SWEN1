using Npgsql;
using System;
using System.Collections.Generic;
using MTCG.Core.Entities;
using System.Collections.Concurrent;


namespace MTCG.Data.Repository;
public static class PackageRepository
{
    
    public static async Task<bool> CreatePackage(int? packageId, List<Card> cards, int price)
    {
        if (cards.Count != 5)
        {
            throw new Exception("Ein Paket muss genau 5 Karten enthalten!");
        }

        using var conn = Connection.GetConnection();
        using var trans = conn.BeginTransaction();

        try
        {
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO packages (package_id, price) VALUES (@pid, @p)", conn, trans))
            {
                cmd.Parameters.AddWithValue("pid", packageId);
                cmd.Parameters.AddWithValue("p", price);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var c in cards)
            {
                using (var cmd = new NpgsqlCommand(
                           @"INSERT INTO cards (name, damage, element, card_type) 
                          VALUES (@cname, @cdmg, @celem, @ctype) RETURNING card_id;",
                           conn, trans))
                {
                    cmd.Parameters.AddWithValue("cname", c.Name);
                    cmd.Parameters.AddWithValue("cdmg", c.Damage);
                    cmd.Parameters.AddWithValue("celem", c.Element);
                    cmd.Parameters.AddWithValue("ctype", c is MonsterCard ? "monster" : "spell");

                    c.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    if (c.Id == null)
                    {
                        throw new Exception("Karte konnte nicht in der Datenbank erstellt werden.");
                    }
                }

                using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO packages_cards (package_id, card_id) VALUES (@pid, @cid)", conn, trans))
                {
                    cmd.Parameters.AddWithValue("pid", packageId);
                    cmd.Parameters.AddWithValue("cid", c.Id);
                    await cmd.ExecuteNonQueryAsync();
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

    public static async Task<bool> PurchasePackage(string packageIdString, User user)
    {
        if (!int.TryParse(packageIdString, out int packageId))
        {
            return false;
        }

        using var conn = Connection.GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            int? price = await GetPackagePrice(packageId, conn, trans);
            if (price == null || user.Coins < price)
            {
                return false;
            }

            var cardIds = new List<int>();
            using (var cmd = new NpgsqlCommand(
                @"SELECT card_id FROM packages_cards WHERE package_id = @pid;", conn, trans))
            {
                cmd.Parameters.AddWithValue("pid", packageId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    cardIds.Add(reader.GetInt32(0));
                }
            }

            if (cardIds.Count != 5)
            {
                Console.WriteLine($"Das Paket {packageId} hat nicht genau 5 Karten.");
                return false;
            }

            int newCoins = (int)(user.Coins - price);
            using (var cmd = new NpgsqlCommand(
                @"UPDATE users
                  SET coins = @c
                  WHERE username = @uname;", conn, trans))
            {
                cmd.Parameters.AddWithValue("c", newCoins);
                cmd.Parameters.AddWithValue("uname", user.Username);
                await cmd.ExecuteNonQueryAsync();
            }
            user.Coins = newCoins;

            using (var cmd = new NpgsqlCommand(
                       @"INSERT INTO user_cards (username, card_id, is_in_deck)
                         SELECT @uname, card_id, false
                         FROM packages_cards
                         WHERE package_id = @pid
                         ON CONFLICT DO NOTHING;", conn, trans))
            {
                cmd.Parameters.AddWithValue("uname", user.Username);
                cmd.Parameters.AddWithValue("pid", packageId);
                await cmd.ExecuteNonQueryAsync();
            }

            trans.Commit();
            Console.WriteLine($"Paket {packageId} erfolgreich gekauft.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Kauf des Pakets: {ex.Message}");
            trans.Rollback();
            return false;
        }
    }

    public static async Task<int?> GetPackagePrice(int packageId, NpgsqlConnection conn, NpgsqlTransaction trans)
    {
        using var cmd = new NpgsqlCommand(
            @"SELECT price FROM packages WHERE package_id = @pid;", conn, trans);
        cmd.Parameters.AddWithValue("pid", packageId);
        object? oprice = await cmd.ExecuteScalarAsync();
        
        return oprice != null ? Convert.ToInt32(oprice) : null;
    }

    public static async Task<List<Package>> GetAllPackagesWithCards()
    {
        var packages = new List<Package>();

        using var conn = Connection.GetConnection();
        using var cmd = new NpgsqlCommand(
            @"SELECT p.package_id, p.price, c.card_id, c.name, c.damage, c.element, c.card_type
              FROM packages p
              JOIN packages_cards pc ON p.package_id = pc.package_id
              JOIN cards c ON pc.card_id = c.card_id;", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int packageId = reader.GetInt32(0);
            decimal price = reader.GetDecimal(1);
            int cardId = reader.GetInt32(2);
            string cardName = reader.GetString(3);
            int damage = reader.GetInt32(4);
            string element = reader.GetString(5);
            string cardType = reader.GetString(6);

            Card card;
            if (cardType == "monster")
            {
                card = new MonsterCard(cardId, cardName, damage, element, cardType);
            }
            else
            {
                card = new SpellCard(cardId, cardName, damage, element, cardType);
            }

            var package = packages.Find(p => p.Id.ToString() == packageId.ToString());
            if (package == null)
            {
                package = new Package(packageId.ToString(), new List<Card>(), (int)price);
                packages.Add(package);
            }
            package.Cards.Add(card);
        }

        return packages;
    }
}