namespace MTCG;

public class Card
{
    /// <summary>Eindeutige ID der Karte.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Name der Karte.</summary>
    public string Name { get; private set; }

    /// <summary>Schadenswert der Karte.</summary>
    public int Damage { get; private set; }

    /// <summary>Elementtyp der Karte.</summary>
    public ElementType ElementType { get; private set; }

    /// <summary>Art der Karte (Monster oder Zauber).</summary>
    public CardType CardType { get; private set; }

    public Card(string name, int damage, ElementType elementType, CardType cardType)
    {
        Name = name;
        Damage = damage;
        ElementType = elementType;
        CardType = cardType;
    }

    /// <summary>Berechnet den effektiven Schaden gegen eine andere Karte.</summary>
    public virtual int CalculateEffectiveDamage(Card opponent)
    {
        // Spezialregeln prüfen
        if (IsSpecialRule(opponent))
            return 0;

        // Wenn mindestens eine Zauberkarte beteiligt ist
        if (CardType == CardType.Spell || opponent.CardType == CardType.Spell)
        {
            return ElementType switch
            {
                ElementType.Water when opponent.ElementType == ElementType.Fire => Damage * 2,
                ElementType.Fire when opponent.ElementType == ElementType.Normal => Damage * 2,
                ElementType.Normal when opponent.ElementType == ElementType.Water => Damage * 2,
                ElementType.Fire when opponent.ElementType == ElementType.Water => Damage / 2,
                ElementType.Normal when opponent.ElementType == ElementType.Fire => Damage / 2,
                ElementType.Water when opponent.ElementType == ElementType.Normal => Damage / 2,
                _ => Damage
            };
        }

        return Damage; // Bei reinen Monster-Kämpfen
    }

    private bool IsSpecialRule(Card opponent)
    {
        return
            (Name.Contains("Goblin") && opponent.Name.Contains("Dragon")) ||
            (Name.Contains("Ork") && opponent.Name.Contains("Wizzard")) ||
            (Name.Contains("Knight") && opponent.CardType == CardType.Spell && opponent.ElementType == ElementType.Water) ||
            (opponent.Name.Contains("Kraken") && CardType == CardType.Spell) ||
            (opponent.Name.Contains("FireElves") && Name.Contains("Dragon"));
    }
} 