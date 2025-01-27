namespace MTCG.Core.Entities;

    public class Package
    {
        public string Id { get; private set; }
        public List<Card> Cards { get; private set; }
        public int Price { get; private set; } 


        public Package(string id,List<Card> cards, int price)
        {
            Id = id;
            Cards = cards;
            Price = price;
        }
    }