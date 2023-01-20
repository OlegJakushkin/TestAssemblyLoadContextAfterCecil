using LibraryUser;
using Nethermind.Test.Utils;
using TestInfluence;
using TestLibrary;

namespace TestLibraryUser
{
    [TestFixture]
    public class LibraryTests
    {
        [Test]
        public void TestDafault()
        {
            var user = new UserOfTheLibrary();

            Assert.Pass();
        }

        //Note that this test is as similar as possible to one above. And is representative to real use case
        [Test]
        public void TestWithUpdatedConstructor()
        {
            //Make LibraryToBeModified constructor add one to TestInjection counter. This creates new DLL.
            InsertionFactory.ModifyTypeConstructor<LibraryToBeModified>(typeof(TestInjection), nameof(TestInjection.AddCounter));

            //Create a new AssemblyLoadContext to load the modified libraries respecting new DLLs from InsertionFactory
            AssemblyLoadContextThatUsesInsertionFactoryLibs alc = new();

            //Get an assembly (note that Assembly with UserOfTheLibrary is dependent on the Assembly of LibraryToBeModified
            var assembly = alc.LoadFromAssemblyName(typeof(UserOfTheLibrary).Assembly.GetName());

            //Instantiate an item
            var userType = assembly.GetType(nameof(UserOfTheLibrary));
            object result = Activator.CreateInstance(userType);

            //Check that modified dll actually worked
            Assert.AreEqual(TestInjection.Counter, 2);
            Assert.Pass();
        }
    }
}