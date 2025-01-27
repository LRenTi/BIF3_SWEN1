namespace MTCG.Core.Entities;

    public abstract class Card
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Damage { get; set; }
        public string Element { get; set; }
        public string CardType { get; set; }

        protected Card(int id, string name, int damage, string element, string cardType)
        {
            Id = id;
            Name = name;
            Damage = damage;
            Element = element;
            CardType = cardType;
        }
    }

    public class MonsterCard : Card
    {
        public MonsterCard(int id, string name, int damage, string element, string cardType)
            : base(id, name, damage, element, cardType)
        {
        }
    }

    public class SpellCard : Card
    {
        public SpellCard(int id, string name, int damage, string element, string cardType)
            : base(id, name, damage, element, cardType)
        {
        }
    }