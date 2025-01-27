using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using MTCG.Data.Repository;
using MTCG.Security;
using MTCG.Core.Entities;

namespace MTCG;

    public class BattleHandler : Handler, IHandler
    {
        /// <summary>
        /// Users, which is waiting for an opponent.
        /// </summary>
        private static User? WaitingUser = null;

        /// <summary>
        /// Start a battle. 
        /// </summary>
        /// <param name="e">HttpSvrEventArgs</param>
        /// <returns>Status and Reply</returns>
        [Route("POST", "battles")]
        private async Task<(int Status, JsonObject? Reply)> StartBattle(HttpSvrEventArgs e)
        {
            JsonObject reply = new() { ["success"] = false, ["message"] = "Something went wrong." };
            int status = HttpStatusCode.BAD_REQUEST;

            Console.WriteLine("Starting Battle");
            try
            {
                var auth = await Token.Authenticate(e);
                User? attacker = auth.User;

                if (!auth.Success || attacker == null)
                {
                    return Unauthorized("Please login first.");
                }
                else
                {

                    var attackerDeck = await CardRepository.GetDeckCardIds(attacker.Username);
                    if (attackerDeck.Count > 4)
                        throw new Exception($"User {attacker.Username} has {attackerDeck.Count} Cards in the Deck (it's not allowed to have more than 4 cards).");
                    
                        if (WaitingUser == null)
                        {
                            WaitingUser = attacker;
                            status = HttpStatusCode.OK;
                            reply["success"] = true;
                            reply["message"] = $"You ({attacker.Username}) are in the waiting queue. Please wait for an Opponent...";
                        }
                        else
                        {
                            var defender = WaitingUser;
                            WaitingUser = null;

                            var defenderDeck = await CardRepository.GetDeckCardIds(defender.Username);
                            if (defenderDeck.Count > 4)
                                throw new Exception($"Enemy {defender.Username} has {defenderDeck.Count} more cards in the deck than allowed (max 4).");

                            (var logLines, var outcome) = await DoBattle(attacker, defender);
                            status = HttpStatusCode.OK;
                            reply["success"] = true;
                            reply["message"] = "BATTLE STARTED!";
                            JsonArray arr = new();
                            foreach (var line in logLines) arr.Add(line);
                            reply["battlelog"] = arr;
                            reply["outcome"] = outcome;
                        }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return (status, reply);
        }

        /// <summary>
        /// Spiel geht maximal 100 Runden.
        /// Die Person, die am Ende mehr Runden gewinnt, gewinnt den Kampf.
        /// Während des Spiels verliert der Verlierer seine Karten.
        /// </summary>
        private async Task<(List<string> Log, string Outcome)> DoBattle(User attacker, User defender)
        {
            var battleLog = new List<string>();

            var attackerDeckIds = await CardRepository.GetDeckCardIds(attacker.Username);
            var defenderDeckIds = await CardRepository.GetDeckCardIds(defender.Username);

            if (attackerDeckIds.Count == 0)
                throw new Exception($"User {attacker.Username} doesn't have any cards in the deck.");
            if (defenderDeckIds.Count == 0)
                throw new Exception($"User {defender.Username} doesn't have any cards in the deck.");

            var allCardStates = new List<CardState>();

            foreach (var cid in attackerDeckIds)
            {
                var c = await CardRepository.GetCardById(cid);
                if (c != null)
                {
                    allCardStates.Add(new CardState
                    {
                        Card = c,
                        Owner = attacker.Username,
                        InitialOwner = attacker.Username
                    });
                }
            }

            foreach (var cid in defenderDeckIds)
            {
                var c = await CardRepository.GetCardById(cid);
                if (c != null)
                {
                    allCardStates.Add(new CardState
                    {
                        Card = c,
                        Owner = defender.Username,
                        InitialOwner = defender.Username
                    });
                }
            }

            int attackerRoundWins = 0;
            int defenderRoundWins = 0;
            int drawCount = 0;

            Random rnd = new();

            for (int round = 1; round <= 100; round++)
            {
                var attackerSubset = allCardStates.Where(x => x.Owner == attacker.Username).ToList();
                var defenderSubset = allCardStates.Where(x => x.Owner == defender.Username).ToList();

                CardState? attackerCardState = null;
                CardState? defenderCardState = null;

                if (attackerSubset.Count == 0 && defenderSubset.Count > 0)
                {
                    battleLog.Add($"Battle ends because '{attacker.Username}' has no cards.");
                    break;
                }
                if (defenderSubset.Count == 0 && attackerSubset.Count > 0)
                {
                    battleLog.Add($"Battle ends because '{defender.Username}' has no cards."); 
                    break;
                }
                if (attackerSubset.Count > 0 && defenderSubset.Count > 0)
                {
                    attackerCardState = attackerSubset[rnd.Next(attackerSubset.Count)];
                    defenderCardState = defenderSubset[rnd.Next(defenderSubset.Count)];

                    float attackerDamage = ComputeDamage(attackerCardState.Card, defenderCardState.Card);
                    float defenderDamage = ComputeDamage(defenderCardState.Card, attackerCardState.Card);

                    if (IsGoblinVsDragon(attackerCardState.Card, defenderCardState.Card))
                        defenderDamage = float.MaxValue;
                    if (IsGoblinVsDragon(defenderCardState.Card, attackerCardState.Card))
                        attackerDamage = float.MaxValue;
                    if (IsWizardVsOrk(attackerCardState.Card, defenderCardState.Card))
                        attackerDamage = float.MaxValue;
                    if (IsWizardVsOrk(defenderCardState.Card, attackerCardState.Card))
                        defenderDamage = float.MaxValue;
                    if (IsKnightVsWaterSpell(attackerCardState.Card, defenderCardState.Card))
                        defenderDamage = float.MaxValue;
                    if (IsKnightVsWaterSpell(defenderCardState.Card, attackerCardState.Card))
                        attackerDamage = float.MaxValue;
                    if (IsKrakenImmune(attackerCardState.Card, defenderCardState.Card))
                        attackerDamage = float.MaxValue;
                    if (IsKrakenImmune(defenderCardState.Card, attackerCardState.Card))
                        defenderDamage = float.MaxValue;
                    if (IsFireElvesEvadeDragon(attackerCardState.Card, defenderCardState.Card))
                        attackerDamage = float.MaxValue;
                    if (IsFireElvesEvadeDragon(defenderCardState.Card, attackerCardState.Card))
                        defenderDamage = float.MaxValue;

                    if (Math.Abs(attackerDamage - defenderDamage) < 0.001f)
                    {
                        battleLog.Add($"Round {round}: {attacker.Username}'s {attackerCardState.Card.Name} vs {defender.Username}'s {defenderCardState.Card.Name} => DRAW.");
                        drawCount++;
                    }
                    else if (attackerDamage > defenderDamage)
                    {
                        defenderCardState.Owner = attacker.Username;
                        attackerRoundWins++;
                        battleLog.Add($"Round {round}: {attacker.Username}'s {attackerCardState.Card.Name} defeated {defender.Username}'s {defenderCardState.Card.Name} => {attacker.Username} wins.");
                    }
                    else
                    {
                        attackerCardState.Owner = defender.Username;
                        defenderRoundWins++;
                        battleLog.Add($"Round {round}: {defender.Username}'s {defenderCardState.Card.Name} defeated {attacker.Username}'s {attackerCardState.Card.Name} => {defender.Username} wins.");
                    }
                }
            }

            string outcome;
            if (attackerRoundWins > defenderRoundWins)
            {
                outcome = $"{attacker.Username} wins the Battle ({attackerRoundWins} vs. {defenderRoundWins}). Draws: {drawCount}";
                attacker.Elo += 5;
                attacker.Wins++;
                attacker.Games++;
                attacker.Save();
                defender.Games++;
                defender.Losses++;
                defender.Elo -= 3;
                defender.Save();

            }
            else if (defenderRoundWins > attackerRoundWins)
            {
                outcome = $"{defender.Username} wins the Battle ({defenderRoundWins} vs. {attackerRoundWins}). Draws: {drawCount}";
                defender.Elo -= 3;
                defender.Losses++;
                defender.Games++;
                defender.Save();
                attacker.Games++;
                attacker.Wins++;
                attacker.Elo += 5;
                attacker.Save();
            }
            else
            {
                outcome = $"DRAW (Round-Wins: {attackerRoundWins} vs. {defenderRoundWins}). Draws: {drawCount}";
            }

            return (battleLog, outcome);
        }

        public static float ComputeDamage(Card source, Card target)
        {
            bool isSpellA = source is SpellCard;
            bool isSpellB = target is SpellCard;
            if (isSpellA || isSpellB)
            {
                float m = GetElementMultiplier(source.Element.ToLower(), target.Element.ToLower());
                return source.Damage * m;
            }
            return source.Damage;
        }

        private static float GetElementMultiplier(string selem, string telem)
        {
            if (selem == "water" && telem == "fire") return 2f;
            if (selem == "fire" && telem == "normal") return 2f;
            if (selem == "normal" && telem == "water") return 2f;

            if (selem == "fire" && telem == "water") return 0.5f;
            if (selem == "water" && telem == "normal") return 0.5f;
            if (selem == "normal" && telem == "fire") return 0.5f;

            return 1f;
        }

        public static bool IsGoblinVsDragon(Card c1, Card c2)
        {
            return c1.Name.ToLower().Contains("goblin") && c2.Name.ToLower().Contains("dragon");
        }
        public static bool IsWizardVsOrk(Card c1, Card c2)
        {
            return c1.Name.ToLower().Contains("wizard") && c2.Name.ToLower().Contains("ork");
        }
        public static bool IsKnightVsWaterSpell(Card c1, Card c2)
        {
            if (c1.Name.ToLower().Contains("knight") && c2 is SpellCard s && s.Element.ToLower() == "water")
                return true;
            return false;
        }
        public static bool IsKrakenImmune(Card cA, Card cB)
        {
            if (cA.Name.ToLower().Contains("kraken") && cB is SpellCard)
                return true;
            return false;
        }
        public static bool IsFireElvesEvadeDragon(Card cA, Card cB)
        {
            if (cA.Name.ToLower().Contains("fireelves") && cB.Name.ToLower().Contains("dragon"))
                return true;
            if (cA.Name.ToLower().Contains("fireelf") && cB.Name.ToLower().Contains("dragon"))
                return true;
            return false;
        }
    }

    public class CardState
    {
        public Card Card { get; set; } = default!;
        public string Owner { get; set; } = "";
        public string InitialOwner { get; set; } = "";
    }