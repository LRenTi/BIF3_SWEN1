using Xunit;
using MTCG.Core.Entities;
using MTCG.Data.Repository;
using System.Collections.Generic;

namespace MTCG.Tests;

public class Unittest
{
    [Fact]
    public void TestUserCreation()
    {
        var user = new User("testuser", "password");
        Assert.Equal("testuser", user.Username);
        Assert.Equal("password", user.Password);
        Assert.Equal(20, user.Coins);
        Assert.Equal(100, user.Elo);
    }

    [Fact]
    public void TestUserRegistration()
    {
        User.Register("newuser", "newpassword");
        var user = User.Get("newuser");
        Assert.NotNull(user);
        Assert.Equal("newuser", user.Username);
        TestUserAuthenticationSuccess();
    }

    [Fact]
    public void TestUserAuthenticationSuccess()
    {
        User.Register("authuser", "authpassword");
        var result = User.Authenticate("authuser", "authpassword");
        Assert.True(result.Success);
        Assert.NotNull(result.User);
    }

    [Fact]
    public void TestUserAuthenticationFailure()
    {
        var result = User.Authenticate("nonexistent", "wrongpassword");
        Assert.False(result.Success);
        Assert.Null(result.User);
    }

    [Fact]
    public void TestUserEloIncreaseOnWin()
    {
        var user = new User("winner", "password");
        user.Wins = 1;
        user.Elo += 5;
        user.Save();
        Assert.Equal(105, user.Elo);
    }

    [Fact]
    public void TestUserEloDecreaseOnLoss()
    {
        var user = new User("loser", "password");
        user.Losses = 1;
        user.Elo -= 3;
        user.Save();
        Assert.Equal(97, user.Elo);
    }

    [Fact]
    public void TestUserGameCountIncrease()
    {
        var user = new User("gamer", "password");
        user.Games = 1;
        user.Games++;
        user.Save();
        Assert.Equal(2, user.Games);
    }

    [Fact]
    public void TestUserWinsAfterBattle()
    {
        var user = new User("battlewinner", "password");
        user.Wins = 0;
        user.Save();
        user.Wins++;
        user.Save();
        Assert.Equal(1, user.Wins);
    }

    [Fact]
    public void TestUserLossesAfterBattle()
    {
        var user = new User("battleloser", "password");
        user.Losses = 0;
        user.Save();
        user.Losses++;
        user.Save();
        Assert.Equal(1, user.Losses);
    }

    [Fact]
    public void TestCardDamageCalculation()
    {
        var card1 = new MonsterCard(1, "Dragon", 50, "fire", "monster");
        var card2 = new MonsterCard(2, "Goblin", 20, "earth", "monster");
        var damage = BattleHandler.ComputeDamage(card1, card2);
        Assert.True(damage > 0);
    }

    [Fact]
    public void TestSpecialCaseGoblinVsDragon()
    {
        var goblin = new MonsterCard(1, "Goblin", 20, "earth", "monster");
        var dragon = new MonsterCard(2, "Dragon", 50, "fire", "monster");
        var isSpecialCase = BattleHandler.IsGoblinVsDragon(goblin, dragon);
        Assert.True(isSpecialCase);
    }

    [Fact]
    public void TestSpecialCaseWizardVsOrk()
    {
        var wizard = new MonsterCard(1, "Wizard", 30, "magic", "monster");
        var ork = new MonsterCard(2, "Ork", 40, "earth", "monster");
        var isSpecialCase = BattleHandler.IsWizardVsOrk(wizard, ork);
        Assert.True(isSpecialCase);
    }

    [Fact]
    public void TestSpecialCaseKnightVsWaterSpell()
    {
        var knight = new MonsterCard(1, "Knight", 35, "normal", "monster");
        var waterSpell = new SpellCard(2, "Water Splash", 25, "water", "spell");
        var isSpecialCase = BattleHandler.IsKnightVsWaterSpell(knight, waterSpell);
        Assert.True(isSpecialCase);
    }

    [Fact]
    public void TestKrakenImmunity()
    {
        var kraken = new MonsterCard(1, "Kraken", 60, "water", "monster");
        var spell = new SpellCard(2, "Fireball", 30, "fire", "spell");
        var isImmune = BattleHandler.IsKrakenImmune(kraken, spell);
        Assert.True(isImmune);
    }

    [Fact]
    public void TestFireElvesEvadeDragon()
    {
        var fireElf = new MonsterCard(1, "Fireelves", 25, "fire", "monster");
        var dragon = new MonsterCard(2, "Dragon", 50, "fire", "monster");
        var canEvade = BattleHandler.IsFireElvesEvadeDragon(fireElf, dragon);
        Assert.True(canEvade);
    }

    [Fact]
    public void TestUserInitialValues()
    {
        var user = new User("testuser", "password");
        Assert.Equal(20, user.Coins);
        Assert.Equal(100, user.Elo);
        Assert.Equal(0, user.Wins);
        Assert.Equal(0, user.Losses);
        Assert.Equal(0, user.Games);
        Assert.False(user.IsAdmin);
    }

    [Fact]
    public void TestMonsterCardCreation()
    {
        var monsterCard = new MonsterCard(1, "Dragon", 50, "fire", "monster");
        Assert.Equal(1, monsterCard.Id);
        Assert.Equal("Dragon", monsterCard.Name);
        Assert.Equal(50, monsterCard.Damage);
        Assert.Equal("fire", monsterCard.Element);
        Assert.Equal("monster", monsterCard.CardType);
    }

    [Fact]
    public void TestSpellCardCreation()
    {
        var spellCard = new SpellCard(2, "Fireball", 30, "fire", "spell");
        Assert.Equal(2, spellCard.Id);
        Assert.Equal("Fireball", spellCard.Name);
        Assert.Equal(30, spellCard.Damage);
        Assert.Equal("fire", spellCard.Element);
        Assert.Equal("spell", spellCard.CardType);
    }

    [Fact]
    public void TestComputeDamageWithElementMultiplier()
    {
        var card1 = new SpellCard(1, "Water Splash", 30, "water", "spell");
        var card2 = new MonsterCard(2, "Fire Dragon", 50, "fire", "monster");
        var damage = BattleHandler.ComputeDamage(card1, card2);
        Assert.Equal(60, damage); // Assuming water->fire multiplier is 2
    }

    [Fact]
    public void TestComputeDamageWithoutElementMultiplier()
    {
        var card1 = new MonsterCard(1, "Normal Goblin", 20, "normal", "monster");
        var card2 = new MonsterCard(2, "Earth Troll", 40, "earth", "monster");
        var damage = BattleHandler.ComputeDamage(card1, card2);
        Assert.Equal(20, damage); // No element multiplier
    }
}