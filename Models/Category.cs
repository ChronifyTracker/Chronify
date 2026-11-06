namespace Chronify
{
    public class Category
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; private set; } = string.Empty;
        public string Color { get; private set; } = string.Empty;

        public Category(string name, string color)
        {
            Name = name;
            Color = color;
        }

        public Category()
        {
        }

        public void Update(string name, string color)
        {
            Name = name;
            Color = color;
        }
    }
}
