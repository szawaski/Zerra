namespace Pets.Service
{
    public interface IThing
    {
        public string Text { get; }
    }
    public sealed class Thing : IThing
    {
        public string Text { get; init; }

        public Thing(string text)
        {
            this.Text = text;
        }
    }
}
