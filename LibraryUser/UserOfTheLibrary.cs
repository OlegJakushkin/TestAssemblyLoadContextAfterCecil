using TestLibrary;

namespace LibraryUser
{
    public class UserOfTheLibrary
    {
        public void ToTest()
        {
            var ctx1 = new LibraryToBeModified();
            var ctx2 = new LibraryToBeModified();
            Console.WriteLine($"{ctx1} {ctx2}");
        }
    }
}