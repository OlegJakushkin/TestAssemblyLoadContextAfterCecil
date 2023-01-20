namespace TestLibrary
{
    public class LibraryToBeModified
    {

        private static readonly Random Rnd = new();
        private readonly int _i;

        public LibraryToBeModified()
        {
            _i = Rnd.Next(1000, 9999);
        }
        public override string ToString()
        {
            return _i.ToString();
        }
    }
}