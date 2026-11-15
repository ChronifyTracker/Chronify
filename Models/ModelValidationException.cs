namespace Chronify
{
    public class ModelValidationException : Exception
    {
        public ModelError Error { get; }

        public ModelValidationException(ModelError error) : base(error.ToString())
        {
            Error = error;
        }
    }
}
