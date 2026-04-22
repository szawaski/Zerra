namespace Pets.Service.Services
{
    public sealed class Thing : IThing
    {
        public string Text { get; init; }

        public Thing(string text)
        {
            this.Text = text;
        }
    }
}
