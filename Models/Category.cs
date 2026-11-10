namespace Chronify
{
    public class Category
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; private set; } = string.Empty;
        public string Color { get; private set; } = string.Empty;

        public Category(string name, string color)
        {
            SetName(name);
            SetColor(color);
        }

        public Category()
        {
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Назва категорії обов'язкова.", nameof(name));

            Name = name.Trim();
        }

        public void SetColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                throw new ArgumentException("Колір категорії обов'язковий.", nameof(color));

            Color = color.Trim();
        }
    }
}
