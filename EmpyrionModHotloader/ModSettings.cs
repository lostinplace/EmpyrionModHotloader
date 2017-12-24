using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace EmpyrionModHotloader
{
    public class ModSettings
    {
        public string HotloadModsFolder { get; set; }
        public bool verbose { get; set; }
        public List<String> additionalAdmins { get; set; }

        public string OfflinePattern { get; set; } = @"\.offline\.dll$";

        public ModSettings() { }

        public static ModSettings FromYAML(string input)
        {
            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize<ModSettings>(input);
            return result;
        }
    }

    public enum PermissionLevel
    {
        GameMaster = 3,
        Moderator = 6,
        Admin = 9
    }

    public class AdminRecord
    {
        public string Id { get; set; }
        public PermissionLevel Permission { get; set; }
    }

    public class AdminConfig
    {
        public List<AdminRecord> Elevated { get; set; }

        public static AdminConfig FromYAML(string input)
        {
            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize<AdminConfig>(input);
            return result;
        }
    }
}
