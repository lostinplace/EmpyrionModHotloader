using Eleon.Modding;
using EmpyrionModHotloader;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using System.Linq;
using FakeItEasy;

namespace HotLoaderTests
{
    [Collection("Mod Manager")]
    public class ModManagerTests
    {
        private static Dictionary<string, ModInterface> expectedMods = new Dictionary<string, ModInterface>();

        [Fact]
        public void loadsBasicModsFromPath()
        {
            var dummyModPath = @"..\..\..\modFolderTest\dummy\DummyMod.dll";
            var resultMod = ModManager.loadMod(dummyModPath);
            Assert.Equal("DummyMod", resultMod.title);
            Assert.NotNull(resultMod.mod);
            Assert.False(Equals(resultMod, default(ModManager.ModItem)));
        }

        [Fact]
        public void loadedModActuallyWorks()
        {
            var fakeModApi = A.Fake<ModGameAPI>();

            var dummyModPath = @"..\..\..\modFolderTest\dummy\DummyMod.dll";
            var resultItem = ModManager.loadMod(dummyModPath);
            var mod = resultItem.mod;
            mod.Game_Start(fakeModApi);

            Func<FakeItEasy.Core.IFakeObjectCall, bool> filter = (call) =>
            {
                return call.Method.Name == "Console_Write" && call.GetArgument<string>(0) == "test";
            };
            mod.Game_Event(CmdId.Event_AlliancesAll, 0, "test");

            A.CallTo(fakeModApi).Where(filter, null).MustHaveHappened();
        }

        [Fact]
        public void returnsNullModFromInapplicableAssembly()
        {
            var dummyModPath = @"..\..\..\modFolderTest\notamod\NoOp.dll";
            var resultMod = ModManager.loadMod(dummyModPath);
            Assert.Null(resultMod.title);
            Assert.Null(resultMod.mod);
            Assert.True(Equals(resultMod, default(ModManager.ModItem)));
        }

        [Fact]
        public void findsCandidateDllsInFolder()
        {
            var expected = new List<string>()
            {
                "..\\..\\..\\modFolderTest\\dummy\\DummyMod.dll",
                "..\\..\\..\\modFolderTest\\dummy2\\DummyMod2.dll",
                "..\\..\\..\\modFolderTest\\notamod\\NoOp.dll",
                "..\\..\\..\\modFolderTest\\toomany\\DummyMod.dll",
                "..\\..\\..\\modFolderTest\\toomany\\DummyMod2.dll"
            };
            var fakeFolderPath = @"..\..\..\modFolderTest";
            var actual = ModManager.getCandidateModPaths(fakeFolderPath);
            Assert.Equal(expected, actual);

        }

        [Fact]
        public void loadsModsFromCandidates()
        {
            var candidates = new List<string>()
            {
                "..\\..\\..\\modFolderTest\\dummy\\DummyMod.dll",
                "..\\..\\..\\modFolderTest\\dummy2\\DummyMod2.dll",
                "..\\..\\..\\modFolderTest\\notamod\\NoOp.dll",
            };

            var titles = new List<string>()
            {
                "DummyMod",
                "DummyMod2"
            };
            
            var actual = ModManager.getModsFromPaths(candidates).ToList();
            
            Assert.True(actual.Count == 2);
            var actualTitles = actual.Select(x => x.title).ToList();
            Assert.Equal(actualTitles, titles);
            

        }
    }
}
