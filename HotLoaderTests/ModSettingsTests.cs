
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
    [Collection("Mod Settings")]
    public class ModSettingsTests
    {

        [Fact]
        public void canLoadFromYaml()
        {
            var sample = @"--- 
HotloadModsFolder: testing2
verbose: true
OfflinePattern: \.offline\.dll$
additionalAdmins:
- 11111";

            var actual = ModSettings.FromYAML(sample);
            Assert.Equal(actual.HotloadModsFolder, "testing2");
            Assert.True(actual.verbose);
        }
        
        [Fact]
        public void canLoadAdminConfigFromYAML()
        {
            var sample = @"Elevated:
- Id: 00000000000000000 
  Permission: 3         
- Id: 11111111111111111
  Permission: 6";

            var actual = AdminConfig.FromYAML(sample);
            Assert.Equal("00000000000000000", actual.Elevated[0].Id);
            Assert.Equal(PermissionLevel.GameMaster, actual.Elevated[0].Permission);
        }
    }
}
